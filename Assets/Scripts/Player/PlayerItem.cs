using System.Collections;
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
    [SerializeField] private int currentIndex = -1;

    private PlayerIdentity _playerIdentity;

    private bool isReloading;
    private Coroutine reloadCoroutine;
    private GameObject reloadingWeaponObject;

    public bool IsReloading => isReloading;

    public Item[] Items => items;
    public int CurrentIndex => currentIndex;

    private int reloadingWeaponId = -1;

    private bool remoteIsReloading;
    private int remoteReloadWeaponId = -1;

    private void Start()
    {
        playerSpike = GetComponent<PlayerSpike>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        InitializeDefaultItems();

        // 프리팹 Inspector에 이전 값이 남아 있어도 강제로 초기화
        currentIndex = -1;

        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

    void Update()
    {
        if (InGameChatUI.BlocksGameplayInput)
            return;

        if (!gameObject.CompareTag("Player"))
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
        {
            return;
        }

        if (_playerIdentity == null ||
            NetworkManager.Instance == null)
        {
            return;
        }

        if (_playerIdentity.PlayerId !=
            NetworkManager.Instance.MyPlayerId)
        {
            return;
        }

        if (Keyboard.current == null)
            return;

        // 장전 중에도 무기 슬롯 변경은 허용한다.
        // 실제 취소 처리는 Switch() 안에서 수행한다.
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Switch(0);
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Switch(1);
            return;
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Switch(2);
            return;
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Switch(3);
            return;
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Switch(4);
            return;
        }

        // R키 장전
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryStartReload();
            return;
        }

        // 장전 중에는 드롭/픽업 같은 나머지 입력만 차단한다.
        if (isReloading)
            return;

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

            if (di == null)
                di = hit.GetComponentInParent<DroppedItem>();

            if (di == null)
                continue;

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
        if (dropped == null)
            return;

        dropped.Pickup(this);
    }

    public void EquipItem(int slot, DroppedItem dropped)
    {
        Debug.LogWarning("[PLAYER ITEM] EquipItem is disabled. Use server pickup flow.");
    }

    private void DropCurrentItem()
    {
        if (currentIndex < 0 ||
            currentIndex >= items.Length)
        {
            Debug.LogWarning(
                $"[PLAYER ITEM DROP] invalid currentIndex={currentIndex}"
            );

            return;
        }

        if (items[currentIndex] == null)
        {
            Debug.LogWarning(
                $"[PLAYER ITEM DROP] current item is null. index={currentIndex}"
            );

            return;
        }

        // 4번 인덱스는 5번 슬롯 스파이크
        if (currentIndex == 4)
        {
            if (playerSpike == null)
            {
                playerSpike =
                    GetComponent<PlayerSpike>();
            }

            if (playerSpike == null)
            {
                Debug.LogError(
                    "[PLAYER ITEM DROP] PlayerSpike component not found"
                );

                return;
            }

            if (!playerSpike.hasSpike)
            {
                Debug.LogWarning(
                    "[PLAYER ITEM DROP] player does not have spike"
                );

                return;
            }

            // 실제 스파이크 드랍 패킷 전송
            playerSpike.RequestDrop();

            Debug.Log(
                "[PLAYER ITEM DROP] spike drop requested"
            );

            return;
        }

        Weapon weapon =
            items[currentIndex].GetComponent<Weapon>();

        if (weapon == null)
        {
            Debug.LogWarning(
                $"[PLAYER ITEM DROP] Weapon component missing. " +
                $"index={currentIndex}"
            );

            return;
        }

        if (NetworkManager.Instance == null)
        {
            Debug.LogError(
                "[PLAYER ITEM DROP] NetworkManager.Instance is null"
            );

            return;
        }

        Vector3 dropPosition =
            dropPoint != null
                ? dropPoint.position
                : transform.position +
                  transform.forward * 0.8f +
                  Vector3.up * 0.5f;

        Vector3 dropDirection =
            dropPoint != null
                ? dropPoint.forward
                : transform.forward;

        NetworkManager.Instance.SendWeaponDropRequest(
            weapon.GetWeaponId(),
            dropPosition,
            dropDirection
        );

        Debug.Log(
            $"[PLAYER ITEM DROP] weapon drop requested. " +
            $"weaponId={weapon.GetWeaponId()}, " +
            $"pos={dropPosition}, dir={dropDirection}"
        );
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

        // 장전 중 다른 슬롯으로 바꾸면 기존 장전을 즉시 취소한다.
        // CancelReload()가 코루틴 정지, 애니메이션 정지,
        // 서버 장전 상태 취소 패킷 전송까지 처리한다.
        if (isReloading)
        {
            int canceledWeaponId = reloadingWeaponId;
            CancelReload(false);

            Debug.Log(
                $"[PLAYER ITEM] reload canceled by weapon switch. " +
                $"weaponId={canceledWeaponId}, targetSlot={index}"
            );
        }

        if (currentIndex >= 0 && currentIndex < items.Length)
        {
            items[currentIndex]?.OnUnequip();
        }

        currentIndex = index;

        items[currentIndex]?.OnEquip();

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();

        if (weapon != null && playerMovement != null)
        {
            playerMovement.SetWeaponWeight(
                weapon.GetWeight()
            );
        }

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
        if (currentIndex < 0 || currentIndex >= items.Length)
            return;

        if (items[currentIndex] == null)
            return;

        if (visibility)
        {
            if (currentIndex == 4 && playerSpike != null && !playerSpike.hasSpike)
            {
                int fallbackIndex = FindFirstAvailableWeaponSlotExceptSpike();

                if (fallbackIndex != -1)
                {
                    SwitchWeaponVisualOnlyBySlot(fallbackIndex);
                    return;
                }
            }
        }

        items[currentIndex].gameObject.SetActive(visibility);
    }

    private int FindFirstAvailableWeaponSlotExceptSpike()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (i == 4)
                continue;

            if (items[i] != null)
                return i;
        }

        return -1;
    }

    private void SwitchWeaponVisualOnlyBySlot(int slot)
    {
        if (slot < 0 || slot >= items.Length)
            return;

        if (items[slot] == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].gameObject.SetActive(i == slot);
        }

        currentIndex = slot;
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

    public void SetWeaponAmmoFromServer(
    int weaponId,
    int magAmmo,
    int reserveAmmo)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            Weapon weapon =
                items[i].GetComponent<Weapon>();

            if (weapon == null)
                continue;

            if (weapon.GetWeaponId() != weaponId)
                continue;

            weapon.SetAmmo(
                magAmmo,
                reserveAmmo
            );

            Debug.Log(
                $"[PLAYER ITEM AMMO APPLY] " +
                $"weaponId={weaponId}, " +
                $"ammo={magAmmo}/{reserveAmmo}"
            );

            return;
        }

        Debug.LogWarning(
            $"[PLAYER ITEM AMMO APPLY] " +
            $"weapon not found. weaponId={weaponId}"
        );
    }

    public void PlayFireVisualFromServer(
    int weaponId)
    {
        Weapon currentWeapon =
            GetCurrentWeapon();

        if (currentWeapon == null)
            return;

        if (currentWeapon.GetWeaponId() != weaponId)
            return;

        currentWeapon.PlayFireVisualFromServer();
    }

    public void PlayKnifeAttackVisualFromServer()
    {
        if (currentIndex < 0 ||
            currentIndex >= items.Length)
        {
            Debug.LogWarning(
                $"[REMOTE KNIFE] invalid currentIndex={currentIndex}"
            );

            return;
        }

        Item currentItem =
            items[currentIndex];

        if (currentItem == null)
        {
            Debug.LogWarning(
                "[REMOTE KNIFE] current item is null"
            );

            return;
        }

        Weapon currentWeapon =
            currentItem.GetComponent<Weapon>();

        if (currentWeapon == null)
        {
            currentWeapon =
                currentItem.GetComponentInChildren<Weapon>(true);
        }

        if (currentWeapon == null)
        {
            Debug.LogWarning(
                "[REMOTE KNIFE] Weapon component not found"
            );

            return;
        }

        if (currentWeapon.GetWeaponType() !=
            WeaponData.WeaponType.Knife)
        {
            Debug.LogWarning(
                $"[REMOTE KNIFE] current weapon is not knife. " +
                $"weaponId={currentWeapon.GetWeaponId()}"
            );

            return;
        }

        KnifeMotion knifeMotion =
            currentItem.GetComponent<KnifeMotion>();

        if (knifeMotion == null)
        {
            knifeMotion =
                currentItem.GetComponentInChildren<KnifeMotion>(
                    true
                );
        }

        if (knifeMotion == null)
        {
            Debug.LogWarning(
                $"[REMOTE KNIFE] KnifeMotion not found. " +
                $"weaponId={currentWeapon.GetWeaponId()}"
            );

            return;
        }

        knifeMotion.PlayAttackFromServer();

        Debug.Log(
            $"[REMOTE KNIFE VISUAL] played. " +
            $"playerId={_playerIdentity?.PlayerId}, " +
            $"weaponId={currentWeapon.GetWeaponId()}"
        );
    }

    public void SetSpikeItemActive(bool value)
    {
        if (items[4] == null)
        {
            Debug.Log("[PLAYER ITEM] Spike slot is empty.");
            return;
        }

        if (!value && currentIndex == 4)
        {
            Switch(1);
        }

        items[4].gameObject.SetActive(false);
    }

    public void GiveSpike(GameObject spikePrefab)
    {
        if (items[4] != null)
            Destroy(items[4].gameObject);

        GameObject obj = Instantiate(spikePrefab, itemHolder);
        obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        items[4] = item;

        obj.SetActive(false);

        Debug.Log("[PLAYER ITEM] Spike added to slot 5");
    }

    public void RemoveSpike()
    {
        if (currentIndex == 4)
        {
            if (items[1] != null)
                Switch(1);
            else if (items[2] != null)
                Switch(2);
        }

        if (items[4] != null)
        {
            Destroy(items[4].gameObject);
            items[4] = null;
        }

        Debug.Log("[PLAYER ITEM] Spike removed from slot 5");
    }

    public void DisableOnDeath()
    {
        CancelReload(false);

        remoteIsReloading = false;
        remoteReloadWeaponId = -1;

        SetWeaponVisibility(false);
        enabled = false;
    }

    public void EnableOnRespawn()
    {
        enabled = true;

        if (currentIndex >= 0 && currentIndex < items.Length && items[currentIndex] != null)
            items[currentIndex].gameObject.SetActive(true);
    }

    public void ApplyWeaponSwitchFromServer(int weaponId)
    {
        // 서버 통지로 받은 무기 변경이다.
        // 여기서는 다시 NetworkManager.SendWeaponSwitch() 보내면 안 된다.

        SwitchWeaponVisualOnly(weaponId);

        Debug.Log($"[PLAYER ITEM] remote weapon switch weaponId={weaponId}");
    }

    private void SwitchWeaponVisualOnly(int weaponId)
    {
        int foundSlot = -1;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            Weapon weapon = items[i].GetComponent<Weapon>();

            if (weapon == null)
                continue;

            bool active = weapon.GetWeaponId() == weaponId;
            items[i].gameObject.SetActive(active);

            if (active)
                foundSlot = i;
        }

        if (foundSlot != -1)
        {
            currentIndex = foundSlot;
        }
        else
        {
            Debug.LogWarning($"[PLAYER ITEM] weaponId not found. weaponId={weaponId}");
        }
    }

    public void SetADSVisualFromServer(bool isADS)
    {
        Weapon currentWeapon = GetCurrentWeapon();

        if (currentWeapon == null)
        {
            Debug.LogWarning("[PLAYER ITEM] ADS visual failed. currentWeapon is null");
            return;
        }

        WeaponADS weaponADS = currentWeapon.GetComponent<WeaponADS>();

        if (weaponADS != null)
            weaponADS.SetADS(isADS);

        Debug.Log($"[PLAYER ITEM] ADS visual from server isADS={isADS}");
    }

    public void RemoveWeaponFromServer(int weaponId)
    {
        int slot = FindSlotByWeaponId(weaponId);

        if (slot == -1)
        {
            Debug.LogWarning($"[PLAYER ITEM] RemoveWeaponFromServer failed. weaponId={weaponId}");
            return;
        }

        bool wasCurrent = currentIndex == slot;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        if (wasCurrent)
        {
            currentIndex = -1;

            int fallbackSlot = FindFirstAvailableWeaponSlotExceptSpike();

            if (fallbackSlot != -1)
                SwitchWeaponVisualOnlyBySlot(fallbackSlot);
        }

        Debug.Log($"[PLAYER ITEM] weapon removed by server. weaponId={weaponId}, slot={slot}");
    }

    private int FindSlotByWeaponId(int weaponId)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            Weapon weapon = items[i].GetComponent<Weapon>();

            if (weapon == null)
                continue;

            if (weapon.GetWeaponId() == weaponId)
                return i;
        }

        return -1;
    }

    public void ApplyWeaponPickupFromServer(
    int weaponId,
    int magAmmo,
    int reserveAmmo)
    {
        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError("[PLAYER ITEM] WeaponDropSpawner.Instance is null");
            return;
        }

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);

        if (data == null)
        {
            Debug.LogError($"[PLAYER ITEM] WeaponData not found. weaponId={weaponId}");
            return;
        }

        int slot = GetSlotIndex(data);

        if (slot < 0 || slot >= items.Length)
        {
            Debug.LogError($"[PLAYER ITEM] invalid slot. weaponId={weaponId}, slot={slot}");
            return;
        }

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        GameObject obj = Instantiate(data.weaponPrefab, itemHolder);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();

        if (item == null)
        {
            Debug.LogError($"[PLAYER ITEM] Item component missing. weaponId={weaponId}");
            Destroy(obj);
            return;
        }

        items[slot] = item;

        Weapon weapon = obj.GetComponent<Weapon>();

        if (weapon != null)
        {
            weapon.Initialize(
                data,
                dropPoint,
                magAmmo,
                reserveAmmo
            );
        }

        obj.SetActive(false);

        SwitchWeaponVisualOnlyBySlot(slot);

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.SetBulletText(magAmmo, reserveAmmo);

        Debug.Log(
            $"[PLAYER ITEM] weapon pickup applied by server. " +
            $"weaponId={weaponId}, slot={slot}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    private void DisableHeldWeaponPhysics(GameObject obj)
    {
        if (obj == null)
            return;

        DroppedItem droppedItem = obj.GetComponent<DroppedItem>();
        if (droppedItem != null)
            Destroy(droppedItem);

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb == null)
                continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            col.enabled = false;
        }
    }

    public void ApplyRoundResetInventoryFromServer(
    int currentWeaponId,
    int primaryWeaponId,
    int primaryMagAmmo,
    int primaryReserveAmmo,
    int pistolWeaponId,
    int pistolMagAmmo,
    int pistolReserveAmmo,
    int knifeWeaponId,
    int knifeMagAmmo,
    int knifeReserveAmmo)
    {
        CancelReload(false);

        remoteIsReloading = false;
        remoteReloadWeaponId = -1;

        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        ClearWeaponSlotsForRoundReset();

        if (primaryWeaponId >= 0)
        {
            CreateWeaponFromServerForRoundReset(
                primaryWeaponId,
                primaryMagAmmo,
                primaryReserveAmmo
            );
        }

        if (pistolWeaponId >= 0)
        {
            CreateWeaponFromServerForRoundReset(
                pistolWeaponId,
                pistolMagAmmo,
                pistolReserveAmmo
            );
        }

        if (knifeWeaponId >= 0)
        {
            CreateWeaponFromServerForRoundReset(
                knifeWeaponId,
                knifeMagAmmo,
                knifeReserveAmmo
            );
        }

        int currentSlot = FindSlotByWeaponId(currentWeaponId);

        bool isLocal =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (currentSlot != -1)
        {
            if (isLocal)
            {
                // 로컬 플레이어는 실제 Switch를 호출해서
                // OnEquip 처리 + 서버에 SendWeaponSwitch까지 보낸다.
                Switch(currentSlot);
            }
            else
            {
                // 원격 플레이어는 서버에 패킷을 보내면 안 되므로 비주얼만 변경
                SwitchWeaponVisualOnlyBySlot(currentSlot);
            }
        }
        else
        {
            int fallbackSlot = FindFirstAvailableWeaponSlotExceptSpike();

            if (fallbackSlot != -1)
            {
                if (isLocal)
                    Switch(fallbackSlot);
                else
                    SwitchWeaponVisualOnlyBySlot(fallbackSlot);
            }
            else
            {
                currentIndex = -1;
            }
        }

        UpdateBulletTextFromRoundResetData(
            currentWeaponId,
            primaryWeaponId,
            primaryMagAmmo,
            primaryReserveAmmo,
            pistolWeaponId,
            pistolMagAmmo,
            pistolReserveAmmo,
            knifeWeaponId,
            knifeMagAmmo,
            knifeReserveAmmo
        );

        Debug.Log(
            $"[PLAYER ITEM] round reset inventory applied. " +
            $"current={currentWeaponId}, primary={primaryWeaponId}, pistol={pistolWeaponId}, knife={knifeWeaponId}"
        );
    }

    private void ClearWeaponSlotsForRoundReset()
    {
        // 0 = 주무기, 1 = 보조무기, 2 = 칼, 3 = 투척무기
        // 4 = 스파이크는 여기서 건드리지 않는다.
        for (int i = 0; i <= 3; i++)
        {
            if (items[i] != null)
            {
                Destroy(items[i].gameObject);
                items[i] = null;
            }
        }

        currentIndex = -1;

        bool isLocal =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isLocal && InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetMainWeapon(null);
            InGameUIManager.Instance.SetSubWeapon(null);
            InGameUIManager.Instance.SetKnife(null);
            InGameUIManager.Instance.SetBulletText(0, 0);
        }
    }

    private void CreateWeaponFromServerForRoundReset(
        int weaponId,
        int magAmmo,
        int reserveAmmo)
    {
        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError("[PLAYER ITEM] WeaponDropSpawner.Instance is null");
            return;
        }

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);

        if (data == null)
        {
            Debug.LogError($"[PLAYER ITEM] WeaponData not found. weaponId={weaponId}");
            return;
        }

        int slot = GetSlotIndex(data);

        if (slot < 0 || slot >= items.Length)
        {
            Debug.LogError($"[PLAYER ITEM] invalid slot. weaponId={weaponId}, slot={slot}");
            return;
        }

        if (slot == 4)
        {
            Debug.LogWarning($"[PLAYER ITEM] weapon cannot use spike slot. weaponId={weaponId}");
            return;
        }

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        GameObject obj = Instantiate(data.weaponPrefab, itemHolder);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();

        if (item == null)
        {
            Debug.LogError($"[PLAYER ITEM] Item component missing. weaponId={weaponId}");
            Destroy(obj);
            return;
        }

        items[slot] = item;

        Weapon weapon = obj.GetComponent<Weapon>();

        if (weapon != null)
        {
            weapon.Initialize(
                data,
                dropPoint,
                magAmmo,
                reserveAmmo
            );
        }

        obj.SetActive(false);

        bool isLocal =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isLocal && InGameUIManager.Instance != null)
        {
            if (slot == 0)
            {
                InGameUIManager.Instance.SetMainWeapon(data);
            }
            else if (slot == 1)
            {
                InGameUIManager.Instance.SetSubWeapon(data.weaponIcon);
            }
            else if (slot == 2)
            {
                InGameUIManager.Instance.SetKnife(data.weaponIcon);
            }
        }

        Debug.Log(
            $"[PLAYER ITEM] round reset weapon created. weaponId={weaponId}, slot={slot}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    private void UpdateBulletTextFromRoundResetData(
        int currentWeaponId,
        int primaryWeaponId,
        int primaryMagAmmo,
        int primaryReserveAmmo,
        int pistolWeaponId,
        int pistolMagAmmo,
        int pistolReserveAmmo,
        int knifeWeaponId,
        int knifeMagAmmo,
        int knifeReserveAmmo)
    {
        bool isLocal =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (!isLocal)
            return;

        if (InGameUIManager.Instance == null)
            return;

        if (currentWeaponId == primaryWeaponId)
        {
            InGameUIManager.Instance.SetBulletText(primaryMagAmmo, primaryReserveAmmo);
            return;
        }

        if (currentWeaponId == pistolWeaponId)
        {
            InGameUIManager.Instance.SetBulletText(pistolMagAmmo, pistolReserveAmmo);
            return;
        }

        if (currentWeaponId == knifeWeaponId)
        {
            InGameUIManager.Instance.SetBulletText(knifeMagAmmo, knifeReserveAmmo);
            return;
        }

        InGameUIManager.Instance.SetBulletText(0, 0);
    }

    public void ApplyPickedWeaponFromServer(
    WeaponData weaponData,
    int magAmmo,
    int reserveAmmo)
    {
        if (weaponData == null)
        {
            Debug.LogError("[PLAYER ITEM] ApplyPickedWeaponFromServer failed. weaponData is null");
            return;
        }

        EquipWeaponByServerData(weaponData, magAmmo, reserveAmmo);
    }

    private void EquipWeaponByServerData(
    WeaponData weaponData,
    int magAmmo,
    int reserveAmmo)
    {
        int slot = GetSlotIndex(weaponData);

        if (slot < 0 || slot >= items.Length)
        {
            Debug.LogError($"[PLAYER ITEM] invalid slot. weaponId={weaponData.weaponId}, slot={slot}");
            return;
        }

        if (slot == 4)
        {
            Debug.LogWarning($"[PLAYER ITEM] spike slot cannot equip weapon. weaponId={weaponData.weaponId}");
            return;
        }

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        GameObject obj = Instantiate(weaponData.weaponPrefab, itemHolder);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogError($"[PLAYER ITEM] Item component missing. weaponId={weaponData.weaponId}");
            Destroy(obj);
            return;
        }

        items[slot] = item;

        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
        {
            weapon.Initialize(
                weaponData,
                dropPoint,
                magAmmo,
                reserveAmmo
            );
        }

        obj.SetActive(false);

        bool isLocal =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isLocal)
        {
            Switch(slot);

            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetBulletText(magAmmo, reserveAmmo);

                if (slot == 0)
                    InGameUIManager.Instance.SetMainWeapon(weaponData);
                else if (slot == 1)
                    InGameUIManager.Instance.SetSubWeapon(weaponData.weaponIcon);
                else if (slot == 2)
                    InGameUIManager.Instance.SetKnife(weaponData.weaponIcon);
            }
        }
        else
        {
            SwitchWeaponVisualOnlyBySlot(slot);
        }

        Debug.Log(
            $"[PLAYER ITEM] weapon pickup equipped by server. " +
            $"weaponId={weaponData.weaponId}, slot={slot}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void RefreshCurrentWeaponAmmoFromServer()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        if (_playerIdentity == null)
        {
            Debug.LogError(
                "[PLAYER ITEM AMMO REFRESH] PlayerIdentity is null"
            );

            return;
        }

        if (NetworkManager.Instance == null)
        {
            Debug.LogError(
                "[PLAYER ITEM AMMO REFRESH] NetworkManager.Instance is null"
            );

            return;
        }

        if (_playerIdentity.PlayerId !=
            NetworkManager.Instance.MyPlayerId)
        {
            return;
        }

        Weapon currentWeapon =
            GetCurrentWeapon();

        if (currentWeapon == null)
        {
            Debug.LogWarning(
                $"[PLAYER ITEM AMMO REFRESH] current weapon is null. " +
                $"currentIndex={currentIndex}"
            );

            return;
        }

        NetworkManager.Instance.SendWeaponSwitch(
            currentWeapon.GetWeaponId()
        );

        Debug.Log(
            $"[PLAYER ITEM AMMO REFRESH] requested. " +
            $"weaponId={currentWeapon.GetWeaponId()}"
        );
    }

    private void TryStartReload()
    {
        if (isReloading)
            return;

        if (currentIndex < 0 ||
            currentIndex >= items.Length)
        {
            return;
        }

        Item currentItem =
            items[currentIndex];

        if (currentItem == null)
            return;

        Weapon currentWeapon =
            currentItem.GetComponent<Weapon>();

        if (currentWeapon == null)
            return;

        // 칼은 장전 불가능
        if (currentWeapon.GetWeaponType() ==
            WeaponData.WeaponType.Knife)
        {
            return;
        }

        if (!currentWeapon.CanReloadNow())
        {
            Debug.Log(
                $"[RELOAD BLOCK] cannot reload. " +
                $"weaponId={currentWeapon.GetWeaponId()}"
            );

            return;
        }

        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError(
                "[RELOAD] WeaponDropSpawner.Instance is null"
            );

            return;
        }

        int weaponId =
            currentWeapon.GetWeaponId();

        WeaponData weaponData =
            WeaponDropSpawner.Instance.GetWeaponData(
                weaponId
            );

        if (weaponData == null)
        {
            Debug.LogError(
                $"[RELOAD] WeaponData not found. " +
                $"weaponId={weaponId}"
            );

            return;
        }

        if (weaponData.reloadTime <= 0f)
        {
            Debug.LogWarning(
                $"[RELOAD] invalid reloadTime. " +
                $"weaponId={weaponId}, " +
                $"reloadTime={weaponData.reloadTime}"
            );

            return;
        }

        // 장전 시작 시 줌 해제
        FPSCameraLook cameraLook =
            GetComponentInChildren<FPSCameraLook>(true);

        if (cameraLook != null)
        {
            cameraLook.ForceExitADS();
        }

        reloadCoroutine =
            StartCoroutine(
                ReloadRoutine(
                    currentItem,
                    weaponId,
                    currentIndex,
                    weaponData.reloadTime
                )
            );
    }

    private IEnumerator ReloadRoutine(
        Item reloadItem,
        int reloadWeaponId,
        int reloadSlot,
        float reloadTime)
    {
        isReloading = true;
        reloadingWeaponId = reloadWeaponId;
        reloadingWeaponObject =
            reloadItem != null
                ? reloadItem.gameObject
                : null;

        // 기존처럼 총기를 숨기지 않고 장전 애니메이션을 재생한다.
        PlayReloadAnimation(reloadItem, reloadTime);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendReloadVisual(
                reloadWeaponId,
                true
            );
        }

        Debug.Log(
            $"[RELOAD START] weaponId={reloadWeaponId}, " +
            $"reloadTime={reloadTime}"
        );

        yield return new WaitForSeconds(reloadTime);

        if (!isReloading)
            yield break;

        bool sameWeapon =
            reloadSlot >= 0 &&
            reloadSlot < items.Length &&
            currentIndex == reloadSlot &&
            items[reloadSlot] != null &&
            items[reloadSlot] == reloadItem;

        if (!sameWeapon)
        {
            FinishReloadState(false);
            yield break;
        }

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendReload();

        Debug.Log(
            $"[RELOAD COMPLETE] weaponId={reloadWeaponId}"
        );

        FinishReloadState(false);
    }

    private void FinishReloadState(bool restoreWeapon)
    {
        if (reloadingWeaponObject != null)
        {
            StopReloadAnimation(reloadingWeaponObject);

            // 예전 장전 구현에서 비활성화된 상태가 남아 있을 때만 복구한다.
            if (restoreWeapon && !reloadingWeaponObject.activeSelf)
                reloadingWeaponObject.SetActive(true);
        }

        if (reloadingWeaponId >= 0 &&
            NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendReloadVisual(
                reloadingWeaponId,
                false
            );
        }

        isReloading = false;
        reloadCoroutine = null;
        reloadingWeaponObject = null;
        reloadingWeaponId = -1;
    }

    public void CancelReload(bool restoreWeapon)
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        if (isReloading || reloadingWeaponId >= 0)
        {
            FinishReloadState(restoreWeapon);
        }
        else
        {
            if (reloadingWeaponObject != null)
                StopReloadAnimation(reloadingWeaponObject);

            isReloading = false;
            reloadingWeaponObject = null;
            reloadingWeaponId = -1;
        }

        Debug.Log("[RELOAD CANCEL]");
    }

    public void SetReloadVisualFromServer(
        int weaponId,
        bool isReloadingValue)
    {
        int slot = FindSlotByWeaponId(weaponId);

        if (slot == -1)
        {
            Debug.LogWarning(
                $"[REMOTE RELOAD] weapon not found. " +
                $"weaponId={weaponId}"
            );

            return;
        }

        remoteIsReloading = isReloadingValue;
        remoteReloadWeaponId =
            isReloadingValue
                ? weaponId
                : -1;

        // 1인칭 장전 애니메이션은 현재 이 플레이어를 관전 중일 때만 표시한다.
        if (!IsCurrentlySpectated())
            return;

        if (slot != currentIndex || items[slot] == null)
            return;

        PlayerDeathController death =
            GetComponent<PlayerDeathController>();

        if (death == null)
        {
            death =
                GetComponentInChildren<PlayerDeathController>(true);
        }

        if (death != null && death.IsDead)
            return;

        Item reloadItem = items[slot];

        // 장전 중 총기를 사라지게 하지 않는다.
        if (!reloadItem.gameObject.activeSelf)
            reloadItem.gameObject.SetActive(true);

        if (isReloadingValue)
        {
            PlayReloadAnimation(reloadItem);
        }
        else
        {
            StopReloadAnimation(reloadItem);
        }

        Debug.Log(
            $"[REMOTE RELOAD] " +
            $"playerId={_playerIdentity?.PlayerId}, " +
            $"weaponId={weaponId}, " +
            $"isReloading={isReloadingValue}"
        );
    }

    public void RefreshRemoteReloadAnimation()
    {
        if (!IsCurrentlySpectated())
            return;

        if (currentIndex < 0 ||
            currentIndex >= items.Length)
        {
            return;
        }

        Item currentItem = items[currentIndex];

        if (currentItem == null)
            return;

        // 관전 카메라 전환이나 상태 갱신 때문에 숨겨졌다면 다시 표시한다.
        if (!currentItem.gameObject.activeSelf)
            currentItem.gameObject.SetActive(true);

        Weapon currentWeapon =
            currentItem.GetComponent<Weapon>();

        if (currentWeapon == null)
        {
            currentWeapon =
                currentItem.GetComponentInChildren<Weapon>(true);
        }

        bool shouldPlayReload =
            remoteIsReloading &&
            remoteReloadWeaponId >= 0 &&
            currentWeapon != null &&
            currentWeapon.GetWeaponId() ==
                remoteReloadWeaponId;

        if (shouldPlayReload)
        {
            PlayReloadAnimation(currentItem);
        }
        else
        {
            StopReloadAnimation(currentItem);
        }
    }

    // 기존 호출부와의 호환성을 유지한다.
    public void RefreshRemoteReloadVisual()
    {
        RefreshRemoteReloadAnimation();
    }

    private void PlayReloadAnimation(
        Item item,
        float reloadTime = -1f)
    {
        if (item == null)
            return;

        ARMotion motion =
            item.GetComponent<ARMotion>();

        if (motion == null)
        {
            motion =
                item.GetComponentInChildren<ARMotion>(true);
        }

        if (motion == null)
        {
            Debug.LogWarning(
                $"[RELOAD ANIMATION] ARMotion not found. " +
                $"item={item.name}"
            );

            return;
        }

        if (reloadTime <= 0f)
        {
            Weapon weapon =
                item.GetComponent<Weapon>();

            if (weapon == null)
            {
                weapon =
                    item.GetComponentInChildren<Weapon>(true);
            }

            if (weapon != null)
                reloadTime = weapon.GetWeaponReLoadTime();
        }

        if (reloadTime <= 0f)
        {
            Debug.LogWarning(
                $"[RELOAD ANIMATION] invalid reloadTime. " +
                $"item={item.name}, reloadTime={reloadTime}"
            );

            return;
        }

        motion.PlayReload(reloadTime);
    }

    private void StopReloadAnimation(Item item)
    {
        if (item == null)
            return;

        StopReloadAnimation(item.gameObject);
    }

    private void StopReloadAnimation(
        GameObject weaponObject)
    {
        if (weaponObject == null)
            return;

        ARMotion motion =
            weaponObject.GetComponent<ARMotion>();

        if (motion == null)
        {
            motion =
                weaponObject.GetComponentInChildren<ARMotion>(true);
        }

        motion?.StopReload();
    }

    private bool IsCurrentlySpectated()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        if (_playerIdentity == null || SpectatorManager.Instance == null)
            return false;

        return SpectatorManager.Instance.IsSpectating &&
               SpectatorManager.Instance.CurrentTargetPlayerId == _playerIdentity.PlayerId;
    }

    // KSW : 에디터 무기 변경 테스트용 추가
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || items == null) return;
        if (currentIndex >= 0 && currentIndex < items.Length && items[currentIndex] != null)
        {
            int targetIndex = currentIndex;
            currentIndex = -2;
            Switch(targetIndex);
            Debug.Log($"[EDITOR DEBUG] Inspector forced switch to Slot {targetIndex}");
        }
    }

#endif
}