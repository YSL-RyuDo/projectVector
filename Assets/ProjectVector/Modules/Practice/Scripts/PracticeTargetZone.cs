using UnityEngine;

// Authored character-sized training regions. Never modify live player health or send hit packets.
public sealed class PracticeTargetZone : MonoBehaviour
{
    public EnemySpawner spawner;
    public bool isHead;
    public bool isLegs;
    public void Hit()
    {
        var input=FindFirstObjectByType<PracticeWeaponInput>();
        HitAt(input != null && input.aimCamera != null ? input.aimCamera.transform.position : transform.position, transform.position);
    }
    public void HitAt(Vector3 origin, Vector3 point)
    {
        var input=FindFirstObjectByType<PracticeWeaponInput>();
        var weapon=input?.inventory.GetCurrentWeapon()?.Data;
        int baseDamage=input?.range.loadout?.DamageFor(weapon) ?? weapon?.damage ?? 0;
        Hit(WeaponDamage.Calculate(weapon != null ? weapon.weaponId : -1,baseDamage,WeaponDamage.Distance(origin,point),isHead?1:isLegs?2:0),input?.Fired ?? -1,spawner.Generation);
    }
    public void Hit(int damage,int shotId,int generation)
    {
        if(generation!=spawner.Generation || !spawner.Running || spawner.Paused) return;
        FindFirstObjectByType<PracticeWeaponInput>()?.RecordHit();
        if(spawner.RegisterDamage(damage,isHead?1:isLegs?2:0,shotId)) spawner.enemyObject.SetActive(false);
    }
}
