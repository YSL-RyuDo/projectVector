using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float delay = 3f;
    public float explosionRadius = 5f;
    public GameObject explosionEffect;

    void Start()
    {
        Invoke("Explode", delay);
    }

    void Explode()
    {
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );

            SphereCollider col =
                effect.AddComponent<SphereCollider>();

            col.isTrigger = true;
            col.radius = explosionRadius;

            Destroy(effect, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("플레이어 수류탄 영향받음");
            }

            if (hit.CompareTag("Enemy"))
            {
                Debug.Log("에너미 수류탄 영향받음");
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}