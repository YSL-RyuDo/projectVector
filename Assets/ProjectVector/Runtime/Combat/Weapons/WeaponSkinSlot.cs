using UnityEngine;

public class WeaponSkinSlot : MonoBehaviour
{
    [SerializeField] private Transform visualModelSocket; // "VisualModel" 위치
    [SerializeField] private GameObject defaultVisualModel; // 기본 모델(스킨 없을 때)

    private GameObject spawnedModel;
    private RuntimeAnimatorController visualAnimatorController;
    private Renderer[] defaultVisualRenderers;
    private bool initialized;

    public Animator ActiveVisualAnimator { get; private set; }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        // Cache renderers from the runtime instance. Nested prefab references
        // can otherwise leave the default drop model visible under the skin.
        defaultVisualRenderers = GetComponentsInChildren<Renderer>(true);

        Animator rootAnimator = GetComponent<Animator>();

        if (rootAnimator != null)
        {
            visualAnimatorController = rootAnimator.runtimeAnimatorController;
            rootAnimator.enabled = false;
        }

        if (visualAnimatorController == null && defaultVisualModel != null)
        {
            Animator defaultAnimator = defaultVisualModel.GetComponent<Animator>();
            if (defaultAnimator != null)
                visualAnimatorController = defaultAnimator.runtimeAnimatorController;
        }

        BindVisualAnimator(defaultVisualModel);
    }

    public void ApplySkin(WeaponSkinData skin)
    {
        // Starting weapons can be created below an inactive holder, which
        // delays Awake. Initialize here as well so round one applies skins.
        EnsureInitialized();

        if (visualModelSocket == null)
        {
            Debug.LogError($"[WEAPON SKIN SLOT] visualModelSocket not assigned on {name}.", this);
            return;
        }

        if (spawnedModel != null)
        {
            spawnedModel.SetActive(false);
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        if (skin == null || skin.visualModelPrefab == null)
        {
            SetDefaultVisualVisible(true);

            BindVisualAnimator(defaultVisualModel);
            return;
        }

        SetDefaultVisualVisible(false);

        spawnedModel = Instantiate(skin.visualModelPrefab, visualModelSocket, false);

        // The skin replaces the default visual in this context. Held and dropped
        // prefabs can use different root corrections, so copy the default root.
        if (defaultVisualModel != null)
        {
            Transform defaultTransform = defaultVisualModel.transform;
            spawnedModel.transform.localPosition = defaultTransform.localPosition;
            spawnedModel.transform.localRotation = defaultTransform.localRotation;
            spawnedModel.transform.localScale = defaultTransform.localScale;
        }

        int visualLayer = defaultVisualModel != null
            ? defaultVisualModel.layer
            : visualModelSocket.gameObject.layer;
        SetLayerRecursively(spawnedModel.transform, visualLayer);
        BindVisualAnimator(spawnedModel);
    }

    private void SetDefaultVisualVisible(bool visible)
    {
        if (defaultVisualModel != null && defaultVisualModel.scene.IsValid())
            defaultVisualModel.SetActive(visible);

        if (defaultVisualRenderers == null)
            return;

        for (int i = 0; i < defaultVisualRenderers.Length; i++)
        {
            Renderer renderer = defaultVisualRenderers[i];
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private void BindVisualAnimator(GameObject visualModel)
    {
        ActiveVisualAnimator = null;

        if (visualModel != null && visualAnimatorController != null)
        {
            Animator visualAnimator = visualModel.GetComponent<Animator>();

            if (visualAnimator == null)
                visualAnimator = visualModel.AddComponent<Animator>();

            visualAnimator.runtimeAnimatorController = visualAnimatorController;
            visualAnimator.applyRootMotion = false;
            visualAnimator.enabled = true;
            ActiveVisualAnimator = visualAnimator;
        }

        ARMotion motion = GetComponent<ARMotion>();
        if (motion != null)
            motion.SetAnimator(ActiveVisualAnimator);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}