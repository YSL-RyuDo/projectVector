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
    [SerializeField] private Transform itemHolder;   // 1인칭 무기 홀더 (카메라 자식)
    [SerializeField] private Transform tpItemHolder; // 3인칭 무기 홀더 (3인칭 캐릭터 손 Bone)
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private LayerMask itemLayer;

    [Header("Skin")]
    [SerializeField] private WeaponSkinDatabase skinDatabase;
    private PlayerSkinLoadout skinLoadout;

    private Item[] items = new Item[5]; // 0~4 (5번 = Spike) - 1인칭
    private GameObject[] tpWeaponObjects = new GameObject[5]; // 3인칭 무기 오브젝트 저장 배열
    private PlayerSpike playerSpike;
    [SerializeField] private int currentIndex = -1;

    private PlayerIdentity _playerIdentity;
    private bool defaultItemsInitialized;

    private bool isReloading;
    private Coroutine reloadCoroutine;
    private GameObject reloadingWeaponObject;

    public bool IsReloading => isReloading;

    public Item[] Items => items;
    public int CurrentIndex => currentIndex;

    private int reloadingWeaponId = -1;

    private bool remoteIsReloading;
    private int remoteReloadWeaponId = -1;

    void Awake()
    {
        if (skinDatabase != null)
            skinDatabase.Register();

        skinLoadout = GetComponent<PlayerSkinLoadout>();

        if (skinLoadout == null)
            Debug.LogWarning("[PLAYER ITEM] PlayerSkinLoadout not found. skins will not apply.");
    }

    private void Start()
    {
        InitializeForMatch();
    }

    public void InitializeForMatch()
    {
        if (playerSpike == null)
            playerSpike = GetComponent<PlayerSpike>();

        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        if (defaultItemsInitialized)
            return;

        defaultItemsInitialized = true;
        InitializeDefaultItems();

        currentIndex = -1;

        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

    void Update()
    {
        var practice = PracticePlayerContext.For(this);
        if (practice != null && practice.range.MenuOpen) return;
        if (VectorVoicePanel.BlocksGameplayInput || InGameChatUI.BlocksGameplayInput)
            return;

        if (!gameObject.CompareTag("Player"))
            return;

        if (GameController.Instance != null && GameController.Instance.IsPaused)
            return;

        if (_playerIdentity == null || (practice == null && NetworkManager.Instance == null))
            return;

        if (practice == null && _playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        if (Keyboard.current == null)
            return;

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

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryStartReload();
            return;
        }

        if (isReloading)
            return;

        if (practice != null) return; // Practice weapon changes use the authored armory, not server drop requests.
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropCurrentItem();
            Debug.Log("g");
        }
    }

    private void InitializeDefaultItems()
    {
        // 중복 생성을 방지하기 위해 기존 tpItemHolder의 자식들을 한번 비워줍니다.
        ClearAllTpWeaponObjects();

        items[0] = null;

        if (secondary != null)
            CreateStartingItem(secondary, 1);
        else
            items[1] = null;

        if (melee != null)
            CreateStartingItem(melee, 2);
        else
            items[2] = null;

        items[3] = null;

        if (spike != null)
        {
            GameObject obj = Instantiate(spike, itemHolder);
            obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
            obj.transform.localRotation = Quaternion.identity;

            Item item = obj.GetComponent<Item>();
            items[4] = item;
            obj.SetActive(false);

            if (tpItemHolder != null)
            {
                GameObject tpObj = Instantiate(spike, tpItemHolder);
                tpObj.transform.localPosition = Vector3.zero;
                tpObj.transform.localRotation = Quaternion.identity;
                DisableHeldWeaponPhysics(tpObj);
                tpWeaponObjects[4] = tpObj;
                tpObj.SetActive(false);
            }
        }
        else
        {
            items[4] = null;
        }
    }

    private void ClearAllTpWeaponObjects()
    {
        for (int i = 0; i < tpWeaponObjects.Length; i++)
        {
            if (tpWeaponObjects[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(tpWeaponObjects[i]);
                else
                    DestroyImmediate(tpWeaponObjects[i]);

                tpWeaponObjects[i] = null;
            }
        }
    }

    private void CreateStartingItem(WeaponData data, int slot)
    {
        GameObject obj = SpawnWeaponObject(data, data.magSize, data.maxReserveAmmo, null, slot);

        Item item = obj.GetComponent<Item>();
        items[slot] = item;

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
        if (currentIndex < 0 || currentIndex >= items.Length)
            return;

        if (items[currentIndex] == null)
            return;

        if (currentIndex == 4)
        {
            if (playerSpike == null)
                playerSpike = GetComponent<PlayerSpike>();

            if (playerSpike == null)
                return;

            if (!playerSpike.hasSpike)
                return;

            playerSpike.RequestDrop();
            return;
        }

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();

        if (weapon == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        Vector3 dropPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;

        Vector3 dropDirection = dropPoint != null
            ? dropPoint.forward
            : transform.forward;

        NetworkManager.Instance.SendWeaponDropRequest(
            weapon.GetWeaponId(),
            dropPosition,
            dropDirection
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
        {
            items[index].OnEquip();
            return;
        }

        if (index == 4 && playerSpike != null && !playerSpike.hasSpike)
        {
            Debug.Log("스파이크가 없어서 5번 슬롯으로 전환할 수 없습니다.");
            return;
        }

        if (isReloading)
        {
            int canceledWeaponId = reloadingWeaponId;
            CancelReload(false);
        }

        if (currentIndex >= 0 && currentIndex < items.Length)
        {
            items[currentIndex]?.OnUnequip();
        }

        currentIndex = index;

        // 1인칭 및 3인칭 무기들의 활성화 상태를 한 번에 동기화
        SwitchWeaponVisualOnlyBySlot(currentIndex);

        items[currentIndex]?.OnEquip();

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();

        if (weapon != null && playerMovement != null)
        {
            playerMovement.SetWeaponWeight(weapon.GetWeight());
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
            case WeaponData.WeaponType.Knife:
                return 2;
            default:
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
        if (tpWeaponObjects[currentIndex] != null)
            tpWeaponObjects[currentIndex].SetActive(visibility);
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

        // 현재 슬롯 갱신
        currentIndex = slot;

        // 1인칭 무기
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].gameObject.SetActive(i == slot);
        }

        // 기존 tpWeaponObjects는 스파이크 등 레거시용
        for (int i = 0; i < tpWeaponObjects.Length; i++)
        {
            if (tpWeaponObjects[i] != null)
                tpWeaponObjects[i].SetActive(i == slot);
        }

        // ★ 실제 3인칭 WeaponHolder 갱신
        RefreshWeaponHolderModel();
    }

    private void RefreshWeaponHolderModel()
    {
        WeaponHolderModel holder =
            GetComponentInChildren<WeaponHolderModel>(true);

        if (holder == null)
            return;

        holder.RefreshNow();
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

            if (tpWeaponObjects[i] != null)
            {
                Destroy(tpWeaponObjects[i]);
                tpWeaponObjects[i] = null;
            }
        }

        currentIndex = -1;
        items[0] = null;

        if (secondary != null)
            CreateStartingItem(secondary, 1);

        if (melee != null)
            CreateStartingItem(melee, 2);

        items[3] = null;

        if (spike != null)
        {
            GameObject obj = Instantiate(spike, itemHolder);
            obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
            obj.transform.localRotation = Quaternion.identity;

            Item item = obj.GetComponent<Item>();
            items[4] = item;
            obj.SetActive(false);

            if (tpItemHolder != null)
            {
                GameObject tpObj = Instantiate(spike, tpItemHolder);
                tpObj.transform.localPosition = Vector3.zero;
                tpObj.transform.localRotation = Quaternion.identity;
                DisableHeldWeaponPhysics(tpObj);
                tpWeaponObjects[4] = tpObj;
                tpObj.SetActive(false);
            }
        }

        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

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
            return;

        if (items[0] != null)
        {
            items[0].OnDrop();
            Destroy(items[0].gameObject);
            items[0] = null;
        }

        if (tpWeaponObjects[0] != null)
        {
            Destroy(tpWeaponObjects[0]);
            tpWeaponObjects[0] = null;
        }

        GameObject obj = SpawnWeaponObject(weaponData, weaponData.magSize, weaponData.maxReserveAmmo, null, 0);

        Item item = obj.GetComponent<Item>();
        items[0] = item;

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

    public void SetWeaponAmmoFromServer(int weaponId, int magAmmo, int reserveAmmo)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            Weapon weapon = items[i].GetComponent<Weapon>();

            if (weapon == null)
                continue;

            if (weapon.GetWeaponId() != weaponId)
                continue;

            weapon.SetAmmo(magAmmo, reserveAmmo);
            return;
        }
    }

    public void PlayFireVisualFromServer(int weaponId)
    {
        Weapon currentWeapon = GetCurrentWeapon();

        if (currentWeapon == null)
            return;

        if (currentWeapon.GetWeaponId() != weaponId)
            return;

        currentWeapon.PlayFireVisualFromServer();
    }

    public void PlayKnifeAttackVisualFromServer()
    {
        if (currentIndex < 0 || currentIndex >= items.Length)
            return;

        Item currentItem = items[currentIndex];

        if (currentItem == null)
            return;

        Weapon currentWeapon = currentItem.GetComponent<Weapon>() ?? currentItem.GetComponentInChildren<Weapon>(true);

        if (currentWeapon == null || currentWeapon.GetWeaponType() != WeaponData.WeaponType.Knife)
            return;

        KnifeMotion knifeMotion = currentItem.GetComponent<KnifeMotion>() ?? currentItem.GetComponentInChildren<KnifeMotion>(true);

        if (knifeMotion != null)
            knifeMotion.PlayAttackFromServer();
    }

    public void SetSpikeItemActive(bool value)
    {
        if (items[4] == null)
            return;

        if (!value && currentIndex == 4)
            Switch(1);

        items[4].gameObject.SetActive(false);
        if (tpWeaponObjects[4] != null)
            tpWeaponObjects[4].SetActive(false);
    }

    public void GiveSpike(GameObject spikePrefab)
    {
        if (items[4] != null)
            Destroy(items[4].gameObject);

        if (tpWeaponObjects[4] != null)
            Destroy(tpWeaponObjects[4]);

        GameObject obj = Instantiate(spikePrefab, itemHolder);
        obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        items[4] = item;
        obj.SetActive(false);

        if (tpItemHolder != null)
        {
            GameObject tpObj = Instantiate(spikePrefab, tpItemHolder);
            tpObj.transform.localPosition = Vector3.zero;
            tpObj.transform.localRotation = Quaternion.identity;
            DisableHeldWeaponPhysics(tpObj);
            tpWeaponObjects[4] = tpObj;
            tpObj.SetActive(false);
        }
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

        if (tpWeaponObjects[4] != null)
        {
            Destroy(tpWeaponObjects[4]);
            tpWeaponObjects[4] = null;
        }
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

        if (currentIndex >= 0 && currentIndex < items.Length)
        {
            if (items[currentIndex] != null)
                items[currentIndex].gameObject.SetActive(true);
            if (tpWeaponObjects[currentIndex] != null)
                tpWeaponObjects[currentIndex].SetActive(true);
        }
    }

    public void ApplyWeaponSwitchFromServer(int weaponId)
    {
        SwitchWeaponVisualOnly(weaponId);
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

            if (tpWeaponObjects[i] != null)
                tpWeaponObjects[i].SetActive(active);

            if (active)
                foundSlot = i;
        }

        if (foundSlot != -1)
        {
            currentIndex = foundSlot;
            RefreshWeaponHolderModel();
        }
    }

    public void SetADSVisualFromServer(bool isADS)
    {
        Weapon currentWeapon = GetCurrentWeapon();

        if (currentWeapon == null)
            return;

        WeaponADS weaponADS = currentWeapon.GetComponent<WeaponADS>();
    }

    public void RemoveWeaponFromServer(int weaponId)
    {
        int slot = FindSlotByWeaponId(weaponId);

        if (slot == -1)
            return;

        bool wasCurrent = currentIndex == slot;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        if (tpWeaponObjects[slot] != null)
        {
            Destroy(tpWeaponObjects[slot]);
            tpWeaponObjects[slot] = null;
        }

        if (wasCurrent)
        {
            currentIndex = -1;

            int fallbackSlot = FindFirstAvailableWeaponSlotExceptSpike();

            if (fallbackSlot != -1)
                SwitchWeaponVisualOnlyBySlot(fallbackSlot);
        }
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

    public void ApplyWeaponPickupFromServer(int weaponId, int magAmmo, int reserveAmmo)
    {
        if (WeaponDropSpawner.Instance == null)
            return;

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);

        if (data == null)
            return;

        int slot = GetSlotIndex(data);

        if (slot < 0 || slot >= items.Length)
            return;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        if (tpWeaponObjects[slot] != null)
        {
            Destroy(tpWeaponObjects[slot]);
            tpWeaponObjects[slot] = null;
        }

        GameObject obj = SpawnWeaponObject(data, magAmmo, reserveAmmo, null, slot);

        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();

        if (item == null)
        {
            Destroy(obj);
            return;
        }

        items[slot] = item;

        obj.SetActive(false);

        SwitchWeaponVisualOnlyBySlot(slot);

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.SetBulletText(magAmmo, reserveAmmo);
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
        int primarySkinId,
        int pistolWeaponId,
        int pistolMagAmmo,
        int pistolReserveAmmo,
        int pistolSkinId,
        int knifeWeaponId,
        int knifeMagAmmo,
        int knifeReserveAmmo,
        int knifeSkinId)
    {
        CancelReload(false);

        remoteIsReloading = false;
        remoteReloadWeaponId = -1;

        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        ClearWeaponSlotsForRoundReset();

        if (primaryWeaponId >= 0)
            CreateWeaponFromServerForRoundReset(primaryWeaponId, primaryMagAmmo, primaryReserveAmmo, primarySkinId);

        if (pistolWeaponId >= 0)
            CreateWeaponFromServerForRoundReset(pistolWeaponId, pistolMagAmmo, pistolReserveAmmo, pistolSkinId);

        if (knifeWeaponId >= 0)
            CreateWeaponFromServerForRoundReset(knifeWeaponId, knifeMagAmmo, knifeReserveAmmo, knifeSkinId);

        int currentSlot = FindSlotByWeaponId(currentWeaponId);

        bool isLocal = _playerIdentity != null &&
                      NetworkManager.Instance != null &&
                      _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (currentSlot != -1)
        {
            if (isLocal)
                Switch(currentSlot);
            else
                SwitchWeaponVisualOnlyBySlot(currentSlot);
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
            primaryWeaponId, primaryMagAmmo, primaryReserveAmmo,
            pistolWeaponId, pistolMagAmmo, pistolReserveAmmo,
            knifeWeaponId, knifeMagAmmo, knifeReserveAmmo
        );

        RefreshWeaponHolderModel();
    }

    private void ClearWeaponSlotsForRoundReset()
    {
        for (int i = 0; i <= 3; i++)
        {
            if (items[i] != null)
            {
                Destroy(items[i].gameObject);
                items[i] = null;
            }

            if (tpWeaponObjects[i] != null)
            {
                Destroy(tpWeaponObjects[i]);
                tpWeaponObjects[i] = null;
            }
        }

        currentIndex = -1;

        bool isLocal = _playerIdentity != null &&
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

    private void CreateWeaponFromServerForRoundReset(int weaponId, int magAmmo, int reserveAmmo, int skinId)
    {
        if (WeaponDropSpawner.Instance == null)
            return;

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);

        if (data == null)
            return;

        int slot = GetSlotIndex(data);

        if (slot < 0 || slot >= items.Length || slot == 4)
            return;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        if (tpWeaponObjects[slot] != null)
        {
            Destroy(tpWeaponObjects[slot]);
            tpWeaponObjects[slot] = null;
        }

        GameObject obj = SpawnWeaponObject(data, magAmmo, reserveAmmo, skinId, slot);

        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();

        if (item == null)
        {
            Destroy(obj);
            return;
        }

        items[slot] = item;

        obj.SetActive(false);

        bool isLocal = _playerIdentity != null &&
                      NetworkManager.Instance != null &&
                      _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isLocal && InGameUIManager.Instance != null)
        {
            if (slot == 0)
                InGameUIManager.Instance.SetMainWeapon(data);
            else if (slot == 1)
                InGameUIManager.Instance.SetSubWeapon(data.weaponIcon);
            else if (slot == 2)
                InGameUIManager.Instance.SetKnife(data.weaponIcon);
        }
    }

    private void UpdateBulletTextFromRoundResetData(
        int currentWeaponId,
        int primaryWeaponId, int primaryMagAmmo, int primaryReserveAmmo,
        int pistolWeaponId, int pistolMagAmmo, int pistolReserveAmmo,
        int knifeWeaponId, int knifeMagAmmo, int knifeReserveAmmo)
    {
        bool isLocal = _playerIdentity != null &&
                      NetworkManager.Instance != null &&
                      _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (!isLocal || InGameUIManager.Instance == null)
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

    public void ApplyPickedWeaponFromServer(WeaponData weaponData, int magAmmo, int reserveAmmo, int skinId = 0)
    {
        if (weaponData == null)
            return;

        EquipWeaponByServerData(weaponData, magAmmo, reserveAmmo, skinId);
    }

    private void EquipWeaponByServerData(WeaponData weaponData, int magAmmo, int reserveAmmo, int skinId = 0)
    {
        int slot = GetSlotIndex(weaponData);

        if (slot < 0 || slot >= items.Length || slot == 4)
            return;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        if (tpWeaponObjects[slot] != null)
        {
            Destroy(tpWeaponObjects[slot]);
            tpWeaponObjects[slot] = null;
        }

        GameObject obj = SpawnWeaponObject(weaponData, magAmmo, reserveAmmo, skinId, slot);

        DisableHeldWeaponPhysics(obj);

        Item item = obj.GetComponent<Item>();
        if (item == null)
        {
            Destroy(obj);
            return;
        }

        items[slot] = item;

        obj.SetActive(false);

        RefreshWeaponHolderModel();

        bool isLocal = _playerIdentity != null &&
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
    }

    public void RefreshCurrentWeaponAmmoFromServer()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        if (_playerIdentity == null || NetworkManager.Instance == null)
            return;

        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        Weapon currentWeapon = GetCurrentWeapon();

        if (currentWeapon == null)
            return;

        NetworkManager.Instance.SendWeaponSwitch(currentWeapon.GetWeaponId());
    }

    private void TryStartReload()
    {
        if (isReloading)
            return;

        if (currentIndex < 0 || currentIndex >= items.Length)
            return;

        Item currentItem = items[currentIndex];

        if (currentItem == null)
            return;

        Weapon currentWeapon = currentItem.GetComponent<Weapon>();

        if (currentWeapon == null)
            return;

        if (currentWeapon.GetWeaponType() == WeaponData.WeaponType.Knife)
            return;

        if (!currentWeapon.CanReloadNow())
            return;

        int weaponId = currentWeapon.GetWeaponId();

        WeaponData weaponData = PracticePlayerContext.For(this) != null ? currentWeapon.Data : WeaponDropSpawner.Instance?.GetWeaponData(weaponId);

        if (weaponData == null || weaponData.reloadTime <= 0f)
            return;

        FPSCameraLook cameraLook = GetComponentInChildren<FPSCameraLook>(true);

        if (cameraLook != null)
            cameraLook.ForceExitADS();

        reloadCoroutine = StartCoroutine(
            ReloadRoutine(currentItem, weaponId, currentIndex, weaponData.reloadTime)
        );
    }

    private IEnumerator ReloadRoutine(Item reloadItem, int reloadWeaponId, int reloadSlot, float reloadTime)
    {
        isReloading = true;
        reloadingWeaponId = reloadWeaponId;
        reloadingWeaponObject = reloadItem != null ? reloadItem.gameObject : null;

        PlayReloadAnimation(reloadItem, reloadTime);

        if (PracticePlayerContext.For(this) == null && NetworkManager.Instance != null)
            NetworkManager.Instance.SendReloadVisual(reloadWeaponId, true);

        yield return new WaitForSeconds(reloadTime);

        if (!isReloading)
            yield break;

        bool sameWeapon = reloadSlot >= 0 &&
                          reloadSlot < items.Length &&
                          currentIndex == reloadSlot &&
                          items[reloadSlot] != null &&
                          items[reloadSlot] == reloadItem;

        if (!sameWeapon)
        {
            FinishReloadState(false);
            yield break;
        }

        if (PracticePlayerContext.For(this) != null)
        {
            var trainingWeapon=reloadItem.GetComponent<Weapon>();
            if(trainingWeapon!=null)
            {
                // Real reload transfers reserve into the magazine; F5 is explicit resupply.
                int transfer=Mathf.Min(trainingWeapon.ReserveAmmo,Mathf.Max(0,trainingWeapon.MagazineCapacity-trainingWeapon.CurrentAmmo));
                trainingWeapon.SetAmmo(trainingWeapon.CurrentAmmo+transfer,trainingWeapon.ReserveAmmo-transfer);
            }
        }
        else if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendReload();

        FinishReloadState(false);
    }

    private void FinishReloadState(bool restoreWeapon)
    {
        if (reloadingWeaponObject != null)
        {
            StopReloadAnimation(reloadingWeaponObject);

            if (restoreWeapon && !reloadingWeaponObject.activeSelf)
                reloadingWeaponObject.SetActive(true);
        }

        if (PracticePlayerContext.For(this) == null && reloadingWeaponId >= 0 && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendReloadVisual(reloadingWeaponId, false);
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
    }

    public void SetReloadVisualFromServer(int weaponId, bool isReloadingValue)
    {
        int slot = FindSlotByWeaponId(weaponId);

        if (slot == -1)
            return;

        remoteIsReloading = isReloadingValue;
        remoteReloadWeaponId = isReloadingValue ? weaponId : -1;

        if (!IsCurrentlySpectated())
            return;

        if (slot != currentIndex || items[slot] == null)
            return;

        PlayerDeathController death = GetComponent<PlayerDeathController>() ?? GetComponentInChildren<PlayerDeathController>(true);

        if (death != null && death.IsDead)
            return;

        Item reloadItem = items[slot];

        if (!reloadItem.gameObject.activeSelf)
            reloadItem.gameObject.SetActive(true);

        if (isReloadingValue)
            PlayReloadAnimation(reloadItem);
        else
            StopReloadAnimation(reloadItem);
    }

    public void RefreshRemoteReloadAnimation()
    {
        if (!IsCurrentlySpectated())
            return;

        if (currentIndex < 0 || currentIndex >= items.Length)
            return;

        Item currentItem = items[currentIndex];

        if (currentItem == null)
            return;

        if (!currentItem.gameObject.activeSelf)
            currentItem.gameObject.SetActive(true);

        Weapon currentWeapon = currentItem.GetComponent<Weapon>() ?? currentItem.GetComponentInChildren<Weapon>(true);

        bool shouldPlayReload = remoteIsReloading &&
                                remoteReloadWeaponId >= 0 &&
                                currentWeapon != null &&
                                currentWeapon.GetWeaponId() == remoteReloadWeaponId;

        if (shouldPlayReload)
            PlayReloadAnimation(currentItem);
        else
            StopReloadAnimation(currentItem);
    }

    public void RefreshRemoteReloadVisual()
    {
        RefreshRemoteReloadAnimation();
    }

    private void PlayReloadAnimation(Item item, float reloadTime = -1f)
    {
        if (item == null)
            return;

        ARMotion motion = item.GetComponent<ARMotion>() ?? item.GetComponentInChildren<ARMotion>(true);

        if (motion == null)
            return;

        if (reloadTime <= 0f)
        {
            Weapon weapon = item.GetComponent<Weapon>() ?? item.GetComponentInChildren<Weapon>(true);
            if (weapon != null)
                reloadTime = weapon.GetWeaponReLoadTime();
        }

        if (reloadTime <= 0f)
            return;

        motion.PlayReload(reloadTime);
    }

    private void StopReloadAnimation(Item item)
    {
        if (item == null)
            return;

        StopReloadAnimation(item.gameObject);
    }

    private void StopReloadAnimation(GameObject weaponObject)
    {
        if (weaponObject == null)
            return;

        ARMotion motion = weaponObject.GetComponent<ARMotion>() ?? weaponObject.GetComponentInChildren<ARMotion>(true);
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

    public bool IsLookingAtPickupItem()
    {
        return GetLookedDroppedItem() != null;
    }

    public bool TryPickupLookedItem()
    {
        DroppedItem target = GetLookedDroppedItem();

        if (target == null)
            return false;

        target.Pickup(this);
        return true;
    }

    private DroppedItem GetLookedDroppedItem()
    {
        Camera interactionCamera = GetInteractionCamera();

        if (interactionCamera == null)
            return null;

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer, QueryTriggerInteraction.Ignore))
            return null;

        DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>() ?? hit.collider.GetComponentInParent<DroppedItem>();

        return droppedItem;
    }

    private Camera GetInteractionCamera()
    {
        PlayerCameraController cameraController = GetComponent<PlayerCameraController>() ?? GetComponentInChildren<PlayerCameraController>(true);

        if (cameraController != null && cameraController.PlayerCamera != null)
            return cameraController.PlayerCamera;

        return Camera.main;
    }

    private GameObject SpawnWeaponObject(WeaponData data, int magAmmo, int reserveAmmo, int? forcedSkinId = null, int slot = -1)
    {
        // 1인칭 무기만 생성
        GameObject fpObj =
            Instantiate(data.weaponPrefab, itemHolder);

        fpObj.transform.localPosition = Vector3.zero;
        fpObj.transform.localRotation = Quaternion.identity;

        Weapon weapon =
            fpObj.GetComponent<Weapon>();

        if (weapon != null)
        {
            weapon.Initialize(
                data,
                dropPoint,
                magAmmo,
                reserveAmmo
            );

            int skinId =
                forcedSkinId ??
                (skinLoadout != null
                    ? skinLoadout.GetSkinId(data.weaponId)
                    : 0);

            WeaponSkinDatabase database =
                skinDatabase != null
                    ? skinDatabase
                    : WeaponSkinDatabase.Instance;

            WeaponSkinData skin =
                (skinId > 0 && database != null)
                    ? database.GetById(
                        data.weaponId,
                        skinId
                    )
                    : null;

            weapon.ApplySkin(skin, data);
        }

        return fpObj;
    }

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
