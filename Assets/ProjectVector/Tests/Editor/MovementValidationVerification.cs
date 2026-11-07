using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MovementValidationVerification
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var net=new GameObject("CorrectionNetwork").AddComponent<NetworkManager>(); NetworkManager.Instance=net;
        var actor=new GameObject("CorrectionActor");var identity=actor.AddComponent<PlayerIdentity>();identity.PlayerId=7;
        var hp=actor.AddComponent<PlayerHP>();hp.SetHp(100);var cc=actor.AddComponent<CharacterController>();
        typeof(NetworkManager).GetField("myPlayerId",F).SetValue(net,(uint)7);
        var apply=typeof(NetworkManager).GetMethod("HandleMovementCorrection",F);
        byte[] packet=new byte[13];packet[0]=1;
        Array.Copy(BitConverter.GetBytes(2f),0,packet,1,4);Array.Copy(BitConverter.GetBytes(3f),0,packet,5,4);Array.Copy(BitConverter.GetBytes(4f),0,packet,9,4);
        actor.transform.rotation=Quaternion.Euler(0,55,0);apply.Invoke(net,new object[]{packet});
        if(actor.transform.position!=new Vector3(2,3,4)||!cc.enabled||hp.CurrentHP!=100||Mathf.Abs(actor.transform.eulerAngles.y-55)>.01f)throw new Exception("Correction reset unrelated state");
        typeof(NetworkManager).GetField("movementRound",F).SetValue(net,(byte)2);
        apply.Invoke(net,new object[]{packet});
        if(net.MovementCorrections!=1)throw new Exception("Stale round correction applied");
        packet[0]=2;Array.Copy(BitConverter.GetBytes(float.NaN),0,packet,1,4);apply.Invoke(net,new object[]{packet});
        if(net.MovementCorrections!=1)throw new Exception("NaN correction applied");
        Array.Copy(BitConverter.GetBytes(2f),0,packet,1,4);
        typeof(NetworkManager).GetField("<ReconnectBusy>k__BackingField",F).SetValue(net,true);apply.Invoke(net,new object[]{packet});
        if(net.MovementCorrections!=1)throw new Exception("Correction interrupted reconnect snapshot");
        typeof(NetworkManager).GetField("<ReconnectBusy>k__BackingField",F).SetValue(net,false);hp.SetHp(0);apply.Invoke(net,new object[]{packet});
        if(net.MovementCorrections!=1)throw new Exception("Correction moved dead actor");
        UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(net.gameObject);
        Debug.Log("MOVEMENT_CORRECTION_PASS: accepted position only; rotation/health/controller retained; stale round, NaN, dead, reconnect ignored");
    }
}
