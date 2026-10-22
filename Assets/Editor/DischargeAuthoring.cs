#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
using UI=ScannerFeedbackAuthoring;

public static class DischargeAuthoring
{
    public const string Dir="Assets/Art/Discharge";
    public const string Actor="Assets/Prefab/Character/Initiator/Initiator.prefab";
    public const string Hud="Assets/Art/EMP/ElectricStatusHUD.prefab";
    static readonly Color Cyan=new(.12f,.72f,.92f), Amber=new(.95f,.48f,.12f);
    static Transform Child(Transform p,string n){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
    static void Refs(Object target,string field,IEnumerable<Object> values)
    {var s=new SerializedObject(target);var p=s.FindProperty(field);var a=values.ToArray();p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static Material Mat(string name,Color color,bool glow=false)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.7f);m.SetFloat("_Smoothness",.5f);
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.8f);}EditorUtility.SetDirty(m);return m;
    }
    static void MeshPart(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale,Quaternion rotation)
    {
        var t=Child(parent,name);t.localPosition=position;t.localScale=scale;t.localRotation=rotation;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static Mesh Wing()
    {
        var outline=new[]{new Vector2(-.06f,-.12f),new Vector2(-.40f,-.24f),new Vector2(-.49f,-.16f),new Vector2(-.24f,.16f),new Vector2(-.08f,.18f)};
        var v=new List<Vector3>();foreach(float y in new[]{-.035f,.035f})foreach(var p in outline)v.Add(new Vector3(p.x,y,p.y));
        var triangles=new List<int>();int n=outline.Length;
        for(int i=1;i<n-1;i++){triangles.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});}
        for(int i=0;i<n;i++){int j=(i+1)%n;triangles.AddRange(new[]{i,n+i,j,j,n+i,n+j});}
        var flat=triangles.Select(i=>v[i]).ToArray();var mesh=new Mesh{name="SweptRelayWing"};mesh.vertices=flat;mesh.triangles=Enumerable.Range(0,flat.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Dir+"/SweptRelayWing.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);return existing;}AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static LineRenderer Line(Transform p,string name,Color color,float width)
    {
        var line=Child(p,name).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyBeam.mat");
        line.useWorldSpace=true;line.positionCount=0;line.enabled=false;line.widthCurve=AnimationCurve.Constant(0,1,1);line.widthMultiplier=width;line.numCapVertices=2;line.numCornerVertices=2;
        line.startColor=line.endColor=color;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
    }
    static ParticleSystem Sparks(Transform parent,int index)
    {
        var ps=Child(parent,"ConfirmedHitSparks_"+index).gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.playOnAwake=false;m.loop=false;m.duration=.4f;m.startLifetime=new ParticleSystem.MinMaxCurve(.08f,.24f);m.startSpeed=new ParticleSystem.MinMaxCurve(1,4);m.startSize=new ParticleSystem.MinMaxCurve(.012f,.025f);m.startColor=Cyan;m.maxParticles=32;m.simulationSpace=ParticleSystemSimulationSpace.World;
        var e=ps.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,22)});var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Sphere;sh.radius=.08f;
        var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Cyan,0),new GradientColorKey(Cyan,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});colors.color=gradient;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyParticle.mat");r.renderMode=ParticleSystemRenderMode.Stretch;r.lengthScale=2;r.velocityScale=.04f;return ps;
    }
    static GameObject Device()
    {
        var black=Mat("Graphite",new Color(.035f,.05f,.07f));var metal=Mat("MachinedTitanium",new Color(.32f,.40f,.46f));var cyan=Mat("Emitter",Cyan,true);var amber=Mat("Copper",Amber);
        var core=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Scanner/BeveledSensorCore.asset");if(!core)throw new Exception("Missing existing sensor mesh");var wing=Wing();
        var root=new GameObject("DischargeRelay");
        MeshPart(root.transform,"ArmoredCore",core,black,Vector3.zero,new Vector3(1.4f,1.7f,1.9f),Quaternion.identity);
        MeshPart(root.transform,"EmitterBelow",core,cyan,new Vector3(0,-.12f,0),new Vector3(.6f,.3f,.6f),Quaternion.identity);
        for(int i=0;i<2;i++){
            var side=Child(root.transform,i==0?"PortWing":"StarboardWing");side.localRotation=Quaternion.Euler(0,i*180,0);
            MeshPart(side,"SweptArmor",wing,black,Vector3.zero,Vector3.one,Quaternion.identity);
            MeshPart(side,"InsetPlate",wing,metal,Vector3.up*.035f,new Vector3(.88f,.22f,.84f),Quaternion.identity);
            MeshPart(side,"ChargedRail",wing,cyan,Vector3.up*.045f,new Vector3(.82f,.1f,.35f),Quaternion.identity);
            for(int j=0;j<3;j++)MeshPart(side,"Coil_"+j,core,amber,new Vector3(-.30f,.045f,-.12f+j*.065f),new Vector3(.28f,.28f,.19f),Quaternion.Euler(90,0,0));
        }
        MeshPart(root.transform,"DorsalFin",wing,metal,new Vector3(0,.06f,.03f),new Vector3(.55f,.5f,.5f),Quaternion.Euler(0,0,-90));
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Dir+"/DischargeRelay.prefab");Object.DestroyImmediate(root);return prefab;
    }
    static DischargeVfx Effect(GameObject device)
    {
        var root=new GameObject("DischargeSequence");var fx=root.AddComponent<DischargeVfx>();
        try{
            var relayRoot=Child(root.transform,"Relays");var models=new List<Transform>();
            for(int i=0;i<7;i++){var g=(GameObject)PrefabUtility.InstantiatePrefab(device,relayRoot);g.name="Relay_"+(i+1);models.Add(g.transform);}
            Refs(fx,"_relays",models);var warning=Child(root.transform,"Telegraph");
            UI.Ref(fx,"_boundary",Line(warning,"ConeBoundary_120deg",new Color(1,.52f,.15f,.85f),.045f));
            Refs(fx,"_bands",Enumerable.Range(0,7).Select(i=>Line(warning,"Wavefront_"+i,new Color(.9f,.38f,.1f,.38f),.025f)));
            var strikes=Child(root.transform,"ServerConfirmedStrikes");Refs(fx,"_bolts",Enumerable.Range(0,13).Select(i=>Line(strikes,i<7?"GroundStrike_"+i:"TargetStrike_"+(i-7),i<7?Cyan:new Color(.62f,.9f,1),.035f)));
            var sector=SectorMesh();var sectorMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Sector.mat");
            if(!sectorMaterial){sectorMaterial=new Material(Shader.Find("Vector/DischargeSector"));AssetDatabase.CreateAsset(sectorMaterial,Dir+"/Sector.mat");}
            Refs(fx,"_sectors",Enumerable.Range(0,7).Select(i=>{
                var t=Child(strikes,"FullDamageBand_"+(i+1));var f=t.gameObject.AddComponent<MeshFilter>();f.sharedMesh=sector;
                var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=sectorMaterial;r.enabled=false;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return f;
            }));
            Refs(fx,"_boltCores",Enumerable.Range(0,13).Select(i=>Line(strikes,"LightningCore_"+i,new Color(.78f,.95f,1),.035f)));
            Refs(fx,"_fanBolts",Enumerable.Range(0,42).Select(i=>Line(strikes,"ForkedLightning_"+i,new Color(.13f,.65f,.95f,.95f),.23f)));
            Refs(fx,"_fanCores",Enumerable.Range(0,42).Select(i=>Line(strikes,"ForkedCore_"+i,new Color(.62f,.91f,1),.065f)));
            Refs(fx,"_chargeRings",Enumerable.Range(0,7).Select(i=>Line(relayRoot,"ChargeHalo_"+i,new Color(.18f,.8f,1),.025f)));
            Refs(fx,"_shockRings",Enumerable.Range(0,7).Select(i=>Line(strikes,"ExpandingShock_"+i,new Color(.12f,.66f,1,.8f),.05f)));
            Refs(fx,"_fractures",Enumerable.Range(0,7).Select(i=>Line(strikes,"ResidualGroundArcs_"+i,new Color(.35f,.85f,1,.8f),.03f)));
            Refs(fx,"_groundSparks",Enumerable.Range(0,7).Select(i=>{
                var p=Sparks(strikes,100+i);var m=p.main;m.maxParticles=100;m.startSpeed=new ParticleSystem.MinMaxCurve(3,8);m.startLifetime=new ParticleSystem.MinMaxCurve(.12f,.4f);m.startSize=new ParticleSystem.MinMaxCurve(.025f,.065f);m.gravityModifier=.6f;
                var e=p.emission;e.SetBursts(new[]{new ParticleSystem.Burst(0,65)});var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.45f;return p;
            }));
            Refs(fx,"_strikeLights",Enumerable.Range(0,7).Select(i=>{
                var light=Child(strikes,"StrikeLight_"+i).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=Cyan;light.range=5;light.intensity=0;light.shadows=LightShadows.None;return light;
            }));
            Refs(fx,"_impactRings",Enumerable.Range(0,6).Select(i=>Line(strikes,"TargetImpact_"+i,Cyan,.025f)));Refs(fx,"_sparks",Enumerable.Range(0,6).Select(i=>Sparks(strikes,i)));
            var audio=Child(root.transform,"WorldAudio").gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=26;audio.rolloffMode=AudioRolloffMode.Linear;
            UI.Ref(fx,"_audio",audio);UI.Ref(fx,"_deploy",AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/DischargeCharge.wav"));UI.Ref(fx,"_strike",AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/DischargeImpact.wav"));
            return PrefabUtility.SaveAsPrefabAsset(root,Dir+"/DischargeSequence.prefab").GetComponent<DischargeVfx>();
        }finally{Object.DestroyImmediate(root);}
    }
    static void BindActor()
    {
        var root=PrefabUtility.LoadPrefabContents(Actor);
        try{
            var skill=root.GetComponent<InitiatorSkill_X>();var data=new SerializedObject(skill);var rig=data.FindProperty("_gesture").objectReferenceValue as EmpGestureRig;
            if(!rig)rig=root.AddComponent<EmpGestureRig>();
            var source=new SerializedObject(root.GetComponent<EmpGestureRig>());
            var s=new SerializedObject(rig);s.CopyFromSerializedProperty(source.FindProperty("_poses"));
            s.FindProperty("_motionType").enumValueIndex=(int)FirstPersonSkillMotion.Motion.Ultimate;s.FindProperty("_windup").floatValue=.64f;s.FindProperty("_recovery").floatValue=.5f;
            var poses=s.FindProperty("_poses");for(int i=0;i<poses.arraySize;i++)poses.GetArrayElementAtIndex(i).FindPropertyRelative("energy").objectReferenceValue=null;
            s.ApplyModifiedPropertiesWithoutUndo();UI.Ref(skill,"_gesture",rig);
            UI.Ref(root.GetComponent<FirstPersonSkillMotion>(),"_ultimate",ScannerPolishAuthoring.Clip(root,true));
            var motion=root.GetComponent<FirstPersonSkillMotion>();
            var arms=root.transform.Find("CameraRoot/Controller Model_Riging (1)").GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name=="Arm1"||r.name=="Arm2").ToArray();
            Refs(motion,"_ultimateRenderers",arms);Refs(motion,"_ultimateLeftMeshes",arms.Select(LeftMesh));
            PrefabUtility.SaveAsPrefabAsset(root,Actor);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BindHud(DischargeVfx fx)
    {
        var root=PrefabUtility.LoadPrefabContents(Hud);
        try{
            var ui=root.GetComponent<DischargeDirector>();if(!ui)ui=root.AddComponent<DischargeDirector>();UI.Ref(ui,"_effect",fx);
            var owner=UI.Group(root.transform,"DischargeConfirmed",-156,242,34);UI.Bar(owner.transform,"Background",0,0,242,34,new Color(.02f,.035f,.05f,.83f));
            UI.Bar(owner.transform,"Accent",-120,0,2,34,Cyan);UI.Label(owner.transform,"Key","X",-102,0,28,28,19,Cyan);
            var text=UI.Label(owner.transform,"Count","방전 적중  1명",12,0,200,32,20,new Color(.65f,.9f,1));
            var victim=UI.Group(root.transform,"DischargeReceived",-215,228,32);UI.Bar(victim.transform,"Background",0,0,228,32,new Color(.06f,.03f,.02f,.8f));UI.Label(victim.transform,"Text","방전에 맞음",0,0,220,32,20,new Color(1,.69f,.35f));
            var pulse=UI.Group(root.transform,"DischargeImpactPulse",0,1920,1080);
            foreach(int side in new[]{-1,1}){
                UI.Bar(pulse.transform,"Edge_"+side,side*420,0,4,135,new Color(.35f,.82f,1,.8f));
                UI.Bar(pulse.transform,"Top_"+side,side*405,67,34,3,Cyan);UI.Bar(pulse.transform,"Bottom_"+side,side*405,-67,34,3,Cyan);
            }
            UI.Ref(ui,"_ownerNotice",owner);UI.Ref(ui,"_ownerText",text);UI.Ref(ui,"_victimNotice",victim);UI.Ref(ui,"_victimPulse",pulse);
            UI.Ref(ui,"_ownerAudio",UI.Sound(root.transform,"DischargeConfirmedAudio","Confirmed",.55f,.78f));UI.Ref(ui,"_victimAudio",UI.Sound(root.transform,"DischargeReceivedAudio","Interrupted",.48f,.7f));
            ui.ResetFeedback();PrefabUtility.SaveAsPrefabAsset(root,Hud);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch authoring only");
        Directory.CreateDirectory(Dir);Audio(false);Audio(true);AssetDatabase.Refresh();BindHud(Effect(Device()));BindActor();AssetDatabase.SaveAssets();Debug.Log("DISCHARGE_AUTHOR_PASS");
    }
    static void Audio(bool impact)
    {
        const int rate=44100;float duration=impact?.55f:.7f;int count=(int)(rate*duration);var random=new System.Random(impact?401:402);
        using var stream=new BinaryWriter(File.Create(Dir+(impact?"/DischargeImpact.wav":"/DischargeCharge.wav")));
        stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+count*2);stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(count*2);
        float low=0;
        for(int i=0;i<count;i++){
            float t=i/(float)rate,n=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,n,.07f);
            float sample;
            if(impact){float transient=Mathf.Exp(-t*65),tail=Mathf.Exp(-t*9);sample=n*transient*.5f+low*tail*.6f+Mathf.Sin(2*Mathf.PI*(90*t-45*t*t))*tail*.32f;}
            else {float ramp=t/duration;float envelope=Mathf.Sin(Mathf.Clamp01(ramp)*Mathf.PI)*.4f;sample=(Mathf.Sin(2*Mathf.PI*(180*t+420*t*t))*.6f+n*.13f)*envelope;}
            float fade=Mathf.Min(1,t/.003f)*Mathf.Clamp01((duration-t)/.025f);stream.Write((short)(Mathf.Clamp(sample*fade,-.9f,.9f)*32767));
        }
    }
    static Mesh SectorMesh()
    {
        const int columns=33,rows=3;var vertices=new Vector3[columns*rows];var uv=new Vector2[vertices.Length];var indices=new List<int>();
        for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){
            int i=row*columns+col;uv[i]=new Vector2(row/(float)(rows-1),col/(float)(columns-1));
            vertices[i]=new Vector3(col,0,row);
            if(row<rows-1&&col<columns-1)indices.AddRange(new[]{i,i+columns,i+1,i+1,i+columns,i+columns+1});
        }
        var mesh=new Mesh{name="DamageBandTemplate"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateBounds();
        string path=Dir+"/DamageBandTemplate.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh LeftMesh(SkinnedMeshRenderer renderer)
    {
        var mesh=Object.Instantiate(renderer.sharedMesh);mesh.name="Ultimate_LeftOnly_"+renderer.name;var weights=mesh.boneWeights;
        float Left(int vertex){var w=weights[vertex];float total=0;int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};for(int i=0;i<4;i++)if(renderer.bones[ids[i]].name.Contains("Left"))total+=values[i];return total;}
        for(int sub=0;sub<mesh.subMeshCount;sub++){
            var source=mesh.GetTriangles(sub);var keep=new List<int>();
            for(int i=0;i<source.Length;i+=3)if((Left(source[i])+Left(source[i+1])+Left(source[i+2]))/3>.5f)keep.AddRange(new[]{source[i],source[i+1],source[i+2]});mesh.SetTriangles(keep,sub);
        }
        string path=Dir+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
#endif
