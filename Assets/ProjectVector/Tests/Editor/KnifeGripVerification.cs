#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KnifeGripVerification
{
    public static void AuthorAndCapture()
    {
        const string knifePath="Assets/ProjectVector/Shared/Weapons/Assets/Knife/Knife.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(knifePath);
        if(!prefab.GetComponent<FirstPersonKnifeGrip>())prefab.AddComponent<FirstPersonKnifeGrip>();
        PrefabUtility.SaveAsPrefabAsset(prefab,knifePath);PrefabUtility.UnloadPrefabContents(prefab);AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var light=new GameObject("Grip verification light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-25,0);light.intensity=1.2f;RenderSettings.ambientLight=Color.gray;
        foreach(string role in new[]{"Initiator","Controller","Sentinel","Entry"}) {
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ProjectVector/Shared/Characters/Prefabs/{role}/{role}.prefab"));
            var movement=actor.GetComponent<PlayerMovement>();var arms=(GameObject)new SerializedObject(movement).FindProperty("firstPersonArmModel").objectReferenceValue;
            var holder=(Transform)new SerializedObject(actor.GetComponent<PlayerItem>()).FindProperty("itemHolder").objectReferenceValue;
            foreach(var r in actor.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            arms.SetActive(true);foreach(var r in arms.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(r.name=="Arm1" || r.name=="Arm2"){r.enabled=true;r.gameObject.SetActive(true);r.forceRenderingOff=false;}
            var anim=arms.GetComponent<Animator>();anim.runtimeAnimatorController.animationClips.First(c=>c.name=="Pistol").SampleAnimation(arms,0);anim.enabled=false;
            var knife=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(knifePath),holder,false);var grip=knife.GetComponent<FirstPersonKnifeGrip>();grip.Align();
            if(grip.GripError>.0001f)throw new Exception("Knife palm mismatch: "+role);
            var real=actor.GetComponent<PlayerCameraController>().PlayerCamera;
            foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
            var camera=new GameObject("Grip verification camera").AddComponent<Camera>();camera.enabled=false;camera.transform.SetPositionAndRotation(real.transform.position,real.transform.rotation);camera.fieldOfView=90;camera.nearClipPlane=.01f;camera.cullingMask=~0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.21f,.29f,.36f);
            var rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT"),"Knife-Grip-"+role+".png"),tex.EncodeToPNG());RenderTexture.active=old;
            Object.DestroyImmediate(tex);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(actor);
        }
        Debug.Log("KNIFE_PALM_ALIGNMENT_PASS four roles");
    }
    public static void Inspect()
    {
        var result=new StringBuilder();
        foreach(string role in new[]{"Initiator","Controller","Sentinel","Entry"}) {
            string path=$"Assets/ProjectVector/Shared/Characters/Prefabs/{role}/{role}.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            var movement=root.GetComponent<PlayerMovement>();var arms=(GameObject)new SerializedObject(movement).FindProperty("firstPersonArmModel").objectReferenceValue;
            var holder=(Transform)new SerializedObject(root.GetComponent<PlayerItem>()).FindProperty("itemHolder").objectReferenceValue;
            var cam=root.GetComponentInChildren<Camera>(true);var anim=arms.GetComponent<Animator>();
            var clips=anim.runtimeAnimatorController.animationClips;result.AppendLine(role+" holder="+AnimationUtility.CalculateTransformPath(holder,arms.transform)+" scale="+holder.lossyScale+" arms="+arms.transform.localPosition);
            result.AppendLine(string.Join(",",clips.Select(c=>c.name)));
            foreach(var clip in clips.Where(c=>c.name=="Pistol").Take(1))clip.SampleAnimation(arms,0);
            foreach(var t in arms.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("RightHand") || t.name=="mixamorig:RightForeArm"))
                result.AppendLine(t.name+" pos="+cam.transform.InverseTransformPoint(t.position).ToString("F4")+" local="+t.localEulerAngles+" axes="+cam.transform.InverseTransformDirection(t.right)+" / "+cam.transform.InverseTransformDirection(t.up)+" / "+cam.transform.InverseTransformDirection(t.forward));
            PrefabUtility.UnloadPrefabContents(root);
        }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT"),"KnifeRig.txt"),result.ToString());
    }
}
#endif
