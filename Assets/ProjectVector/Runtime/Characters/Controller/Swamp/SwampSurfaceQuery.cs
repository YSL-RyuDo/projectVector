using System;
using UnityEngine;

// Shared visibility rule for the ground mesh and slow: solid cover, not actors, bounds a swamp.
public static class SwampSurfaceQuery
{
    public static bool IsActor(Collider c) => c.GetComponentInParent<CharacterController>() || c.GetComponentInParent<PlayerIdentity>() || c.GetComponentInParent<SwampTrap>();
    public static bool Blocked(Vector3 floorOrigin,Vector3 floorTarget,out RaycastHit blocker)
    {
        Vector3 from=floorOrigin+Vector3.up*.35f,to=floorTarget+Vector3.up*.35f;
        Vector3 delta=to-from;blocker=default;
        if(delta.sqrMagnitude<.0001f)return false;
        var hits=Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits){if(IsActor(hit.collider))continue;blocker=hit;return true;}
        return false;
    }
    public static Vector3 Foot(PlayerMovement player)
    {
        var cc=player.GetComponent<CharacterController>();
        return player.transform.position+Vector3.up*(cc?(cc.center.y-cc.height*.5f)*player.transform.lossyScale.y:0);
    }
}
