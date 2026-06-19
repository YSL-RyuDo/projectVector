using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Server")]
    public int weaponId;

    public enum WeaponType
    {
        AR,
        SMG,
        Shotgun,
        LMG,
        DMR,
        Pistol,
        SR,
        Knife
    }

    [Header("Type")]
    public WeaponType weaponType;

    [Header("Prefab")]
    public GameObject weaponPrefab;
    public GameObject dropPrefab;

    [Header("UI")]
    public Sprite weaponIcon;

    [Header("Fire")]
    public float fireRate;  // πﬂªÁ º”µµ
    public float range;     // ªÁ¡§ ∞≈∏Æ
    public int damage;

    [Header("Bullet")]
    public float muzzleVelocity; // ≈∫º”
    public GameObject bulletHolePrefab;

    [Header("Ammo")]
    public int magSize;
    public int maxReserveAmmo;
    public float reloadTime;

    [Header("ADS")]
    public float normalFOV;
    public float adsFOV;
    public float adsSpeed;

    [Header("Recoil")]
    public Vector2[] recoilPattern;
    public float recoilMultiplier = 1f;
    public float adsRecoilMultiplier = 1f;

    [Header("Spread")]
    public float hipFireSpread = 0.02f;   // ¿œπ› ªÁ∞› ≈∫∆€¡¸
    public float adsSpread = 0.005f;      // ADS ≈∫∆€¡¸
    public float moveSpreadMultiplier;    // øÚ¡˜¿œ Ω√ ≈∫∆€¡¸ πË¿≤

    [Header("Etc")]
    public float weight;
}
