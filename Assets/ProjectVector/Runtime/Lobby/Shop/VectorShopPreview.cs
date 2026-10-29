using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Owns only temporary shop preview objects; gameplay prefabs and their materials are never edited.
public sealed class VectorShopPreview : MonoBehaviour, IDragHandler
{
    private GameObject stage, pivot;
    private static readonly HashSet<int> occupiedStages = new();
    private int stageSlot = -1;
    private RenderTexture texture;
    private readonly Dictionary<Renderer, Material[]> originals = new();
    private readonly List<Material> holograms = new();
    private readonly List<Material> previewMaterials = new();
    private Transform modelRoot;
    private Renderer[] modelRenderers;
    public void Show(GameObject prefab)
    {
        Clear();
        if (prefab == null) return;
        stage = new GameObject("ShopPreviewStage");
        stageSlot = 0;
        while (occupiedStages.Contains(stageSlot)) ++stageSlot;
        occupiedStages.Add(stageSlot);
        stage.transform.position = new Vector3(10000 + stageSlot * 500, 10000, 10000);
        stage.SetActive(false);
        pivot = new GameObject("RotationPivot");
        pivot.transform.SetParent(stage.transform, false);
        modelRoot = new GameObject("ModelCenter").transform;
        modelRoot.SetParent(pivot.transform, false);
        var model = Instantiate(prefab, modelRoot, false);
        foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (var camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
        foreach (var audio in model.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        foreach (var animator in model.GetComponentsInChildren<Animator>(true))
        { animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        model.SetActive(true);
        stage.SetActive(true);
        foreach (var animator in model.GetComponentsInChildren<Animator>())
            if (animator.runtimeAnimatorController != null) animator.Update(0);
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Clear(); return; }
        modelRenderers = renderers;
        // Skinned renderer bounds can include animation padding far beyond the actual body.
        var bounds = VisibleBounds();
        modelRoot.position += stage.transform.position - bounds.center;
        ApplyPreviewMaterials(renderers);
        float size = Mathf.Max(0.1f, bounds.size.magnitude);
        var cameraObject = new GameObject("ShopPreviewCamera", typeof(Camera));
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 0, -size * 1.1f);
        var previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0, 0, 0, 0);
        previewCamera.cullingMask = 1 << 31;
        previewCamera.nearClipPlane = size * 0.01f;
        previewCamera.farClipPlane = size * 6f;
        previewCamera.fieldOfView = 45f;
        previewCamera.allowHDR = false;
        texture = new RenderTexture(1024, 1024, 24);
        previewCamera.targetTexture = texture;
        GetComponent<RawImage>().texture = texture;
        bool character = model.GetComponentInChildren<Animator>(true) != null;
        // The preview camera looks along +Z; imported characters face +Z by default.
        float facing = character ? 180 : 160;
        pivot.transform.localRotation = Quaternion.Euler(0, facing, 0);
    }
    private Bounds VisibleBounds()
    {
        var result = new Bounds(); bool first = true;
        foreach (var renderer in modelRenderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            var bounds = renderer.bounds;
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                var local = skinned.sharedMesh.bounds; var matrix = skinned.transform.localToWorldMatrix;
                bounds = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
                for (int i = 0; i < 8; ++i)
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                        new Vector3((i&1)==0?-1:1, (i&2)==0?-1:1, (i&4)==0?-1:1))));
            }
            if (first) { result = bounds; first = false; } else result.Encapsulate(bounds);
        }
        return result;
    }
    private void ApplyPreviewMaterials(Renderer[] renderers)
    {
        var shader = Resources.Load<Shader>("Shop/VectorPreview");
        if (shader == null) return;
        foreach (var renderer in renderers)
        {
            var source = renderer.sharedMaterials;
            var replacements = new Material[source.Length];
            for (int i = 0; i < source.Length; ++i)
            {
                var material = new Material(shader); previewMaterials.Add(material); replacements[i] = material;
                CopyAlbedo(source[i], material);
            }
            renderer.sharedMaterials = replacements;
        }
    }
    private static void CopyAlbedo(Material source, Material target)
    {
        if (source == null) return;
        foreach (var property in new[] { "_BaseMap", "_MainTex", "_BaseColorMap" })
            if (source.HasProperty(property) && source.GetTexture(property) != null)
            {
                target.SetTexture("_BaseMap", source.GetTexture(property));
                target.SetTextureScale("_BaseMap", source.GetTextureScale(property));
                target.SetTextureOffset("_BaseMap", source.GetTextureOffset(property));
                break;
            }
        target.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
            source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
    }
    public IEnumerator Reveal(float seconds, Color color)
    {
        var shader = Resources.Load<Shader>("Shop/VectorHologram");
        if (shader == null || pivot == null) yield break;
        foreach (var renderer in pivot.GetComponentsInChildren<Renderer>())
        {
            var source = renderer.sharedMaterials; originals[renderer] = source;
            var replacements = new Material[source.Length];
            for (int i = 0; i < source.Length; ++i)
            {
                var material = new Material(shader); holograms.Add(material); replacements[i] = material;
                CopyAlbedo(source[i], material);
                material.SetColor("_GlowColor", color);
            }
            renderer.sharedMaterials = replacements;
        }
        for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            foreach (var material in holograms) material.SetFloat("_Reveal", elapsed / seconds);
            yield return null;
        }
        RestoreMaterials();
        if (pivot != null)
            foreach (var animator in pivot.GetComponentsInChildren<Animator>())
                foreach (var parameter in animator.parameters)
                    if (parameter.name == "Greeting" && parameter.type == AnimatorControllerParameterType.Trigger) animator.SetTrigger("Greeting");
    }
    private void RestoreMaterials()
    {
        foreach (var pair in originals) if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        originals.Clear();
        foreach (var material in holograms) if (material != null) Destroy(material);
        holograms.Clear();
    }
    public void OnDrag(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && pivot != null)
            pivot.transform.Rotate(Vector3.up, -data.delta.x * 0.5f, Space.World);
    }
    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();
    private void Clear()
    {
        RestoreMaterials();
        foreach (var material in previewMaterials) if (material != null) Destroy(material);
        previewMaterials.Clear(); modelRoot = null; modelRenderers = null;
        if (stage != null) { stage.SetActive(false); Destroy(stage); }
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (TryGetComponent<RawImage>(out var image)) image.texture = null;
        stage = null; pivot = null; texture = null;
        if (stageSlot >= 0) occupiedStages.Remove(stageSlot);
        stageSlot = -1;
    }
}
