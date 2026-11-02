using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(SlimeHabitat))]
public sealed class HabitatStateAura : MonoBehaviour
{
    [Header("Aura do bioma")]
    [SerializeField] private Material auraMaterial;
    [SerializeField, ColorUsage(true, true)] private Color auraColor = new Color(0.4f, 1f, 0.65f, 1f);
    [SerializeField, Min(0.2f)] private float burstDuration = 0.85f;
    [SerializeField, Min(1f)] private float expansionRadius = 2.2f;
    [SerializeField, Range(0, 40)] private int restorationParticles = 18;
    [SerializeField, Range(0, 40)] private int upgradeParticles = 30;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly MeshRenderer[] rings = new MeshRenderer[2];
    private readonly System.Random visualRandom = new System.Random();
    private MaterialPropertyBlock ringProperties;
    private SlimeHabitat habitat;
    private BoxCollider habitatCollider;
    private Transform effectRoot;
    private Mesh ringMesh;
    private ParticleSystem sparks;
    private Material sparkMaterial;
    private Texture2D sparkTexture;
    private Color burstColor;
    private float baseRadius;
    private float currentDuration;
    private float currentExpansion;
    private float elapsed;
    private bool initialized;
    private bool animating;

    private void Awake()
    {
        habitat = GetComponent<SlimeHabitat>();
        habitatCollider = GetComponent<BoxCollider>();
        if (habitat == null || habitatCollider == null || auraMaterial == null)
        {
            Debug.LogWarning("Configure o material da aura e o collider do habitat.", this);
            enabled = false;
            return;
        }
        ringProperties = new MaterialPropertyBlock();
        CreateEffect();
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        habitat.Restored += HandleRestored;
        habitat.HabitatUpgraded += HandleUpgraded;
    }

    private void OnDisable()
    {
        if (habitat != null)
        {
            habitat.Restored -= HandleRestored;
            habitat.HabitatUpgraded -= HandleUpgraded;
        }
        animating = false;
        foreach (MeshRenderer ring in rings)
            if (ring != null) ring.enabled = false;
        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void HandleRestored() => PlayBurst(false);
    private void HandleUpgraded(int level) { if (level >= 2) PlayBurst(true); }

    private void PlayBurst(bool upgraded)
    {
        if (!initialized || !isActiveAndEnabled) return;
        // Stay outside HabitatVisualPivot: clicks never scale the aura or its origin.
        effectRoot.localPosition = habitatCollider.center + Vector3.up * (habitatCollider.size.y * 0.5f + 0.04f);
        baseRadius = Mathf.Max(0.1f, Mathf.Max(habitatCollider.size.x, habitatCollider.size.z) * 0.55f);
        currentDuration = Mathf.Max(0.2f, burstDuration) * (upgraded ? 1.25f : 1f);
        currentExpansion = Mathf.Max(1f, expansionRadius) * (upgraded ? 1.2f : 1f);
        burstColor = upgraded ? Color.Lerp(auraColor, new Color(1f, 0.86f, 0.42f, 1f), 0.45f) : auraColor;
        burstColor *= upgraded ? 2.2f : 1.6f;
        burstColor.a = Mathf.Clamp01(auraColor.a);
        elapsed = 0f;
        animating = true;
        AnimateRings();

        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        sparkMaterial.SetColor(BaseColorId, burstColor);
        sparks.Play(true);
        int count = Mathf.Clamp(upgraded ? upgradeParticles : restorationParticles, 0, 40);
        for (int i = 0; i < count; i++)
        {
            float angle = VisualRange(0f, Mathf.PI * 2f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var particle = new ParticleSystem.EmitParams
            {
                position = radial * (VisualRange(0.1f, 0.7f) * baseRadius),
                velocity = (radial * VisualRange(0.8f, 1.6f) + Vector3.up * VisualRange(1f, 2.2f))
                    * baseRadius * (upgraded ? 1.2f : 1f),
                startLifetime = VisualRange(0.55f, 0.9f) * (upgraded ? 1.2f : 1f),
                startSize = VisualRange(0.08f, 0.16f) * baseRadius * (upgraded ? 1.2f : 1f),
                startColor = Color.white
            };
            sparks.Emit(particle, 1);
        }
    }

    private float VisualRange(float min, float max) => Mathf.Lerp(min, max, (float)visualRandom.NextDouble());

    private void Update()
    {
        if (!animating) return;
        elapsed += Time.unscaledDeltaTime;
        AnimateRings();
        if (elapsed >= currentDuration) animating = false;
    }

    private void AnimateRings()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            float delay = i * currentDuration * 0.14f;
            float progress = Mathf.Clamp01((elapsed - delay) / (currentDuration - delay));
            MeshRenderer ring = rings[i];
            ring.enabled = elapsed >= delay && progress < 1f;
            float ease = 1f - Mathf.Pow(1f - progress, 3f);
            float radius = baseRadius * Mathf.Lerp(0.25f, currentExpansion, ease);
            ring.transform.localScale = new Vector3(radius, 1f, radius);
            Color color = burstColor;
            color.a *= (1f - progress) * (1f - progress) * (i == 0 ? 1f : 0.65f);
            ringProperties.SetColor(BaseColorId, color);
            ring.SetPropertyBlock(ringProperties);
        }
    }

