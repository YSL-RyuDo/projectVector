using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TESTPlayerItem : MonoBehaviour
{

    //튜토리얼용 추가
    [Header("Tutorial Weapon")]
    [SerializeField] private GameObject tutorialP92Prefab;

    [Header("References")]
    [SerializeField] private WeaponData primary;
    [SerializeField] private WeaponData secondary;
    [SerializeField] private WeaponData melee;
    [SerializeField] private WeaponData thrown;
    [SerializeField] private GameObject spike;

    [Header("Others")]
    [SerializeField] private Transform itemHolder;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private LayerMask itemLayer;

    private Item[] items = new Item[5];
    private PlayerSpike playerSpike;
    [SerializeField] private int currentIndex = -1;

    private bool isReloading;
    private Coroutine reloadCoroutine;
    private GameObject reloadingWeaponObject;
    private int reloadingWeaponId = -1;

    public bool IsReloading => isReloading;
    public Item[] Items => items;
    public int CurrentIndex => currentIndex;

    // PlayerMovement → TESTPlayerMovement로 변경
    private TESTPlayerMovement playerMovement;

    private void Start()
    {
        playerSpike = GetComponent<PlayerSpike>();
        playerMovement = GetComponent<TESTPlayerMovement>();

        InitializeDefaultItems();

        currentIndex = -1;

        if (items[1] != null)
            Switch(1);
        else if (items[2] != null)
            Switch(2);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) { Switch(0); return; }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) { Switch(1); return; }
        if (Keyboard.current.digit3Key.wasPressedThisFrame) { Switch(2); return; }
        if (Keyboard.current.digit4Key.wasPressedThisFrame) { Switch(3); return; }
        if (Keyboard.current.digit5Key.wasPressedThisFrame) { Switch(4); return; }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryStartReload();
            return;
        }

        if (isReloading) return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropCurrentItem();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    private void InitializeDefaultItems()
    {
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
        }
        else
        {
            items[4] = null;
        }
    }

    private void CreateStartingItem(WeaponData data, int slot)
    {
        GameObject obj = Instantiate(data.weaponPrefab, itemHolder);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        items[slot] = item;

        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
            weapon.Initialize(data, dropPoint, data.magSize, data.maxReserveAmmo);

        obj.SetActive(false);
    }

    void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, itemLayer);

        DroppedItem nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            DroppedItem di = hit.GetComponent<DroppedItem>();
            if (di == null) di = hit.GetComponentInParent<DroppedItem>();
            if (di == null) continue;

            float dist = Vector3.Distance(transform.position, di.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = di;
            }
        }

        if (nearest != null)
            nearest.PickupLocal(this);
    }

    private void DropCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Length) return;
        if (items[currentIndex] == null) return;

        // 스파이크 슬롯
        if (currentIndex == 4)
        {
            if (playerSpike == null) playerSpike = GetComponent<PlayerSpike>();
            if (playerSpike == null || !playerSpike.hasSpike) return;

            playerSpike.RequestDrop();
            return;
        }

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();
        if (weapon == null) return;

        // 로컬 테스트: 드롭 위치 계산 후 바로 처리
        Vector3 dropPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;

        Vector3 dropDirection = dropPoint != null ? dropPoint.forward : transform.forward;

        Debug.Log($"[TEST DROP] weaponId={weapon.GetWeaponId()}, pos={dropPosition}");

        // 슬롯에서 제거
        Destroy(items[currentIndex].gameObject);
        items[currentIndex] = null;

        int fallback = FindFirstAvailableWeaponSlotExceptSpike();
        if (fallback != -1)
            Switch(fallback);
        else
            currentIndex = -1;
    }

    void Switch(int index)
    {
        if (index < 0 || index >= items.Length) return;
        if (items[index] == null)
        {
            Debug.Log($"[TEST ITEM] slot empty. index={index}");
            return;
        }
        if (currentIndex == index) return;

        if (index == 4 && playerSpike != null && !playerSpike.hasSpike)
        {
            Debug.Log("스파이크가 없어서 5번 슬롯으로 전환할 수 없습니다.");
            return;
        }

        if (isReloading)
        {
            int canceledWeaponId = reloadingWeaponId;
            CancelReload(false);
            Debug.Log($"[TEST ITEM] reload canceled by weapon switch. weaponId={canceledWeaponId}, targetSlot={index}");
        }

        if (currentIndex >= 0 && currentIndex < items.Length)
            items[currentIndex]?.OnUnequip();

        currentIndex = index;
        items[currentIndex]?.OnEquip();

        Weapon weapon = items[currentIndex].GetComponent<Weapon>();

        if (weapon != null && playerMovement != null)
            playerMovement.SetWeaponWeight(weapon.GetWeight());

        Debug.Log($"[TEST ITEM] switched to slot={index}");
    }

    public int GetSlotIndex(WeaponData data)
    {
        if (data == null) return 0;

        switch (data.weaponType)
        {
            case WeaponData.WeaponType.Pistol: return 1;
            case WeaponData.WeaponType.Knife: return 2;
            default: return 0;
        }
    }

    public void SetWeaponVisibility(bool visibility)
    {
        if (currentIndex < 0 || currentIndex >= items.Length) return;
        if (items[currentIndex] == null) return;

        if (visibility && currentIndex == 4 && playerSpike != null && !playerSpike.hasSpike)
        {
            int fallback = FindFirstAvailableWeaponSlotExceptSpike();
            if (fallback != -1) { SwitchWeaponVisualOnlyBySlot(fallback); return; }
        }

        items[currentIndex].gameObject.SetActive(visibility);
    }

    private int FindFirstAvailableWeaponSlotExceptSpike()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (i == 4) continue;
            if (items[i] != null) return i;
        }
        return -1;
    }

    private void SwitchWeaponVisualOnlyBySlot(int slot)
    {
        if (slot < 0 || slot >= items.Length) return;
        if (items[slot] == null) return;

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
            if (items[i] == null) continue;
            Weapon weapon = items[i].GetComponent<Weapon>();
            weapon?.ReloadFull();
        }

        if (items[1] != null) Switch(1);
        else if (items[2] != null) Switch(2);
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
        items[0] = null;

        if (secondary != null) CreateStartingItem(secondary, 1);
        else items[1] = null;

        if (melee != null) CreateStartingItem(melee, 2);
        else items[2] = null;

        items[3] = null;

        if (spike != null)
        {
            GameObject obj = Instantiate(spike, itemHolder);
            obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
            obj.transform.localRotation = Quaternion.identity;
            items[4] = obj.GetComponent<Item>();
            obj.SetActive(false);
        }
        else
        {
            items[4] = null;
        }

        if (items[1] != null) Switch(1);
        else if (items[2] != null) Switch(2);
    }

    public void EquipPrimaryWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;
        if (GetSlotIndex(weaponData) != 0) return;

        if (items[0] != null)
        {
            items[0].OnDrop();
            Destroy(items[0].gameObject);
            items[0] = null;
        }

        GameObject obj = Instantiate(weaponData.weaponPrefab, itemHolder);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        items[0] = obj.GetComponent<Item>();

        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
            weapon.Initialize(weaponData, dropPoint, weaponData.magSize, weaponData.maxReserveAmmo);

        obj.SetActive(false);
        Switch(0);
    }

    public Weapon GetCurrentWeapon()
    {
        if (currentIndex < 0 || currentIndex >= items.Length) return null;
        if (items[currentIndex] == null) return null;
        return items[currentIndex].GetComponent<Weapon>();
    }

    public void DisableOnDeath()
    {
        CancelReload(false);
        SetWeaponVisibility(false);
        enabled = false;
    }

    public void EnableOnRespawn()
    {
        enabled = true;
        if (currentIndex >= 0 && currentIndex < items.Length && items[currentIndex] != null)
            items[currentIndex].gameObject.SetActive(true);
    }

    public void RemoveSpike()
    {
        if (currentIndex == 4)
        {
            if (items[1] != null) Switch(1);
            else if (items[2] != null) Switch(2);
        }

        if (items[4] != null)
        {
            Destroy(items[4].gameObject);
            items[4] = null;
        }
    }

    public void SetSpikeItemActive(bool value)
    {
        if (items[4] == null) return;
        if (!value && currentIndex == 4) Switch(1);
        items[4].gameObject.SetActive(false);
    }

    public void GiveSpike(GameObject spikePrefab)
    {
        if (items[4] != null) Destroy(items[4].gameObject);

        GameObject obj = Instantiate(spikePrefab, itemHolder);
        obj.transform.localPosition = new Vector3(0.75f, -0.2f, 1f);
        obj.transform.localRotation = Quaternion.identity;

        items[4] = obj.GetComponent<Item>();
        obj.SetActive(false);
    }

    // ───────────────────────────── Reload ─────────────────────────────

    private void TryStartReload()
    {
        if (isReloading) return;
        if (currentIndex < 0 || currentIndex >= items.Length) return;

        Item currentItem = items[currentIndex];
        if (currentItem == null) return;

        Weapon currentWeapon = currentItem.GetComponent<Weapon>();
        if (currentWeapon == null) return;
        if (currentWeapon.GetWeaponType() == WeaponData.WeaponType.Knife) return;
        if (!currentWeapon.CanReloadNow()) return;

        float reloadTime = currentWeapon.GetWeaponReLoadTime();
        if (reloadTime <= 0f) return;

        // 장전 시작 시 ADS 해제
        FPSCameraLook cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        cameraLook?.ForceExitADS();

        reloadCoroutine = StartCoroutine(
            ReloadRoutine(currentItem, currentWeapon.GetWeaponId(), currentIndex, reloadTime)
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
        reloadingWeaponObject = reloadItem != null ? reloadItem.gameObject : null;

        PlayReloadAnimation(reloadItem, reloadTime);

        Debug.Log($"[TEST RELOAD START] weaponId={reloadWeaponId}, time={reloadTime}");

        yield return new WaitForSeconds(reloadTime);

        if (!isReloading) yield break;

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

        // 로컬 탄약 보충
        Weapon weapon = reloadItem.GetComponent<Weapon>();
        if (weapon != null)
            weapon.ReloadFull();

        Debug.Log($"[TEST RELOAD COMPLETE] weaponId={reloadWeaponId}");

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

        Debug.Log("[TEST RELOAD CANCEL]");
    }

    private void PlayReloadAnimation(Item item, float reloadTime = -1f)
    {
        if (item == null) return;

        ARMotion motion = item.GetComponent<ARMotion>();
        if (motion == null) motion = item.GetComponentInChildren<ARMotion>(true);
        if (motion == null) return;

        if (reloadTime <= 0f)
        {
            Weapon weapon = item.GetComponent<Weapon>();
            if (weapon == null) weapon = item.GetComponentInChildren<Weapon>(true);
            if (weapon != null) reloadTime = weapon.GetWeaponReLoadTime();
        }

        if (reloadTime <= 0f) return;

        motion.PlayReload(reloadTime);
    }

    private void StopReloadAnimation(Item item)
    {
        if (item == null) return;
        StopReloadAnimation(item.gameObject);
    }

    private void StopReloadAnimation(GameObject weaponObject)
    {
        if (weaponObject == null) return;

        ARMotion motion = weaponObject.GetComponent<ARMotion>();
        if (motion == null) motion = weaponObject.GetComponentInChildren<ARMotion>(true);
        motion?.StopReload();
    }

    private void DisableHeldWeaponPhysics(GameObject obj)
    {
        if (obj == null) return;

        DroppedItem droppedItem = obj.GetComponent<DroppedItem>();
        if (droppedItem != null) Destroy(droppedItem);

        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb == null) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        foreach (Collider col in obj.GetComponentsInChildren<Collider>(true))
        {
            if (col != null) col.enabled = false;
        }
    }

    public void ApplyPickedWeaponLocal(WeaponData weaponData, int magAmmo, int reserveAmmo)
    {
        if (weaponData == null) return;

        int slot = GetSlotIndex(weaponData);
        if (slot < 0 || slot >= items.Length || slot == 4) return;

        if (items[slot] != null)
        {
            Destroy(items[slot].gameObject);
            items[slot] = null;
        }

        GameObject obj = Instantiate(
        tutorialP92Prefab,
        itemHolder
        );
        /*
        GameObject obj = Instantiate(weaponData.weaponPrefab, itemHolder);
        */
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Item item = obj.GetComponent<Item>();
        if (item == null) { Destroy(obj); return; }

        items[slot] = item;

        Weapon weapon = obj.GetComponent<Weapon>();
        if (weapon != null)
            weapon.Initialize(weaponData, dropPoint, magAmmo, reserveAmmo);

        obj.SetActive(false);
        Switch(slot);
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