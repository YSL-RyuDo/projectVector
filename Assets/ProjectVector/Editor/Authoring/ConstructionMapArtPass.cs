#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ConstructionMapAuthoring;
using Object=UnityEngine.Object;

// Saved modular environmental art, never runtime geometry or a capture-only overlay.
public static class ConstructionMapArtPass
{
    const string Art="Assets/ProjectVector/Shared/Maps/Art/Construction/";
    static Material cream,brick,glass,rubber;
    public static Material Cream=>cream;
    public static void InitializeMaterials()
    {
        cream=Mat("LimestonePaint",new Color(.68f,.67f,.60f));brick=Mat("TerracottaInfill",new Color(.49f,.29f,.21f));glass=Mat("DustyClerestory",new Color(.21f,.38f,.43f));rubber=Mat("DarkRecess",new Color(.11f,.15f,.17f));
    }
    public static void ServiceBay(Vector3 p,float yaw,float width,Material accent,string id)=>Shutter(p,yaw,width,accent,id);
    public static void FinishMaterials(){Surface(concrete,"ConcreteGrain",71,.16f);Surface(dark,"AsphaltGrain",17,.22f);MeterUVs();}
    static void Detail(string name,Vector3 p,Vector3 s,Material m)=>Box(name,p,s,m,detail,false);
    static void Shutter(Vector3 p,float yaw,float width,Material accent,string id)
    {
        var group=new GameObject("ClosedServiceBay_"+id).transform;group.SetParent(detail,false);group.position=p;group.rotation=Quaternion.Euler(0,yaw,0);
        Box("Recess",new Vector3(0,1.6f,0),new Vector3(width+.3f,3.2f,.08f),rubber,group,false);
        Box("ClosedRollerShutter",new Vector3(0,1.55f,-.055f),new Vector3(width,3.05f,.07f),steel,group,false);
        for(int i=1;i<16;i++)Box("RollerSlat",new Vector3(0,i*.19f,-.105f),new Vector3(width,.025f,.025f),dark,group,false);
        foreach(float sign in new[]{-1f,1f})Box("BayFrame",new Vector3(sign*(width/2+.12f),1.65f,-.13f),new Vector3(.18f,3.3f,.22f),cream,group,false);
        Box("BayHeader",new Vector3(0,3.45f,-.12f),new Vector3(width+.6f,.55f,.25f),accent,group,false);
        Box("Threshold",new Vector3(0,.045f,-.16f),new Vector3(width+.6f,.09f,.32f),steel,group,false);
        Label(id,p+Quaternion.Euler(0,yaw,0)*new Vector3(0,3.46f,-.26f),yaw,2.7f,white.color);
    }
    static void Pipe(Vector3 a,Vector3 b,float diameter)
    {
        Beam("ServiceConduit",a,b,diameter,steel,detail);
        for(int i=0;i<=4;i++){var p=Vector3.Lerp(a,b,i/4f);Detail("PipeWallBracket",p,new Vector3(.18f,.07f,.3f),dark);}
    }
    static void Facade(float x,float z,float w,float d,Material accent,string id)
    {
        // A precast structural frame, painted lower course, masonry infill and closed bays.
        foreach(float sign in new[]{-1f,1f}){
            Detail("PaintedLowerCourse",new Vector3(x,.55f,z+sign*(d/2+.33f)),new Vector3(w,1.1f,.08f),cream);
            for(float offset=-w/2;offset<=w/2+.1f;offset+=w/4){
                Detail("PrecastPilaster",new Vector3(x+offset,2.7f,z+sign*(d/2+.36f)),new Vector3(.3f,5.4f,.16f),white);
                Detail("ColumnShoe",new Vector3(x+offset,.15f,z+sign*(d/2+.40f)),new Vector3(.45f,.3f,.24f),steel);
            }
            for(float offset=-d/2;offset<=d/2+.1f;offset+=d/4){
                Detail("SidePilaster",new Vector3(x+sign*(w/2+.36f),2.7f,z+offset),new Vector3(.16f,5.4f,.3f),white);
            }
            Detail("FloorEdgeStripe",new Vector3(x,4.95f,z+sign*(d/2+.35f)),new Vector3(w,.16f,.12f),accent);
            // Connect all upper columns, not two unsupported floating beams.
            Detail("UpperSideBeam",new Vector3(x+sign*(w/2-.7f),8.4f,z),new Vector3(.5f,.4f,d-1.4f),concrete);
            Detail("UpperCrossBeam",new Vector3(x,8.4f,z),new Vector3(w-1.4f,.4f,.5f),concrete);
            // Rebar grows out of the slab, at the unfinished roofline only.
            foreach(float xx in new[]{x-w/2+.7f,x,x+w/2-.7f})for(int k=0;k<2;k++)
                Detail("ColumnRebar",new Vector3(xx+(k-.5f)*.2f,9.1f,z+sign*(d/2-.7f)),new Vector3(.045f,1.1f,.045f),steel);
        }
        Shutter(new Vector3(x-w*.23f,0,z-d/2-.405f),0,3.1f,accent,id+" / 01");
        Shutter(new Vector3(x+w*.23f,0,z-d/2-.405f),0,3.1f,accent,id+" / 02");
        // Back faces have large glazed clerestory strips above player silhouettes.
        foreach(float xx in new[]{x-w*.26f,x+w*.26f}){
            Detail("ClerestoryFrame",new Vector3(xx,3.95f,z+d/2+.37f),new Vector3(w*.36f,1.05f,.13f),steel);
            Detail("ClerestoryGlass",new Vector3(xx,3.95f,z+d/2+.45f),new Vector3(w*.36f-.12f,.88f,.03f),glass);
            for(int i=-1;i<=1;i++)Detail("WindowMullion",new Vector3(xx+i*w*.09f,3.95f,z+d/2+.48f),new Vector3(.055f,.95f,.04f),white);
        }
        float side=x<0?-1:1;float px=x+side*(w/2+.43f);
        // Side-facing plant doors make flank lanes read as part of a working facility.
        Shutter(new Vector3(px,0,z),side>0?90:270,4,accent,id);
        Pipe(new Vector3(px,1,z-d*.35f),new Vector3(px,4.5f,z-d*.35f),.13f);
        Pipe(new Vector3(px,4.5f,z-d*.35f),new Vector3(px,4.5f,z+d*.35f),.13f);
        // Asymmetric second-storey masonry, avoiding five identical concrete boxes.
        Detail("MasonryInfill",new Vector3(x,6.9f,z+d/2-.8f),new Vector3(w*.48f,2.5f,.32f),brick);
        for(int row=0;row<8;row++)Detail("MasonryCourse",new Vector3(x,5.75f+row*.31f,z+d/2-.63f),new Vector3(w*.48f,.028f,.025f),cream);
        for(int row=0;row<4;row++)for(int j=0;j<6;j++)Detail("MasonryJoint",new Vector3(x-w*.23f+j*w*.08f+(row%2)*.2f,5.9f+row*.62f,z+d/2-.61f),new Vector3(.025f,.3f,.025f),cream);
        // Diagonal steel bracing is fixed to the column joints.
        Beam("RoofBracing",new Vector3(x-w/2+.7f,5.6f,z-d/2+.7f),new Vector3(x,8.4f,z-d/2+.7f),.1f,steel,detail);
    }
    static void Surface(Material material,string name,int seed,float grain)
    {
        string path=Art+name+".asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!tex){tex=new Texture2D(128,128,TextureFormat.RGB24,true);tex.name=name;AssetDatabase.CreateAsset(tex,path);}
        var random=new System.Random(seed);var pixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
            // Low contrast: surface roughness, not a noisy camouflage pattern behind players.
            float n=(float)random.NextDouble();float cloud=Mathf.PerlinNoise(x/24f+seed,y/24f+seed);
            float value=1-grain*(.65f*n+.35f*cloud);pixels[y*128+x]=new Color(value,value,value);
        }
        tex.SetPixels(pixels);tex.Apply(true);tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Trilinear;EditorUtility.SetDirty(tex);
        material.SetTexture("_BaseMap",tex);EditorUtility.SetDirty(material);
    }
    static void MeterUVs()
    {
        // Persistent, metre-scaled UVs prevent a 96m floor stretching a single texture pixel.
        var meshes=new Dictionary<string,Mesh>();
        foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
            var r=f.GetComponent<Renderer>();if(!r || !(r.sharedMaterial==concrete||r.sharedMaterial==dark))continue;
            var original=f.sharedMesh;if(!original||original.vertexCount>100)continue;
            Vector3 scale=f.transform.lossyScale;string key=$"Surface_{scale.x:F3}_{scale.y:F3}_{scale.z:F3}_{original.vertexCount}";
            if(!meshes.TryGetValue(key,out var mesh)){
                string path=Art+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=Object.Instantiate(original);mesh.name=key;AssetDatabase.CreateAsset(mesh,path);}
                var v=original.vertices;var n=original.normals;var uv=new Vector2[v.Length];
                for(int i=0;i<v.Length;i++){var p=Vector3.Scale(v[i],scale);var normal=n[i];uv[i]=Mathf.Abs(normal.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(normal.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}
                mesh.uv=uv;EditorUtility.SetDirty(mesh);meshes[key]=mesh;
            }
            f.sharedMesh=mesh;
        }
    }
    public static void Run()
    {
        cream=Mat("LimestonePaint",new Color(.68f,.67f,.60f));brick=Mat("TerracottaInfill",new Color(.49f,.29f,.21f));glass=Mat("DustyClerestory",new Color(.21f,.38f,.43f));rubber=Mat("DarkRecess",new Color(.11f,.15f,.17f));
        Facade(-16,-24,17,20,amber,"FORMWORK");Facade(16,-24,17,20,blue,"WORKSHOP");
        Facade(-16,0,12,16,amber,"A SERVICE");Facade(16,0,12,16,blue,"B SERVICE");Facade(0,24,24,10,steel,"POWER HOUSE");
        // Break up the tall perimeter with a structural rhythm and construction hoarding.
        foreach(float sign in new[]{-1f,1f})for(int i=-42;i<=42;i+=12){
            Detail("PerimeterPier",new Vector3(i,3,sign*47.62f),new Vector3(.45f,6,.18f),white);
            Detail("PerimeterPier",new Vector3(sign*47.62f,3,i),new Vector3(.18f,6,.45f),white);
            Detail("PerimeterLowerPanel",new Vector3(i,1.15f,sign*47.57f),new Vector3(11.4f,2.3f,.11f),cream);
        }
        // Crane bolts and foundation are placed directly under the mast, not under its boom.
        foreach(float dx in new[]{-1.3f,1.3f})foreach(float dz in new[]{-1.3f,1.3f}){
            Detail("CraneAnchorPlate",new Vector3(-40+dx,.82f,25+dz),new Vector3(.5f,.04f,.5f),steel);
            Detail("CraneAnchorBolt",new Vector3(-40+dx,.90f,25+dz),new Vector3(.07f,.16f,.07f),dark);
        }
        // A crane mast collision is a simple box; the lattice is not a walk-through decoration.
        var mast=new GameObject("CraneMastCollision");mast.transform.SetParent(geometry,false);mast.transform.position=new Vector3(-40,12.8f,25);mast.AddComponent<BoxCollider>().size=new Vector3(2.1f,24,2.1f);
        Label("LIFT ZONE",new Vector3(-40,.825f,26.9f),0,2.2f,steel.color,true);
        // Supported timber and beam bundles give each yard a real construction purpose.
        foreach(var p in new[]{new Vector3(-44,0,39),new Vector3(43,0,39)}){
            Prop("Props/Pallet_01",p,1.5f,90);
            Prop("Props/ConstructionMat_03",p+Vector3.up*.22f,.8f,90,false);
        }
        Prop("Props/WoodenPlank_03",new Vector3(-45,0,13),1.2f,90);
        Prop("Props/MetalTile_01",new Vector3(43,1.2f,24),1.2f,0);
        Prop("Props/CementC2_03",new Vector3(43,1.2f,19),1.15f,0);
        // A lightweight loading gantry behind A, with every beam ending at a column.
        foreach(float x in new[]{-35f,-23f})Box("A_LoadingGantryPost",new Vector3(x,2.7f,37),new Vector3(.32f,5.4f,.32f),steel);
        Detail("A_LoadingGantryBeam",new Vector3(-29,5.4f,37),new Vector3(12.32f,.45f,.38f),amber);
        Beam("GantryKnee",new Vector3(-35,4,37),new Vector3(-33.5f,5.4f,37),.13f,steel,detail);
        Beam("GantryKnee",new Vector3(-23,4,37),new Vector3(-24.5f,5.4f,37),.13f,steel,detail);
        Label("A / CRANE YARD",new Vector3(-29,5.43f,36.78f),0,3.3f,white.color);
        // High level banners are mounted to slabs, never suspended in open air.
        Detail("WorksSignBacking",new Vector3(-16,6.9f,-33.6f),new Vector3(7,2.1f,.2f),steel);
        Label("VECTOR WORKS\nPRECAST / 07",new Vector3(-16,6.9f,-33.72f),0,4.2f,white.color);
        Detail("WorksSignBacking",new Vector3(16,6.9f,-33.6f),new Vector3(7,2.1f,.2f),blue);
        Label("MATERIALS\nDELIVERY / 02",new Vector3(16,6.9f,-33.72f),0,4.2f,white.color);
        // Threshold traffic marks keep spawn exits readable without adding more collision.
        foreach(float side in new[]{-1f,1f})for(int i=0;i<6;i++)Detail("TrafficCrossing",new Vector3(side*12,.028f,-39+i*.55f),new Vector3(3,.012f,.24f),white);
        Surface(concrete,"ConcreteGrain",71,.16f);Surface(dark,"AsphaltGrain",17,.22f);MeterUVs();
    }
}
#endif
