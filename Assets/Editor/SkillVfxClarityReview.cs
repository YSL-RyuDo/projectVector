#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Offline, deterministic capture of the production prefabs and VFX components.
// Calls presentation confirmation directly: this is NOT a multiplayer recording.
public static class SkillVfxClarityReview
{
    const string Output="C:/Users/GCT/Downloads/Vector_VFX_Clarity_20261022";
    public static void Before()=>Capture("before");
    public static void After()=>Capture("after");
    static GameObject Actor(string name,Vector3 position,Vector3 forward)
    {
        var a=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{name}/{name}.prefab"));
        a.transform.SetPositionAndRotation(position,Quaternion.LookRotation(forward));
        foreach(Transform child in a.transform)if(child.name!="Model"&&child.name!="EMP"&&child.name!="ElectricStatusFX")child.gameObject.SetActive(false);
        var root=a.GetComponent<Renderer>();if(root)root.enabled=false;
        foreach(var c in a.GetComponentsInChildren<Camera>(true))c.enabled=false;
        var animator=a.transform.Find("Model").GetComponentInChildren<Animator>();
        string idle=name=="Sentinel"?"Sentinel_Lobby_Idle":name+"_Idle";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Character Animation/{name}/Lobby/{idle}.anim");
        if(animator&&clip)clip.SampleAnimation(animator.gameObject,.3f);
        return a;
    }
    static void Capture(string version)
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch editor only");
        foreach(string mapName in new[]{"TestMap","Desert"})
        foreach(string skill in new[]{"Drone","EMP"})
        foreach(string view in new[]{"Caster","Victim","Observer"})
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{mapName}.prefab"));
            var info=map.GetComponent<MapInfo>();if(info.barriers)info.barriers.SetActive(false);
            var spawn=info.GetSpawnPoint(0,0);Vector3 origin=spawn.position+Vector3.up*.04f;
            Vector3 forward=info.GetSpawnPoint(1,0).position-spawn.position;forward.y=0;forward.Normalize();
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            var caster=Actor("Initiator",origin,forward);
            var victim=Actor("Sentinel",origin+forward*4+right*1.1f,-forward);
            var other=Actor("Entry",origin+forward*5-right*1.5f,-forward);
            var camera=new GameObject("OfflineCaptureCamera").AddComponent<Camera>();
            camera.nearClipPlane=.03f;camera.farClipPlane=400;camera.fieldOfView=65;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.16f,.21f);
            if(view=="Caster"){
                caster.transform.Find("Model").gameObject.SetActive(false);
                camera.transform.position=origin+Vector3.up*1.7f;
                camera.transform.LookAt(victim.transform.position+Vector3.up*1.2f);
            }else if(view=="Victim"){
                victim.transform.Find("Model").gameObject.SetActive(false);
                camera.transform.position=victim.transform.position+Vector3.up*1.65f;
                camera.transform.LookAt(origin+Vector3.up*1.3f);
            }else{
                camera.transform.position=origin-forward*8+right*7+Vector3.up*7;
                camera.transform.LookAt(origin+forward*2+Vector3.up*.6f);
            }
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.62f;
            var sun=new GameObject("CaptureSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.4f;sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var drone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Drone.prefab"));
            PrefabUtility.UnpackPrefabInstance(drone,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var c in drone.GetComponentsInChildren<Camera>(true))c.enabled=false;
            drone.transform.position=origin+forward*1.8f+Vector3.up*1.6f-right*.65f;
            var end=victim.transform.position+Vector3.up*1.1f;drone.transform.LookAt(end);
            drone.SetActive(skill=="Drone");
            var droneFx=drone.GetComponent<DroneCombatVfx>();var emp=caster.GetComponent<EmpCastVfx>();
            var status=victim.GetComponent<ElectricStatusVfx>();
            var particles=Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var p in particles)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            string folder=$"{Output}/{version}/{skill}-{mapName}-{view}";Directory.CreateDirectory(folder);
            var rt=new RenderTexture(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);camera.targetTexture=rt;
            for(int frame=0;frame<120;frame++){
                if(skill=="Drone"){
                    if(frame==24)droneFx.Fire(drone.transform.position+drone.transform.forward*.35f,end);
                    if(frame==27)droneFx.ConfirmImpact(end,-forward);
                    // Second discharge deliberately misses: no confirmed impact.
                    if(frame==60)droneFx.Fire(drone.transform.position+drone.transform.forward*.35f,end+right*1.2f);
                    if(frame==88){droneFx.BreakApart();drone.transform.Find("drone").gameObject.SetActive(false);}
                    droneFx.Tick(1f/30);
                }else{
                    // Only the presentation fixture state is supplied here.
                    // Edit mode cannot run the live server-status coroutine.
                    if(frame==24){emp.Confirm(origin,new[]{end,other.transform.position+Vector3.up*1.1f});SetStun(victim,true);}
                    if(frame==84)SetStun(victim,false);
                    emp.Tick(1f/30);status.Tick(1f/30);
                }
                foreach(var p in particles)if(p&&p.isPlaying){p.Simulate(1f/30,false,false);p.Play(false);}
                camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();
                File.WriteAllBytes(folder+"/"+frame.ToString("D4")+".jpg",tex.EncodeToJPG(90));
            }
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
            Debug.Log("VFX_CAPTURE_SEGMENT "+version+" "+skill+" "+mapName+" "+view);
        }
        Debug.Log("VFX_CAPTURE_PASS "+version);
    }
    static void SetStun(GameObject actor,bool value)=>typeof(ElectricShock).GetField("<IsStunned>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(actor.GetComponent<ElectricShock>(),value);
}
#endif
