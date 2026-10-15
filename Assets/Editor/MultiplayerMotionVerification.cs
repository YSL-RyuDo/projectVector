#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MultiplayerMotionVerification
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static RemoteMotionBuffer.Pose Pose(double t,float x,float yaw=0) => new RemoteMotionBuffer.Pose {time=t,position=new Vector3(x,0,0),yaw=yaw};
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Batch only");
        var report=new StringBuilder();
        foreach(int fps in new[]{30,60,144})
        {
            var buffer=new RemoteMotionBuffer();
            int next=0;float previous=-1;float maxStep=0;
            // 20 Hz source, 80 ms latency, up to 40 ms jitter; some TCP bursts.
            for(int frame=0;frame<fps*4;frame++)
            {
                double now=frame/(double)fps;
                while(next<=60)
                {
                    double sent=next*.05;
                    double arrival=sent+.08+(next%4==0?.04:0);
                    if(arrival>now) break;
                    buffer.Push(Pose(sent,(float)Math.Min(sent*5,10)),now);next++;
                }
                if(!buffer.Sample(now,out var p)) continue;
                Check(p.position.x+0.001f>=previous,"Position moved backwards under jitter");
                if(previous>=0) maxStep=Mathf.Max(maxStep,p.position.x-previous);
                previous=p.position.x;
            }
            Check(Mathf.Abs(previous-10)<0.001f,"Stop endpoint drift");
            Check(maxStep<.7f,"Ordinary jitter produced teleport");
            report.AppendLine($"PASS {fps} FPS: 20 Hz / latency + jitter + bursts, monotonic motion, exact stop, max step={maxStep:F3}m");
        }
        var b=new RemoteMotionBuffer();
        Check(!b.Sample(0,out _),"Empty buffer moved actor before first packet");
        b.Push(Pose(1,1,359),10);b.Push(Pose(1.1,2,1),10.1);
        Check(!b.Push(Pose(1.05,99),10.11),"Stale packet accepted");
        b.Sample(10.15,out var mid);Check(Mathf.Abs(Mathf.DeltaAngle(mid.yaw,0))<.01f,"Yaw wrap took long path");
        b.Sample(99,out var stalled);Check(stalled.position.x==2,"Network stall extrapolated through cover");
        Check(!b.Push(Pose(double.NaN,9),100),"NaN accepted");
        b.Clear();b.Push(Pose(1,50),100);b.Sample(100,out var reset);Check(reset.position.x==50,"Respawn/reconnect retained old path");
        b.Push(Pose(1.1,100),100.1);b.Sample(100.1,out var teleport);Check(teleport.position.x==100,"Teleport glided across map");
        report.AppendLine("PASS empty buffer, stale/NaN rejection, yaw wrap, no extrapolation, respawn reset, teleport discontinuity");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var network=new GameObject("NetworkTest").AddComponent<NetworkManager>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(NetworkManager).GetField("myPlayerId",flags).SetValue(network,(uint)7);
        var receive=typeof(NetworkManager).GetMethod("HandleMoveState",flags);
        receive.Invoke(network,new object[]{new byte[1]});
        var self=new byte[34];Buffer.BlockCopy(BitConverter.GetBytes((uint)7),0,self,0,4);
        receive.Invoke(network,new object[]{self}); // Self echo exits before remote transform lookup.
        UnityEngine.Object.DestroyImmediate(network.gameObject);
        report.AppendLine("PASS truncated movement ignored and self echo ignored");
        var actor=new GameObject("ReloadLifecycleTest");
        var item=actor.AddComponent<PlayerItem>();
        typeof(PlayerItem).GetField("isReloading",flags).SetValue(item,true);
        typeof(PlayerItem).GetMethod("OnDisable",flags).Invoke(item,null);
        Check(!item.IsReloading,"Disabled component kept pending reload");
        var hp=actor.AddComponent<PlayerHP>();hp.SetHp(0);
        Check((bool)typeof(PlayerItem).GetMethod("WeaponActionBlocked",flags).Invoke(item,null),"Dead actor can start reload");
        item.SetReloadVisualFromServer(0,true);
        Check(!(bool)typeof(PlayerItem).GetField("remoteIsReloading",flags).GetValue(item),"Late reload resurrected dead presentation");
        UnityEngine.Object.DestroyImmediate(actor);
        report.AppendLine("PASS disable cancels reload, dead actions blocked, late reload visual ignored");
        string output=Path.Combine(Path.GetTempPath(),"VectorMultiplayerMotionVerification.txt");
        File.WriteAllText(output,report.ToString());Debug.Log("MULTIPLAYER_MOTION_PASS\n"+report);
    }
}
#endif
