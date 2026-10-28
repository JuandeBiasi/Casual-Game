using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(110)]
public sealed class TutorialPopupView : MonoBehaviour
{
    [Header("Interface do tutorial")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameObject optionsPanelRoot;
    [SerializeField] private RectTransform card;
    [SerializeField] private RectTransform highlight;
    [SerializeField] private CanvasGroup highlightGroup;
    [SerializeField] private TMP_Text stepText, titleText, bodyText, primaryText;
    [SerializeField] private Button primaryButton, skipButton;

    public event Action ContinuePressed;
    public event Action SkipPressed;
    public bool IsConfigured { get; private set; }

    private readonly Vector3[] corners = new Vector3[4];
    private RectTransform layoutSpace, uiTarget, avoidedWindow;
    private Collider worldTarget;
    private bool showing, hasPrimary;
    private float revealTime;
    private Camera UiCamera => targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;

    private void Awake()
    {
        layoutSpace = transform as RectTransform;
        if (worldCamera == null) worldCamera = Camera.main;
        if (layoutSpace == null || targetCanvas == null || worldCamera == null || card == null ||
            highlight == null || highlightGroup == null || stepText == null || titleText == null ||
            bodyText == null || primaryText == null || primaryButton == null || skipButton == null)
        {
            Debug.LogError("Configure as referências visuais do tutorial.", this);
            enabled = false;
            return;
        }
        primaryButton.onClick.AddListener(Continue);
        skipButton.onClick.AddListener(Skip);
        // Only the small card receives input. The target marker never blocks taps.
        highlightGroup.blocksRaycasts = false;
        highlightGroup.interactable = false;
        IsConfigured = true;
        Hide();
    }

    public void Show(string step, string title, string body, string primaryLabel = null)
    {
        if (!IsConfigured) return;
        if (!showing || titleText.text != title) revealTime = 0f;
        showing = true;
        stepText.text = step;
        titleText.text = title;
        bodyText.text = body;
        hasPrimary = !string.IsNullOrEmpty(primaryLabel);
        primaryText.text = primaryLabel ?? "";
        primaryButton.gameObject.SetActive(hasPrimary);
        card.sizeDelta = new Vector2(620f, hasPrimary ? 280f : 220f);
        enabled = true;
    }

    public void Hide()
    {
        showing = false;
        if (card != null) card.gameObject.SetActive(false);
        if (highlight != null) highlight.gameObject.SetActive(false);
        enabled = false;
    }

    public void ClearTarget() { worldTarget = null; uiTarget = null; }
    public void FocusWorld(Collider target) { worldTarget = target; uiTarget = null; }
    public void FocusUI(RectTransform target) { uiTarget = target; worldTarget = null; }
    public void AvoidWindow(RectTransform target) => avoidedWindow = target;
    private void Continue() => ContinuePressed?.Invoke();
    private void Skip() => SkipPressed?.Invoke();

    private void LateUpdate()
    {
        if (!showing) return;
        if (optionsPanelRoot != null && optionsPanelRoot.activeInHierarchy)
        {
            card.gameObject.SetActive(false);
            highlight.gameObject.SetActive(false);
            return;
        }
        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
        if (!ToLocal(safe.min, out Vector2 min) || !ToLocal(safe.max, out Vector2 max)) return;
        min = Vector2.Max(min, layoutSpace.rect.min) + new Vector2(24f, 180f);
        max = Vector2.Min(max, layoutSpace.rect.max) - new Vector2(24f, 150f);
        if (max.x <= min.x || max.y <= min.y) return;
        Rect available = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        bool hasTarget = TryTargetRect(out Rect target);
        Rect obstacle = default;
        bool hasWindow = avoidedWindow != null && avoidedWindow.gameObject.activeInHierarchy &&
            TryUiRect(avoidedWindow, out obstacle);

        Vector2 size = card.sizeDelta;
        float scale = Mathf.Min(1f, Mathf.Min(available.width / size.x, available.height / size.y));
        Vector2 position = ChooseCardPosition(available, size * scale, obstacle, hasWindow, target, hasTarget);
        revealTime = Mathf.Min(0.16f, revealTime + Time.unscaledDeltaTime);
        float animatedScale = scale * Mathf.Lerp(0.94f, 1f, Mathf.SmoothStep(0f, 1f, revealTime / 0.16f));
        card.localScale = new Vector3(animatedScale, animatedScale, 1f);
        card.localPosition = position;
        card.gameObject.SetActive(true);

        highlight.gameObject.SetActive(hasTarget);
        if (hasTarget)
        {
            highlight.localPosition = target.center;
            highlight.sizeDelta = target.size + Vector2.one * 24f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            float expansion = 1f + 0.025f * pulse;
            highlight.localScale = new Vector3(expansion, expansion, 1f);
            highlightGroup.alpha = 0.65f + 0.35f * pulse;
        }
    }

    internal static Vector2 ChooseCardPosition(Rect available, Vector2 size,
        Rect window, bool hasWindow, Rect target, bool hasTarget)
    {
        Vector2 half = size * 0.5f;
        Vector2 best = default;
        float bestScore = float.PositiveInfinity;
        // Prefer bottom-center; try the top and sides when a habitat/window occupies it.
        for (int i = 0; i < 8; i++)
        {
            float x = i < 2 ? available.center.x : i == 2 || i == 4 || i == 6
                ? available.xMin + half.x : available.xMax - half.x;
            float y = i == 0 || i == 2 || i == 3 ? available.yMin + half.y
                : i == 1 || i == 4 || i == 5 ? available.yMax - half.y : available.center.y;
            Vector2 candidate = new Vector2(x, y);
            Rect cardRect = new Rect(candidate - half, size);
            float score = (hasWindow ? Overlap(cardRect, window) * 4f : 0f) +
                (hasTarget ? Overlap(cardRect, target) * 2f : 0f) + i * 0.01f;
            if (score < bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    private static float Overlap(Rect a, Rect b) =>
        Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
        Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

    private bool TryTargetRect(out Rect rect)
    {
        rect = default;
        if (uiTarget != null) return uiTarget.gameObject.activeInHierarchy && TryUiRect(uiTarget, out rect);
        if (worldTarget == null || !worldTarget.gameObject.activeInHierarchy || !worldTarget.enabled) return false;
        Bounds bounds = worldTarget.bounds;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            Vector3 point = worldCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f)));
            if (point.z <= 0f || !ToLocal(point, out Vector2 local)) return false;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return rect.width > 0f && rect.height > 0f;
    }

    private bool TryUiRect(RectTransform source, out Rect rect)
    {
        source.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            if (!ToLocal(RectTransformUtility.WorldToScreenPoint(UiCamera, corners[i]), out Vector2 point))
            { rect = default; return false; }
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private bool ToLocal(Vector2 point, out Vector2 local) =>
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layoutSpace, point, UiCamera, out local);

    private void OnDisable()
    {
        if (card != null) card.gameObject.SetActive(false);
        if (highlight != null) highlight.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (primaryButton != null) primaryButton.onClick.RemoveListener(Continue);
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }
}
