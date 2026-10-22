using UnityEngine;

// Physical ceilings take priority. The launch-relative ceiling also bounds open sky.
public static class DroneFlightSafety
{
    // 1.5-1.7m characters; low cover 1.6-2.7m, tall fences about 3.8m.
    // Typical standing release + this allowance gives roughly 2.6-2.8m above floor.
    public const float MaxClimb = 1.25f; // Must match DRONE_MAX_CLIMB on the server.
    // Ground clearance is telemetry only. It must not raise the launch-relative ceiling.
    public static bool TryGetGroundClearance(Vector3 position, Transform self, out float height)
    {
        height=float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(position, Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            var collider=hit.collider;
            if ((self && collider.transform.IsChildOf(self)) || hit.normal.y < .25f) continue;
            if (collider.GetComponentInParent<PlayerIdentity>() || collider.GetComponentInParent<Drone>()) continue;
            height=Mathf.Min(height,hit.distance);
        }
        if (!float.IsInfinity(height)) return true;
        height=float.NaN;
        return false;
    }
    public static Vector3 Constrain(Vector3 position, Vector3 delta, float ceiling, float radius, Transform self)
    {
        delta.y = Mathf.Min(delta.y, Mathf.Max(0, ceiling - position.y));
        float distance = delta.magnitude;
        if (distance < 0.00001f) return Vector3.zero;
        float allowed = distance;
        foreach (var hit in Physics.SphereCastAll(position, radius, delta / distance, distance + 0.06f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (self != null && hit.collider.transform.IsChildOf(self)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - 0.06f));
        }
        return delta / distance * allowed;
    }
}
