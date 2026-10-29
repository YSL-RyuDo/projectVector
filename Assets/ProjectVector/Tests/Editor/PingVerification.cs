using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Pings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PingVerification
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    public static void Run()
    {
        Check(Application.isBatchMode,"Batch only");
        EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var managers=UnityEngine.Object.FindObjectsByType<PingManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Check(managers.Length==1,"Exactly one local ping HUD required");var m=managers[0];
        Check(m.NetworkMode&&m.canvasRoot&&m.wheel&&m.hudMap&&m.fullMap&&m.mapCamera,"Saved scene bindings missing");
        Check(m.confirmSound&&m.dangerSound&&m.markerPool.Length==6,"Saved sound/pool missing");
        foreach(var t in m.GetComponentsInChildren<UnityEngine.UI.Text>(true))Check(t.font&&t.font.name.Contains("Pretendard"),"Font not authored");
        int nodes=m.GetComponentsInChildren<Transform>(true).Length;Call(m,"Awake");m.ConfigureNetwork(true,0);
        Call(m.GetComponent<PingBridge>(),"Awake");
        var mask=(LayerMask)typeof(PingSystem).GetField("hitMask",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m.GetComponent<PingSystem>());
        Check((mask.value&(1<<LayerMask.NameToLayer("MiniMap")))!=0,"TestMap world geometry was excluded from aim ray");
        m.wheel.Initialize(m.Options,170,52);
        for(int i=0;i<4;i++)
        {
            m.wheel.Open();float a=i*90*Mathf.Deg2Rad;m.wheel.Move(new Vector2(Mathf.Sin(a),Mathf.Cos(a))*100);
            Check(m.wheel.SelectedIndex==i,"Wheel direction incorrect");
        }
        m.wheel.Open();Check(m.wheel.SelectedIndex==-1,"Center must cancel");m.wheel.Close();
        Check(m.ReceiveConfirmed(new ConfirmedPing("1",1,0,PingType.Danger,new Vector3(1,2,3))),"Own team rejected");
        Check(!m.ReceiveConfirmed(new ConfirmedPing("1",1,0,PingType.Danger,Vector3.one)),"Duplicate accepted");
        Check(!m.ReceiveConfirmed(new ConfirmedPing("2",2,1,PingType.Attack,Vector3.one)),"Enemy marker leak");
        Check(!m.ReceiveConfirmed(new ConfirmedPing("3",2,0,PingType.Attack,new Vector3(float.NaN,0,0))),"Nonfinite accepted");
        Check(m.ReceiveConfirmed(new ConfirmedPing("4",1,0,PingType.Defend,Vector3.one))&&m.ActiveMarkerCount==1,"Sender markers stacked");
        Check(m.ReceiveConfirmed(new ConfirmedPing("5",2,0,PingType.Attack,Vector3.one))&&m.ActiveMarkerCount==2,"Different ally not shown");
        m.ConfigureNetwork(true,1);Check(m.ActiveMarkerCount==0,"Team/session change retained markers");
        var n=new GameObject("PacketTest").AddComponent<NetworkManager>();int received=0;n.TeamPingReceived+=p=>{received++;Check(p.SenderId==10&&p.TeamId==1&&p.Type==PingType.Defend&&p.Position.x==4,"Packet mismatch");};
        using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
        {w.Write((ulong)99);w.Write((uint)10);w.Write((byte)1);w.Write((byte)3);w.Write((ushort)3000);w.Write(4f);w.Write(5f);w.Write(6f);Call(n,"HandleTeamPing",stream.ToArray());}
        Call(n,"HandleTeamPing",new byte[27]);Check(received==1,"Malformed/valid packet count");UnityEngine.Object.DestroyImmediate(n.gameObject);
        Check(nodes==m.GetComponentsInChildren<Transform>(true).Length,"Runtime created UI hierarchy");
        Debug.Log("TEAM_PING_CLIENT_PASS: authored scene/pool/font, wheel directions/cancel, team filtering, duplicates, replacement, session cleanup and packet decode");
    }
    public static void Build(){Run();RankedCaptureTestEditor.Build();}
}
