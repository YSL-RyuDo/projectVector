#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShotConsistencyVerification
{
    static readonly string Output = Path.Combine(Path.GetTempPath(), "VectorShotVerification-20261015");
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static GameObject Box(string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        go.transform.position=pos; go.transform.localScale=size; return go;
    }
    static Bullet Shot(PlayerIdentity owner, Vector3 origin, Vector3 dir, float speed=880, float range=100)
    {
        var go=new GameObject("VerificationBullet"); go.transform.position=origin;
        go.AddComponent<SphereCollider>().radius=0.05f;
        go.AddComponent<Rigidbody>();
        var bullet=go.AddComponent<Bullet>(); bullet.Init(owner,range,speed,dir,false);
        return bullet;
    }
    public static void Run()
    {
        Check(Application.isBatchMode,"Run in isolated batch mode");
        VerifyMergedHitboxes();
        Directory.CreateDirectory(Output);
        var report=new StringBuilder();
        var fixtures=new StringBuilder();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var owner=new GameObject("Shooter").AddComponent<PlayerIdentity>(); owner.PlayerId=1; owner.Team=0;
            var target=Box("EnemyHead",new Vector3(0,0,5),Vector3.one);
            var identity=target.AddComponent<PlayerIdentity>(); identity.PlayerId=2; identity.Team=1;
            uint shotId=100;
            Action<string,Bullet,bool> verify=(name,bullet,expected)=>
            {
                for(int step=0;step<600 && !bullet.HasFinished;step++) bullet.Advance(1f/60);
                bool actual=bullet.LastImpactCollider!=null && bullet.LastImpactCollider.GetComponentInParent<PlayerIdentity>()==identity;
                Check(actual==expected,name+" wrong impact target");
                bool head=actual && WeaponShotTrace.IsHead(bullet.LastImpactCollider);
                fixtures.AppendLine($"{name} {++shotId} {(actual?1:0)} {(head?1:0)}");
                report.AppendLine($"PASS {name}: target={(actual?"enemy":"none/cover")}, point={bullet.LastImpactPoint:F4}");
                UnityEngine.Object.DestroyImmediate(bullet.gameObject);
            };
            target.transform.position=new Vector3(0,0,0.65f);
            verify("close_enemy",Shot(owner,Vector3.zero,Vector3.forward),true);
            target.transform.position=Vector3.zero;
            verify("origin_inside_enemy",Shot(owner,Vector3.zero,Vector3.forward),true);
            target.transform.position=new Vector3(0,0,5);
            var wall=Box("ThinWall",new Vector3(0,0,2),new Vector3(1,3,0.002f));
            verify("thin_wall",Shot(owner,Vector3.zero,Vector3.forward),false);
            wall.transform.position=Vector3.zero;
            verify("origin_inside_wall",Shot(owner,Vector3.zero,Vector3.forward),false);
            wall.transform.localScale=new Vector3(1,3,1);wall.transform.position=new Vector3(0.52f,0,2);
            verify("corner_clear_2cm",Shot(owner,Vector3.zero,Vector3.forward),true);
            wall.transform.position=new Vector3(0.49f,0,2);
            verify("corner_blocked_1cm",Shot(owner,Vector3.zero,Vector3.forward),false);
            UnityEngine.Object.DestroyImmediate(wall);
            var friendly=Box("Friendly",new Vector3(0,0,2),Vector3.one);
            var friendId=friendly.AddComponent<PlayerIdentity>();friendId.PlayerId=3;friendId.Team=0;
            Check(WeaponShotTrace.FirstHit(new Ray(Vector3.zero,Vector3.forward),100,~0,owner,owner.transform,out var aimed) && aimed.collider.gameObject==target,"hitscan friendly policy differs");
            verify("friendly_in_front",Shot(owner,Vector3.zero,Vector3.forward),true);
            UnityEngine.Object.DestroyImmediate(friendly);
            target.transform.position=new Vector3(0,0,10);
            var moving=Shot(owner,Vector3.zero,Vector3.forward,100);moving.Advance(0.02f);
            target.transform.position=new Vector3(5,0,10);
            verify("target_moves_out",moving,false);
            moving=Shot(owner,Vector3.zero,Vector3.forward,100);moving.Advance(0.02f);
            target.transform.position=new Vector3(0,0,10);
            verify("target_moves_in",moving,true);
            verify("range_limit",Shot(owner,Vector3.zero,Vector3.forward,880,5),false);
            foreach(float dt in new[]{1f/30,1f/60,1f/144})
            {
                var bullet=Shot(owner,Vector3.zero,Vector3.forward);
                for(int n=0;n<600&&!bullet.HasFinished;n++)bullet.Advance(dt);
                Check(bullet.LastImpactCollider!=null && Mathf.Abs(bullet.LastImpactPoint.z-9.5f)<0.001f,"frame-rate dependent impact");
                bullet.Advance(dt);Check(bullet.LastImpactCollider.gameObject==target,"duplicate changed impact");
                UnityEngine.Object.DestroyImmediate(bullet.gameObject);
            }
            var visual=Shot(owner,new Vector3(0.4f,-0.2f,0.2f),Vector3.forward);
            visual.InitVisual(new Vector3(0,0,9.5f),Vector3.back,false,1000);
            visual.Advance(1);Check(visual.transform.position==new Vector3(0,0,9.5f),"hitscan tracer missed resolved endpoint");
            Check(visual.LastImpactCollider==null,"visual tracer produced a second hit");
            report.AppendLine("PASS 30/60/144Hz sweep, duplicate contact, hitscan visual endpoint (offset muzzle)");
            UnityEngine.Object.DestroyImmediate(visual.gameObject);
            var network=new GameObject("ConfirmationVerification").AddComponent<NetworkManager>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var pending=(Dictionary<ulong,(uint target,float time)>)typeof(NetworkManager).GetField("pendingTrackedHits",flags).GetValue(network);
            var confirm=typeof(NetworkManager).GetMethod("HandleShotConfirmed",flags);
            byte[] body=new byte[18];
            Buffer.BlockCopy(BitConverter.GetBytes((uint)7),0,body,0,4);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)2),0,body,4,4);
            Buffer.BlockCopy(BitConverter.GetBytes(20),0,body,12,4);
            int feedback=0;Action onHit=()=>feedback++;HitManager.OnEnemyHit+=onHit;
            try
            {
                confirm.Invoke(network,new object[]{body});Check(feedback==0,"unrequested confirmation shown");
                pending[7UL<<8]=(99,Time.unscaledTime);confirm.Invoke(network,new object[]{body});Check(feedback==0,"wrong target confirmation shown");
                pending[7UL<<8]=(2,Time.unscaledTime);confirm.Invoke(network,new object[]{body});Check(feedback==1,"accepted damage missing feedback");
                confirm.Invoke(network,new object[]{body});Check(feedback==1,"duplicate confirmation shown twice");
                pending[7UL<<8]=(2,Time.unscaledTime-6);confirm.Invoke(network,new object[]{body});Check(feedback==1,"expired confirmation shown");
            }
            finally{HitManager.OnEnemyHit-=onHit;UnityEngine.Object.DestroyImmediate(network.gameObject);}
            report.AppendLine("PASS server-confirmed hit feedback: matching shot/target only, no unsolicited/duplicate/expired marker");
            foreach(string map in new[]{"TestMap","Desert"})
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/"+map+".prefab"));
                root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var info=root.GetComponent<MapInfo>();if(info.barriers!=null)info.barriers.SetActive(false);
                int checks=0;
                foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
                for(int yaw=0;yaw<360;yaw+=30)
                {
                    Vector3 origin=spawn.position+Vector3.up*1.3f;
                    Vector3 dir=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                    if(WeaponShotTrace.EnclosingCollider(origin,~0,null,null)!=null)continue;
                    if(!WeaponShotTrace.FirstHit(new Ray(origin,dir),200,~0,null,null,out var hit))continue;
                    var bullet=Shot(null,origin,dir,880,200);
                    for(int n=0;n<100&&!bullet.HasFinished;n++)bullet.Advance(1f/60);
                    Check(bullet.LastImpactCollider==hit.collider && Vector3.Distance(bullet.LastImpactPoint,hit.point)<0.01f,map+" ray/projectile mismatch");
                    checks++;UnityEngine.Object.DestroyImmediate(bullet.gameObject);
                }
                Check(checks>=6,map+" insufficient actual-map geometry checks");
                report.AppendLine($"PASS {map}: {checks} actual-map camera ray / projectile impact comparisons");
            }
            report.AppendLine("CLIENT_PASS: geometry and flight tested; no server rewind/lag compensation claimed.");
        }
        finally
        {
            File.WriteAllText(Path.Combine(Output,"client-results.txt"),report.ToString());
            File.WriteAllText(Path.Combine(Output,"impact-fixtures.txt"),fixtures.ToString());
        }
    }

    static void VerifyMergedHitboxes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string role in new[] { "Controller", "Entry", "Initiator", "Sentinel" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{role}/{role}.prefab");
            var parts = prefab.GetComponentsInChildren<HitCollider>(true);
            foreach (HitPart part in Enum.GetValues(typeof(HitPart)))
                Check(parts.Any(p => p.Part == part), role + " missing authored " + part);
        }
        var actor = Box("MovementCapsuleFixture", new Vector3(0, 0, 5), Vector3.one * 3);
        actor.AddComponent<PlayerIdentity>().PlayerId = 2;
        var region = Box("MisleadingHeadName", new Vector3(0, 0, 5), Vector3.one);
        region.transform.SetParent(actor.transform, true);
        var marker = region.AddComponent<HitCollider>();
        foreach (HitPart part in Enum.GetValues(typeof(HitPart)))
        {
            typeof(HitCollider).GetField("hitPart", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(marker, part);
            byte expected = part == HitPart.Head ? (byte)1 : part == HitPart.Legs ? (byte)2 : (byte)0;
            Check(WeaponShotTrace.GetHitZone(region.GetComponent<Collider>()) == expected, "Authored/wire region mapping");
            Check(WeaponShotTrace.FirstHit(new Ray(Vector3.zero, Vector3.forward), 10, ~0, null, null, out var hit) &&
                hit.collider == region.GetComponent<Collider>(), "Movement capsule masked an authored hitbox");
        }
        var wall = Box("Cover", new Vector3(0, 0, 2), Vector3.one);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Weapon" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var weapon in prefab.GetComponentsInChildren<Weapon>(true))
            {
                int mask = new SerializedObject(weapon).FindProperty("hitMask").intValue;
                Check(WeaponShotTrace.FirstHit(new Ray(Vector3.zero, Vector3.forward), 10, mask, null, null, out var hit) &&
                    hit.collider == wall.GetComponent<Collider>(), prefab.name + " weapon mask shoots through cover");
            }
        }
        UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(actor);
        var net = new GameObject("HitRegionWireFixture").AddComponent<NetworkManager>();
        var transport = (NetworkClient)typeof(NetworkManager).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(net);
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        System.Net.Sockets.TcpClient peer = null;
        try
        {
            listener.Start(); transport.Connect("127.0.0.1", ((System.Net.IPEndPoint)listener.LocalEndpoint).Port);
            peer = listener.AcceptTcpClient(); peer.GetStream().ReadTimeout = 3000;
            for (byte part = 0; part < 3; part++)
            {
                net.SendTrackedHit(2, part, 123, 4);
                var packet = new byte[14]; int count = 0;
                while (count < packet.Length) { int n = peer.GetStream().Read(packet, count, packet.Length - count); Check(n > 0, "Wire closed"); count += n; }
                Check(BitConverter.ToUInt16(packet, 0) == 14 && packet[8] == part &&
                    BitConverter.ToUInt32(packet, 9) == 123 && packet[13] == 4, "Merged tracked-hit wire layout");
            }
        }
        finally { transport.Close(); peer?.Close(); listener.Stop(); UnityEngine.Object.DestroyImmediate(net.gameObject); }
        Debug.Log("MERGED_HITBOX_PASS: four authored characters, body/head/legs wire mapping, movement capsule filtering, weapon cover masks, real TCP tracked hit payload");
    }
}
#endif
