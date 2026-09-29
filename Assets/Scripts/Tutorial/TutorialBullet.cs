using UnityEngine;

public class TutoEnemyBullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private int damage = 10;

    private float maxDistance;
    private float bulletSpeed;
    private Vector3 startPos;
    private Vector3 direction;

    private bool hit;

    private Rigidbody rb;
    private Collider bulletCollider;

    public void Init(float range, float speed, Vector3 dir)
    {
        maxDistance = range;
        bulletSpeed = speed;
        direction = dir.normalized;

        startPos = transform.position;
        hit = false;

        rb = GetComponent<Rigidbody>();
        bulletCollider = GetComponent<Collider>();

        if (bulletCollider == null)
        {
            Debug.LogError("[TUTO ENEMY BULLET] Collider missing.", this);
            Destroy(gameObject);
            return;
        }

        if (rb == null)
        {
            Debug.LogError("[TUTO ENEMY BULLET] Rigidbody missing.", this);
            Destroy(gameObject);
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.linearVelocity = direction * bulletSpeed;
    }

    private void Update()
    {
        if (Vector3.Distance(startPos, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        if (hit)
            return;

        TESTPlayerHP playerHP =
            collision.collider.GetComponentInParent<TESTPlayerHP>();

        if (playerHP != null)
        {
            hit = true;

            playerHP.Damage(damage);

            Destroy(gameObject);
            return;
        }

        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];

            SpawnBulletHole(
                contact.point,
                contact.normal
            );
        }

        Destroy(gameObject);
    }

    private void SpawnBulletHole(Vector3 point, Vector3 normal)
    {
        if (bulletHolePrefab == null)
            return;

        GameObject hole = Instantiate(
            bulletHolePrefab,
            point + normal * 0.001f,
            Quaternion.LookRotation(normal) *
            Quaternion.Euler(0f, 180f, 0f)
        );

        hole.transform.SetParent(null);
    }
}