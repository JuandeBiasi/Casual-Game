using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TotemManaFeedback : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PrimordialTree totem;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text popupTemplate;

    [Header("Animação discreta")]
    [SerializeField, Min(0.1f)] private float duration = 0.7f;
    [SerializeField, Min(0f)] private float riseDistance = 65f;
    [SerializeField, Min(0f)] private float horizontalSpread = 22f;
    [SerializeField, Min(0f)] private float horizontalDrift = 16f;
    [SerializeField, Range(0f, 10f)] private float rotationSpread = 4f;
    [SerializeField, Range(0.5f, 1f)] private float startScale = 0.85f;
    [SerializeField, Range(1, 24)] private int maxSimultaneousPopups = 12;

    [Header("Bolinhas coloridas")]
    [SerializeField] private Image dotTemplate;
    [SerializeField, Range(0, 12)] private int dotsPerTap = 5;
    [SerializeField, Min(0.1f)] private float dotDuration = 0.5f;
    [SerializeField, Min(0f)] private float dotSpeed = 120f;
    [SerializeField, Min(0f)] private float dotGravity = 190f;
    [SerializeField] private Vector2 dotSizeRange = new Vector2(8f, 14f);
    [SerializeField] private Color[] dotColors =
    {
        new Color(1f, 0.72f, 0.28f, 0.9f),
        new Color(0.36f, 0.85f, 1f, 0.9f),
        new Color(1f, 0.45f, 0.7f, 0.9f),
        new Color(0.46f, 1f, 0.64f, 0.9f)
    };

    private sealed class Popup
    {
        public TMP_Text Text;
        public RectTransform Rect;
        public Vector2 StartPosition;
        public float Drift;
        public float Elapsed;
        public bool Active;
    }

    private sealed class Dot
    {
        public Image Image;
        public RectTransform Rect;
        public Vector2 StartPosition;
        public Vector2 Velocity;
        public Color Color;
        public float Elapsed;
        public bool Active;
    }

    private Popup[] popups;
    private Dot[] dots;
    private Color textColor;
    private int nextPopup;
    private int nextDot;

    private void Awake()
    {
        if (totem == null || spawnPoint == null || worldCamera == null ||
            targetCanvas == null || popupRoot == null || popupTemplate == null)
        {
            Debug.LogWarning("Configure as referências do número flutuante do totem.", this);
            enabled = false;
            return;
        }

        textColor = popupTemplate.color;
        popupTemplate.gameObject.SetActive(false);
        popups = new Popup[Mathf.Clamp(maxSimultaneousPopups, 1, 24)];

        for (int i = 0; i < popups.Length; i++)
        {
            TMP_Text text = Instantiate(popupTemplate, popupRoot);
            text.name = $"ManaPopup_{i + 1}";
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            popups[i] = new Popup { Text = text, Rect = text.rectTransform };
        }

        InitializeDots();
    }

    private void OnEnable()
    {
        if (totem != null) totem.ManaTapped += ShowPopup;
    }

    private void OnDisable()
    {
        if (totem != null) totem.ManaTapped -= ShowPopup;
        if (popups == null) return;

        foreach (Popup popup in popups)
        {
            popup.Active = false;
            popup.Text.gameObject.SetActive(false);
        }

        if (dots == null) return;
        foreach (Dot dot in dots)
        {
            dot.Active = false;
            dot.Image.gameObject.SetActive(false);
        }
    }

    private void ShowPopup(int gainedMana)
    {
        if (gainedMana <= 0 || popups == null) return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(spawnPoint.position);
        if (screenPosition.z <= 0f) return;

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupRoot, screenPosition, uiCamera, out Vector2 localPosition))
            return;

        ShowDots(localPosition);

        // Recycle the oldest entry if very fast taps fill the pool.
        Popup popup = popups[nextPopup];
        nextPopup = (nextPopup + 1) % popups.Length;
        popup.StartPosition = localPosition + new Vector2(
            Random.Range(-horizontalSpread, horizontalSpread), 0f);
        popup.Drift = Random.Range(-horizontalDrift, horizontalDrift);
        popup.Elapsed = 0f;
        popup.Active = true;

        // The amount comes from the successful tap, never from a second setting.
        popup.Text.text = $"+{gainedMana:N0}";
        popup.Text.color = textColor;
        popup.Rect.anchoredPosition = popup.StartPosition;
        popup.Rect.localScale = Vector3.one * startScale;
        popup.Rect.localRotation = Quaternion.Euler(
            0f, 0f, Random.Range(-rotationSpread, rotationSpread));
        popup.Text.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (popups == null) return;
        UpdateDots(Time.unscaledDeltaTime);
        float lifetime = Mathf.Max(0.1f, duration);

        foreach (Popup popup in popups)
        {
            if (!popup.Active) continue;
            popup.Elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(popup.Elapsed / lifetime);

            if (progress >= 1f)
            {
                popup.Active = false;
                popup.Text.gameObject.SetActive(false);
                continue;
            }

            float rise = 1f - (1f - progress) * (1f - progress);
            popup.Rect.anchoredPosition = popup.StartPosition +
                new Vector2(popup.Drift * progress, riseDistance * rise);
            popup.Rect.localScale = Vector3.one * Mathf.Lerp(
                startScale, 1f, Mathf.Clamp01(progress / 0.2f));

            Color color = textColor;
            color.a *= 1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(0.25f, 1f, progress));
            popup.Text.color = color;
        }
    }

    private void InitializeDots()
    {
        if (dotTemplate == null) return;
        dotTemplate.gameObject.SetActive(false);
        int poolSize = popups.Length * Mathf.Max(1, Mathf.Clamp(dotsPerTap, 0, 12));
        dots = new Dot[poolSize];

        for (int i = 0; i < dots.Length; i++)
        {
            Image image = Instantiate(dotTemplate, popupRoot);
            image.name = $"ManaDot_{i + 1}";
            image.raycastTarget = false;
            image.rectTransform.SetAsFirstSibling();
            image.gameObject.SetActive(false);
            dots[i] = new Dot { Image = image, Rect = image.rectTransform };
        }
    }

    private void ShowDots(Vector2 origin)
    {
        if (dots == null || dots.Length == 0) return;
        int count = Mathf.Min(Mathf.Clamp(dotsPerTap, 0, 12), dots.Length);
        float minSize = Mathf.Max(1f, Mathf.Min(dotSizeRange.x, dotSizeRange.y));
        float maxSize = Mathf.Max(minSize, Mathf.Max(dotSizeRange.x, dotSizeRange.y));

        for (int i = 0; i < count; i++)
        {
            Dot dot = dots[nextDot];
            nextDot = (nextDot + 1) % dots.Length;
            float angle = Random.Range(25f, 155f) * Mathf.Deg2Rad;
            float speed = Mathf.Max(0f, dotSpeed) * Random.Range(0.65f, 1.15f);
            float size = Random.Range(minSize, maxSize);
            dot.StartPosition = origin + new Vector2(Random.Range(-6f, 6f), 0f);
            dot.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            dot.Color = dotColors != null && dotColors.Length > 0
                ? dotColors[Random.Range(0, dotColors.Length)]
                : textColor;
            dot.Elapsed = 0f;
            dot.Active = true;
            dot.Rect.anchoredPosition = dot.StartPosition;
            dot.Rect.sizeDelta = new Vector2(size, size);
            dot.Rect.localScale = Vector3.one;
            dot.Image.color = dot.Color;
            dot.Image.gameObject.SetActive(true);
        }
    }

    private void UpdateDots(float deltaTime)
    {
        if (dots == null) return;
        float lifetime = Mathf.Max(0.1f, dotDuration);

        foreach (Dot dot in dots)
        {
            if (!dot.Active) continue;
            dot.Elapsed += deltaTime;
            float progress = Mathf.Clamp01(dot.Elapsed / lifetime);

            if (progress >= 1f)
            {
                dot.Active = false;
                dot.Image.gameObject.SetActive(false);
                continue;
            }

            dot.Rect.anchoredPosition = dot.StartPosition + dot.Velocity * dot.Elapsed +
                Vector2.down * (0.5f * Mathf.Max(0f, dotGravity) * dot.Elapsed * dot.Elapsed);
            dot.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, progress);
            Color color = dot.Color;
            color.a *= 1f - Mathf.SmoothStep(0f, 1f, progress);
            dot.Image.color = color;
        }
    }
}
