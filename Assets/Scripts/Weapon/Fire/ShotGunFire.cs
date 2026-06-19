using UnityEngine;

public class ShotGunFire : IFireStrategy
{
    private Weapon weapon;
    private int pelletCount;

    public ShotGunFire(Weapon weapon, int pelletCount = 12)
    {
        this.weapon = weapon;
        this.pelletCount = pelletCount;
    }

    public void Fire()
    {
        if (weapon == null)
            return;

        for (int i = 0; i < pelletCount; i++)
        {
            weapon.FireProjectile();
        }
    }
}