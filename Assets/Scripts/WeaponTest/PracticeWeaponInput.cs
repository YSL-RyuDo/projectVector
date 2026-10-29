using TMPro;
using UnityEngine;

// Presentation/statistics bridge only. Weapon.OnUse owns input, ADS, recoil, spread and bullets.
public sealed class PracticeWeaponInput : MonoBehaviour
{
    public PlayerItem inventory;
    public PracticeRangeController range;
    public Camera aimCamera;
    public TMP_Text ammo, hitMarker;
    public AudioSource sound;
    public WeaponRecoil recoil;
    public int Fired { get; private set; }
    private float hitUntil;
    private void Update()
    {
        var weapon=inventory.GetCurrentWeapon();
        if(weapon==null || weapon.Data==null)
        {
            ammo.text=inventory.CurrentIndex==4?"디바이스":"";
            hitMarker.gameObject.SetActive(false);
            return;
        }
        ammo.text=inventory.IsReloading ? "재장전 중" : $"{weapon.Data.name}   {(range.infiniteAmmo ? "∞" : $"{weapon.CurrentAmmo} / {weapon.ReserveAmmo}")}";
        hitMarker.gameObject.SetActive(Time.time<hitUntil);
    }
    public void RecordShot(){++Fired;range.spawner.RecordShot();}
    public void RecordHit(){hitUntil=Time.time+.12f;}
    public bool TryFire()
    {
        int before=Fired;
        inventory.GetCurrentWeapon()?.OnUse();
        return Fired>before;
    }
    // Compatibility for the preserved legacy test player, not the active Game prefab.
    public void SetADS(bool value)=>GetComponentInChildren<FPSCameraLook>()?.SetADSFromServer(value);
}
