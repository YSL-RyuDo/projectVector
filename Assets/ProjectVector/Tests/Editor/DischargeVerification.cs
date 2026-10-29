#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class DischargeVerification
{
    public const string Output="C:/Users/GCT/Documents/DischargeReview-20261023";
    const float Warning=2f, Interval=.15f;
    static T Field<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("DISCHARGE_CHECK "+message);}
    static GameObject Load(string path)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static void Light()
    {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.6f;
        var sun=new GameObject("ReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.7f;sun.transform.rotation=Quaternion.Euler(48,-30,0);
    }
    static Camera CameraAt(Vector3 pos,Vector3 target)
    {
        var c=new GameObject("ReviewCamera").AddComponent<Camera>();c.transform.position=pos;c.transform.LookAt(target);c.fieldOfView=65;c.nearClipPlane=.02f;c.farClipPlane=400;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.29f,.39f,.48f);return c;
    }
    static void Save(Camera camera,string name)
    {
        Directory.CreateDirectory(Output);var rt=new RenderTexture(1600,900,24);var t=new Texture2D(1600,900,TextureFormat.RGB24,false);
        camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();
        File.WriteAllBytes(Output+"/"+name+".png",t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(t);
    }
    static GameObject ActorAt(string type,Vector3 p)
    {
        var actor=Load($"Assets/ProjectVector/Shared/Characters/Prefabs/{type}/{type}.prefab");actor.transform.position=p;
        foreach(Transform child in actor.transform)if(child.name!="Model")child.gameObject.SetActive(false);
        var rootR=actor.GetComponent<Renderer>();if(rootR)rootR.enabled=false;
        var model=actor.transform.Find("Model");var animator=model.GetComponentInChildren<Animator>();
        string clipName=type=="Sentinel"?"Sentinel_Lobby_Idle":type+"_Idle";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/ProjectVector/Shared/Characters/Animations/{type}/Lobby/{clipName}.anim");if(clip && animator)clip.SampleAnimation(animator.gameObject,.3f);
        return actor;
    }
    public static void Run()
    {
        DischargeAuthoring.Run();Check(Application.isBatchMode,"isolated saved-asset verification");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var device=Load(DischargeAuthoring.Dir+"/DischargeRelay.prefab");Check(device.GetComponentsInChildren<Collider>().Length==0,"relay VFX never obstructs shots or movement");
        Check(device.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial && r.sharedMaterial.shader.isSupported),"model materials supported");
        var camera=CameraAt(new Vector3(.9f,.65f,.85f),Vector3.zero);camera.fieldOfView=45;Save(camera,"Relay-model");
        VerifyHand();
        foreach(string mapName in new[]{"TestMap","Desert"})
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
            var map=Load($"Assets/ProjectVector/Shared/Maps/Prefabs/{mapName}.prefab");map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
            var effect=Load(DischargeAuthoring.Dir+"/DischargeSequence.prefab").GetComponent<DischargeVfx>();
            int samples=0;bool elevated=false;
            foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))for(int angle=0;angle<360;angle+=45)
            {
                Vector3 origin=spawn.position;effect.Begin(origin,Quaternion.Euler(0,angle,0)*Vector3.forward,18,Warning,Interval);effect.Advance(Warning-.05f);
                for(int i=0;i<7;i++){
                    Vector3 ground=effect.GroundPoints[i],hover=effect.RelayPoints[i];
                    Check(Vector3.Distance(ground,DischargeVfx.Ground(ground))<.06f,"ground projection stable "+mapName+" "+samples);
                    Check(hover.y-ground.y>=.34f && hover.y-ground.y<=3.81f,"relay height bounded by floor/ceiling");
                    if(Mathf.Abs(ground.y)>.2f)elevated=true;samples++;
                }
            }
            Check(samples>=336,"both team spawns / 8 directions "+mapName);
            Debug.Log($"DISCHARGE_MAP_PASS {mapName} points={samples} nonzeroTerrain={elevated}");
            // Pick a real spawn orientation with the longest visible corridor for the review.
            Vector3 bestOrigin=info.attackSpawnPoints[0].position,bestDir=Vector3.forward;float best=-1;
            foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))for(int angle=0;angle<360;angle+=15)
            {
                var dir=Quaternion.Euler(0,angle,0)*Vector3.forward;var end=DischargeVfx.Surface(spawn.position,spawn.position+dir*16);
                float length=Vector3.Distance(Vector3.ProjectOnPlane(end-spawn.position,Vector3.up),Vector3.zero);
                if(length>best){best=length;bestOrigin=spawn.position;bestDir=dir;}
            }
            effect.Begin(bestOrigin,bestDir,18,Warning,Interval);effect.Advance(Warning-.05f);
            Vector3 target=DischargeVfx.Surface(bestOrigin,bestOrigin+bestDir*7.5f);
            var victim=ActorAt("Sentinel",target);victim.transform.rotation=Quaternion.LookRotation(-bestDir);
            var caster=ActorAt("Initiator",bestOrigin);caster.transform.rotation=Quaternion.LookRotation(bestDir);
            var hud=Load(DischargeAuthoring.Hud);var ui=hud.GetComponent<DischargeDirector>();ui.ResetFeedback();
            camera=CameraAt(bestOrigin+Vector3.up*1.7f,bestOrigin+bestDir*9+Vector3.up*1.3f);
            var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
            caster.SetActive(false);Save(camera,mapName+"-A-warning");
            effect.Strike(2);effect.Hit(target+Vector3.up);effect.Advance(.035f);ui.ShowOwner(1,1);Save(camera,mapName+"-A-confirmed");
            ui.ResetFeedback();ui.ShowVictim();caster.SetActive(true);victim.SetActive(false);
            camera.transform.position=target+Vector3.up*1.65f;camera.transform.LookAt(bestOrigin+Vector3.up*1.3f);Save(camera,mapName+"-B-hit");
            ui.ResetFeedback();victim.SetActive(true);camera.transform.position=bestOrigin-bestDir*4+Vector3.up*7+Vector3.Cross(Vector3.up,bestDir)*7;camera.transform.LookAt(bestOrigin+bestDir*6+Vector3.up);Save(camera,mapName+"-C-observer");
            Film(effect,camera,ui,bestOrigin,bestDir,target,mapName);
            effect.Begin(bestOrigin,bestDir,18,Warning,Interval);effect.Advance(Warning);effect.Strike(2);effect.Hit(target+Vector3.up);effect.Advance(.035f);
            var beams=Field<LineRenderer[]>(effect,"_bolts");Check(beams[7].enabled && Vector3.Distance(beams[7].GetPosition(14),target+Vector3.up)<.001f,"confirmed impact ends at exact authoritative torso");
            effect.Advance(.3f);Check(beams.All(b=>!b.enabled),"strikes expire without white screen overlay");effect.Finish(true);Check(!Field<LineRenderer>(effect,"_boundary").enabled,"cancel clears warning");
            ui.ShowOwner(1,2);Check(Field<CanvasGroup>(ui,"_ownerNotice").gameObject.activeSelf,"owner confirmation visible");ui.Advance(2);Check(!Field<CanvasGroup>(ui,"_ownerNotice").gameObject.activeSelf,"owner feedback expires");
            ui.ShowVictim();ui.Advance(.3f);Check(!Field<CanvasGroup>(ui,"_victimPulse").gameObject.activeSelf && Field<CanvasGroup>(ui,"_victimNotice").gameObject.activeSelf,"short victim flash, longer readable notice");ui.ResetFeedback();
            Check(hud.GetComponent<ScannerFeedbackUI>() && hud.GetComponent<ElectricStatusUI>(),"existing scanner and EMP HUD preserved");
            Check(hud.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"saved feedback never captures input");
        }
        Debug.Log("DISCHARGE_VERIFICATION_PASS");
        VerifyPackets();
    }
    static void VerifyPackets()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Load(DischargeAuthoring.Hud);var ui=root.GetComponent<DischargeDirector>();
        void Put(byte[] bytes,int offset,uint value)=>Buffer.BlockCopy(BitConverter.GetBytes(value),0,bytes,offset,4);
        void Float(byte[] bytes,int offset,float value)=>Buffer.BlockCopy(BitConverter.GetBytes(value),0,bytes,offset,4);
        var begin=new byte[45];Put(begin,0,100);Put(begin,4,1);Float(begin,28,1);Float(begin,32,18);Float(begin,36,Warning);Float(begin,40,Interval);begin[44]=7;
        ui.Receive(404,begin);ui.Receive(404,begin);var casts=Field<System.Collections.IDictionary>(ui,"_casts");Check(casts.Count==1,"duplicate begin does not spawn duplicate sequences");
        var strike=new byte[130];Put(strike,0,100);Put(strike,4,1);strike[8]=2;strike[9]=2;Put(strike,10,2);Put(strike,30,3);Float(strike,18,1);Float(strike,38,1);
        ui.Receive(405,strike);ui.Receive(405,strike);var cast=casts[(uint)100];var count=(int)cast.GetType().GetField("count").GetValue(cast);Check(count==2,"single strike delivers two unique authoritative targets exactly once");
        var end=new byte[9];Put(end,0,100);Put(end,4,1);ui.Receive(406,end);strike[8]=3;Put(strike,10,4);ui.Receive(405,strike);Check((int)cast.GetType().GetField("count").GetValue(cast)==2,"late strike after end rejected");
        ui.Receive(405,new byte[12]);ui.Receive(404,new byte[2]);ui.Receive(403,new byte[1]);
        ui.ResetFeedback();ui.ConfirmHit(99,1,100,2,1);Check(!Field<CanvasGroup>(ui,"_ownerNotice").gameObject.activeSelf&&!Field<CanvasGroup>(ui,"_victimNotice").gameObject.activeSelf,"uninvolved spectator gets no private hit notice");
        ui.ConfirmHit(1,1,100,2,1);Check(Field<CanvasGroup>(ui,"_ownerNotice").gameObject.activeSelf&&!Field<CanvasGroup>(ui,"_victimNotice").gameObject.activeSelf,"A sees confirmation only");
        ui.ResetFeedback();ui.ConfirmHit(2,1,100,2,1);Check(Field<CanvasGroup>(ui,"_victimNotice").gameObject.activeSelf&&!Field<CanvasGroup>(ui,"_ownerNotice").gameObject.activeSelf,"B sees received hit only");
        ui.ClearAll();ui.Receive(405,strike);Check(casts.Count==0,"previous-round strike ignored after clear");
        Debug.Log("DISCHARGE_PACKET_FEEDBACK_PASS");
    }
    static void VerifyHand()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var actor=Load(DischargeAuthoring.Actor);var rig=Field<EmpGestureRig>(actor.GetComponent<InitiatorSkill_X>(),"_gesture");
        Check(rig && rig!=Field<EmpGestureRig>(actor.GetComponent<InitiatorSkill_E>(),"_gesture"),"independent X/E gesture binding");
        var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
        var realCamera=actor.GetComponent<PlayerCameraController>().PlayerCamera;var camera=CameraAt(realCamera.transform.position,realCamera.transform.position+realCamera.transform.forward);camera.fieldOfView=90;camera.cullingMask=realCamera.cullingMask;
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(Transform child in actor.transform)if(child.name!="CameraRoot")child.gameObject.SetActive(false);
        var rootR=actor.GetComponent<Renderer>();if(rootR)rootR.enabled=false;actor.GetComponent<PlayerItem>()?.SetWeaponVisibility(false);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true)){
            if(r.name=="HeadUV" || r.name=="THair6UV")r.enabled=false;
            if(r.name=="Arm1" || r.name=="Arm2"){r.gameObject.SetActive(true);r.enabled=true;r.forceRenderingOff=false;}
        }
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name=="Arm1"||r.name=="Arm2")){
            double left=0,right=0;
            foreach(var w in skin.sharedMesh.boneWeights){int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] weights={w.weight0,w.weight1,w.weight2,w.weight3};
                for(int i=0;i<4;i++){if(skin.bones[ids[i]].name.Contains("Left"))left+=weights[i];if(skin.bones[ids[i]].name.Contains("Right"))right+=weights[i];}}
            Debug.Log($"DISCHARGE_ARM_MESH {skin.name} left={left} right={right}");
        }
        rig.Begin();rig.Advance(.45f);Save(camera,"Left-hand-windup");rig.Advance(.2f);Check(rig.Ready && !rig.WeaponVisible,"left command waits for windup, gun hidden");Save(camera,"Left-hand-command");
        var hand=bones.First(t=>t.name=="mixamorig:LeftHand");Vector3 viewport=camera.WorldToViewportPoint(hand.position);
        Check(viewport.x>.05f && viewport.x<.55f && viewport.y>0 && viewport.y<.55f && viewport.z>.1f,"left hand in lower-left frame "+viewport);
        for(int i=0;i<bones.Length;i++)Check(Vector3.Distance(positions[i],bones[i].localPosition)<.0001f,"bone length preserved "+bones[i].name);
        rig.Release();rig.Advance(.6f);Check(!rig.Active,"release restores weapons");
        for(int i=0;i<bones.Length;i++)Check(Quaternion.Angle(rotations[i],bones[i].localRotation)<.1f,"original firearm pose restored "+bones[i].name);
        rig.Begin();rig.Advance(.25f);rig.Cancel();rig.Advance(.6f);Check(!rig.Active,"pre-release cancellation recovers");
        rig.Begin();rig.Advance(.5f);rig.ResetPresentation();Check(!rig.Active,"death reset recovers");
    }
    static void Film(DischargeVfx fx,Camera camera,DischargeDirector ui,Vector3 origin,Vector3 direction,Vector3 target,string map)
    {
        // Explicit editor animation preview, not a gameplay recording or hit test.
        string dir=Output+"/"+map+"-frames";Directory.CreateDirectory(dir);
        var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        fx.Begin(origin,direction,18,Warning,Interval);int next=0;
        var particles=fx.GetComponentsInChildren<ParticleSystem>();
        for(int frame=0;frame<126;frame++){
            float time=frame/30f;
            while(next<7 && time>=Warning+next*Interval){fx.Strike(next);if(next==2)fx.Hit(target+Vector3.up);next++;}
            if(time>=Warning+6*Interval+.03f)fx.Finish(false);
            fx.Advance(1/30f);foreach(var p in particles)if(p.isPlaying)p.Simulate(1/30f,true,false,false);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+frame.ToString("D4")+".png",tex.EncodeToPNG());
            if(frame==72)Save(camera,map+"-Full-wave");
            camera.targetTexture=rt;
        }
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
    }
}
#endif
