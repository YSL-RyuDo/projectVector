using UnityEngine;

public class WeaponHolderModel : MonoBehaviour
{
    [System.Serializable]
    public struct WeaponTransformPreset
    {
        public Vector3 localPosition;
        public Vector3 localRotation;
    }

    [Header("References")]
    private PlayerItem playerItem;

    private GameObject[] myHolderModels = new GameObject[5];
    private Item[] mirroredSourceItems = new Item[5];

    private int lastIndex = -1;

    [Header("Weapon Transform Presets")]
    [SerializeField] private WeaponTransformPreset _pistolPreset;
    [SerializeField] private WeaponTransformPreset _riflePreset;
    [SerializeField] private WeaponTransformPreset _knifePreset;


    public void ConfigureRuntimeDefaults(
        Vector3 localPosition,
        Vector3 localRotation)
    {
        WeaponTransformPreset preset = new WeaponTransformPreset
        {
            localPosition = localPosition,
            localRotation = localRotation
        };

        _pistolPreset = preset;
        _riflePreset = preset;
        _knifePreset = preset;
    }


    private void OnEnable()
    {
        RefreshNow();
    }


    private void Start()
    {
        if (!TryResolvePlayerItem())
        {
            Debug.LogError("[WeaponHolderModel] PlayerItem을 찾을 수 없습니다.");

            enabled = false;
            return;
        }

        RefreshNow();
    }

    private bool TryResolvePlayerItem()
    {
        if (playerItem == null)
            playerItem = GetComponentInParent<PlayerItem>(true);

        return playerItem != null;
    }


    public void RefreshNow()
    {
        if (!TryResolvePlayerItem())
            return;

        SyncModelsFromPlayerItem();

        int currentIndex = playerItem.CurrentIndex;

        lastIndex = currentIndex;
        UpdateWeaponVisibility(currentIndex);
    }


    private void SyncModelsFromPlayerItem()
    {
        Item[] targetItems = playerItem.Items;

        if (targetItems == null)
            return;

        int slotCount = Mathf.Min(
            targetItems.Length,
            myHolderModels.Length
        );

        for (int i = 0; i < slotCount; i++)
        {
            Item sourceItem = targetItems[i];

            if (mirroredSourceItems[i] == sourceItem)
                continue;

            if (myHolderModels[i] != null)
            {
                Destroy(myHolderModels[i]);
                myHolderModels[i] = null;
            }

            mirroredSourceItems[i] = sourceItem;

            if (sourceItem == null)
                continue;

            Weapon sourceWeapon =
                sourceItem.GetComponent<Weapon>();

            if (sourceWeapon == null)
                continue;

            WeaponData weaponData = sourceWeapon.Data;

            if (weaponData == null || weaponData.weaponPrefab == null)
                continue;

            GameObject clone = Instantiate(
                weaponData.weaponPrefab,
                transform
            );


            WeaponTransformPreset selectedPreset =
                GetPresetByItem(sourceItem);


            clone.transform.localPosition =
                selectedPreset.localPosition;

            clone.transform.localRotation =
                Quaternion.Euler(
                    selectedPreset.localRotation
                );

            Weapon cloneWeapon =
                clone.GetComponent<Weapon>();

            if (cloneWeapon != null)
            {
                cloneWeapon.ApplySkin(
                    sourceWeapon.AppliedSkin,
                    weaponData
                );
            }


            SetLayerRecursively(
                clone.transform,
                gameObject.layer
            );

            CleanUpComponents(clone);


            myHolderModels[i] = clone;

            clone.SetActive(false);
        }
    }


    private WeaponTransformPreset GetPresetByItem(Item item)
    {
        if (item == null)
            return _pistolPreset;

        Weapon weapon =
            item.GetComponent<Weapon>();

        if (weapon != null)
        {
            WeaponData.WeaponType wType =
                weapon.GetWeaponType();

            if (
                wType == WeaponData.WeaponType.AR ||
                wType == WeaponData.WeaponType.SMG ||
                wType == WeaponData.WeaponType.Shotgun ||
                wType == WeaponData.WeaponType.LMG ||
                wType == WeaponData.WeaponType.DMR ||
                wType == WeaponData.WeaponType.SR
            )
            {
                return _riflePreset;
            }

            if (wType == WeaponData.WeaponType.Knife)
            {
                return _knifePreset;
            }
        }

        return _pistolPreset;
    }


    private void UpdateWeaponVisibility(int activeIndex)
    {
        for (int i = 0; i < myHolderModels.Length; i++)
        {
            if (myHolderModels[i] == null)
                continue;

            myHolderModels[i].SetActive(
                i == activeIndex && playerItem.IsHeldWeaponVisible
            );
        }
    }


    private void CleanUpComponents(GameObject targetObj)
    {
        if (targetObj == null)
            return;

        MonoBehaviour[] scripts =
            targetObj.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
            {
                script.enabled = false;
                Destroy(script);
            }
        }


        Rigidbody[] rigidbodies =
            targetObj.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
                Destroy(rb);
        }


        Collider[] colliders =
            targetObj.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col != null)
                Destroy(col);
        }
    }


    private static void SetLayerRecursively(
        Transform root,
        int layer)
    {
        if (root == null)
            return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(
                root.GetChild(i),
                layer
            );
        }
    }

    // 외부에서 무기 상태가 변경됐을 때 호출
    public void ForceRefresh()
    {
        for (int i = 0; i < mirroredSourceItems.Length; i++)
        {
            mirroredSourceItems[i] = null;
        }

        lastIndex = -1;

        RefreshNow();
    }
}