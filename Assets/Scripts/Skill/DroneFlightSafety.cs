using UnityEngine;

// Physical ceilings take priority. The launch-relative ceiling also bounds open sky.
public static class DroneFlightSafety
{
    public const float MaxClimb = 8f; // Must match the server's deployment ceiling.
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
