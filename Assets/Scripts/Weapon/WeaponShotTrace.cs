using System;
using UnityEngine;

// One collision policy for camera rays and swept projectiles. Friendly players
// are transparent, matching the existing projectile/friendly-fire rules.
public static class WeaponShotTrace
{
    static bool Ignore(Collider collider, PlayerIdentity owner, Transform ownerRoot)
    {
        if (collider == null || collider.GetComponentInParent<Bullet>() != null) return true;
        if (ownerRoot != null && collider.transform.IsChildOf(ownerRoot)) return true;
        var player = collider.GetComponentInParent<PlayerIdentity>();
        return owner != null && player != null && (player.PlayerId == owner.PlayerId || player.Team == owner.Team);
    }

    public static bool FirstHit(Ray ray, float range, int mask, PlayerIdentity owner, Transform ownerRoot, out RaycastHit hit)
    {
        hit = default;
        if (range <= 0f || ray.direction.sqrMagnitude < 0.000001f) return false;
        Physics.SyncTransforms();
        var hits = Physics.RaycastAll(ray, range, mask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var candidate in hits)
        {
            if (Ignore(candidate.collider, owner, ownerRoot)) continue;
            hit = candidate;
            return true;
        }
        return false;
    }

    // Raycasts do not report a collider enclosing their origin. Check that case
    // separately so close contacts cannot become shots through the enclosing wall.
    public static Collider EnclosingCollider(Vector3 origin, int mask, PlayerIdentity owner, Transform ownerRoot)
    {
        Collider player = null;
        Physics.SyncTransforms();
        foreach (var collider in Physics.OverlapSphere(origin, 0.001f, mask, QueryTriggerInteraction.Ignore))
        {
            if (Ignore(collider, owner, ownerRoot) || (collider.ClosestPoint(origin) - origin).sqrMagnitude > 0.000001f) continue;
            if (collider.GetComponentInParent<PlayerIdentity>() == null) return collider;
            player = collider;
        }
        return player;
    }

    public static bool IsHead(Collider collider)
    {
        if (collider == null) return false;
        string name = collider.name.ToLowerInvariant();
        return name.Contains("head") || name.Contains("머리");
    }
}
