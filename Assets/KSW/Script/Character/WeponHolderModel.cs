using UnityEngine;

public class WeaponHolderModel : MonoBehaviour
{
    [Header("References")]
    private PlayerItem playerItem;

    private GameObject[] myHolderModels = new GameObject[5];
    private int lastIndex = -1;

    [SerializeField] private Vector3 _localPosition;
    [SerializeField] private Vector3 _localRotation;

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

                clone.transform.localPosition = new Vector3(_localPosition.x, _localPosition.y, _localPosition.z);
                clone.transform.localRotation = Quaternion.Euler(_localRotation.x, _localRotation.y, _localRotation.z);

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