using UnityEngine;

public class ProjectileFire : IFireStrategy
{
    private Weapon weapon;

    public ProjectileFire(Weapon weapon)
    {
        this.weapon = weapon;
    }

    public void Fire()
    {
        weapon.FireProjectile();
    }
}