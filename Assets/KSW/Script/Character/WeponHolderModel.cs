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
    private int lastIndex = -1;

    // 2. 인스펙터에서 무기군별로 따로 설정할 수 있는 변수들
    [Header("Weapon Transform Presets")]
    [SerializeField] private WeaponTransformPreset _pistolPreset;   // 권총 (WeaponType.Pistol 혹은 기본)
    [SerializeField] private WeaponTransformPreset _riflePreset;    // 긴총 (AR, SMG, Shotgun, LMG, DMR, SR)
    [SerializeField] private WeaponTransformPreset _knifePreset;    // 칼 (Knife)

    void Start()
    {
        if (playerItem == null) playerItem = GetComponentInParent<PlayerItem>();

        if (playerItem == null)
        {
            Debug.LogError($"[WeaponHolderModel] PlayerItem을 찾을 수 없습니다!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (playerItem == null) return;

        // 1. PlayerItem에 실시간으로 무기가 생기거나 버려졌는지 체크해서 내 자식으로 복사/삭제
        SyncModelsFromPlayerItem();

        // 2. 현재 들고 있는 인덱스가 바뀌면 복사본들만 껐다 켜기
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
        if (targetItems == null) return;

        for (int i = 0; i < targetItems.Length; i++)
        {
            if (targetItems[i] != null && myHolderModels[i] == null)
            {
                GameObject clone = Instantiate(targetItems[i].gameObject, transform);

                // --- [수정] 무기 종류를 판단해서 맞는 프리셋을 적용 ---
                WeaponTransformPreset selectedPreset = GetPresetByItem(targetItems[i]);

                clone.transform.localPosition = selectedPreset.localPosition;
                clone.transform.localRotation = Quaternion.Euler(selectedPreset.localRotation);
                // ----------------------------------------------------

                CleanUpComponents(clone);

                myHolderModels[i] = clone;

                clone.SetActive(false);
                lastIndex = -2;
            }
            else if (targetItems[i] == null && myHolderModels[i] != null)
            {
                Destroy(myHolderModels[i]);
                myHolderModels[i] = null;
            }
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
        // 렌더러(MeshRenderer 등)를 제외한 모든 스크립트(사격 로직 등) 파괴
        MonoBehaviour[] scripts = targetObj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            if (script != null && !(script is Renderer))
                Destroy(script);
        }

        // 물리 및 충돌체 파괴 (벽에 걸리거나 혼자 날아가는 것 방지)
        foreach (var rb in targetObj.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var col in targetObj.GetComponentsInChildren<Collider>(true)) Destroy(col);
    }
}