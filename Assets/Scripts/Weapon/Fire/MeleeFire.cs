using UnityEngine;

public class MeleeFire : IFireStrategy
{
    private Transform attackPoint;
    private float range;
    private int damage;

    public MeleeFire(Transform point, float range, int dmg)
    {
        attackPoint = point;
        this.range = range;
        damage = dmg;
    }

    public void Fire()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, range);

        foreach (var hit in hits)
        {
            var hp = hit.GetComponent<PlayerHP>();
            if (hp != null)
            {
                //hp.TakeDamage(damage);
            }
        }
    }
}