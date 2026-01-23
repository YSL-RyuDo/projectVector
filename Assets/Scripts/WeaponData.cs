using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject weaponPrefab;
    public GameObject dropPrefab;

    [Header("Fire")]
    public float fireRate;
    public float range;
    public float recoil;

    [Header("Ammo")]
    public int magSize;
    public int maxReserveAmmo;
    public float reloadTime;

    [Header("ADS")]
    public float normalFOV;
    public float adsFOV;
    public float adsSpeed;
}
