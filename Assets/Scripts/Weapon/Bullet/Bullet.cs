using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;

    private PlayerHP owner;
    private int damage;
    private float maxDistance;

    private Vector3 startPos;
    private Vector3 direction;

    private Rigidbody rb;

    public void Init(
        PlayerHP ownerHP,
        int dmg,
        float range,
        float speed,
        Vector3 dir
    )
    {
        owner = ownerHP;
        damage = dmg;
        maxDistance = range;
        direction = dir.normalized;

        startPos = transform.position;

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;   // ÇÙ½É ºÎºÐ
        }
    }

    void Update()
    {
        if (Vector3.Distance(startPos, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        SpawnBulletHole(contact.point, contact.normal);

        var hp = collision.collider.GetComponentInParent<PlayerHP>();

        if (hp != null && hp != owner && !hp.IsDead)
        {
            var identity = hp.GetComponent<PlayerIdentity>();
            if (identity != null)
            {
                //NetworkManager.Instance.SendHit(identity.PlayerId, damage);
            }
        }

        Destroy(gameObject);
    }

    void SpawnBulletHole(Vector3 point, Vector3 normal)
    {
        if (bulletHolePrefab == null)
            return;

        GameObject hole = Instantiate(
            bulletHolePrefab,
            point + normal * 0.001f,
            Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 180f, 0f)
        );

        hole.transform.SetParent(null);
    }
}