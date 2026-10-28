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

    [Header("Pulo ao ser comprado")]
    [SerializeField, Min(0.2f)] private float spawnDuration = 0.55f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.22f;
    [SerializeField, Range(0f, 0.25f)] private float landingSquash = 0.12f;

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
    private bool spawning;
    private float spawnElapsed;

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

    public bool PlaySpawn(out Vector3 worldOrigin)
    {
        worldOrigin = default;
        if (visual == null || !isActiveAndEnabled || !visual.gameObject.activeInHierarchy) return false;
        worldOrigin = visual.parent.TransformPoint(restPosition);
        spawnElapsed = 0f;
        spawning = true;
        ApplyPose(1f, 0f, restRotation);
        return true;
    }

    private void LateUpdate()
    {
        if (visual == null || !visual.gameObject.activeInHierarchy) { Stop(); return; }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (spawning)
        {
            AnimateSpawn(dt);
            return;
        }
        phase = Mathf.Repeat(phase + dt * Mathf.Max(0.1f, cyclesPerSecond) * speedVariation * Mathf.PI * 2f, Mathf.PI * 4f);
        fadeIn = Mathf.Min(1f, fadeIn + dt * 5f);
        float blend = Mathf.SmoothStep(0f, 1f, fadeIn);
        float wave = Mathf.Sin(phase);
        float stretch = 1f + wave * Mathf.Clamp(breathingAmount, 0f, 0.15f) * blend;
        float sway = Mathf.Sin(phase * 0.5f + 0.7f) * Mathf.Clamp(swayAngle, 0f, 5f) * blend;
        Quaternion rotation = Quaternion.AngleAxis(sway, Vector3.forward) * restRotation;
        float lift = (wave + 1f) * 0.5f * Mathf.Clamp(bobHeight, 0f, 0.05f) * blend;
        ApplyPose(stretch, lift, rotation);
    }

    private void AnimateSpawn(float dt)
    {
        spawnElapsed += dt;
        float progress = Mathf.Clamp01(spawnElapsed / Mathf.Max(0.2f, spawnDuration));
        float squash = Mathf.Clamp(landingSquash, 0f, 0.25f);
        float stretch;
        float lift = 0f;
        if (progress < 0.12f)
        {
            stretch = Mathf.Lerp(1f, 1f - squash, Mathf.SmoothStep(0f, 1f, progress / 0.12f));
        }
        else if (progress < 0.78f)
        {
            float flight = (progress - 0.12f) / 0.66f;
            lift = 4f * Mathf.Max(0f, jumpHeight) * flight * (1f - flight);
            stretch = flight < 0.3f
                ? Mathf.Lerp(1f - squash, 1.1f, Mathf.SmoothStep(0f, 1f, flight / 0.3f))
                : Mathf.Lerp(1.1f, 1f, Mathf.SmoothStep(0f, 1f, (flight - 0.3f) / 0.7f));
        }
        else
        {
            stretch = 1f - squash * Mathf.Sin((progress - 0.78f) / 0.22f * Mathf.PI);
        }
        ApplyPose(stretch, lift, restRotation);
        if (progress >= 1f)
        {
            spawning = false;
            phase = initialPhase;
            fadeIn = 0f;
            ApplyPose(1f, 0f, restRotation);
        }
    }

    private void ApplyPose(float stretch, float lift, Quaternion rotation)
    {
        float width = 1f / Mathf.Sqrt(stretch);
        Vector3 factors = new Vector3(heightAxis == 0 ? stretch : width,
            heightAxis == 1 ? stretch : width, heightAxis == 2 ? stretch : width);
        visual.localScale = Vector3.Scale(restScale, factors);
        visual.SetLocalPositionAndRotation(restPosition + Vector3.up * lift, rotation);
    }

    private void RestorePose()
    {
        spawning = false;
        if (visual == null) return;
        visual.SetLocalPositionAndRotation(restPosition, restRotation);
        visual.localScale = restScale;
        phase = initialPhase;
        fadeIn = 0f;
    }

    private void OnDisable() => RestorePose();
    private void OnDestroy() => RestorePose();
}
