using UnityEngine;

public class WeaponSkinSlot : MonoBehaviour
{
    [SerializeField] private Transform visualModelSocket; // "VisualModel" 위치
    [SerializeField] private GameObject defaultVisualModel; // 기본 모델(스킨 없을 때)

    private GameObject spawnedModel;

    public void ApplySkin(WeaponSkinData skin)
    {
        if (visualModelSocket == null)
        {
            Debug.LogError($"[WEAPON SKIN SLOT] visualModelSocket not assigned on {name}.", this);
            return;
        }

        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        if (skin == null || skin.visualModelPrefab == null)
        {
            if (defaultVisualModel != null)
                defaultVisualModel.SetActive(true);
            return;
        }

        if (defaultVisualModel != null)
            defaultVisualModel.SetActive(false);

        spawnedModel = Instantiate(
            skin.visualModelPrefab,
            visualModelSocket.position,
            visualModelSocket.rotation,
            visualModelSocket
        );
    }
}