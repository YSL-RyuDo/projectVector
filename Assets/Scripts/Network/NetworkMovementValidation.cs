using System;
using UnityEngine;

public partial class NetworkManager
{
    byte movementRound=1;
    public int MovementCorrections { get; private set; }
    void HandleMovementCorrection(byte[] body)
    {
        if(body==null || body.Length!=13 || body[0]!=movementRound || ReconnectBusy || MyPlayer==null) return;
        var position=new Vector3(BitConverter.ToSingle(body,1),BitConverter.ToSingle(body,5),BitConverter.ToSingle(body,9));
        if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z)) return;
        if(MyPlayer.GetComponent<PlayerHP>()?.IsDead==true) return;
        var cc=MyPlayer.GetComponent<CharacterController>(); bool enabled=cc!=null && cc.enabled;
        if(enabled) cc.enabled=false;
        MyPlayer.transform.position=position;
        if(enabled) cc.enabled=true;
        MovementCorrections++;
        // No health/ammo/skill reset and no view rotation: only restore accepted position.
    }
}
