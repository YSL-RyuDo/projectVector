#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ScannerPolishVerification
{
    static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);Debug.Log("SCANNER_CHECK "+message);}
    static GameObject Load(string path)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static void Lighting()
    {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.65f;
        var light=new GameObject("ReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.transform.rotation=Quaternion.Euler(45,-30,0);
    }
    static Camera Camera(Vector3 position,Vector3 target)
    {
        var c=new GameObject("ReviewCamera").AddComponent<Camera>();c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=45;c.nearClipPlane=.01f;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.055f,.075f,.10f);return c;
    }
    static void Save(Camera camera,string name)
    {
        Directory.CreateDirectory(ScannerPolishAuthoring.Output);var rt=new RenderTexture(1280,720,24);var t=new Texture2D(1280,720,TextureFormat.RGB24,false);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes(ScannerPolishAuthoring.Output+"/"+name+".png",t.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(t);
    }
    public static void Run()
    {
        Check(Application.isBatchMode,"isolated editor");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Lighting();
        var scanner=Load("Assets/Prefab/Character/Initiator/Scanner.prefab");var fx=scanner.GetComponent<ScannerPresentation>();
        Check(fx && Field<ScannerPresentation>(scanner.GetComponent<ScannerProjectile>(),"_presentation")==fx,"saved projectile presentation binding");
        Check(!scanner.transform.Find("Cube"),"legacy metre-wide placeholder removed");
        Check(scanner.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterial && r.sharedMaterial.shader.isSupported),"all scanner materials supported");
        var beams=Field<LineRenderer[]>(fx,"_beams");var pulse=Field<LineRenderer>(fx,"_pulse");
        fx.Advance(.1f);Check(beams.All(l=>!l.enabled) && !pulse.enabled,"flight has no false hit or detection effect");
        var camera=Camera(new Vector3(.6f,.45f,.65f),Vector3.up*.05f);Save(camera,"Scanner-Folded");
        fx.Anchor();fx.Advance(.4f);Save(camera,"Scanner-Deployed");
        Check(Field<Transform>(fx,"_visual").localPosition==Vector3.zero,"visual centered on shootable collider");
        Check(pulse.widthMultiplier<.02f && pulse.positionCount==65,"thin unfilled scan pulse");
        var targets=new[]{new Vector3(2,1,0),new Vector3(-2,1,0),new Vector3(0,1,2)};
        foreach(var target in targets)fx.Zap(target);fx.Advance(.05f);
        for(int i=0;i<3;i++)Check(beams[i].enabled && Vector3.Distance(beams[i].GetPosition(1),targets[i])<.001f,"independent confirmed endpoint "+i);
        fx.Advance(.25f);Check(beams.All(b=>!b.enabled),"confirmed impact trails expire");
        fx.End(false);fx.Advance(.23f);Check(!pulse.enabled && beams.All(b=>!b.enabled),"destroyed scanner clears all scan and hit lines");
        Object.DestroyImmediate(scanner);Object.DestroyImmediate(camera.gameObject);
        var actor=Load("Assets/Prefab/Character/Initiator/Initiator.prefab");var rig=actor.GetComponent<ScannerCastRig>();
        var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToArray();
        var original=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
        Check(rig && Field<ScannerCastRig>(actor.GetComponent<InitiatorSkill_C>(),"_rig")==rig,"saved actor rig binding");
        var actual=actor.GetComponent<PlayerCameraController>().PlayerCamera;
        camera=Camera(actual.transform.position,actual.transform.position+actual.transform.forward);camera.fieldOfView=90;camera.cullingMask=actual.cullingMask;
        Check(Field<GameObject>(rig,"_firstDevice").GetComponentsInChildren<Renderer>(true).All(r=>(actual.cullingMask&(1<<r.gameObject.layer))!=0),"held scanner is visible through actual player camera layer mask");
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(Transform child in actor.transform)if(child.name!="CameraRoot")child.gameObject.SetActive(false);
        var rootRenderer=actor.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        actor.GetComponent<PlayerItem>()?.SetWeaponVisibility(false);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true)){
            if(r.name=="HeadUV" || r.name=="THair6UV")r.enabled=false;
            if(r.name=="Arm1" || r.name=="Arm2"){r.gameObject.SetActive(true);r.enabled=true;r.forceRenderingOff=false;}
        }
        rig.Begin();Check(rig.Active && rig.WeaponVisible && !rig.Ready,"holster precedes ready state");
        rig.Advance(.18f);rig.Advance(.33f);Check(rig.Ready && !rig.WeaponVisible,"prepared skill blocks weapon view");
        Save(camera,"Scanner-FirstPerson-Ready");
        for(int i=0;i<bones.Length;i++)Check(Vector3.Distance(original[i],bones[i].localPosition)<.0001f,"bone length preserved "+bones[i].name);
        var hand=Field<Transform>(rig,"_firstHand");var point=camera.WorldToViewportPoint(hand.position);
        Check(point.z>.1f && point.x>.05f && point.x<.7f && point.y>-.1f && point.y<.7f,"left hand within lower-left camera frame "+point);
        rig.Throw();rig.Advance(.16f);Save(camera,"Scanner-FirstPerson-Windup");rig.Advance(.16f);Check(rig.ReleaseReady,"release waits for throw animation");Save(camera,"Scanner-FirstPerson-Release");
        Check(Vector3.Distance(rig.ReleasePosition,Field<GameObject>(rig,"_firstDevice").transform.position)<.01f,"release position matches held device endpoint");
        rig.Return();rig.Advance(.31f);rig.Advance(.2f);Check(!rig.Active && rig.WeaponVisible,"return completes weapon draw");
        for(int i=0;i<bones.Length;i++)Check(Quaternion.Angle(rotations[i],bones[i].localRotation)<.1f,"original rotation restored "+bones[i].name);
        rig.Begin();rig.Advance(.18f);rig.Advance(.32f);rig.Return();rig.Advance(.31f);rig.Advance(.2f);Check(!rig.Active,"cancel works without consuming a throw");
        rig.Begin();rig.Advance(.18f);rig.Advance(.32f);rig.ResetPresentation();Check(!rig.Active && !Field<GameObject>(rig,"_firstDevice").activeSelf,"death/disable reset hides held device");
        Debug.Log("SCANNER_VERIFICATION_PASS");
    }
}
#endif
