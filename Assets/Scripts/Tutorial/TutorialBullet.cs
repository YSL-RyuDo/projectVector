using UnityEngine;

public class TutorialBullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private int damage = 10;

    private Rigidbody rb;

    public void Init(float range, float speed, Vector3 direction)
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, range / speed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TutoEnemyMove enemy =
            collision.collider.GetComponentInParent<TutoEnemyMove>();

        if (enemy != null)
        {
            enemy.Damage(damage);
            Destroy(gameObject);
            return;
        }

        //자국 생성
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            SpawnBulletHole(contact.point, contact.normal);
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
            Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 180f, 0f)
        );

        hole.transform.SetParent(null);
    }
}