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

            Destroy(effect, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            Debug.Log("폭발 범위 안 감지: " + hit.name);
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