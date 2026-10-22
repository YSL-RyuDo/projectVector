#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

public static class EmpPolishAuthoring
{
    const string Dir="Assets/Art/EMP";
    const string DevicePath=Dir+"/WristEmitter.prefab";
    const string ScenePath="Assets/Scenes/Game_TestVersion.unity";
    public const string Output="C:/Users/GCT/Downloads/Vector_EMP_20261022";
    static readonly Color Cyan=new Color(.13f,.8f,1);
    static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Refs(Object target,string field,IEnumerable<Object> values){var a=values.ToArray();var s=new SerializedObject(target);var p=s.FindProperty(field);p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static Transform Child(Transform parent,string name){var t=parent.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(parent,false);}return t;}
    static T Component<T>(Transform t)where T:Component {var c=t.GetComponent<T>();return c?c:t.gameObject.AddComponent<T>();}
    static Material Mat(string name,Color color,float metal,float smooth,bool glow=false)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}
        EditorUtility.SetDirty(m);return m;
    }
    static Mesh SaveMesh(string name,Mesh mesh){string path=Dir+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
    static Mesh Housing()
    {
        var outline=new[]{new Vector2(-.075f,-.16f),new Vector2(.075f,-.16f),new Vector2(.095f,-.14f),new Vector2(.095f,.14f),new Vector2(.075f,.16f),new Vector2(-.075f,.16f),new Vector2(-.095f,.14f),new Vector2(-.095f,-.14f)};
        var v=new List<Vector3>();var tri=new List<int>();
        for(int ring=0;ring<3;ring++)foreach(var p in outline){float scale=ring==2?.86f:1;v.Add(new Vector3(p.x*scale,ring==0?-.03f:ring==1?.018f:.035f,p.y*scale));}
        for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++){int a=ring*8+i,b=ring*8+(i+1)%8,c=a+8,d=b+8;tri.AddRange(new[]{a,c,b,b,c,d});}
        for(int i=1;i<7;i++){tri.AddRange(new[]{16,16+i+1,16+i});tri.AddRange(new[]{0,i,i+1});}
        // Split face vertices to keep the machined bevel crisp, not pillow-shaped.
        var flat=tri.Select(i=>v[i]).ToArray();var m=new Mesh{name="BevelledEmitterHousing"};m.vertices=flat;m.triangles=Enumerable.Range(0,flat.Length).ToArray();m.RecalculateNormals();m.RecalculateBounds();return SaveMesh("EmitterHousing",m);
    }
    static Mesh Coil()
    {
        var v=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<64;i++)for(int j=0;j<10;j++){float a=i*Mathf.PI*2/64,b=j*Mathf.PI*2/10;float r=.065f+Mathf.Cos(b)*.006f;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*.006f,Mathf.Sin(a)*r));}
        for(int i=0;i<64;i++)for(int j=0;j<10;j++){int a=i*10+j,b=((i+1)%64)*10+j,c=i*10+(j+1)%10,d=((i+1)%64)*10+(j+1)%10;tris.AddRange(new[]{a,c,b,b,c,d});}
        var m=new Mesh{name="CopperInductionCoil"};m.SetVertices(v);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();return SaveMesh("InductionCoil",m);
    }
    static Transform MeshPart(Transform parent,string name,Mesh mesh,Material material,Vector3 pos,Vector3 scale)
    {
        var t=Child(parent,name);t.localPosition=pos;t.localScale=scale;
        Component<MeshFilter>(t).sharedMesh=mesh;Component<MeshRenderer>(t).sharedMaterial=material;return t;
    }
    static Transform Box(Transform parent,string name,Material material,Vector3 pos,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;
    }
    static void AuthorDevice()
    {
        var black=Mat("Graphite",new Color(.055f,.07f,.085f),.7f,.55f);var edge=Mat("Titanium",new Color(.22f,.28f,.32f),.85f,.65f);
        var copper=Mat("Copper",new Color(.66f,.29f,.10f),.8f,.65f);var glow=Mat("IonCore",Cyan,.4f,.6f,true);
        var g=new GameObject("WristEmitter");
        try{
            MeshPart(g.transform,"BevelledHousing",Housing(),black,Vector3.zero,Vector3.one);
            Box(g.transform,"RecessedPlate",edge,new Vector3(0,.035f,0),new Vector3(.13f,.008f,.245f));
            var coil=Coil();
            for(int i=0;i<3;i++){
                var group=Child(g.transform,"InductionCell_"+(i+1));group.localPosition=new Vector3(0,.048f,(i-1)*.08f);
                MeshPart(group,"CopperWinding",coil,copper,Vector3.zero,new Vector3(.85f,1,.46f));
                MeshPart(group,"EnergyCore",coil,glow,Vector3.up*.004f,new Vector3(.54f,.6f,.25f));
                Box(group,"CoreBridge",glow,new Vector3(0,0,0),new Vector3(.035f,.012f,.008f));
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++){
                var vent=Child(g.transform,"Vent_"+side+"_"+i);vent.localPosition=new Vector3(side*.085f,.022f,(i-1)*.08f);
                Box(vent,"ArmoredFin",edge,Vector3.zero,new Vector3(.02f,.025f,.055f));
                Box(vent,"CopperEdge",copper,new Vector3(0,.014f,0),new Vector3(.009f,.004f,.05f));
            }
            Box(g.transform,"FrontLens",glow,new Vector3(0,.006f,.161f),new Vector3(.09f,.012f,.005f));
            for(int z=-1;z<=1;z+=2)Box(g.transform,"Cuff_"+z,black,new Vector3(0,-.038f,z*.1f),new Vector3(.22f,.018f,.028f));
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(g.transform,"Fastener",copper,new Vector3(x*.073f,.027f,z*.128f),Vector3.one*.009f);
            PrefabUtility.SaveAsPrefabAsset(g,DevicePath);
        }finally{Object.DestroyImmediate(g);}
    }
    static LineRenderer Line(Transform parent,string name,bool world,bool loop)
    {
        var l=Component<LineRenderer>(Child(parent,name));l.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyBeam.mat");l.useWorldSpace=world;l.loop=loop;l.enabled=false;
        l.widthCurve=AnimationCurve.Constant(0,1,1);l.numCapVertices=2;l.numCornerVertices=2;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;return l;
    }
    static ParticleSystem Sparks(Transform parent,string name,int count)
    {
        var p=Component<ParticleSystem>(Child(parent,name));p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=p.main;m.playOnAwake=false;m.loop=false;m.duration=.6f;m.startLifetime=new ParticleSystem.MinMaxCurve(.15f,.4f);m.startSpeed=new ParticleSystem.MinMaxCurve(1.5f,5);m.startSize=new ParticleSystem.MinMaxCurve(.015f,.04f);m.startColor=Cyan;m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=80;
        var e=p.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var s=p.shape;s.shapeType=ParticleSystemShapeType.Sphere;s.radius=.18f;
        var colors=p.colorOverLifetime;colors.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Cyan,0),new GradientColorKey(Cyan,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});colors.color=grad;
        var r=p.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyParticle.mat");r.renderMode=ParticleSystemRenderMode.Stretch;r.lengthScale=2;r.velocityScale=.025f;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return p;
    }
    static AudioClip Sound()
    {
        string path=Dir+"/MagneticPulse.wav";const int rate=44100,count=22050;
        using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            var rng=new System.Random(218);double phase=0;for(int i=0;i<count;i++){double t=i/(double)rate;phase+=2*Math.PI*(65+220*Math.Exp(-t*18))/rate;double envelope=Math.Min(1,t/.003)*Math.Exp(-t*11);double noise=(rng.NextDouble()*2-1)*Math.Exp(-t*65);double value=.52*Math.Sin(phase)*envelope+.22*noise+.12*Math.Sin(phase*5.13)*envelope;w.Write((short)(Math.Clamp(value,-1,1)*28000));}}
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void AuthorActors(AudioClip cast)
    {
        foreach(string name in new[]{"Initiator","Controller","Entry","Sentinel"}){
            string path=$"Assets/Prefab/Character/{name}/{name}.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{
                var receiver=Component<ElectricStatusVfx>(root.transform);var group=Child(root.transform,"ElectricStatusFX");Ref(receiver,"_status",root.GetComponent<ElectricShock>());
                Refs(receiver,"_arcs",new[]{Line(group,"StunHelixA",false,false),Line(group,"StunHelixB",false,false)});
                var sparks=Sparks(group,"StunDischarge",20);sparks.transform.localPosition=Vector3.up;Ref(receiver,"_sparks",sparks);
                if(name=="Initiator"){
                    var fx=Component<EmpCastVfx>(root.transform);var parent=Child(root.transform,"EMP");
                    Refs(fx,"_waveRings",Enumerable.Range(0,3).Select(i=>Line(parent,"Wave_"+i,true,true)));
                    Refs(fx,"_targetArcs",Enumerable.Range(0,8).Select(i=>Line(parent,"ConfirmedArc_"+i,true,false)));
                    Ref(fx,"_burst",Sparks(parent,"CastSparks",36));Ref(root.GetComponent<InitiatorSkill_E>(),"_presentation",fx);
                    var castAudio=Component<AudioSource>(Child(parent,"PulseAudio"));castAudio.playOnAwake=false;castAudio.clip=cast;castAudio.spatialBlend=1;castAudio.minDistance=2;castAudio.maxDistance=22;castAudio.volume=.6f;Ref(fx,"_castAudio",castAudio);
                    var hit=Component<AudioSource>(Child(parent,"ConfirmedAudio"));hit.playOnAwake=false;hit.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Drone/Confirmed.wav");hit.spatialBlend=1;hit.minDistance=1;hit.maxDistance=10;hit.volume=.28f;Ref(fx,"_hitAudio",hit);
                    foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="EMPWristEmitter").ToArray())Object.DestroyImmediate(old.gameObject);
                    Refs(fx,"_vents",Array.Empty<Transform>());Refs(fx,"_devices",Array.Empty<GameObject>());Refs(fx,"_cores",Array.Empty<Renderer>());
                    AuthorGesture(root);
                    var legacy=root.GetComponent<LineRenderer>();if(legacy)legacy.enabled=false;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
    static void AuthorGesture(GameObject root)
    {
        var rig=Component<EmpGestureRig>(root.transform);Ref(root.GetComponent<InitiatorSkill_E>(),"_gesture",rig);
        var data=new SerializedObject(rig);var poses=data.FindProperty("_poses");poses.arraySize=2;
        for(int index=0;index<2;index++)
        {
            bool fps=index==0;
            var model=root.transform.Find(fps?"CameraRoot/Controller Model_Riging (1)":"Model");
            var frame=fps?model.parent:root.transform;
            var all=model.GetComponentsInChildren<Transform>(true);
            if(fps)foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(skin.name=="Arm1" || skin.name=="Arm2")skin.updateWhenOffscreen=true;
            Func<string,Transform> bone=n=>all.First(t=>t.name=="mixamorig:"+n || t.name.EndsWith("_"+n) || t.name==n);
            var bones=new[]{bone("LeftArm"),bone("LeftForeArm"),bone("LeftHand"),bone("RightArm")};
            var leftHand=bone("LeftHand");
            var fingerCandidates=leftHand.GetComponentsInChildren<Transform>(true).Where(t=>t!=leftHand && (t.name.StartsWith("mixamorig:") || t.name.StartsWith("Character1_"))).ToArray();
            var fingerBase=fingerCandidates.FirstOrDefault(t=>t.name.IndexOf("Middle",StringComparison.OrdinalIgnoreCase)>=0)??fingerCandidates.First();
            var ends=new[]{bone("LeftForeArm"),leftHand,fingerBase,bone("RightForeArm")};
            var rest=bones.Select(t=>t.localRotation).ToArray();
            var poseRoot=Child(frame,"EMPGesturePoses");
            var anchor=Child(poseRoot,"LeftShoulderAnchor");anchor.localPosition=new Vector3(-.33f,-.45f,.4f);
            var charge=new Transform[4];var release=new Transform[4];
            for(int stage=0;stage<2;stage++)
            {
                var dirs=stage==0?new[]{new Vector3(.2f,-.1f,.8f),new Vector3(.15f,.8f,.55f),new Vector3(0,.9f,.25f),new Vector3(0,-1,-.3f)}:
                    new[]{new Vector3(.1f,.05f,.9f),new Vector3(0,.2f,1),new Vector3(0,1,.1f),new Vector3(0,-1,-.3f)};
                for(int i=0;i<bones.Length;i++)
                {
                    bones[i].rotation=Quaternion.FromToRotation(ends[i].position-bones[i].position,frame.TransformDirection(dirs[i]))*bones[i].rotation;
                    if(fps && i==2)bones[i].rotation=Quaternion.AngleAxis(90,frame.TransformDirection(dirs[i]).normalized)*bones[i].rotation;
                    var target=Child(poseRoot,(stage==0?"Gather_":"Release_")+bones[i].name);
                    target.rotation=bones[i].rotation;(stage==0?charge:release)[i]=target;
                }
                for(int i=0;i<bones.Length;i++)bones[i].localRotation=rest[i];
            }
            var hand=bone("LeftHand");var fingers=fingerCandidates;
            var energy=Line(hand,"EMPHandCharge",false,false);energy.gameObject.layer=hand.gameObject.layer;
            energy.positionCount=0;energy.widthMultiplier=.004f;
            // Hand bones are centimetre-scaled, so keep effect geometry in world metres.
            energy.transform.localScale=Vector3.one/Mathf.Max(.001f,hand.lossyScale.x);
            var p=poses.GetArrayElementAtIndex(index);
            p.FindPropertyRelative("frame").objectReferenceValue=frame;p.FindPropertyRelative("viewModel").objectReferenceValue=fps?model:null;
            p.FindPropertyRelative("shoulder").objectReferenceValue=bones[0];p.FindPropertyRelative("shoulderAnchor").objectReferenceValue=anchor;
            p.FindPropertyRelative("hand").objectReferenceValue=hand;p.FindPropertyRelative("energy").objectReferenceValue=energy;
            p.FindPropertyRelative("openWrist").quaternionValue=hand.localRotation;
            foreach(var pair in new[]{("bones",bones),("charge",charge),("release",release),("fingers",fingers)}){
                var a=p.FindPropertyRelative(pair.Item1);a.arraySize=pair.Item2.Length;for(int i=0;i<pair.Item2.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=pair.Item2[i];
            }
            var open=p.FindPropertyRelative("openHand");open.arraySize=fingers.Length;for(int i=0;i<fingers.Length;i++)open.GetArrayElementAtIndex(i).quaternionValue=fingers[i].localRotation;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color){var im=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
    static TMP_Text Text(Transform parent,string name,string text,float x,float y,float w,float h,float size){var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset");t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.9f,.96f,1);t.raycastTarget=false;return t;}
    static void AuthorUI()
    {
        string originalScene=File.ReadAllText(ScenePath);
        var scene=EditorSceneManager.OpenScene(ScenePath);var old=GameObject.Find("ElectricStatusCanvas");if(old)Object.DestroyImmediate(old);
        var root=new GameObject("ElectricStatusCanvas",typeof(Canvas),typeof(CanvasScaler));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=45;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var ui=root.AddComponent<ElectricStatusUI>();var panel=Rect(root.transform,"LocalStatus",0,-230,228,53);
        Image(panel,"Backing",0,0,228,53,new Color(.025f,.045f,.055f,.86f));Image(panel,"Accent",-112,0,3,53,Cyan);
        var icon=Image(panel,"SkillIcon",-81,0,32,32,Cyan);icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/Initiator/Initiator_E.png");icon.preserveAspect=true;
        var label=Text(panel,"Status","기절  2.0초",15,6,160,28,22);
        var stacks=Enumerable.Range(0,3).Select(i=>Image(panel,"Stack_"+(i+1),-35+i*43,-15,36,3,new Color(.22f,.28f,.32f))).ToArray();
        var fill=Image(panel,"Remaining",0,-26,228,3,Cyan);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;
        // A saved white sprite is required for Filled geometry; no runtime texture creation.
        string pixelPath=Dir+"/FlatFill.png";var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();File.WriteAllBytes(pixelPath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(pixelPath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(pixelPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(pixelPath);
        var confirmation=Rect(root.transform,"CastConfirmation",0,-73,300,30);var text=Text(confirmation,"ConfirmedCount","방전  2명",0,0,300,30,19);
        Ref(ui,"_statusRoot",panel.gameObject);Ref(ui,"_statusText",label);Refs(ui,"_stacks",stacks);Ref(ui,"_remaining",fill);Ref(ui,"_confirmationRoot",confirmation.gameObject);Ref(ui,"_confirmationText",text);
        panel.gameObject.SetActive(false);confirmation.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,Dir+"/ElectricStatusHUD.prefab",InteractionMode.AutomatedAction);EditorSceneManager.SaveScene(scene);
        // Unity may reserialize unrelated nested UI prefabs when opening this scene.
        // Keep the authored HUD instance only; preserve every other original YAML document.
        SaveOnlyHud(originalScene,File.ReadAllText(ScenePath));
    }
    static void SaveOnlyHud(string original,string authored)
    {
        const string roots="--- !u!1660057539 &9223372036854775807";
        string guid=AssetDatabase.AssetPathToGUID(Dir+"/ElectricStatusHUD.prefab");
        var docs=System.Text.RegularExpressions.Regex.Split(authored,@"(?m)(?=^--- !u!)");
        var instance=docs.Single(d=>d.StartsWith("--- !u!1001 ")&&d.Contains("guid: "+guid));
        string instanceId=System.Text.RegularExpressions.Regex.Match(instance,@"&([0-9]+)").Groups[1].Value;
        var transform=docs.SingleOrDefault(d=>d.StartsWith("--- !u!224 ")&&d.Contains("m_PrefabInstance: {fileID: "+instanceId+"}"))??"";
        string rootId=transform.Length>0?System.Text.RegularExpressions.Regex.Match(transform,@"&([0-9]+)").Groups[1].Value:instanceId;
        var oldDocs=System.Text.RegularExpressions.Regex.Split(original,@"(?m)(?=^--- !u!)");
        foreach(var old in oldDocs.Where(d=>d.StartsWith("--- !u!1001 ")&&d.Contains("guid: "+guid))){
            string id=System.Text.RegularExpressions.Regex.Match(old,@"&([0-9]+)").Groups[1].Value;
            original=original.Replace(old,"");
            original=System.Text.RegularExpressions.Regex.Replace(original,@"(?m)^  - \{fileID: "+id+@"\}\r?\n","");
            foreach(var t in oldDocs.Where(d=>d.StartsWith("--- !u!224 ")&&d.Contains("m_PrefabInstance: {fileID: "+id+"}"))){
                string tid=System.Text.RegularExpressions.Regex.Match(t,@"&([0-9]+)").Groups[1].Value;
                original=original.Replace(t,"");original=System.Text.RegularExpressions.Regex.Replace(original,@"(?m)^  - \{fileID: "+tid+@"\}\r?\n","");
            }
        }
        if(!original.Contains(roots))throw new Exception("Missing scene roots, refusing scene merge");
        File.WriteAllText(ScenePath,original.Replace(roots,instance+transform+roots).TrimEnd()+"\n  - {fileID: "+rootId+"}\n");
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Use an isolated editor batch, never an unsaved user scene");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();AuthorActors(Sound());FirstPersonSkillMotionAuthoring.Run();AuthorUI();AssetDatabase.SaveAssets();
        SkillVfxClarityAuthoring.Run();
        Debug.Log("EMP_AUTHORING_PASS: device-free hand gesture, 4 receivers, independent confirmed arcs, 7m cast waves and saved local HUD");
    }
}
#endif