    private void CreateEffect()
    {
        GameObject root = new GameObject("HabitatAuraFX");
        root.layer = gameObject.layer;
        root.SetActive(false);
        effectRoot = root.transform;
        effectRoot.SetParent(transform, false);
        ringMesh = CreateRingMesh();
        for (int i = 0; i < rings.Length; i++)
        {
            GameObject ring = new GameObject("AuraRing" + (i + 1));
            ring.layer = gameObject.layer;
            ring.transform.SetParent(effectRoot, false);
            ring.transform.localPosition = Vector3.up * (i * 0.015f);
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = auraMaterial;
            ConfigureRenderer(renderer);
            renderer.enabled = false;
            rings[i] = renderer;
        }

        sparkTexture = CreateSparkTexture();
        sparkMaterial = new Material(auraMaterial) { name = "Habitat Aura Sparks (runtime)", hideFlags = HideFlags.DontSave };
        sparkMaterial.SetTexture("_BaseMap", sparkTexture);
        GameObject sparkObject = new GameObject("AuraSparks");
        sparkObject.layer = gameObject.layer;
        sparkObject.transform.SetParent(effectRoot, false);
        sparks = sparkObject.AddComponent<ParticleSystem>();
        var main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.5f;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 0.12f;
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = true;
        var emission = sparks.emission; emission.enabled = false;
        var shape = sparks.shape; shape.enabled = false;
        var size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var fade = sparks.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(gradient);
        ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.sharedMaterial = sparkMaterial;
        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkRenderer.sortMode = ParticleSystemSortMode.Distance;
        sparkRenderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
        ConfigureRenderer(sparkRenderer);
        root.SetActive(true);
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    internal static void ConfigureRenderer(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    internal static Mesh CreateRingMesh(bool keepReadable = false)
    {
        const int segments = 48;
        float[] radii = { 0.72f, 0.83f, 0.93f, 1f };
        float[] alpha = { 0f, 0.8f, 0.55f, 0f };
        Vector3[] vertices = new Vector3[segments * 4];
        Vector3[] normals = new Vector3[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 18];
        int index = 0;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            for (int j = 0; j < 4; j++)
            {
                int v = i * 4 + j;
                vertices[v] = new Vector3(Mathf.Cos(angle) * radii[j], 0f, Mathf.Sin(angle) * radii[j]);
                normals[v] = Vector3.up;
                colors[v] = new Color(1f, 1f, 1f, alpha[j]);
                uv[v] = new Vector2(vertices[v].x * 0.5f + 0.5f, vertices[v].z * 0.5f + 0.5f);
                if (j == 3) continue;
                int a = i * 4 + j, b = ((i + 1) % segments) * 4 + j;
                triangles[index++] = a; triangles[index++] = a + 1; triangles[index++] = b + 1;
                triangles[index++] = a; triangles[index++] = b + 1; triangles[index++] = b;
            }
        }
        Mesh mesh = new Mesh { name = "Habitat Aura Ring (runtime)", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices; mesh.normals = normals; mesh.colors = colors; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.UploadMeshData(!keepReadable);
        return mesh;
    }

    internal static Texture2D CreateSparkTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "Habitat Aura Glow (runtime)", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Clamp01(1f - dx * dx - dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDestroy()
    {
        if (effectRoot != null) Destroy(effectRoot.gameObject);
        if (ringMesh != null) Destroy(ringMesh);
        if (sparkMaterial != null) Destroy(sparkMaterial);
        if (sparkTexture != null) Destroy(sparkTexture);
    }
}
