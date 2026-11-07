using UnityEngine;

public class TutorialBullet : MonoBehaviour
{
    public enum ShooterType
    {
        Player,
        Enemy
    }

    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private int damage = 10;

    private Rigidbody rb;
    private ShooterType shooterType;

    public void Init(
        float range,
        float speed,
        Vector3 direction,
        ShooterType shooter)
    {
        shooterType = shooter;

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

        // 플레이어가 쏜 총알 -> 적만 데미지
        if (shooterType == ShooterType.Player)
        {
            TutoEnemyMove enemy =
                collision.collider.GetComponentInParent<TutoEnemyMove>();

            if (enemy != null)
            {
                enemy.Damage(damage);
                Destroy(gameObject);
                return;
            }
        }

        // 적이 쏜 총알 -> 플레이어만 데미지
        else if (shooterType == ShooterType.Enemy)
        {
            TESTPlayerHP playerHP =
                collision.collider.GetComponentInParent<TESTPlayerHP>();

            if (playerHP != null)
            {
                playerHP.Damage(damage);
                Destroy(gameObject);
                return;
            }
        }

        // 벽이나 일반 오브젝트에 맞은 경우
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
            Quaternion.LookRotation(normal) *
            Quaternion.Euler(0f, 180f, 0f)
        );

        hole.transform.SetParent(null);
    }
}