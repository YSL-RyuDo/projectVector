using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Weapon Àü¿ë")]
    public WeaponData weaponData;
    public int magAmmo;
    public int reserveAmmo;

    private void Start()
    {
        Destroy(gameObject, 180f);
    }

    public void Pickup(PlayerItem player)
    {
        player.PickupItem(this);
    }
}