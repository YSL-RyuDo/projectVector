using UnityEngine;

public class RaycastFire : IFireStrategy
{
    private Weapon weapon;

    public RaycastFire(Weapon weapon)
    {
        this.weapon = weapon;
    }

    public void Fire()
    {
        weapon.FireRaycast();
    }
}