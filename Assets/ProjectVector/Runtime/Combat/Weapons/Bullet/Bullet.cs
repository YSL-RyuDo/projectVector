using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;
    private PlayerIdentity ownerIdentity;
    private float remainingDistance, bulletSpeed;
    private Vector3 direction;
    private bool canSendHit, finished, visualOnly, visualSurface;
    private Vector3 visualTarget, visualNormal;
    private int hitMask = ~0;
    private uint shotId;
    private byte pelletIndex;
    private Func<RaycastHit, bool> deviceImpact;
    private int practiceDamage, practiceShot, practiceGeneration;
    private int practiceWeaponId = -1;
    private Vector3 damageOrigin;

    public Collider LastImpactCollider { get; private set; }
    public Vector3 LastImpactPoint { get; private set; }
    public bool HasFinished => finished;

    public void Init(PlayerIdentity owner, float range, float speed, Vector3 dir, bool sendHitFromThisBullet,
        int mask = ~0, uint shot = 0, byte pellet = 0, Func<RaycastHit, bool> onDeviceImpact = null, Vector3? firedOrigin = null)
    {
        ownerIdentity = owner;
        damageOrigin = firedOrigin ?? transform.position;
        remainingDistance = Mathf.Max(0f, range);
        bulletSpeed = Mathf.Max(0f, speed);
        direction = dir.normalized;
        canSendHit = sendHitFromThisBullet;
        hitMask = mask; shotId = shot; pelletIndex = pellet; deviceImpact = onDeviceImpact;
        finished = false; visualOnly = false;
        var practice = PracticePlayerContext.For(owner);
        if (practice != null)
        {
            var weapon = practice.GetComponent<PlayerItem>()?.GetCurrentWeapon()?.Data;
            practiceDamage = practice.range.loadout?.DamageFor(weapon) ?? weapon?.damage ?? 0;
            practiceWeaponId = weapon != null ? weapon.weaponId : -1;
            practiceShot = practice.GetComponent<PracticeWeaponInput>()?.Fired ?? -1;
            practiceGeneration = practice.range.spawner.Generation;
        }
        DisablePhysicalContact();
    }

    public void InitVisual(Vector3 target, Vector3 normal, bool surface, float speed)
    {
        visualOnly = true; finished = false; canSendHit = false;
        visualTarget = target; visualNormal = normal; visualSurface = surface;
        bulletSpeed = Mathf.Max(1f, speed);
        DisablePhysicalContact();
    }

    private void DisablePhysicalContact()
    {
        // A sphere/CCD collision can hit a corner the aim ray missed. The swept
        // ray owns impact; this object is only its presentation, never a second hit.
        foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    private void FixedUpdate() => Advance(Time.fixedDeltaTime);

    public void Advance(float deltaTime)
    {
        if (finished || deltaTime <= 0f) return;
        if (visualOnly)
        {
            transform.position = Vector3.MoveTowards(transform.position, visualTarget, bulletSpeed * deltaTime);
            if ((transform.position - visualTarget).sqrMagnitude < 0.000001f)
            {
                if (visualSurface) SpawnBulletHole(visualTarget, visualNormal);
                Finish();
            }
            return;
        }
        Vector3 origin = transform.position;
        Transform root = ownerIdentity != null ? ownerIdentity.transform : null;
        var enclosing = WeaponShotTrace.EnclosingCollider(origin, hitMask, ownerIdentity, root);
        if (enclosing != null) { ResolveImpact(enclosing, origin, -direction, null); return; }
        float distance = Mathf.Min(remainingDistance, bulletSpeed * deltaTime);
        if (WeaponShotTrace.FirstHit(new Ray(origin, direction), distance, hitMask, ownerIdentity, root, out var hit))
        {
            transform.position = hit.point;
            ResolveImpact(hit.collider, hit.point, hit.normal, hit);
            return;
        }
        transform.position = origin + direction * distance;
        remainingDistance -= distance;
        if (remainingDistance <= 0f || bulletSpeed <= 0f) Finish();
    }

    private void ResolveImpact(Collider collider, Vector3 point, Vector3 normal, RaycastHit? rayHit)
    {
        if (finished) return;
        LastImpactCollider = collider; LastImpactPoint = point;
        // Set terminal state BEFORE callbacks, preventing repeated contact damage.
        Finish();
        if (PracticePlayerContext.For(ownerIdentity) != null && collider.TryGetComponent<PracticeTargetZone>(out var target))
        {
            if (canSendHit) target.Hit(WeaponDamage.Calculate(practiceWeaponId, practiceDamage,
                WeaponDamage.Distance(damageOrigin, point), target.isHead ? 1 : target.isLegs ? 2 : 0), practiceShot, practiceGeneration);
            return;
        }
        if (canSendHit && rayHit.HasValue && deviceImpact != null && deviceImpact(rayHit.Value)) return;
        var player = collider.GetComponentInParent<PlayerIdentity>();
        if (player != null)
        {
            var hp = player.GetComponentInChildren<PlayerHP>(true);
            if (hp != null && hp.IsDead) return;
            if (canSendHit && ownerIdentity != null && player.PlayerId != ownerIdentity.PlayerId &&
                player.Team != ownerIdentity.Team && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendTrackedHit(player.PlayerId, WeaponShotTrace.GetHitZone(collider), shotId, pelletIndex, point);
            }
            return;
        }
        SpawnBulletHole(point, normal);
    }

    private void Finish()
    {
        finished = true;
        enabled = false;
        if (Application.isPlaying) Destroy(gameObject);
    }

    private void SpawnBulletHole(Vector3 point, Vector3 normal)
    {
        if (bulletHolePrefab == null || normal.sqrMagnitude < 0.01f) return;
        var hole = Instantiate(bulletHolePrefab, point + normal * 0.001f,
            Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 180f, 0f));
        hole.transform.SetParent(null);
    }
}
