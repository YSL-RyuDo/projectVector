using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] private Transform itemHolder;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private LayerMask itemLayer;

    private Item[] items = new Item[4]; // 0~3 (4번 = Spike)
    private PlayerSpike playerSpike;
    private int currentIndex = 0;

    private void Start()
    {
        playerSpike = GetComponent<PlayerSpike>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            items[currentIndex]?.OnUse();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            items[currentIndex]?.OnAltUse();

        if (Keyboard.current.gKey.wasPressedThisFrame)
            DropCurrentItem();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            TryPickup();

        if (Keyboard.current.digit1Key.wasPressedThisFrame) Switch(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) Switch(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) Switch(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) Switch(3);
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

        // 주운 무기로 즉시 교체
        Switch(slot);

        Destroy(dropped.gameObject);
    }

    void DropCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Length || items[currentIndex] == null) return;
        if (currentIndex == 2) return;

        items[currentIndex].OnDrop();
        Destroy(items[currentIndex].gameObject); // .gameObject 파괴
        items[currentIndex] = null;

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
        if (currentIndex == index || items[index] == null) return;

        items[currentIndex]?.OnUnequip();

        currentIndex = index;

        items[currentIndex]?.OnEquip();
    }

    int GetSlotIndex(WeaponData data)
    {
        if (data == null) return 0;

        switch (data.weaponType)
        {
            case WeaponData.WeaponType.Pistol:
                return 1;
            case WeaponData.WeaponType.Shotgun: // Knife 대용으로 사용 중인 경우
                return 2;
            default: // AR, SMG, SR 등은 모두 0번 슬롯
                return 0;
        }
    }
}