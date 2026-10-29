#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class EmpGestureVerification
{
    public static void RunAll(){Run();DronePolishVerification.Run();}
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("EMP_GESTURE_CHECK "+message);}
    static void Save(Camera c,string name)
    {
        // Several poses are sampled in one editor frame. Bake the current bone
        // matrices so the preview does not reuse the first render's skin cache.
        var skins=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Where(s=>s.enabled).ToArray();
        var previews=new System.Collections.Generic.List<GameObject>();
        var meshes=new System.Collections.Generic.List<Mesh>();
        foreach(var skin in skins){
            var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
            var go=new GameObject("SampledSkin",typeof(MeshFilter),typeof(MeshRenderer));
            // BakeMesh already contains the skin's scale; do not apply it twice.
            go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.layer=skin.gameObject.layer;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            skin.enabled=false;previews.Add(go);
        }
        var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        Directory.CreateDirectory(EmpPolishAuthoring.Output);File.WriteAllBytes(EmpPolishAuthoring.Output+"/"+name+".png",image.EncodeToPNG());
        c.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        foreach(var go in previews)Object.DestroyImmediate(go);
        foreach(var mesh in meshes)Object.DestroyImmediate(mesh);
        foreach(var skin in skins)skin.enabled=true;
    }
    public static void Run()
    {
        Check(Application.isBatchMode,"isolated editor");EmpPolishAuthoring.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab"));
        var rig=actor.GetComponent<EmpGestureRig>();var skill=actor.GetComponent<InitiatorSkill_E>();
        var poses=(EmpGestureRig.Pose[])typeof(EmpGestureRig).GetField("_poses",Flags).GetValue(rig);
        Check(poses.Length==2 && poses.All(p=>p.bones.Length==4 && p.fingers.Length>5),"first and third person saved arm and open hand poses");
        Check(!actor.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="EMPWristEmitter"),"no wrist device attached");
        var original=poses.SelectMany(p=>p.bones).Select(t=>t.localRotation).ToArray();
        rig.Begin();rig.Advance(.24f);Check(!rig.Ready && skill.BlocksActions,"no cast before palm release");
        Check(!GameplayActionPolicy.CanAct(actor.GetComponent<PlayerItem>(),GameplayAction.Fire,false),"fire blocked during gesture");
        Check(!GameplayActionPolicy.CanAct(actor.GetComponent<PlayerItem>(),GameplayAction.Reload,false),"reload blocked during gesture");
        Check(!GameplayActionPolicy.CanAct(actor.GetComponent<PlayerItem>(),GameplayAction.WeaponChange,false),"weapon input deferred during gesture");
        rig.Advance(.26f);Check(rig.Ready,"release event at half a second");rig.Release();rig.Advance(.4f);
        Check(!skill.BlocksActions && !rig.Active,"weapon action unlocks after recovery");
        Check(poses.SelectMany(p=>p.bones).Select((t,i)=>Quaternion.Angle(t.localRotation,original[i])<.01f).All(v=>v),"all bones restored after release");
        rig.Begin();rig.Advance(.15f);rig.Cancel();rig.Advance(.4f);Check(!rig.Active && !poses.Any(p=>p.energy.enabled),"cancel clears charge and pose");
        rig.Begin();rig.Advance(.2f);skill.ResetCast();Check(!rig.Active,"death and round reset clear gesture");
        rig.Begin();rig.Advance(3.1f);Check(!rig.Active,"missing remote result times out safely");
        var full=actor.transform.Find("Model");full.gameObject.SetActive(false);
        var rootRenderer=actor.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        var camera=actor.GetComponentInChildren<Camera>(true);camera.enabled=true;camera.cullingMask=~0;camera.fieldOfView=90;camera.nearClipPlane=.03f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.10f,.14f);
        var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
        var neutralShoulder=poses[0].shoulder.position+camera.transform.TransformVector(new Vector3(-.34f,-.10f,.13f));
        var weaponIdle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/AR.anim");
        weaponIdle.SampleAnimation(model.gameObject,0);
        var shoulder=poses[0].shoulder.position;
        var motion=actor.GetComponent<FirstPersonSkillMotion>();
        var motionBones=(Transform[])typeof(FirstPersonSkillMotion).GetField("_bones",Flags).GetValue(motion);
        foreach(var kind in new[]{FirstPersonSkillMotion.Motion.Emp,FirstPersonSkillMotion.Motion.Drone}){
            weaponIdle.SampleAnimation(model.gameObject,0);
            var baseRotations=motionBones.Select(t=>t.localRotation).ToArray();
            var basePositions=motionBones.Select(t=>t.localPosition).ToArray();
            motion.Apply(kind,.16f,1);var authored=motionBones.Select(t=>t.localRotation).ToArray();
            motion.Restore(kind);
            Check(motionBones.Select((t,i)=>Quaternion.Angle(t.localRotation,baseRotations[i])<.03f && Vector3.Distance(t.localPosition,basePositions[i])<.0001f).All(v=>v),kind+" restores full firearm skeleton");
            AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/Pistol.anim").SampleAnimation(model.gameObject,0);
            motion.Apply(kind,.16f,1);
            Check(motionBones.Select((t,i)=>Quaternion.Angle(t.localRotation,authored[i])<.03f).All(v=>v),kind+" pose independent of rifle or pistol");
            motion.Restore(kind);
        }
        weaponIdle.SampleAnimation(model.gameObject,0);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))
        {
            if(r.name=="HeadUV"||r.name=="THair6UV")r.enabled=false;
            if(r.name=="Arm1"||r.name=="Arm2"){r.gameObject.SetActive(true);r.enabled=true;r.forceRenderingOff=false;}
        }
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.6f;
        var sun=new GameObject("ReviewLight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.transform.rotation=Quaternion.Euler(40,-30,0);
        Save(camera,"EMP-Weapon-Base");
        rig.Begin();rig.Advance(.34f);Save(camera,"EMP-Hand-Gather");rig.Advance(.16f);
        Save(camera,"EMP-Hand-Release");
        foreach(float fov in new[]{60f,90f,110f})
        {
            rig.ResetPresentation();camera.fieldOfView=fov;rig.Begin();rig.Advance(.5f);
            var hand=camera.WorldToViewportPoint(poses[0].hand.position);
            Check(hand.z>.05f && hand.x>.1f && hand.x<.8f && hand.y>.05f && hand.y<.8f,"hand stays in frame at FOV "+fov);
            Check(Vector3.Distance(poses[0].shoulder.position,neutralShoulder)<.001f,"authored neutral shoulder position at FOV "+fov);
        }
        rig.ResetPresentation();camera.fieldOfView=90;
        weaponIdle.SampleAnimation(model.gameObject,0);
        var drone=actor.GetComponent<DroneLaunchRig>();
        typeof(DroneLaunchRig).GetMethod("Awake",Flags).Invoke(drone,null);
        drone.SetPrepared(true);
        for(int i=0;i<100;i++)typeof(DroneLaunchRig).GetMethod("Advance",Flags).Invoke(drone,new object[]{1f/120});
        Save(camera,"Drone-Hand-Revised");drone.ResetPresentation();
        camera.fieldOfView=60;weaponIdle.SampleAnimation(model.gameObject,0);
        drone.SetPrepared(true);
        for(int i=0;i<100;i++)typeof(DroneLaunchRig).GetMethod("Advance",Flags).Invoke(drone,new object[]{1f/120});
        Save(camera,"Drone-Hand-GameplayFov");drone.ResetPresentation();
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));
        var info=map.GetComponent<MapInfo>();if(info.barriers)info.barriers.SetActive(false);
        var spawn=info.GetSpawnPoint(0,0);actor.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
        weaponIdle.SampleAnimation(model.gameObject,0);drone.SetPrepared(true);
        for(int i=0;i<100;i++)typeof(DroneLaunchRig).GetMethod("Advance",Flags).Invoke(drone,new object[]{1f/120});
        Save(camera,"Drone-Authored-TestMap");drone.ResetPresentation();
        rig.Begin();rig.Advance(.5f);Save(camera,"EMP-Authored-TestMap");rig.ResetPresentation();
        Object.DestroyImmediate(map);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        model.gameObject.SetActive(false);full.gameObject.SetActive(true);
        var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character Animation/Initiator/Lobby/Initiator_Idle.anim");var animator=full.GetComponentInChildren<Animator>();if(idle&&animator)idle.SampleAnimation(animator.gameObject,.3f);
        rig.Begin();rig.Advance(.5f);
        camera.transform.position=actor.transform.position+new Vector3(2,1.6f,3);camera.transform.LookAt(actor.transform.position+Vector3.up*1.1f);camera.fieldOfView=45;
        Save(camera,"EMP-Hand-ThirdPerson");rig.ResetPresentation();
        Debug.Log("EMP_GESTURE_PASS: no device, two rigs, release timing, weapon gates, recovery, cancellation, reset and timeout");
    }
}
#endif
