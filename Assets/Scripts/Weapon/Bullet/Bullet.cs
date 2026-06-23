using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;

    private PlayerIdentity ownerIdentity;
    private int ownerPlayerId;
    private byte ownerTeam;

    private float maxDistance;
    private float bulletSpeed;
    private Vector3 startPos;
    private Vector3 direction;

    private bool canSendHit;
    private bool hitSent;

    private Rigidbody rb;
    private Collider bulletCollider;

    public void Init(
        PlayerIdentity owner,
        float range,
        float speed,
        Vector3 dir,
        bool sendHitFromThisBullet)
    {
        ownerIdentity = owner;
        ownerPlayerId = owner != null ? (int)owner.PlayerId : -1;
        ownerTeam = owner != null ? owner.Team : (byte)255;

        maxDistance = range;
        bulletSpeed = speed;
        direction = dir.normalized;

        canSendHit = sendHitFromThisBullet;
        hitSent = false;

        startPos = transform.position;

        rb = GetComponent<Rigidbody>();
        bulletCollider = GetComponent<Collider>();

        if (bulletCollider == null)
        {
            Debug.LogError("[BULLET] Collider missing.", this);
            Destroy(gameObject);
            return;
        }

        if (rb == null)
        {
            Debug.LogError("[BULLET] Rigidbody missing.", this);
            Destroy(gameObject);
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        IgnoreOwnerColliders();

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

        PlayerIdentity targetIdentity =
            collision.collider.GetComponentInParent<PlayerIdentity>();

        if (targetIdentity != null)
        {
            HandlePlayerCollision(collision, targetIdentity);
            return;
        }

        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            SpawnBulletHole(contact.point, contact.normal);
        }

        Destroy(gameObject);
    }

    private void HandlePlayerCollision(Collision collision, PlayerIdentity targetIdentity)
    {
        if (targetIdentity == null)
            return;

        // 자기 자신은 무시
        if (ownerIdentity != null &&
            targetIdentity.PlayerId == ownerIdentity.PlayerId)
        {
            IgnoreThisCollision(collision.collider);
            RestoreVelocity();
            return;
        }

        // 같은 팀은 클라에서 관통 처리
        // 서버에도 아군 사격 방어가 있으므로 이중 방어 구조
        if (ownerIdentity != null &&
            targetIdentity.Team == ownerTeam)
        {
            Debug.Log(
                $"[BULLET] Friendly ignored. owner={ownerPlayerId}, target={targetIdentity.PlayerId}"
            );

            IgnoreThisCollision(collision.collider);
            RestoreVelocity();
            return;
        }

        PlayerHP targetHP = targetIdentity.GetComponent<PlayerHP>();

        if (targetHP != null && targetHP.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        // 쏜 사람 클라이언트에서 생성된 총알만 서버에 Hit 전송
        if (canSendHit && !hitSent)
        {
            hitSent = true;

            if (NetworkManager.Instance != null)
            {
                Debug.Log(
                    $"[BULLET HIT SEND] targetPlayerId={targetIdentity.PlayerId}, owner={ownerPlayerId}"
                );

                NetworkManager.Instance.SendHit(targetIdentity.PlayerId);
                HitManager.TriggerEnemyHit();
            }
        }

        Destroy(gameObject);
    }

    private void IgnoreOwnerColliders()
    {
        if (ownerIdentity == null || bulletCollider == null)
            return;

        Collider[] ownerColliders =
            ownerIdentity.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in ownerColliders)
        {
            if (col == null)
                continue;

            if (col == bulletCollider)
                continue;

            Physics.IgnoreCollision(bulletCollider, col, true);
        }
    }

    private void IgnoreThisCollision(Collider other)
    {
        if (bulletCollider == null || other == null)
            return;

        Physics.IgnoreCollision(bulletCollider, other, true);
    }

    private void RestoreVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity = direction * bulletSpeed;
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