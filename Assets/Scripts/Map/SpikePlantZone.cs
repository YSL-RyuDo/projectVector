using UnityEngine;

public class SpikePlantZone : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<SpikePlantZone> Active = new();
    private BoxCollider box;
    private void OnEnable() { box = GetComponent<BoxCollider>(); Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }
    public static bool Contains(Vector3 position)
    {
        foreach (var zone in Active)
        {
            if (zone == null || zone.box == null || !zone.box.enabled) continue;
            var m=zone.box.transform.worldToLocalMatrix;
            var p=m.MultiplyPoint3x4(position)-zone.box.center;
            var half=zone.box.size*.5f;
            // Match server: horizontal site boundary; 0.75m vertical foot/root tolerance.
            if (Mathf.Abs(p.x)<=half.x+Mathf.Abs(m.m01)*.75f+.001f &&
                Mathf.Abs(p.y)<=half.y+Mathf.Abs(m.m11)*.75f+.001f &&
                Mathf.Abs(p.z)<=half.z+Mathf.Abs(m.m21)*.75f+.001f) return true;
        }
        return false;
    }
}
