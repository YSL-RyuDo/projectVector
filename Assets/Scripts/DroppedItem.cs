using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Server Drop Info")]
    public uint dropId;

    [Header("Weapon 전용")]
    public WeaponData weaponData;
    public int magAmmo;
    public int reserveAmmo;

    private bool pickupRequested;

    public void InitFromServer(
        uint serverDropId,
        WeaponData data,
        int mag,
        int reserve)
    {
        dropId = serverDropId;
        weaponData = data;
        magAmmo = mag;
        reserveAmmo = reserve;
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
            Debug.LogWarning("[DROPPED ITEM] dropId is 0. 서버에서 생성된 아이템이 아닙니다.");
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
}