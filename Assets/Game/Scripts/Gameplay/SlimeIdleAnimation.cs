using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SlimeVisualSlot))]
public sealed class SlimeIdleAnimation : MonoBehaviour
{
    [Header("Movimento leve dos modelos atuais")]
    [SerializeField, Range(0f, 0.15f)] private float breathingAmount = 0.045f;
    [SerializeField, Min(0.1f)] private float cyclesPerSecond = 0.65f;
    [SerializeField, Range(0f, 5f)] private float swayAngle = 1.5f;
    [SerializeField, Range(0f, 0.05f)] private float bobHeight = 0.008f;

    private Transform visual;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restScale;
    private int heightAxis;
    private float phase;
    private float initialPhase;
    private float speedVariation;
    private float fadeIn;
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        uint seed = unchecked((uint)GetInstanceID() * 2654435761u);
        initialPhase = (seed & 0xffffu) / 65535f * Mathf.PI * 4f;
        speedVariation = Mathf.Lerp(0.85f, 1.15f, (seed >> 16) / 65535f);
        initialized = true;
    }

    public void Play(Transform target)
    {
        if (target == null) { Stop(); return; }
        if (target == visual) { enabled = true; return; } // Do not recapture an animated pose.
        RestorePose();
        visual = null;
        if (target.parent != transform)
        {
            Debug.LogWarning("O modelo animado deve ser filho direto do seu SlimeVisualSlot.", this);
            enabled = false;
            return;
        }
        Initialize();
        visual = target;
        restPosition = visual.localPosition;
        restRotation = visual.localRotation;
        restScale = visual.localScale;

        // Water is imported with a rotated root: stretch the axis that actually points up.
        Vector3 up = Quaternion.Inverse(restRotation) * Vector3.up;
        float x = Mathf.Abs(up.x), y = Mathf.Abs(up.y), z = Mathf.Abs(up.z);
        heightAxis = x >= y && x >= z ? 0 : z > y ? 2 : 1;
        phase = initialPhase;
        fadeIn = 0f;
        enabled = true;
    }

    public void Stop()
    {
        RestorePose();
        visual = null;
        enabled = false;
    }

    private void LateUpdate()
    {
        if (visual == null || !visual.gameObject.activeInHierarchy) { Stop(); return; }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        phase = Mathf.Repeat(phase + dt * Mathf.Max(0.1f, cyclesPerSecond) * speedVariation * Mathf.PI * 2f, Mathf.PI * 4f);
        fadeIn = Mathf.Min(1f, fadeIn + dt * 5f);
        float blend = Mathf.SmoothStep(0f, 1f, fadeIn);
        float wave = Mathf.Sin(phase);
        float stretch = 1f + wave * Mathf.Clamp(breathingAmount, 0f, 0.15f) * blend;
        float width = 1f / Mathf.Sqrt(stretch);
        Vector3 factors = new Vector3(heightAxis == 0 ? stretch : width,
            heightAxis == 1 ? stretch : width, heightAxis == 2 ? stretch : width);
        visual.localScale = Vector3.Scale(restScale, factors);

        float sway = Mathf.Sin(phase * 0.5f + 0.7f) * Mathf.Clamp(swayAngle, 0f, 5f) * blend;
        Quaternion rotation = Quaternion.AngleAxis(sway, Vector3.forward) * restRotation;
        Vector3 position = restPosition + Vector3.up * ((wave + 1f) * 0.5f * Mathf.Clamp(bobHeight, 0f, 0.05f) * blend);
        visual.SetLocalPositionAndRotation(position, rotation);
    }

    private void RestorePose()
    {
        if (visual == null) return;
        visual.SetLocalPositionAndRotation(restPosition, restRotation);
        visual.localScale = restScale;
        phase = initialPhase;
        fadeIn = 0f;
    }

    private void OnDisable() => RestorePose();
    private void OnDestroy() => RestorePose();
}
