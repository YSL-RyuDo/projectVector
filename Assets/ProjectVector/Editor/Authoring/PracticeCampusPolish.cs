#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PracticeCampusPolish
{
    const string Scene="Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity", AssetsDir="Assets/ProjectVector/Modules/Practice/Assets/";
    static Transform root, detail;
    static Material dark, pale, accent, stone, soil, grip, lamp;
    public static void Apply()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(Scene);
        root=GameObject.Find("HydroTrainingCampus").transform;
        if(root.Find("ArtFinish")!=null)throw new Exception("Art finish already authored; do not overwrite manual edits.");
        File.Copy(Scene,Path.Combine(Path.GetTempPath(),"WeaponTest-before-art-polish.unity"),true);
        detail=new GameObject("ArtFinish").transform;detail.SetParent(root,false);
        dark=Surface("Charcoal",new Color(.16f,.21f,.21f),.13f);
        pale=Surface("ChalkConcrete",new Color(.64f,.65f,.59f),.28f);
        accent=Surface("OxidePaint",new Color(.64f,.24f,.13f),.13f);
        stone=Surface("CliffStone",new Color(.30f,.36f,.34f),.3f);
        soil=Surface("AlpineGround",new Color(.22f,.29f,.21f),.5f);
        grip=Surface("RangeAsphalt",new Color(.24f,.28f,.28f),.4f);
        lamp=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.96f,.88f,.65f)};
        lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.84f,.56f)*2);
        AssetDatabase.CreateAsset(lamp,AssetsDir+"FinishLamp.mat");
        ConvertBundledMaterials();
        foreach(var t in root.GetComponentsInChildren<Transform>())
        {
            if(t.name=="Foundation" || t.name=="ContinuousWalkway")t.GetComponent<Renderer>().sharedMaterial=pale;
            if(t.name=="ContinuousCampusGround")t.GetComponent<Renderer>().sharedMaterial=soil;
            if(t.name=="FloorJoint")t.GetComponent<Renderer>().sharedMaterial=dark;
            if(t.name=="IntakeTower")t.GetComponent<Renderer>().sharedMaterial=pale;
        }
        for(int i=0;i<4;i++)FinishBay(200+i*32,i);
        Landscape();Lighting();
        // Only the new static environment is eligible for static batching; gameplay stays untouched.
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
            if(r.GetComponent<TMPro.TMP_Text>()==null)GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Scene);PracticeCampusEditor.Validate();
        Capture();
        Debug.Log("CAMPUS_ART_PASS: authored surfaces, facade cladding, drainage, equipment dressing, layered ridge, scoped shadows. Gameplay references and existing colliders preserved.");
    }
    static Material Surface(string name,Color color,float weather)
    {
        var m=new Material(Shader.Find("Vector/PracticeSurface"));m.SetColor("_BaseColor",color);m.SetFloat("_DetailStrength",weather);m.SetFloat("_Scale",30);
        AssetDatabase.CreateAsset(m,AssetsDir+"Finish"+name+".mat");return m;
    }
    static void ConvertBundledMaterials()
    {
        var cache=new System.Collections.Generic.Dictionary<Material,Material>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.GetComponent<TMPro.TMP_Text>()!=null)continue;
            var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                var src=mats[i];if(src==null || !AssetDatabase.GetAssetPath(src).StartsWith("Assets/ThirdParty/FPS_Bundle/"))continue;
                if(!cache.TryGetValue(src,out var copy))
                {
                    copy=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    var tex=src.HasProperty("_BaseMap")?src.GetTexture("_BaseMap"):src.mainTexture;
                    copy.SetTexture("_BaseMap",tex);copy.SetColor("_BaseColor",Color.white);
                    copy.SetFloat("_Smoothness",.15f);copy.enableInstancing=true;
                    AssetDatabase.CreateAsset(copy,AssetsDir+"Finish_"+src.name+".mat");cache[src]=copy;
                }
                mats[i]=copy;
            }
            renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        }
    }
    static void FinishBay(float x,int index)
    {
        float depth=index==0?36:31;
        var p=Group("BayFinish_"+(index+1));
        // Thin visual overlays have no colliders: the proven shooting/walking envelope is unchanged.
        Box(p,"WorkingSurface",new Vector3(x,.002f,depth/2),new Vector3(index==0?6:18,.003f,depth),index==1?pale:grip);
        foreach(float side in new[]{-1f,1f})
        {
            Box(p,"WallCladding",new Vector3(x+side*12.7f,1.6f,depth/2),new Vector3(.06f,2.5f,depth),index==1?dark:pale);
            Box(p,"WallStripe",new Vector3(x+side*12.64f,2.7f,depth/2),new Vector3(.07f,.18f,depth),index==1?accent:dark);
            Box(p,"DrainBed",new Vector3(x+side*11.5f,.013f,depth/2),new Vector3(.36f,.012f,depth),dark);
            for(float z=1;z<depth;z+=.45f)Box(p,"DrainGrate",new Vector3(x+side*11.5f,.024f,z),new Vector3(.38f,.018f,.055f),pale);
            for(float z=3;z<depth;z+=6)
            {
                Box(p,"PanelSeam",new Vector3(x+side*12.58f,1.5f,z),new Vector3(.02f,2.3f,.025f),dark);
                Box(p,"WallLampHousing",new Vector3(x+side*12.1f,3.2f,z),new Vector3(.22f,.28f,1.4f),dark);
                Box(p,"WallLampLens",new Vector3(x+side*11.96f,3.16f,z),new Vector3(.025f,.13f,1.15f),lamp);
            }
            // Maintenance dressing confined to wall-side strips, outside target and walking routes.
            Prop("FPS_Map3/Prefabs/Props/Barrel_02.prefab",p,new Vector3(x+side*10.3f,0,depth-2),.75f,0);
            Prop("FPS_Map3/Prefabs/Props/FireExtinguisher_01.prefab",p,new Vector3(x+side*12.2f,0,5),1,side>0?90:270);
            Prop("FPS_Map3/Prefabs/Props/Box_01.prefab",p,new Vector3(x+side*10.6f,0,depth-4),.7f,10);
        }
        for(int n=0;n<6;n++)
        {
            float z=3+n*5;
            Box(p,"ServicePaint",new Vector3(x-9,.017f,z),new Vector3(.12f,.014f,1.7f),pale);
        }
        // Continuous service cable tray and roof luminaires distinguish the indoor halls.
        if(index==1 || index==3)
        {
            foreach(float side in new[]{-6f,6f})
            {
                Box(p,"CableTray",new Vector3(x+side,5.5f,16),new Vector3(.24f,.14f,30),dark);
                for(int z=6;z<=26;z+=10)
                {
                    Box(p,"PendantHousing",new Vector3(x+side,5.2f,z),new Vector3(2.5f,.16f,.32f),dark);
                    Box(p,"PendantLens",new Vector3(x+side,5.1f,z),new Vector3(2.25f,.04f,.22f),lamp);
                }
            }
        }
        if(index==0)
        {
            for(int z=2;z<35;z++)foreach(float side in new[]{-3.35f,3.35f})
                Box(p,"MeterTick",new Vector3(x+side,.017f,z),new Vector3(z%5==0?.6f:.25f,.016f,.045f),pale);
            for(int k=0;k<14;k++)
            {
                var stripe=Box(p,"HazardMark",new Vector3(x-5+k*.8f,.022f,-.9f),new Vector3(.28f,.018f,.7f),accent);
                stripe.transform.rotation=Quaternion.Euler(0,25,0);
            }
            // Narrow front baffles give the firing line a real structure without entering the lane.
            foreach(float side in new[]{-5.3f,5.3f})
            {
                Box(p,"AcousticBaffle",new Vector3(x+side,1.5f,1.2f),new Vector3(.12f,3,3),dark);
                for(int z=0;z<6;z++)Box(p,"BaffleSlat",new Vector3(x+side+.08f,1.5f,z*.45f),new Vector3(.08f,2.8f,.04f),pale);
            }
        }
        if(index==2)
        {
            // Hoist and cable are above the entire combat envelope.
            Box(p,"HoistCarriage",new Vector3(x+2,4.65f,19),new Vector3(1.4f,.5f,.8f),dark);
            Box(p,"HoistCable",new Vector3(x+2,3.9f,19),new Vector3(.04f,1.1f,.04f),dark);
        }
    }
    static float Height(float x,float z)
    {
        float rise=Mathf.SmoothStep(0,1,(z-42)/38);
        return -1+rise*(12+8*Mathf.PerlinNoise(x*.023f,z*.025f)+9*Mathf.PerlinNoise(x*.049f+4,z*.037f));
    }
    static void Landscape()
    {
        var landscape=root.Find("05_ReservoirAndMountainBoundary");
        int n=0;
        foreach(Transform t in landscape)
        {
            if(t.name=="Rock_01")
            {
                // Keep source meshes but bury/flatten the previously oversized round boulders.
                t.localScale=Vector3.Scale(t.localScale,new Vector3(1.15f,.32f,.9f));
                t.position+=new Vector3(0,-1.8f,2);t.GetComponentInChildren<Renderer>().sharedMaterial=stone;
            }
            if(t.name=="Tree_01")
            {
                float x=t.position.x,z=t.position.z;
                if(z>40)z=49+(n++%4)*9;
                t.position=new Vector3(x,Height(x,z)-.4f,z);
                t.localScale=Vector3.one*(1.6f+(n%4)*.2f);
            }
        }
        const int nx=80,nz=32;var verts=new Vector3[(nx+1)*(nz+1)];var triangles=new int[nx*nz*6];
        for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
        {
            float x=140+i*2.7f,z=41+j*3;verts[j*(nx+1)+i]=new Vector3(x,Height(x,z),z);
        }
        int k=0;for(int j=0;j<nz;j++)for(int i=0;i<nx;i++)
        {int a=j*(nx+1)+i;triangles[k++]=a;triangles[k++]=a+nx+1;triangles[k++]=a+1;triangles[k++]=a+1;triangles[k++]=a+nx+1;triangles[k++]=a+nx+2;}
        var mesh=new Mesh{name="HydroAlpineRidge"};mesh.vertices=verts;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,AssetsDir+"HydroAlpineRidge.asset");
        var ridge=new GameObject("LayeredAlpineRidge",typeof(MeshFilter),typeof(MeshRenderer));ridge.transform.SetParent(detail,false);
        ridge.GetComponent<MeshFilter>().sharedMesh=mesh;ridge.GetComponent<MeshRenderer>().sharedMaterial=soil;
        var trees=Group("DistantConifers");
        for(int i=0;i<55;i++)
        {
            float x=153+i*3.6f,z=63+Mathf.PerlinNoise(i*.73f,2)*39;
            Prop("FPS_Map0/Prefabs/Tree_01.prefab",trees,new Vector3(x,Height(x,z)-.4f,z),1.4f+(i%5)*.22f,i*137);
        }
    }
    static void Lighting()
    {
        var pipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/ProjectVector/Shared/Rendering/Settings/PC_RPAsset.asset"));
        pipeline.name="HydroTrainingPipeline";
        var so=new SerializedObject(pipeline);so.FindProperty("m_MainLightShadowsSupported").boolValue=true;
        so.FindProperty("m_ShadowDistance").floatValue=95;so.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
        so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(pipeline,AssetsDir+"HydroTrainingPipeline.asset");
        root.gameObject.AddComponent<PracticeSceneRendering>().trainingPipeline=pipeline;
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)
        {light.transform.rotation=Quaternion.Euler(38,-48,0);light.color=new Color(1,.92f,.78f);light.intensity=1.7f;light.shadows=LightShadows.Soft;}
        RenderSettings.ambientSkyColor=new Color(.38f,.49f,.53f);RenderSettings.ambientEquatorColor=new Color(.30f,.34f,.32f);RenderSettings.ambientGroundColor=new Color(.18f,.20f,.17f);
        RenderSettings.fogColor=new Color(.64f,.72f,.70f);RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=220;
        var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.53f,.57f,.55f));sky.SetColor("_GroundColor",new Color(.35f,.40f,.35f));sky.SetFloat("_AtmosphereThickness",1.1f);sky.SetFloat("_Exposure",1.1f);
        AssetDatabase.CreateAsset(sky,AssetsDir+"HydroSky.mat");RenderSettings.skybox=sky;
    }
    public static void Capture()
    {
        var setting=UnityEngine.Object.FindFirstObjectByType<PracticeSceneRendering>();var old=QualitySettings.renderPipeline;
        try{QualitySettings.renderPipeline=setting.trainingPipeline;PracticeCampusEditor.CaptureCampus();}
        finally{QualitySettings.renderPipeline=old;}
    }
    public static void Refine()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(Scene);root=GameObject.Find("HydroTrainingCampus").transform;detail=root.Find("ArtFinish");
        foreach(string name in new[]{"RangeAsphalt","ChalkConcrete","AlpineGround"})
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetsDir+"Finish"+name+".mat");mat.SetFloat("_DetailStrength",name=="AlpineGround"?.24f:.16f);EditorUtility.SetDirty(mat);
        }
        // Larger visual structures also block movement/fire, all outside the validated firing lanes.
        foreach(var t in detail.GetComponentsInChildren<Transform>())
            if((t.name=="AcousticBaffle"||t.name=="WallCladding")&&t.GetComponent<Collider>()==null)t.gameObject.AddComponent<BoxCollider>();
        if(detail.Find("IndoorFillLights")==null)
        {
            var lights=Group("IndoorFillLights");
            foreach(float x in new[]{232f,296f})foreach(float z in new[]{8f,23f})
            {
                var light=new GameObject("CeilingFill").AddComponent<Light>();light.transform.SetParent(lights,false);light.transform.position=new Vector3(x,4.6f,z);
                light.type=LightType.Point;light.range=17;light.intensity=2;light.color=new Color(1,.88f,.7f);light.shadows=LightShadows.None;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Scene);PracticeCampusEditor.Validate();Capture();
    }
    static Transform Group(string name){var t=new GameObject(name).transform;t.SetParent(detail,false);return t;}
    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material mat)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.position=pos;o.transform.localScale=size;
        o.GetComponent<MeshRenderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static void Prop(string relative,Transform parent,Vector3 pos,float scale,float yaw)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/FPS_Bundle/"+relative);if(asset==null)throw new Exception(relative);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);obj.transform.localScale*=scale;obj.transform.rotation=Quaternion.Euler(0,yaw,0);
        var rs=obj.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        obj.transform.position+=pos-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var c in obj.GetComponentsInChildren<Collider>())c.enabled=false;
    }
}
#endif
