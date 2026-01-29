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

    private GameObject currentWeapon;

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

        for (int i = weaponHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponHolder.GetChild(i).gameObject);
        }

        currentWeapon = Instantiate(
            startWeaponData.weaponPrefab,
            weaponHolder
        );

        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        var pistol = currentWeapon.GetComponent<SimplePistol>();
        pistol.SetAmmo(
            startWeaponData.magSize,
            startWeaponData.maxReserveAmmo
        );
        pistol.SetDropPoint(dropPoint);
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
            PickupWeapon(nearest);
        }
    }

    void PickupWeapon(DroppedWeapon dropped)
    {
        if (currentWeapon != null)
        {
            SimplePistol old = currentWeapon.GetComponent<SimplePistol>();
            old?.DropWeapon();
        }

        currentWeapon = Instantiate(
            dropped.weaponData.weaponPrefab,
            weaponHolder
        );

        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        SimplePistol pistol = currentWeapon.GetComponent<SimplePistol>();
        pistol.SetAmmo(dropped.magAmmo, dropped.reserveAmmo);
        pistol.SetDropPoint(dropPoint);

        Destroy(dropped.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
