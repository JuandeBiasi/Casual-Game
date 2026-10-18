using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(PrimordialTree))]
public sealed class TotemInteractionFeedback : MonoBehaviour
{
    [Header("Parte visual (sem alterar o collider)")]
    [SerializeField] private Transform visualRoot;

    [Header("Contorno amarelo")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField, ColorUsage(true, true)] private Color outlineColor =
        new Color(3f, 2.3f, 0.06f, 1f);
    [SerializeField, Range(0f, 8f)] private float outlineWidth = 2f;
    [SerializeField, HideInInspector] private OutlineMeshReference[] outlineMeshes;

    [Header("Pop ao toque")]
    [SerializeField, Range(0f, 0.2f)] private float scaleAmount = 0.06f;
    [SerializeField, Min(0.05f)] private float scaleDuration = 0.2f;

    [System.Serializable]
    private sealed class OutlineMeshReference
    {
        public Mesh Source;
        public Mesh Smoothed;
    }

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private readonly List<MeshRenderer> outlineRenderers = new List<MeshRenderer>();
    private PrimordialTree totem;
    private MaterialPropertyBlock outlineProperties;
    private Vector3 originalScale;
    private float currentScale = 1f;
    private float pulseStartScale = 1f;
    private float elapsed;
    private bool initialized;
    private bool pulsing;

    private void Awake()
    {
        totem = GetComponent<PrimordialTree>();
        if (visualRoot == null || visualRoot == transform || outlineMaterial == null)
        {
            Debug.LogWarning("Configure o Visual e o material de contorno do totem.", this);
            enabled = false;
            return;
        }

        originalScale = visualRoot.localScale;
        initialized = true;
        CreateOutlines();
        ApplyOutlineProperties();
    }

    private void OnEnable()
    {
        if (totem != null) totem.ManaTapped += HandleManaTapped;
        SetOutlineActive(true);
    }

    private void OnDisable()
    {
        if (totem != null) totem.ManaTapped -= HandleManaTapped;
        pulsing = false;
        currentScale = 1f;
        if (initialized && visualRoot != null) visualRoot.localScale = originalScale;
        SetOutlineActive(false);
    }

    private void HandleManaTapped(int gainedMana)
    {
        if (!initialized || visualRoot == null || gainedMana <= 0) return;
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

    private void CreateOutlines()
    {
        // A separate renderer preserves every original material and adds no collider.
        MeshFilter[] sources = visualRoot.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter source in sources)
        {
            MeshRenderer original = source.GetComponent<MeshRenderer>();
            if (source.sharedMesh == null || original == null || !original.enabled) continue;

            GameObject outline = new GameObject("TotemOutline");
            outline.layer = source.gameObject.layer;
            outline.transform.SetParent(source.transform, false);
            outline.AddComponent<MeshFilter>().sharedMesh = GetOutlineMesh(source.sharedMesh);
            MeshRenderer renderer = outline.AddComponent<MeshRenderer>();
            Material[] materials = new Material[Mathf.Max(1, source.sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.renderingLayerMask = original.renderingLayerMask;
            outlineRenderers.Add(renderer);
        }
    }

    private Mesh GetOutlineMesh(Mesh source)
    {
        if (outlineMeshes != null)
        {
            foreach (OutlineMeshReference reference in outlineMeshes)
                if (reference != null && reference.Source == source && reference.Smoothed != null)
                    return reference.Smoothed;
        }
        return source;
    }

    private void ApplyOutlineProperties()
    {
        if (outlineProperties == null) outlineProperties = new MaterialPropertyBlock();
        outlineProperties.SetColor(OutlineColorId, outlineColor);
        outlineProperties.SetFloat(OutlineWidthId, Mathf.Max(0f, outlineWidth));
        foreach (MeshRenderer renderer in outlineRenderers)
            if (renderer != null) renderer.SetPropertyBlock(outlineProperties);
    }

    private void SetOutlineActive(bool active)
    {
        foreach (MeshRenderer renderer in outlineRenderers)
            if (renderer != null) renderer.gameObject.SetActive(active);
    }

    private void OnValidate()
    {
        if (Application.isPlaying && initialized) ApplyOutlineProperties();
    }

    private void OnDestroy()
    {
        foreach (MeshRenderer renderer in outlineRenderers)
            if (renderer != null) Destroy(renderer.gameObject);
    }
}
