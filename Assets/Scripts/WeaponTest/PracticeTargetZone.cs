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
        var weapon=input?.inventory.GetCurrentWeapon()?.Data;
        Hit(input?.range.loadout?.DamageFor(weapon) ?? weapon?.damage ?? 0,input?.Fired ?? -1,spawner.Generation);
    }
    public void Hit(int damage,int shotId,int generation)
    {
        if(generation!=spawner.Generation || !spawner.Running || spawner.Paused) return;
        FindFirstObjectByType<PracticeWeaponInput>()?.RecordHit();
        if(spawner.RegisterDamage(damage,isHead?1:isLegs?2:0,shotId)) spawner.enemyObject.SetActive(false);
    }
}
