using UnityEngine;

public class TutorialEnemyAttack : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private Transform player;
    [SerializeField] private Collider playerCollider;

    [Header("공격 설정")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackInterval = 1f;

    [Header("회전")]
    [SerializeField] private float rotationSpeed = 5f;

    [Header("총알")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 25f;

    private float attackTimer;

    private void OnEnable()
    {
        attackTimer = 0f;
    }

    private void Update()
    {
        if (player == null)
            return;

        RotateToPlayer();

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange && attackTimer <= 0f)
        {
            Shoot();
        }
    }

    private void RotateToPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        Vector3 targetPosition;

        if (playerCollider != null)
            targetPosition = playerCollider.bounds.center;
        else
            targetPosition = player.position + Vector3.up;

        Vector3 direction =
            (targetPosition - firePoint.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        TutorialBullet enemyBullet =
            bullet.GetComponent<TutorialBullet>();

        if (enemyBullet != null)
        {
            enemyBullet.Init(
             attackRange,
             bulletSpeed,
             direction,
            TutorialBullet.ShooterType.Enemy
            );
        }

        attackTimer = attackInterval;
    }
}