using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponData primary;
    [SerializeField] private WeaponData secondary;
    [SerializeField] private WeaponData melee;
    [SerializeField] private WeaponData thrown;
    [SerializeField] private GameObject spike;

    [Header("others")]
    [SerializeField] private Transform itemHolder;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private LayerMask itemLayer;

    private Item[] items = new Item[5]; // 0~4 (5번 = Spike)
    private PlayerSpike playerSpike;
    private int currentIndex = -1;

    private PlayerIdentity _playerIdentity;

    private void Start()
    {
        playerSpike = GetComponent<PlayerSpike>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        InitializeDefaultItems();

        // 시작 장비는 무조건 권총
        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

    void Update()
    {
        if (!gameObject.CompareTag("Player")) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsAttackRestricted()) return;

            items[currentIndex]?.OnUse();
            Debug.Log("leftbutton");
        }
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (IsAttackRestricted()) return;

            items[currentIndex]?.OnAltUse();
            Debug.Log("rightbutton");
        }
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropCurrentItem();
            Debug.Log("g");
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
            Debug.Log("e");
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) Switch(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) Switch(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) Switch(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) Switch(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) Switch(4);
    }

    private void InitializeDefaultItems()
    {
        // 0번 슬롯: 메인 무기
        // 시작 시 메인 무기는 없음. 상점 구매 후에만 들어간다.
        items[0] = null;

        // 1번 슬롯: 권총
        if (secondary != null)
            CreateStartingItem(secondary, 1);
        else
            items[1] = null;

        // 2번 슬롯: 나이프
        if (melee != null)
            CreateStartingItem(melee, 2);
        else
            items[2] = null;

        // 3번 슬롯: 투척무기
        // 현재는 사용하지 않으므로 비워둔다.
        // 여기에 AWM이 들어가면 4번 키로 스나이퍼가 나온다.
        items[3] = null;

        // 4번 슬롯: 스파이크
        if (spike != null)
        {
            GameObject obj = Instantiate(spike, itemHolder);
            obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
            obj.transform.localRotation = Quaternion.identity;

            Item item = obj.GetComponent<Item>();
            items[4] = item;

            obj.SetActive(false);
        }
        else
        {
            items[4] = null;
        }
    }

    private void CreateStartingItem(WeaponData data, int slot)
    {
        // 프리팹 생성 및 부모 설정
        GameObject obj = Instantiate(data.weaponPrefab, itemHolder);

        // 위치 및 회전 초기화
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Item 컴포넌트 할당
        Item item = obj.GetComponent<Item>();
        items[slot] = item;

        // Weapon 컴포넌트 초기화 (기본 탄약 정보 등)
        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
        {
            // 초기 생성 시에는 최대 탄약으로 설정
            weapon.Initialize(data, dropPoint, data.magSize, data.maxReserveAmmo);
        }

        // 일단 모두 비활성화 (나중에 Switch에서 활성화됨)
        obj.SetActive(false);
    }

    void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            pickupRange,
            itemLayer
        );

        DroppedItem nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            DroppedItem di = hit.GetComponent<DroppedItem>();
            if (di == null) continue;

            float dist = Vector3.Distance(transform.position, di.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = di;
            }
        }

        if (nearest != null)
        {
            nearest.Pickup(this);
        }
    }

    public void PickupItem(DroppedItem dropped)
    {
        if (dropped.weaponData == null)
        {
            playerSpike.hasSpike = true;
            Debug.Log("Spike를 획득했습니다.");
            Destroy(dropped.gameObject);
            return;
        }

        int slot = GetSlotIndex(dropped.weaponData);
        EquipItem(slot, dropped);
    }

    public void EquipItem(int slot, DroppedItem dropped)
    {
        if (items[slot] != null)
        {
            items[slot].OnDrop();
            Destroy(items[slot].gameObject); // 반드시 .gameObject 파괴
        }

        GameObject obj = Instantiate(dropped.weaponData.weaponPrefab, itemHolder);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        items[slot] = item;

        // 무기 컴포넌트 설정 (탄약 및 데이터 초기화)
        Weapon weapon = obj.GetComponent<Weapon>();
        weapon?.Initialize(dropped.weaponData, dropPoint, dropped.magAmmo, dropped.reserveAmmo);
        
        Switch(slot);

        Destroy(dropped.gameObject);
    }

    void DropCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Length || items[currentIndex] == null) return;
        if (currentIndex == 2) return;

        items[currentIndex].OnDrop();

        if (currentIndex != 4)
        {
            Destroy(items[currentIndex].gameObject); // .gameObject 파괴
            items[currentIndex] = null;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                Switch(i);
                return;
            }
        }
    }

    void Switch(int index)
    {
        if (index < 0 || index >= items.Length)
            return;

        if (items[index] == null)
        {
            Debug.Log($"[PLAYER ITEM] slot empty. index={index}");
            return;
        }

        if (currentIndex == index)
            return;

        if (index == 4 && playerSpike != null && !playerSpike.hasSpike)
        {
            Debug.Log("스파이크가 없어서 5번 슬롯으로 전환할 수 없습니다.");
            return;
        }

        if (currentIndex >= 0 && currentIndex < items.Length)
        {
            items[currentIndex]?.OnUnequip();
        }

        currentIndex = index;

        items[currentIndex]?.OnEquip();

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();

        if (weapon != null &&
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId)
        {
            NetworkManager.Instance.SendWeaponSwitch(weapon.GetWeaponId());
        }
    }

    public int GetSlotIndex(WeaponData data)
    {
        if (data == null) return 0;

        switch (data.weaponType)
        {
            case WeaponData.WeaponType.Pistol:
                return 1;
            case WeaponData.WeaponType.Knife: // Knife 대용으로 사용 중인 경우
                return 2;
            default: // AR, SMG, SR 등은 모두 0번 슬롯
                return 0;
        }
    }

    public void SetWeaponVisibility(bool visibility)
    {
        if (visibility)
        {
            // 만약 현재 인덱스가 스파이크(4번)인데, 스파이크를 더 이상 가지고 있지 않다면
            if (currentIndex == 4 && !playerSpike.hasSpike)
            {
                // 0번(주무기)부터 순서대로 검사해서 있는 무기를 장착
                int fallbackIndex = -1;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && i != 4) // 스파이크 제외하고 있는 무기 찾기
                    {
                        fallbackIndex = i;
                        break;
                    }
                }

                // 바꿀 무기를 찾았다면 해당 무기로 스위치
                if (fallbackIndex != -1)
                {
                    Switch(fallbackIndex);
                    return; // Switch 함수 내부에서 SetActive(true)를 해주므로 여기서 종료
                }
            }
        }

        if (items[currentIndex] != null)
        {
            items[currentIndex].gameObject.SetActive(visibility);
        }
    }

    public void ResetAllAmmo()
    {
        for (int i = 0; i <= 1; i++)
        {
            if (items[i] == null)
                continue;

            Weapon weapon = items[i].GetComponent<Weapon>();

            if (weapon == null)
                continue;

            weapon.ReloadFull();
        }

        // 라운드 시작 기본 장착은 권총
        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

    public void ResetToDefault()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                Destroy(items[i].gameObject);
                items[i] = null;
            }
        }

        currentIndex = -1;

        // 라운드 리셋 후에도 메인 무기는 없음
        items[0] = null;

        // 권총 재생성
        if (secondary != null)
            CreateStartingItem(secondary, 1);
        else
            items[1] = null;

        // 나이프 재생성
        if (melee != null)
            CreateStartingItem(melee, 2);
        else
            items[2] = null;

        // 투척무기는 현재 없음
        items[3] = null;

        // 스파이크 재생성
        if (spike != null)
        {
            GameObject obj = Instantiate(spike, itemHolder);
            obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
            obj.transform.localRotation = Quaternion.identity;

            Item item = obj.GetComponent<Item>();
            items[4] = item;

            obj.SetActive(false);
        }
        else
        {
            items[4] = null;
        }

        // 기본 장착은 권총
        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }
    // 사격 조건
    bool IsAttackRestricted()
    {
        if (currentIndex < 0 || currentIndex >= items.Length)
            return true;

        if (items[currentIndex] == null)
            return true;

        if (RoundManager.Instance != null && !RoundManager.Instance.CanAttack)
            return true;

        return false;
    }

    public void EquipPrimaryWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
            return;

        if (GetSlotIndex(weaponData) != 0)
        {
            Debug.LogWarning($"[PLAYER ITEM] EquipPrimaryWeapon failed. weaponType={weaponData.weaponType}");
            return;
        }

        if (items[0] != null)
        {
            items[0].OnDrop();
            Destroy(items[0].gameObject);
            items[0] = null;
        }

        GameObject obj = Instantiate(weaponData.weaponPrefab, itemHolder);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        items[0] = item;

        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
            weapon.Initialize(weaponData, dropPoint, weaponData.magSize, weaponData.maxReserveAmmo);

        obj.SetActive(false);

        Switch(0);

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.SetMainWeapon(weaponData);
    }

    public Weapon GetCurrentWeapon()
    {
        if (currentIndex < 0 || currentIndex >= items.Length)
            return null;

        if (items[currentIndex] == null)
            return null;

        return items[currentIndex].GetComponent<Weapon>();
    }
}