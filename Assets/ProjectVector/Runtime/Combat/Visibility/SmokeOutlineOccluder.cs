using System.Collections.Generic;
using UnityEngine;

// Saved on the existing smoke prefab. No colliders or UI are generated at runtime.
// Network managers scale that prefab by the server radius; this is a unit sphere.
[DisallowMultipleComponent]
public sealed class SmokeOutlineOccluder : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField, Min(.001f)] private float _radius = 1f;
    private static readonly HashSet<SmokeOutlineOccluder> Active = new();
    public Vector3 WorldCenter => transform.TransformPoint(_center);
    public float WorldRadius
    {
        get
        {
            var scale = transform.lossyScale;
            return _radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
    }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    private void OnDestroy() => Active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();

    // Clamped segment, not an infinite ray: smoke behind the camera/target does not hide it.
    // Endpoints are included, so a camera or target already inside smoke is handled too.
    public static bool IntersectsSegment(Vector3 from, Vector3 to, Vector3 center, float radius)
    {
        var delta = to - from;
        float lengthSquared = delta.sqrMagnitude;
        float t = lengthSquared > 1e-10f ? Mathf.Clamp01(Vector3.Dot(center - from, delta) / lengthSquared) : 0;
        return (from + delta * t - center).sqrMagnitude <= radius * radius;
    }
    public static bool BlocksOutline(Vector3 viewer, Bounds target)
    {
        foreach (var smoke in Active)
        {
            if (!smoke || !smoke.isActiveAndEnabled) continue;
            var center = smoke.WorldCenter;
            float radius = smoke.WorldRadius;
            if (radius <= 0) continue;
            // A hollow smoke shell does not block sight within its own interior.
            // Require the complete body inside: straddling the shell must not leak
            // an outline on its far side. Continue (not return) so another smoke
            // between these players can still conceal the target.
            var farthest = Abs(target.center - center) + target.extents;
            if ((viewer - center).sqrMagnitude < radius * radius &&
                farthest.sqrMagnitude < radius * radius) continue;
            // Even a partially submerged body loses its entire outline, regardless of team.
            if (target.SqrDistance(center) <= radius * radius) return true;
            if (IntersectsSegment(viewer, target.center, center, radius)) return true;
            // Head/feet/shoulder edges must not leak just because the torso ray misses smoke.
            var e = target.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = target.center + new Vector3(
                    (corner & 1) == 0 ? -e.x : e.x,
                    (corner & 2) == 0 ? -e.y : e.y,
                    (corner & 4) == 0 ? -e.z : e.z);
                if (IntersectsSegment(viewer, point, center, radius)) return true;
            }
        }
        return false;
    }
    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.5f, .7f, .8f, .8f);
        Gizmos.DrawWireSphere(WorldCenter, WorldRadius);
    }
}
