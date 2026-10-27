using UnityEngine;

[DisallowMultipleComponent]
public sealed class ClickScaleFeedback : MonoBehaviour
{
    [Header("Parte visual (sem alterar o collider)")]
    [SerializeField] private Transform visualRoot;

    [Header("Pop ao toque — mesma curva do totem")]
    [SerializeField, Range(0f, 0.2f)] private float scaleAmount = 0.06f;
    [SerializeField, Min(0.05f)] private float scaleDuration = 0.2f;

    private Vector3 originalScale;
    private float currentScale = 1f;
    private float pulseStartScale = 1f;
    private float elapsed;
    private bool initialized;
    private bool pulsing;

    private void Awake()
    {
        if (visualRoot == null || visualRoot == transform)
        {
            Debug.LogWarning("Configure uma parte visual separada para animar a escala.", this);
            enabled = false;
            return;
        }
        originalScale = visualRoot.localScale;
        initialized = true;
    }

    public void Play()
    {
        if (!initialized || !isActiveAndEnabled || visualRoot == null) return;
        pulseStartScale = currentScale;
        elapsed = 0f;
        pulsing = true;
    }

    private void Update()
    {
        if (!pulsing || visualRoot == null) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, scaleDuration));
        float peak = 1f + Mathf.Clamp(scaleAmount, 0f, 0.2f);

        if (progress < 0.3f)
        {
            float attack = progress / 0.3f;
            currentScale = Mathf.Lerp(pulseStartScale, peak, 1f - (1f - attack) * (1f - attack));
        }
        else
        {
            currentScale = Mathf.Lerp(peak, 1f, Mathf.SmoothStep(0f, 1f, (progress - 0.3f) / 0.7f));
        }

        visualRoot.localScale = originalScale * currentScale;
        if (progress >= 1f)
        {
            pulsing = false;
            currentScale = 1f;
            visualRoot.localScale = originalScale;
        }
    }

    private void OnDisable()
    {
        pulsing = false;
        currentScale = 1f;
        if (initialized && visualRoot != null) visualRoot.localScale = originalScale;
    }
}
