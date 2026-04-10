using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Weapon Àü¿ë")]
    public WeaponData weaponData;
    public int magAmmo;
    public int reserveAmmo;

    public void Pickup(PlayerItem player)
    {
        player.PickupItem(this);
    }
}