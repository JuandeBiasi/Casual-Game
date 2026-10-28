using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SlimeHabitat))]
public sealed class SlimePurchaseFeedback : MonoBehaviour
{
    [Header("Slots deste habitat, na ordem de compra")]
    [SerializeField] private SlimeVisualSlot[] slimeSlots;

    [Header("Pequena aura de nascimento")]
    [SerializeField] private Material auraMaterial;
    [SerializeField, ColorUsage(true, true)] private Color auraColor = new Color(0.4f, 1f, 0.65f, 1f);
    [SerializeField, Min(0.1f)] private float auraRadius = 0.38f;
    [SerializeField, Min(0.2f)] private float auraDuration = 0.5f;
    [SerializeField, Range(0, 16)] private int sparkCount = 10;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly System.Random visualRandom = new System.Random();
    private SlimeHabitat habitat;
    private Transform effectRoot;
    private ParticleSystem rings;
    private ParticleSystem sparks;
    private Mesh ringMesh;
    private Texture2D sparkTexture;
    private Material sparkMaterial;

    private void Awake() => habitat = GetComponent<SlimeHabitat>();

    private void OnEnable()
    {
        if (habitat == null) habitat = GetComponent<SlimeHabitat>();
        if (habitat != null) habitat.SlimePurchased += HandleSlimePurchased;
    }

    private void OnDisable()
    {
        if (habitat != null) habitat.SlimePurchased -= HandleSlimePurchased;
        if (rings != null) rings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void HandleSlimePurchased(SlimeRarity rarity)
    {
        int index = habitat.SlimeCount - 1;
        if (slimeSlots == null || index < 0 || index >= slimeSlots.Length || slimeSlots[index] == null) return;
        if (!slimeSlots[index].PlayPurchaseFeedback(out Vector3 origin)) return;
        if (auraMaterial == null)
        {
            Debug.LogWarning("Configure o material da aura de nascimento dos slimes.", this);
            return;
        }
        if (effectRoot == null) CreateEffect();
        EmitBurst(effectRoot.InverseTransformPoint(origin) + Vector3.up * 0.02f);
    }

    private void CreateEffect()
    {
        GameObject root = new GameObject("SlimePurchaseFX");
        root.layer = gameObject.layer;
        root.SetActive(false);
        effectRoot = root.transform;
        effectRoot.SetParent(transform, false); // Separate from the models' jump and breathing.
        Color color = Color.Lerp(auraColor, new Color(1f, 0.86f, 0.42f, 1f), 0.45f) * 2f;
        color.a = Mathf.Clamp01(auraColor.a);

        ringMesh = HabitatStateAura.CreateRingMesh(keepReadable: true);
        rings = CreateEmitter("SpawnRings", 12);
        var size = rings.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.18f, 1f, 1f));
        ParticleSystemRenderer ringRenderer = rings.GetComponent<ParticleSystemRenderer>();
        ringRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        ringRenderer.alignment = ParticleSystemRenderSpace.Local;
        ringRenderer.mesh = ringMesh;
        ringRenderer.sharedMaterial = auraMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetColor(BaseColorId, color);
        ringRenderer.SetPropertyBlock(properties);

        sparkTexture = HabitatStateAura.CreateSparkTexture();
        sparkMaterial = new Material(auraMaterial) { name = "Slime Spawn Sparks (runtime)", hideFlags = HideFlags.DontSave };
        sparkMaterial.SetTexture("_BaseMap", sparkTexture);
        sparkMaterial.SetColor(BaseColorId, color);
        sparks = CreateEmitter("SpawnSparks", 96);
        var sparkSize = sparks.sizeOverLifetime;
        sparkSize.enabled = true;
        sparkSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkRenderer.sharedMaterial = sparkMaterial;
        root.SetActive(true);
    }

    private ParticleSystem CreateEmitter(string name, int maxParticles)
    {
        GameObject emitter = new GameObject(name);
        emitter.layer = gameObject.layer;
        emitter.transform.SetParent(effectRoot, false);
        ParticleSystem system = emitter.AddComponent<ParticleSystem>();
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startSpeed = 0f;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.35f), new GradientAlphaKey(0f, 1f) });
        var fade = system.colorOverLifetime;
        fade.enabled = true;
        fade.color = new ParticleSystem.MinMaxGradient(gradient);
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
        HabitatStateAura.ConfigureRenderer(renderer);
        return system;
    }

    private void EmitBurst(Vector3 origin)
    {
        float duration = Mathf.Max(0.2f, auraDuration);
        float radius = Mathf.Max(0.1f, auraRadius);
        rings.Play(true);
        for (int i = 0; i < 2; i++)
        {
            var ring = new ParticleSystem.EmitParams {
                position = origin, velocity = Vector3.zero, startColor = Color.white,
                startSize = radius * (i == 0 ? 1f : 1.15f), startLifetime = duration * (i == 0 ? 1f : 1.2f),
                rotation3D = Vector3.zero
            };
            rings.Emit(ring, 1);
        }
        sparks.Play(true);
        int count = Mathf.Clamp(sparkCount, 0, 16);
        for (int i = 0; i < count; i++)
        {
            float angle = Range(0f, Mathf.PI * 2f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var spark = new ParticleSystem.EmitParams {
                position = origin + radial * Range(0f, radius * 0.5f),
                velocity = radial * Range(0.15f, 0.5f) + Vector3.up * Range(0.4f, 0.9f),
                startColor = Color.white, startSize = Range(0.035f, 0.065f), startLifetime = duration * Range(0.65f, 1f)
            };
            sparks.Emit(spark, 1);
        }
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)visualRandom.NextDouble());

    private void OnDestroy()
    {
        if (effectRoot != null) Destroy(effectRoot.gameObject);
        if (ringMesh != null) Destroy(ringMesh);
        if (sparkMaterial != null) Destroy(sparkMaterial);
        if (sparkTexture != null) Destroy(sparkTexture);
    }
}
