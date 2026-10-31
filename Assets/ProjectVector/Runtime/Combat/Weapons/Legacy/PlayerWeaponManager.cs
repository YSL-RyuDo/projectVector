using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public Transform dropPoint;

    [Header("Pickup")]
    public float pickupRange = 2.5f;
    public LayerMask weaponLayer;

    [Header("Start Weapon")]
    public WeaponData startWeaponData;

    private Weapon currentWeapon;

    void Start()
    {
        SpawnStartWeapon();
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupWeapon();
        }
    }

    public void SpawnStartWeapon()
    {
        if (startWeaponData == null)
            return;

        EquipWeapon(startWeaponData,
            startWeaponData.magSize,
            startWeaponData.maxReserveAmmo);
    }

    void EquipWeapon(WeaponData data, int mag, int reserve)
    {
        if (currentWeapon != null)
        {
            currentWeapon.DropWeapon();
        }

        ClearHolder();

        GameObject obj = Instantiate(data.weaponPrefab, weaponHolder);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        currentWeapon = obj.GetComponent<Weapon>();

        currentWeapon.Initialize(data, dropPoint, mag, reserve);
    }

    void TryPickupWeapon()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            pickupRange,
            weaponLayer
        );

        DroppedWeapon nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            DroppedWeapon dw = hit.GetComponent<DroppedWeapon>();
            if (dw == null)
                continue;

            float dist = Vector3.Distance(transform.position, dw.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = dw;
            }
        }

        if (nearest != null)
        {
            EquipWeapon(
                nearest.weaponData,
                nearest.magAmmo,
                nearest.reserveAmmo
            );

            Destroy(nearest.gameObject);
        }
    }

    void ClearHolder()
    {
        for (int i = weaponHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponHolder.GetChild(i).gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}