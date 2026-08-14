using UnityEngine;

public class WeaponHolderModel : MonoBehaviour
{
    // 1. 무기 타입별 트랜스폼 설정을 인스펙터에서 편하게 조절하기 위한 구조체
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

    // 2. 인스펙터에서 무기군별로 따로 설정할 수 있는 변수들
    [Header("Weapon Transform Presets")]
    [SerializeField] private WeaponTransformPreset _pistolPreset;   // 권총 (WeaponType.Pistol 혹은 기본)
    [SerializeField] private WeaponTransformPreset _riflePreset;    // 긴총 (AR, SMG, Shotgun, LMG, DMR, SR)
    [SerializeField] private WeaponTransformPreset _knifePreset;    // 칼 (Knife)

    public void ConfigureRuntimeDefaults(Vector3 localPosition, Vector3 localRotation)
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

    void Start()
    {
        if (!TryResolvePlayerItem())
        {
            Debug.LogError($"[WeaponHolderModel] PlayerItem을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        RefreshNow();
    }

    void Update()
    {
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
        if (lastIndex != currentIndex)
        {
            lastIndex = currentIndex;
            UpdateWeaponVisibility(currentIndex);
        }
    }
    private void SyncModelsFromPlayerItem()
    {
        Item[] targetItems = playerItem.Items;
        if (targetItems == null)
            return;

        int slotCount = Mathf.Min(targetItems.Length, myHolderModels.Length);

        for (int i = 0; i < slotCount; i++)
        {
            Item sourceItem = targetItems[i];

            // A pickup or round reset can replace an item inside the same slot
            // without leaving the slot null for a frame. Track the source
            // instance so the third-person clone and its exact skin are rebuilt.
            if (mirroredSourceItems[i] == sourceItem)
                continue;

            mirroredSourceItems[i] = sourceItem;

            if (myHolderModels[i] != null)
            {
                Destroy(myHolderModels[i]);
                myHolderModels[i] = null;
            }

            if (sourceItem == null)
                continue;

            GameObject clone = Instantiate(sourceItem.gameObject, transform);
            WeaponTransformPreset selectedPreset = GetPresetByItem(sourceItem);

            // Explicitly copy the applied skin. Runtime child cloning alone is
            // unreliable when the source first-person holder is inactive.
            Weapon sourceWeapon = sourceItem.GetComponent<Weapon>();
            Weapon cloneWeapon = clone.GetComponent<Weapon>();

            if (sourceWeapon != null && cloneWeapon != null)
            {
                cloneWeapon.ApplySkin(sourceWeapon.AppliedSkin, sourceWeapon.Data);
            }

            clone.transform.localPosition = selectedPreset.localPosition;
            clone.transform.localRotation = Quaternion.Euler(selectedPreset.localRotation);
            SetLayerRecursively(clone.transform, gameObject.layer);
            CleanUpComponents(clone);

            myHolderModels[i] = clone;
            clone.SetActive(false);
            lastIndex = -2;
        }
    }
    // 3. 아이템의 WeaponType을 분석해서 올바른 프리셋을 반환하는 헬퍼 함수
    private WeaponTransformPreset GetPresetByItem(Item item)
    {
        // 아이템 오브젝트에서 Weapon 컴포넌트를 가져옴
        Weapon weapon = item.GetComponent<Weapon>();
        if (weapon != null)
        {
            WeaponData.WeaponType wType = weapon.GetWeaponType();

            // 긴총 계열 분기
            if (wType == WeaponData.WeaponType.AR ||
                wType == WeaponData.WeaponType.SMG ||
                wType == WeaponData.WeaponType.Shotgun ||
                wType == WeaponData.WeaponType.LMG ||
                wType == WeaponData.WeaponType.DMR ||
                wType == WeaponData.WeaponType.SR)
            {
                return _riflePreset;
            }
            // 칼 분기
            else if (wType == WeaponData.WeaponType.Knife)
            {
                return _knifePreset;
            }
        }

        // Weapon 컴포넌트가 없거나 권총(Pistol) 등 기본 무기일 경우 피스톨 프리셋 반환
        return _pistolPreset;
    }

    private void UpdateWeaponVisibility(int activeIndex)
    {
        for (int i = 0; i < myHolderModels.Length; i++)
        {
            if (myHolderModels[i] == null) continue;

            // 현재 들고 있는 인덱스의 복사본만 켜고, 나머지는 끔!
            myHolderModels[i].SetActive(i == activeIndex);
        }
    }

    private void CleanUpComponents(GameObject targetObj)
    {
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

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
            return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}