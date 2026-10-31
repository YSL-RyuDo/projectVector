#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class ControllerForestAuthoring
{
    public const string Dir="Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/Forest", Actor="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/Controller.prefab", Area="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DreamForestArea.prefab";
    static void Ref(Object o,string key,Object v){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static T Add<T>(GameObject g) where T:Component => g.GetComponent<T>()??g.AddComponent<T>();
    static Material Mat(string name,string shader){string p=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,p);}return m;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();Sound("Bloom",3,false);Sound("BreathingMist",4,true);
        var shell=Mat("ForestBoundary","Vector/DreamForestShell");
        shell.SetColor("_CloudShadow",new Color(.41f,.49f,.66f));shell.SetColor("_CloudBody",new Color(.73f,.79f,.91f));shell.SetColor("_CloudHighlight",new Color(.94f,.96f,1));
        shell.SetColor("_Deep",new Color(.50f,.58f,.74f));shell.SetColor("_Light",new Color(.77f,.83f,.94f));shell.SetColor("_Accent",new Color(.69f,.78f,.94f));
        shell.SetFloat("_InteriorMist",0);shell.SetFloat("_EdgeMist",.24f);EditorUtility.SetDirty(shell);
        var root=PrefabUtility.LoadPrefabContents(Area);
        try{
            for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach(var reverb in root.GetComponents<AudioReverbZone>())Object.DestroyImmediate(reverb);
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="MistBoundary";sphere.transform.SetParent(root.transform,false);sphere.transform.localScale=new Vector3(1,DreamForestPresentation.HeightRatio,1);Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var renderer=sphere.GetComponent<MeshRenderer>();renderer.sharedMaterial=shell;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var fx=Add<DreamForestPresentation>(root);Ref(fx,"_shell",renderer);Ref(fx,"_bloom",Audio(root.transform,"Bloom",false,.45f));Ref(fx,"_bed",Audio(root.transform,"BreathingMist",true,.14f));
            var area=new SerializedObject(root.GetComponent<DreamForestArea>());area.FindProperty("defaultMaxRadius").floatValue=7.5f;area.FindProperty("expandSpeed").floatValue=3.75f;area.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Area);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        Motion();
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/ProjectVector/Shared/Rendering/Settings/PC_Renderer.asset");
        var feature=data.rendererFeatures.OfType<DreamForestRendererFeature>().FirstOrDefault();
        if(!feature){feature=ScriptableObject.CreateInstance<DreamForestRendererFeature>();feature.name="DreamForestDepthFog";AssetDatabase.AddObjectToAsset(feature,data);data.rendererFeatures.Add(feature);}
        feature.passMaterial=Mat("ForestDepth","Vector/DreamForestDepth");feature.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;feature.requirements=ScriptableRenderPassInput.Depth;feature.fetchColorBuffer=true;feature.SetActive(true);
        var serialized=new SerializedObject(data);var map=serialized.FindProperty("m_RendererFeatureMap");map.arraySize=data.rendererFeatures.Count;
        for(int i=0;i<data.rendererFeatures.Count;i++){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i],out string guid,out long id);map.GetArrayElementAtIndex(i).longValue=id;}
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(data);data.SetDirty();AssetDatabase.SaveAssets();Debug.Log("FOREST_AUTHOR_PASS");
    }
    static AudioSource Audio(Transform parent,string name,bool loop,float volume)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=loop;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/"+name+".wav");a.volume=volume;a.spatialBlend=1;a.minDistance=6;a.maxDistance=35;a.dopplerLevel=0;return a;
    }
    static void Sound(string name,int seconds,bool loop)
    {
        const int rate=24000;int count=rate*seconds;string p=Dir+"/"+name+".wav";
        using(var w=new BinaryWriter(File.Create(p))){
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            var rng=new System.Random(483);float low=0,slow=0;
            for(int i=0;i<count;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,.09f);slow=Mathf.Lerp(slow,low,.018f);float envelope=loop?Mathf.Min(1,Mathf.Min(t,seconds-t)*12):Mathf.Sin(Mathf.Clamp01(t/seconds)*Mathf.PI)*Mathf.Exp(-t*.45f);float tone=Mathf.Sin(t*2*Mathf.PI*96)+Mathf.Sin(t*2*Mathf.PI*144)*.4f;float value=((low-slow)*.7f+slow*.8f+tone*.035f)*envelope;w.Write((short)(Mathf.Clamp(value,-1,1)*29000));}
        }AssetDatabase.ImportAsset(p);
    }
    static void Motion()
    {
        var root=PrefabUtility.LoadPrefabContents(Actor);
        try{
            var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model||t.name=="Armature"||t.name.StartsWith("mixamorig:")).ToArray();
            var rotations=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();
            Transform Bone(string n)=>bones.First(t=>t.name=="mixamorig:"+n);
            void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}}
            float[] times={0,.30f,.72f,.9f,1.25f};
            var targets=new[]{new Vector3(.28f,-.28f,.32f),new Vector3(.17f,-.13f,.38f),new Vector3(.20f,-.06f,.52f),new Vector3(.37f,-.12f,.52f),new Vector3(.38f,-.36f,.32f)};
            var rs=new Quaternion[5][];var ps=new Vector3[5][];
            for(int f=0;f<5;f++){
                Reset();model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(.10f,-.17f,.20f)));
                var upper=Bone("RightArm");var fore=Bone("RightForeArm");var hand=Bone("RightHand");
                upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,camera.TransformDirection(new Vector3(1,-.5f,.15f)))*upper.rotation;
                SkillArmPose.Reach(upper,fore,hand,camera,targets[f],1);
                var finger=hand.GetComponentsInChildren<Transform>(true).Last(t=>t.name.StartsWith("mixamorig:"));hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,camera.TransformDirection(new Vector3(-.22f,.65f,1)))*hand.rotation;
                var left=Bone("LeftArm");left.rotation=Quaternion.FromToRotation(Bone("LeftForeArm").position-left.position,camera.TransformDirection(new Vector3(-.3f,-1,-.35f)))*left.rotation;
                rs[f]=bones.Select(t=>t.localRotation).ToArray();ps[f]=bones.Select(t=>t.localPosition).ToArray();
            }
            Reset();string path=Dir+"/ForestRelease_RightHand.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();clip.name="ForestRelease_RightHand";clip.frameRate=60;
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
                string bp=AnimationUtility.CalculateTransformPath(bones[i],model);
                clip.SetCurve(bp,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],rs[f][i][axis])).ToArray()));
                if(axis<3)clip.SetCurve(bp,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],ps[f][i][axis])).ToArray()));
            }
            clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
            var motion=Add<ControllerForestMotion>(root);Ref(motion,"_model",model);Ref(motion,"_clip",clip);Ref(motion,"_device",null);
            var s=new SerializedObject(motion);var array=s.FindProperty("_bones");array.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];s.ApplyModifiedPropertiesWithoutUndo();
            Ref(root.GetComponent<ControllerSkill_X>(),"_motion",motion);var oldAudio=root.GetComponent<DreamForestAudio>();if(oldAudio)Object.DestroyImmediate(oldAudio);PrefabUtility.SaveAsPrefabAsset(root,Actor);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
