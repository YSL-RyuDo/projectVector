#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DroneAltitudeVerification
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("DRONE_ALTITUDE_CHECK "+message);}
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
    public static void RunAndBuild(){Run();RankedCaptureTestEditor.Build();}
    public static void MeasureGeometry()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(string character in new[]{"Initiator","Controller","Entry","Sentinel"}){
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{character}/{character}.prefab"));
            var cc=actor.GetComponent<CharacterController>();
            var renderers=actor.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);Debug.Log($"DRONE_CHARACTER_SIZE {character} modelHeight={bounds.size.y:F3} capsule={cc.height*actor.transform.lossyScale.y:F3}");}
            Object.DestroyImmediate(actor);
        }
        foreach(string mapName in new[]{"TestMap","Desert"}){
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{mapName}.prefab"));Physics.SyncTransforms();
            foreach(var group in map.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger&&c.enabled&&c.bounds.size.y>.5f&&c.bounds.size.x>.3f&&c.bounds.size.z>.3f).GroupBy(c=>c.name))
                Debug.Log($"DRONE_COVER_SIZE {mapName} {group.Key} count={group.Count()} height={string.Join("/",group.Select(c=>c.bounds.size.y.ToString("F2")).Distinct())}");
            Object.DestroyImmediate(map);
        }
    }
    public static void Run()
    {
        Check(Application.isBatchMode,"isolated batch only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(30,1,30);
        var person=GameObject.CreatePrimitive(PrimitiveType.Capsule);person.AddComponent<PlayerIdentity>();person.transform.position=Vector3.up;
        var trigger=GameObject.CreatePrimitive(PrimitiveType.Cube);trigger.transform.position=Vector3.up*2.5f;trigger.GetComponent<Collider>().isTrigger=true;
        Physics.SyncTransforms();
        Check(DroneFlightSafety.TryGetGroundClearance(Vector3.up*4,null,out float clearance)&&Mathf.Abs(clearance-4)<.001f,"ground reading ignores player and trigger beneath drone");
        Check(!DroneFlightSafety.TryGetGroundClearance(new Vector3(100,4,100),null,out clearance)&&float.IsNaN(clearance),"no ground reports unavailable, not zero or launch-relative height");
        Object.DestroyImmediate(person);Object.DestroyImmediate(trigger);Object.DestroyImmediate(floor);
        var drone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Drone.prefab"));
        var movement=drone.GetComponent<DroneMovement>();Call(movement,"Awake");Set(movement,"_ceiling",100f);
        Check(Field<float>(movement,"_verticalSpeed")==2 && Field<float>(movement,"_moveSpeed")==8,"serialized vertical 2m/s, horizontal remains 8m/s");
        foreach(float direction in new[]{1f,-1f}){
            drone.transform.position=Vector3.up*20;Set(movement,"_currentVelocity",Vector3.zero);Set(movement,"_targetVelocity",Vector3.up*(2*direction));
            float peak=0;
            for(int i=0;i<100;i++){Call(movement,"Move");peak=Mathf.Max(peak,Mathf.Abs(movement.Velocity.y));drone.transform.position+=movement.Velocity*Time.fixedDeltaTime;}
            Check(Mathf.Abs(peak-2)<.001f,"vertical cap applies in direction "+direction);
            float stopStart=drone.transform.position.y;Set(movement,"_targetVelocity",Vector3.zero);
            for(int i=0;i<30;i++){Call(movement,"Move");drone.transform.position+=movement.Velocity*Time.fixedDeltaTime;}
            Check(Mathf.Abs(drone.transform.position.y-stopStart)<.27f&&Mathf.Abs(movement.Velocity.y)<.001f,"vertical release brakes within 27cm");
        }
        Object.DestroyImmediate(drone);
        foreach(string mapName in new[]{"TestMap","Desert"}){
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{mapName}.prefab"));
            var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);
            foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints)){
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab"));
                actor.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
                var weapon=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/AR.anim").SampleAnimation(weapon.gameObject,0);
                var rig=actor.GetComponent<DroneLaunchRig>();Call(rig,"Awake");rig.SetPrepared(true);
                for(int i=0;i<90;i++)Call(rig,"Advance",1f/120);
                Check(rig.TryGetReleasePosition(out var release),mapName+" ready launch endpoint");
                Physics.SyncTransforms();
                Check(DroneFlightSafety.TryGetGroundClearance(release,null,out clearance),mapName+" floor found at release");
                Debug.Log($"DRONE_MAP_MEASURE {mapName} {spawn.name} groundClearance={clearance:F3} worldY={release.y:F3}");
                Check(clearance>.5f && clearance<2.2f,mapName+" release below old elevated spawn and above floor");
                var overlaps=Physics.OverlapSphere(release,.48f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(actor.transform)).ToArray();
                Check(overlaps.Length==0,mapName+" release body has clear space: "+string.Join(",",overlaps.Select(c=>c.name)));
                rig.BeginLaunch();Call(rig,"Advance",rig.LaunchDuration-.00001f);
                Check(Vector3.Distance(Field<GameObject>(rig,"_device").transform.position,release)<.001f,mapName+" visible device endpoint matches network spawn");
                Vector3 elevated=release+Vector3.up;
                Check(DroneFlightSafety.TryGetGroundClearance(elevated,null,out float higher)&&Mathf.Abs(higher-clearance-1)<.002f,mapName+" altitude increases by actual 1m above ground");
                // Ground changes never increase the existing launch-relative safety limit.
                var ceiling=release.y+DroneFlightSafety.MaxClimb;
                var limited=DroneFlightSafety.Constrain(new Vector3(release.x,ceiling,release.z),Vector3.up,ceiling,.48f,null);
                Check(limited.y==0,mapName+" reduced 1.25m launch ceiling enforced");
                Vector3 flight=release;
                for(int i=0;i<1500;i++)flight+=DroneFlightSafety.Constrain(flight,Vector3.up*.04f,ceiling,.48f,null);
                Check(flight.y<=ceiling+.001f,mapName+" sustained ascent cannot exceed ceiling");
                Check(DroneFlightSafety.Constrain(flight,Vector3.down*.1f,ceiling,.48f,null).y<0,mapName+" descent remains available at ceiling");
                Object.DestroyImmediate(actor);
            }
            Object.DestroyImmediate(map);
        }
        SkillVfxClarityVerification.Run();
        Debug.Log("DRONE_ALTITUDE_PASS");
    }
}
#endif
