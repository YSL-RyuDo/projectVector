#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ControllerSmokeAuthoring
{
    public const string Dir="Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller", Actor="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/Controller.prefab";
    public const string ScenePath="Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity";
    static readonly Color Navy=new Color(.08f,.12f,.20f), Sky=new Color(.25f,.68f,.88f), Silver=new Color(.79f,.85f,.94f);
    static void Ref(Object o,string key,Object value){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static T Add<T>(GameObject g) where T:Component { var c=g.GetComponent<T>();return c?c:g.AddComponent<T>(); }
    static Material Material(string name,string shader)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/"+name+".mat");
        if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");}return m;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch authoring only");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
        Effect();Motion();SceneUI();Palette();AssetDatabase.SaveAssets();
        Debug.Log("CONTROLLER_SMOKE_AUTHOR_PASS");
    }
    static void Effect()
    {
        var mat=Material("SmokeVolume","Vector/ControllerSmoke");
        mat.SetFloat("_FlowSpeed",.24f);mat.SetFloat("_EdgeMist",.3f);mat.SetFloat("_ContactSoftness",.035f);mat.SetFloat("_BrushWarp",.18f);mat.SetFloat("_BrushScale",1.15f);mat.SetFloat("_BrushSoftness",2.6f);
        mat.SetTexture("_BrushMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/SmokeBrushMask.png"));
        mat.SetFloat("_PatternStrength",.72f);mat.SetFloat("_BandDensity",8);
        mat.SetVector("_FlowAxis",new Vector4(-.32f,.93f,.16f,0));
        mat.SetColor("_Deep",new Color(.32f,.4f,.49f));mat.SetColor("_Light",new Color(.76f,.82f,.88f));mat.SetColor("_Accent",Sky);mat.SetFloat("_InteriorMist",.1f);
        mat.SetColor("_CloudShadow",new Color(.64f,.70f,.86f));mat.SetColor("_CloudBody",new Color(.86f,.90f,.98f));mat.SetColor("_CloudHighlight",new Color(.97f,.985f,1));EditorUtility.SetDirty(mat);
        Audio();
        var root=new GameObject("ControllerSmoke");
        try {
            root.AddComponent<SmokeOutlineOccluder>();
            var volume=GameObject.CreatePrimitive(PrimitiveType.Sphere);volume.name="BlueGreyVolume";volume.transform.SetParent(root.transform,false);volume.transform.localScale=Vector3.one*2;
            Object.DestroyImmediate(volume.GetComponent<Collider>());
            var r=volume.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            var audio=root.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=24;audio.volume=.35f;audio.rolloffMode=AudioRolloffMode.Linear;audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/SmokeBloom.wav");
            var fx=root.AddComponent<ControllerSmokeVfx>();Ref(fx,"_volume",r);Ref(fx,"_audio",audio);
            PrefabUtility.SaveAsPrefabAsset(root,Dir+"/ControllerSmoke.prefab");
        } finally { Object.DestroyImmediate(root); }
    }
    static void Audio()
    {
        // Original filtered-noise pressure bloom, authored once as a normal audio asset.
        string path=Dir+"/SmokeBloom.wav";const int rate=24000,count=36000;
        using(var stream=new BinaryWriter(File.Create(path))) {
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+count*2);stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(count*2);
            var random=new System.Random(417);float low=0;
            for(int i=0;i<count;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.09f);float env=Mathf.Min(1,t/.04f)*Mathf.Pow(1-t/1.5f,2);float bass=Mathf.Sin(2*Mathf.PI*(72*t-13*t*t))*Mathf.Exp(-t*7);stream.Write((short)(Mathf.Clamp((low*.8f+bass*.18f)*env,-1,1)*24000));}
        }
        AssetDatabase.ImportAsset(path);
    }
    static void Motion()
    {
        var root=PrefabUtility.LoadPrefabContents(Actor);
        try {
            var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");
            if(!model)throw new Exception("Controller viewmodel missing");
            var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model||t.name=="Armature"||t.name.StartsWith("mixamorig:")).ToArray();
            var rotations=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();
            Transform Bone(string name)=>bones.First(t=>t.name=="mixamorig:"+name);
            void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}}
            var samples=new Quaternion[3][];var ps=new Vector3[3][];
            for(int frame=0;frame<3;frame++){
                Reset();var upper=Bone("RightArm");var fore=Bone("RightForeArm");var hand=Bone("RightHand");
                // Keep shoulder and segment lengths intact; move only the entire first-person presentation root.
                model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(.05f,-.12f,.12f)));
                upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,camera.TransformDirection(new Vector3(1,-.5f,.15f)))*upper.rotation;
                SkillArmPose.Reach(upper,fore,hand,camera,Vector3.Lerp(new Vector3(.19f,-.17f,.36f),new Vector3(.12f,-.1f,.43f),frame*.5f),1);
                var tip=hand.GetComponentsInChildren<Transform>(true).Last(t=>t.name.StartsWith("mixamorig:"));
                hand.rotation=Quaternion.FromToRotation(tip.position-hand.position,camera.TransformDirection(new Vector3(-.2f,.4f,.9f)))*hand.rotation;
                hand.rotation=Quaternion.AngleAxis(frame*10,camera.right)*hand.rotation;
                var left=Bone("LeftArm");var leftFore=Bone("LeftForeArm");left.rotation=Quaternion.FromToRotation(leftFore.position-left.position,camera.TransformDirection(new Vector3(-.25f,-1,-.3f)))*left.rotation;
                model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(.12f,-.07f,.18f)));
                samples[frame]=bones.Select(t=>t.localRotation).ToArray();ps[frame]=bones.Select(t=>t.localPosition).ToArray();
            }
            Reset();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"/SmokeCommand_RightHand.anim");bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();
            clip.name="SmokeCommand_RightHand";clip.frameRate=60;
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
                string path=AnimationUtility.CalculateTransformPath(bones[i],model);
                clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,3).Select(f=>new Keyframe(f*.16f,samples[f][i][axis])).ToArray()));
                if(axis<3)clip.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,3).Select(f=>new Keyframe(f*.16f,ps[f][i][axis])).ToArray()));
            }
            clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,Dir+"/SmokeCommand_RightHand.anim");else EditorUtility.SetDirty(clip);
            var motion=Add<ControllerSmokeMotion>(root);Ref(motion,"_model",model);Ref(motion,"_clip",clip);
            var so=new SerializedObject(motion);var array=so.FindProperty("_bones");array.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];so.ApplyModifiedPropertiesWithoutUndo();
            Ref(root.GetComponent<ControllerSkill_E>(),"_motion",motion);PrefabUtility.SaveAsPrefabAsset(root,Actor);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var old=parent.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color){var r=Rect(parent,name,position,size);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
    static ControllerPanelGraphic Frame(Transform parent,string name,Vector2 position,Vector2 size,Color color){var r=Rect(parent,name,position,size);var g=r.gameObject.AddComponent<ControllerPanelGraphic>();g.color=name=="StatusPanel"||name=="Control0"||name=="Control1"||name=="Control2"?new Color(.025f,.045f,.075f,.96f):color;g.raycastTarget=false;return g;}
    static TMP_Text Text(Transform parent,string name,string text,Vector2 pos,Vector2 size,int fontSize,Color color)
    {
        fontSize=name=="Header"?42:name=="Status"?27:name=="Key"?30:name=="Subheader"?22:24;
        color=name.StartsWith("ControlKey")||name=="Key"||name=="Distance"?new Color(.65f,.9f,1):Color.white;
        if(name=="Distance")size.y=64;
        var r=Rect(parent,name,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");t.text=text;t.fontSize=fontSize;t.color=color;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
        if(name=="Status")t.textWrappingMode=TextWrappingModes.Normal;
        return t;
    }
    static void SceneUI()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var overlay=all.First(t=>t.name=="MiniMap (Overlay)");var map=overlay.GetComponentsInChildren<RawImage>(true).First();
        overlay.GetComponent<Image>().color=new Color(.025f,.04f,.07f,0);
        map.rectTransform.anchoredPosition=new Vector2(-140,0);map.rectTransform.sizeDelta=Vector2.one*720;
        var border=map.GetComponent<UnityEngine.UI.Outline>();if(border){border.effectColor=new Color(.35f,.51f,.65f,.8f);border.effectDistance=new Vector2(2,-2);}
        var root=Rect(overlay,"SmokePresentation",Vector2.zero,new Vector2(1920,1080));
        Frame(root,"MapFrame",new Vector2(-140,0),new Vector2(742,742),Color.clear);
        Frame(root,"KeyFrame",new Vector2(-475,419),new Vector2(56,56),new Color(.08f,.15f,.22f,.9f));
        Text(root,"Key","E",new Vector2(-474,419),new Vector2(24,40),27,Sky);
        Text(root,"Header","연막",new Vector2(-280,422),new Vector2(300,65),38,Silver);
        Text(root,"Subheader","투하 지점 선택",new Vector2(-275,388),new Vector2(310,22),15,new Color(.58f,.68f,.78f));
        Panel(root,"HeaderRule",new Vector2(-140,465),new Vector2(720,2),new Color(.35f,.58f,.72f,.5f));
        string[] keys={"좌클릭","우클릭","E / ESC"},labels={"위치 선택","투하 확정","취소"};
        for(int i=0;i<3;i++){
            float x=-383+i*240;Frame(root,"Control"+i,new Vector2(x,-419),new Vector2(228,54),new Color(.04f,.07f,.12f,.85f));
            Text(root,"ControlKey"+i,keys[i],new Vector2(x-57,-419),new Vector2(85,35),17,Sky);Text(root,"ControlText"+i,labels[i],new Vector2(x+45,-419),new Vector2(112,35),18,Silver);
        }
        Frame(root,"StatusPanel",new Vector2(455,50),new Vector2(350,346),new Color(.04f,.065f,.11f,.88f));
        var icon=Panel(root,"SkillIcon",new Vector2(454,137),new Vector2(74,74),Silver);icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Characters/Portraits/Comtroller/Controller_E.png");icon.preserveAspect=true;
        Panel(root,"StatusRule",new Vector2(455,76),new Vector2(290,1),new Color(.35f,.58f,.72f,.6f));
        var status=Text(root,"Status","투하할 위치를 선택하세요",new Vector2(465,28),new Vector2(310,65),23,Silver);
        var distance=Text(root,"Distance","좌클릭  위치 선택",new Vector2(465,-61),new Vector2(310,42),17,Sky);
        var marker=Rect(map.transform,"SmokeTargetMarker",Vector2.zero,Vector2.one*100);
        var ring=marker.gameObject.AddComponent<SmokeTargetRing>();ring.color=Sky;ring.raycastTarget=false;
        Panel(marker,"CrossH",Vector2.zero,new Vector2(16,2),Silver);Panel(marker,"CrossV",Vector2.zero,new Vector2(2,16),Silver);
        marker.gameObject.SetActive(false);
        var view=Add<SmokeTargetingView>(overlay.gameObject);Ref(view,"_presentation",root.gameObject);Ref(view,"_map",map.rectTransform);Ref(view,"_marker",marker);Ref(view,"_ring",ring);Ref(view,"_status",status);Ref(view,"_distance",distance);
        var manager=all.Where(t=>t).Select(t=>t.GetComponent<ControllerSmokeNetworkManager>()).First(m=>m);Ref(manager,"_smokePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/ControllerSmoke.prefab"));
        foreach(var feedback in all.Where(t=>t).Select(t=>t.GetComponent<DreamForestPostProcessFeedback>()).Where(f=>f)){
            var data=new SerializedObject(feedback);data.FindProperty("dreamColor").colorValue=new Color(.1f,.22f,.37f,1);data.ApplyModifiedPropertiesWithoutUndo();
        }
        overlay.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void Palette()
    {
        // Duplicate materials before retinting: controller palette must not recolor other characters.
        foreach(string name in new[]{"Trap","TrapArea","DecoyTrap","DecoyTrapArea","DreamForestArea"}) {
            string path="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try {
                foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){
                    var main=ps.main;float alpha=main.startColor.color.a;main.startColor=new Color(.46f,.58f,.73f,alpha);
                }
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)if(materials[i]){
                        var source=materials[i];string stem=source.name.StartsWith(name+"_")?source.name:name+"_"+source.name.Replace("/","_");string dest=Dir+"/"+stem+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(dest);
                        if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,dest);}
                        // The legacy material points to a duplicate KSW shader graph.
                        // Bind the controller-owned graph so this palette does not alter that shared asset.
                        if(name=="DreamForestArea" && source.name.Contains("DreamForestMaterial"))
                            mat.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DreamForestShader.shadergraph");
                        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",name.Contains("Area")?new Color(.18f,.43f,.62f,.72f):Silver);
                        if(mat.HasProperty("_Color"))mat.SetColor("_Color",name.Contains("Area")?Sky:Silver);
                        if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",Sky*.18f);
                        EditorUtility.SetDirty(mat);materials[i]=mat;
                    }
                    renderer.sharedMaterials=materials;
                }
                foreach(var decal in root.GetComponentsInChildren<UnityEngine.Rendering.Universal.DecalProjector>(true))if(decal.material){
                    string dest=Dir+"/"+name+"_Decal.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(dest);
                    if(!mat){mat=new Material(decal.material);AssetDatabase.CreateAsset(mat,dest);}
                    if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",new Color(.25f,.58f,.76f,.65f));
                    if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.25f,.58f,.76f,.65f));
                    decal.material=mat;EditorUtility.SetDirty(mat);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
