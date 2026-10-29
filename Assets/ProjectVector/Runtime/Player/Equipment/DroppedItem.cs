using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Server Drop Info")]
    public uint dropId;

    [Header("Weapon Data")]
    public WeaponData weaponData;
    public int magAmmo;
    public int reserveAmmo;
    public int skinId;

    private bool pickupRequested;

    public void InitFromServer(
        uint serverDropId,
        WeaponData data,
        int mag,
        int reserve,
        int skin = 0)
    {
        dropId = serverDropId;
        weaponData = data;
        magAmmo = mag;
        reserveAmmo = reserve;
        skinId = skin;
        pickupRequested = false;
    }

    public void Pickup(PlayerItem player)
    {
        if (pickupRequested)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (dropId == 0)
        {
            Debug.LogWarning("[DROPPED ITEM] invalid dropId=0");
            return;
        }

        pickupRequested = true;

        NetworkManager.Instance.SendWeaponPickupRequest(dropId);

        Debug.Log(
            $"[DROPPED ITEM PICKUP REQUEST] dropId={dropId}, " +
            $"weaponId={(weaponData != null ? weaponData.weaponId : -1)}, " +
            $"ammo={magAmmo}/{reserveAmmo}"
        );
    }

    // 로컬용 함수
    public void PickupLocal(TESTPlayerItem player)
    {
        if (pickupRequested) return;
        if (weaponData == null)
        {
            Debug.LogWarning("[DROPPED ITEM] weaponData is null");
            return;
        }

        pickupRequested = true;

        player.ApplyPickedWeaponLocal(weaponData, magAmmo, reserveAmmo);

        Debug.Log($"[DROPPED ITEM LOCAL PICKUP] weaponId={weaponData.weaponId}, ammo={magAmmo}/{reserveAmmo}");

        Destroy(gameObject);
    }

    public void ResetPickupRequest()
    {
        pickupRequested = false;
    }
}