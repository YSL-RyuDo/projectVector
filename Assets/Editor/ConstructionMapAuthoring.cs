#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Offline authoring only. All architecture, gameplay references and markers are saved assets.
public static class ConstructionMapAuthoring
{
    public const string MapPath="Assets/Prefab/Maps/Construction.prefab";
    const string Art="Assets/Art/Maps/Construction";
    const string Pack="Assets/FPS_Bundle/FPS_Map5/Prefabs/";
    internal static Transform root,geometry,detail;
    internal static Material concrete,dark,steel,amber,blue,white;
    static readonly Dictionary<Material,Material> converted=new();
    static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
    internal static Material Mat(string name,Color color)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.08f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Material Convert(Material source)
    {
        if(!source)return concrete;if(converted.TryGetValue(source,out var m))return m;
        string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
        m=Mat("Bundle_"+source.name+"_"+key.Substring(0,8),Color.white);
        var tex=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture;
        if(!tex && source.HasProperty("_BaseColorMap"))tex=source.GetTexture("_BaseColorMap");
        m.SetTexture("_BaseMap",tex);converted[source]=m;return m;
    }
    static void MapLayer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=LayerMask.NameToLayer("MiniMap");t.gameObject.isStatic=true;}}
    internal static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null,bool collider=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent?parent:geometry,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());MapLayer(g);return g;
    }
    internal static GameObject Prop(string path,Vector3 feet,float scale=1,float yaw=0,bool solid=true,Transform parent=null)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+path+".prefab");if(!source)throw new Exception("Missing bundle asset "+path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(source,parent?parent:detail);PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        g.transform.localScale=Vector3.one*scale;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.position=Vector3.zero;
        var b=ConstructionMapSurvey.BoundsOf(g);g.transform.position=feet-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
        foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        // Deliberately simple solid covers: no gaps between 52 separate pallet colliders.
        if(solid){b=ConstructionMapSurvey.BoundsOf(g);var c=new GameObject(g.name+"_Collision");c.transform.SetParent(parent?parent:detail,false);c.transform.position=b.center;c.AddComponent<BoxCollider>().size=b.size;MapLayer(c);}
        MapLayer(g);return g;
    }
    static void Wall(string name,float x,float z,float length,float yaw=0,float height=4.8f,Material accent=null)
    {
        var group=Group(name,geometry);group.localPosition=new Vector3(x,0,z);group.localRotation=Quaternion.Euler(0,yaw,0);
        int count=Mathf.CeilToInt(length/5);float width=length/count;
        for(int i=0;i<count;i++)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+"Walls/Wall_01.prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(p,group);
            PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            g.transform.localPosition=new Vector3(-length/2+width*(i+.5f),0,0);g.transform.localRotation=Quaternion.identity;g.transform.localScale=new Vector3(width/5,height/3.8f,3);
            foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
            foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            var cbox=g.AddComponent<BoxCollider>();cbox.center=new Vector3(0,1.9f,0);cbox.size=new Vector3(5,3.8f,.2f);MapLayer(g);
        }
        Box(name+"_Cap",new Vector3(0,height+.07f,0),new Vector3(length+.12f,.14f,.72f),dark,group);
        if(accent)Box(name+"_Wayfinding",new Vector3(0,3.15f,0),new Vector3(length,.22f,.63f),accent,group,false);
    }
    static void Building(string name,float x,float z,float w,float d,Material accent)
    {
        // Occupied space is a sealed building, not a maze of inaccessible fake doorways.
        Box(name+"_Solid",new Vector3(x,2.65f,z),new Vector3(w,5.3f,d),concrete);
        foreach(float side in new[]{-1f,1f})
        {
            Wall(name+"_Front",x,z+side*d/2,w,0,5.3f,accent);
            Wall(name+"_Side",x+side*w/2,z,d,90,5.3f,accent);
        }
        Box(name+"_Slab",new Vector3(x,5.45f,z),new Vector3(w+.5f,.3f,d+.5f),white);
        for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)Box(name+"_UnfinishedColumn",new Vector3(x+i*(w/2-.7f),7,z+j*(d/2-.7f)),new Vector3(.45f,3,.45f),concrete);
        Box(name+"_UpperBeam",new Vector3(x,8.4f,z-d/2+.7f),new Vector3(w,.4f,.45f),concrete);
        Box(name+"_UpperBeam",new Vector3(x,8.4f,z+d/2-.7f),new Vector3(w,.4f,.45f),concrete);
        // Steelwork is readable above the combat silhouette band.
    }
    internal static void Label(string text,Vector3 p,float yaw,float size,Color color,bool floor=false)
    {
        var t=Group("Mark_"+text.Replace("\n","_"),detail);t.position=p;t.rotation=Quaternion.Euler(floor?90:0,yaw,0);
        var tm=t.gameObject.AddComponent<TextMeshPro>();tm.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");tm.text=text;tm.fontSize=size;tm.color=color;tm.alignment=TextAlignmentOptions.Center;tm.rectTransform.sizeDelta=new Vector2(20,8);tm.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;MapLayer(tm.gameObject);
    }
    static Transform Point(string name,Vector3 p,float yaw=0,Transform parent=null){var t=Group(name,parent?parent:root);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
    internal static void Site(string name,Vector3 center,Material color,Vector2? dimensions=null)
    {
        var size=dimensions??new Vector2(10,12);
        var t=Point("SpikePlantZone"+name,center+Vector3.up*.25f);var box=t.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(size.x,.5f,size.y);t.gameObject.AddComponent<SpikePlantZone>();
        foreach(float side in new[]{-1f,1f}){Box(name+"_SiteEdge",center+new Vector3(side*size.x/2,.018f,0),new Vector3(.12f,.025f,size.y),color,detail,false);Box(name+"_SiteEdge",center+new Vector3(0,.018f,side*size.y/2),new Vector3(size.x,.025f,.12f),color,detail,false);}
        Label(name,center+new Vector3(0,.04f,0),0,24,color.color,true);
    }
    internal static void Beam(string name,Vector3 a,Vector3 b,float width,Material m,Transform parent=null)
    {
        var g=Box(name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),m,parent,false);g.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Formwork(Vector3 p,Vector3 size,Material m)
    {
        Box("ReusableFormwork",p+Vector3.up*size.y/2,size,m);
        for(float x=-size.x/2+.15f;x<size.x/2;x+=1.1f)foreach(float sign in new[]{-1f,1f})Box("FormworkSteelRib",p+new Vector3(x,size.y/2,sign*(size.z/2+.035f)),new Vector3(.08f,size.y,.06f),steel,detail,false);
        foreach(float h in new[]{.12f,size.y-.12f})foreach(float sign in new[]{-1f,1f})Box("FormworkSteelFrame",p+new Vector3(0,h,sign*(size.z/2+.035f)),new Vector3(size.x,.09f,.07f),steel,detail,false);
    }
    static void FinishArt()
    {
        // Panel joints and mounting details are shallow and non-colliding; large silhouettes stay clean.
        foreach(var g in geometry.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("_Solid")).ToArray())
        {
            var p=g.localPosition;var s=g.localScale;
            foreach(float sign in new[]{-1f,1f}){
                for(float x=-s.x/2+3;x<s.x/2;x+=3)Box("ConcreteJoint",new Vector3(p.x+x,2.4f,p.z+sign*(s.z/2+.315f)),new Vector3(.025f,4.7f,.012f),steel,detail,false);
                Box("FoundationPlinth",new Vector3(p.x,.25f,p.z+sign*(s.z/2+.32f)),new Vector3(s.x,.5f,.12f),steel,detail,false);
                for(float z=-s.z/2+3;z<s.z/2;z+=3)Box("ConcreteJoint",new Vector3(p.x+sign*(s.x/2+.315f),2.4f,p.z+z),new Vector3(.012f,4.7f,.025f),steel,detail,false);
                // Bright safety mesh at upper slab edge, away from head-height aiming.
                for(float x=-s.x/2;x<s.x/2;x+=2){Box("SafetyRailPost",new Vector3(p.x+x,6.05f,p.z+sign*s.z/2),new Vector3(.07f,1,.07f),amber,detail,false);}
                Box("SafetyRail",new Vector3(p.x,6.5f,p.z+sign*s.z/2),new Vector3(s.x,.07f,.07f),amber,detail,false);
            }
        }
        foreach(float x in new[]{-44f,-36f,-28f,-4f,4f,28f,36f,44f})for(float z=-44;z<48;z+=8)
            Box("ExpansionJoint",new Vector3(x,.026f,z),new Vector3(7.98f,.01f,.025f),steel,detail,false);
        // Replace featureless prototype covers with reusable steel shuttering and equipment.
        foreach(string name in new[]{"A_DefaultCover","B_DefaultCover","A_BlindCorner","B_BlindCorner","Mid_Transformer"}){
            var old=geometry.Find(name);var p=old.localPosition;var s=old.localScale;Object.DestroyImmediate(old.gameObject);
            Formwork(p-Vector3.up*s.y/2,s,name.StartsWith("A")?amber:name.StartsWith("B")?blue:steel);
        }
        // Service-cabinet face at mid; grilles do not introduce tiny collision snagging points.
        for(int i=-1;i<=1;i++){
            Box("TransformerDoor",new Vector3(i*1.5f,1.75f,-5.55f),new Vector3(1.25f,2.7f,.08f),concrete,detail,false);
            for(int j=0;j<5;j++)Box("VentSlot",new Vector3(i*1.5f,2+j*.13f,-5.60f),new Vector3(.9f,.045f,.025f),dark,detail,false);
        }
        Label("HIGH VOLTAGE",new Vector3(0,3.05f,-5.61f),0,2.4f,amber.color);
        // A's crane base is a real industrial foundation with four feet, not an unconnected tower.
        for(int i=0;i<4;i++)Prop("Props/CementC2_01",new Vector3(-44,0,18+i*1.2f),1,90,false);
        // B canopy and braces mark the second objective without closing its sky for utility.
        foreach(float z in new[]{14f,29f}){
            Box("B_CanopyColumn",new Vector3(44,3.2f,z),new Vector3(.45f,6.4f,.45f),steel);
            Beam("B_CanopyBrace",new Vector3(44,4.3f,z),new Vector3(39,6.4f,z),.16f,steel);
            Box("B_CanopyBeam",new Vector3(40.5f,6.4f,z),new Vector3(7,.3f,.3f),steel,detail,false);
        }
        Box("B_MetalCanopy",new Vector3(41,6.7f,21.5f),new Vector3(7,.12f,16),blue);
        for(int i=0;i<14;i++)Box("CanopyCorrugation",new Vector3(37.7f+i*.5f,6.79f,21.5f),new Vector3(.08f,.05f,16),steel,detail,false);
        Prop("Props/Scaffolding_02",new Vector3(-23.8f,0,-26),1.3f,90,false);
        Prop("Props/Scaffolding_03",new Vector3(-23.8f,3.25f,-26),1.3f,90,false);
        Prop("Props/ConstructionMat_02",new Vector3(-26.3f,0,-20),1,90,false);
        // Safe spawn yards with two readable exits; no one starts facing a teammate's back.
        Wall("AttackSpawnScreen",0,-36.7f,10,0,3.8f,amber);
        foreach(float s in new[]{-1f,1f})Wall("AttackSpawnSide",s*10,-43,10,90,3.8f,amber);
        Label("ATTACK\nMATERIAL GATE",new Vector3(0,2.3f,-37.04f),0,4.6f,white.color);
        Label("DEFENSE\nCONTROL YARD",new Vector3(0,2.8f,35.34f),180,4.6f,white.color);
        foreach(float s in new[]{-1f,1f}){
            Prop("Props/WC_01",new Vector3(s*16,0,-44),1,0);
            Prop("Props/Light_01",new Vector3(s*7,0,-46),1.2f,0,false);
        }
        Label("01",new Vector3(-16,7.4f,-34.5f),0,8,amber.color);Label("02",new Vector3(16,7.4f,-34.5f),0,8,blue.color);
        // A construction skyline, not the separate city map, and no floating backdrop buildings.
        var bg=root.Find("03_Background_NotPlayable");foreach(Transform child in bg.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        Box("OffsiteGround",new Vector3(0,-.65f,0),new Vector3(180,.4f,180),concrete,bg,false);
        for(int k=0;k<3;k++){
            float x=-62+k*61;float z=62;
            for(int level=0;level<3;level++){
                Box("OffsiteSlab",new Vector3(x,level*4+4,z),new Vector3(22,.4f,16),concrete,bg,false);
                foreach(float a in new[]{-10f,0,10f})foreach(float b in new[]{-7f,7f})Box("OffsiteColumn",new Vector3(x+a,level*4+2,z+b),new Vector3(.65f,4,.65f),concrete,bg,false);
            }
        }
        var backdropCrane=Prop("Props/TowerCrane_02",new Vector3(59,0,56),1.1f,100,false,bg);
        backdropCrane.transform.position+=new Vector3(59,-.45f,56)-LowestFootprint(backdropCrane);
        foreach(var t in bg.GetComponentsInChildren<Transform>())t.gameObject.layer=0;
    }
    // Mast foot is independent of the asymmetric boom/counterweight bounds.
    public static Vector3 LowestFootprint(GameObject g)
    {
        var vertices=g.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
        float bottom=vertices.Min(v=>v.y);var feet=vertices.Where(v=>v.y<bottom+.05f).ToArray();
        return new Vector3((feet.Min(v=>v.x)+feet.Max(v=>v.x))*.5f,bottom,(feet.Min(v=>v.z)+feet.Max(v=>v.z))*.5f);
    }
    [MenuItem("Project Vector/Maps/Author Construction (offline)")]
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Use a separate batch editor with the user editor closed.");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();converted.Clear();
        concrete=Mat("Concrete",new Color(.64f,.65f,.64f));dark=Mat("Asphalt",new Color(.23f,.27f,.29f));steel=Mat("Steel",new Color(.32f,.39f,.42f));amber=Mat("A_SafetyOchre",new Color(.93f,.56f,.15f));blue=Mat("B_PetrolBlue",new Color(.18f,.48f,.59f));white=Mat("Chalk",new Color(.86f,.87f,.83f));
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("Construction").transform;geometry=Group("01_Architecture",root);detail=Group("02_SetDressing",root);
        ConstructionMapLayout.Build();
        var info=root.gameObject.AddComponent<MapInfo>();var points=Group("04_Gameplay",root);
        info.attackSpawnPoints=Enumerable.Range(0,4).Select(i=>Point("AttackSpawn_"+i,new Vector3((i-1.5f)*2,.05f,-42),0,points)).ToArray();
        info.defendSpawnPoints=Enumerable.Range(0,4).Select(i=>Point("DefendSpawn_"+i,new Vector3((i-1.5f)*2,.05f,42),180,points)).ToArray();
        info.spikeSpawnPoint=Point("SpikeSpawn",new Vector3(0,.3f,-40),0,points);
        info.barriers=Group("BuyPhaseBarriers",points).gameObject;
        ConstructionMapLayout.Barriers(info.barriers.transform);
        var refs=points.gameObject.AddComponent<MiniMapWorldReferences>();var sr=new SerializedObject(refs);
        sr.FindProperty("zoneAPosition").objectReferenceValue=root.Find("SpikePlantZoneA");sr.FindProperty("zoneBPosition").objectReferenceValue=root.Find("SpikePlantZoneB");sr.FindProperty("fullMapCenter").objectReferenceValue=Point("MiniMapCenter",Vector3.zero,0,points);sr.FindProperty("fullMapSize").floatValue=52;sr.FindProperty("hudSize").floatValue=28;sr.FindProperty("fullMapHeight").floatValue=100;sr.ApplyModifiedPropertiesWithoutUndo();
        string pipelinePath=Art+"/ConstructionPipeline.asset";
        if(!AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath))AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",pipelinePath);
        var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);var rp=new SerializedObject(pipeline);
        rp.FindProperty("m_MainLightShadowsSupported").boolValue=true;rp.FindProperty("m_ShadowDistance").floatValue=130;rp.FindProperty("m_MSAA").intValue=4;rp.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.AddComponent<ConstructionMapEnvironment>().pipeline=pipeline;
        AssetDatabase.SaveAssets();PrefabUtility.SaveAsPrefabAsset(root.gameObject,MapPath);
        // Saved inspection scene shares the actual gameplay prefab, not a screenshot-only duplicate.
        Object.DestroyImmediate(root.gameObject);root=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapPath))).transform;root.GetComponent<MapInfo>().SetBarriersActive(false);
        ConstructionMapSurvey.Lighting();root.GetComponent<ConstructionMapEnvironment>().Apply();var camera=new GameObject("Overview Camera").AddComponent<Camera>();camera.transform.SetPositionAndRotation(new Vector3(83,76,-94),Quaternion.LookRotation(new Vector3(0,0,4)-new Vector3(83,76,-94)));camera.farClipPlane=400;camera.backgroundColor=new Color(.6f,.72f,.84f);camera.clearFlags=CameraClearFlags.SolidColor;
        Directory.CreateDirectory("Assets/Scenes/Maps");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Maps/Construction.unity");
        ConstructionMapSurvey.Capture("Construction-overview",new Vector3(83,76,-94),new Vector3(0,0,4),1600,1000);
        ConstructionMapSurvey.Capture("Construction-top",new Vector3(0,125,0),Vector3.zero,1200,1200,52);
        ConstructionMapSurvey.Capture("Construction-A",new Vector3(-39,1.35f,8),new Vector3(-29,1.8f,23));
        ConstructionMapSurvey.Capture("Construction-A-long",new Vector3(-40,1.35f,-18),new Vector3(-40,1.35f,18));
        ConstructionMapSurvey.Capture("Construction-B",new Vector3(35,1.35f,17),new Vector3(28,1.8f,29));
        ConstructionMapSurvey.Capture("Construction-mid",new Vector3(5,1.35f,-3),new Vector3(3,1.4f,9));
        ConstructionMapSurvey.Capture("Construction-attack",new Vector3(-3,1.35f,-42),new Vector3(-7,1.5f,-35));
        ConstructionMapSurvey.Capture("Construction-defense",new Vector3(-3,1.35f,42),new Vector3(-9,1.5f,36));
        ConstructionMapSurvey.Capture("Construction-crane-contact",new Vector3(-37,2,17),new Vector3(-39,2.2f,24));
        ConstructionMapSurvey.Capture("Construction-A-connector",new Vector3(-6,1.35f,-12),new Vector3(-20,1.4f,-12));
        ConstructionMapSurvey.Capture("Construction-A-entry",new Vector3(-20,2.07f,1),new Vector3(-26,1.4f,6));
        ConstructionMapSurvey.Capture("Construction-A-service-room",new Vector3(-20,2.25f,-5.5f),new Vector3(-20,2.2f,.7f));
        ConstructionMapSurvey.Capture("Construction-A-service-ramp",new Vector3(-20,1.35f,-12),new Vector3(-20,2.2f,-3));
        ConstructionMapSurvey.Capture("Construction-navigation-top",new Vector3(0,125,0),Vector3.zero,1600,1600,52,LayerMask.GetMask("MiniMap"));
        ConstructionMapSurvey.Capture("Construction-A-entry-cover",new Vector3(-26,1.35f,7),new Vector3(-29,1.4f,18));
        ConstructionMapSurvey.Capture("Construction-B-entry-cover",new Vector3(22,1.35f,24),new Vector3(29,1.4f,26));
        ConstructionMapSurvey.Capture("Construction-B-connector",new Vector3(16,1.35f,24),new Vector3(29,1.5f,26));
        root.GetComponent<ConstructionMapEnvironment>().Restore();
        Debug.Log("CONSTRUCTION_AUTHOR_PASS");
    }
}
#endif
