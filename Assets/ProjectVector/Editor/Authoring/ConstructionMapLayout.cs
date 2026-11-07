#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ConstructionMapAuthoring;

// The route graph is carved into a connected industrial complex. Unused space is
// solid, not a traversable empty apron around decorative, isolated buildings.
public static class ConstructionMapLayout
{
    const int N=96;
    static readonly bool[,] walk=new bool[N,N];
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    // Widen within the existing yard; retain the north/defender-side limit at z=24.
    public static readonly Vector3 APlantCenter=P(-29,17);
    public static readonly Vector2 APlantSize=new(14,14);
    public static readonly Vector3[] AttackStages={P(-40,-22),P(-4,-22),P(21,-4)};
    public static readonly Vector3[] ContactPoints={P(-40,7),P(7,0),P(35,17)};
    public static readonly Vector3[] DefenseStages={P(-30,18),P(7,18),P(29,28)};
    public static readonly Dictionary<string,Vector3[]> Routes=new()
    {
        ["mainA"]=new[]{P(-40,-22),P(-40,8),P(-35,14),P(-29,18)},
        ["mainB"]=new[]{P(21,-4),P(21,2),P(35,2),P(35,17),P(29,26)},
        ["midA"]=new[]{P(-4,-22),P(-4,-12),P(-20,-12),new Vector3(-20,.9f,-3),P(-20,6),P(-26,6),P(-29,18)},
        ["midB"]=new[]{P(-4,-22),P(-4,-12),P(4,-12),P(7,-1),P(9,7),P(16,10),P(16,24),P(22,24),P(29,26)},
        ["rearRotation"]=new[]{P(-29,18),P(-20,23),P(-20,35),P(0,35),P(20,35),P(20,33),P(29,26)},
        ["B_ramp"]=new[]{P(36.5f,20),new Vector3(36.5f,.2f,22),new Vector3(36.5f,.6f,24),new Vector3(36.5f,1,26),new Vector3(36.5f,1.2f,30)}
    };
    static void Rectangle(float x,float z,float w,float d)
    {
        for(int i=0;i<N;i++)for(int j=0;j<N;j++)if(Mathf.Abs(i-47.5f-x)<w/2+.001f&&Mathf.Abs(j-47.5f-z)<d/2+.001f)walk[i,j]=true;
    }
    static void Polygon(params Vector2[] points)
    {
        for(int x=0;x<N;x++)for(int z=0;z<N;z++){
            var p=new Vector2(x-47.5f,z-47.5f);bool inside=false;
            for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].y>p.y)!=(points[j].y>p.y)&&p.x<(points[j].x-points[i].x)*(p.y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
            if(inside)walk[x,z]=true;
        }
    }
    static void Corridor(float width,params Vector3[] points)
    {
        for(int i=1;i<points.Length;i++){
            var a=points[i-1];var b=points[i];if(Mathf.Abs(a.x-b.x)>.01f&&Mathf.Abs(a.z-b.z)>.01f)throw new Exception("Author orthogonal corridors explicitly");
            Rectangle((a.x+b.x)/2,(a.z+b.z)/2,Mathf.Abs(a.x-b.x)+width,Mathf.Abs(a.z-b.z)+width);
        }
    }
    static void Pavement(string name,Material m,float width,params Vector3[] points)
    {
        for(int i=1;i<points.Length;i++){
            var a=points[i-1];var b=points[i];var delta=b-a;
            var g=Box(name,(a+b)/2+Vector3.up*.013f,new Vector3(width,.018f,delta.magnitude),m,detail,false);
            g.transform.rotation=Quaternion.LookRotation(delta);
        }
    }
    static void Masses()
    {
        var used=new bool[N,N];int id=0;
        var roof=Mat("InaccessibleRoof",new Color(.35f,.40f,.43f));
        // Greedy rectangles: exact shared wall/collision footprint, no 9,216 tiny cubes.
        for(int z=0;z<N;z++)for(int x=0;x<N;x++){
            if(walk[x,z]||used[x,z])continue;int w=1,d=1;
            while(x+w<N&&!walk[x+w,z]&&!used[x+w,z])w++;
            bool extend=true;while(z+d<N&&extend){for(int i=x;i<x+w;i++)if(walk[i,z+d]||used[i,z+d]){extend=false;break;}if(extend)d++;}
            for(int i=x;i<x+w;i++)for(int j=z;j<z+d;j++)used[i,j]=true;
            var center=new Vector3(x-48+w/2f,2.65f,z-48+d/2f);
            var mass=Box("ComplexMass_"+id++,center,new Vector3(w,5.3f,d),concrete);
            mass.AddComponent<MapUtilityPlacementBlocker>();
            // The overhead map must distinguish a sealed roof from a walkable yard.
            Box("NonPlayableRoof",new Vector3(center.x,5.305f,center.z),new Vector3(w,.01f,d),roof,detail,false);
        }
        // Exposed wall faces only. Long structural spans get continuous coping,
        // a plinth and sparse pilasters rather than a repeated standalone building.
        for(int orientation=0;orientation<4;orientation++)for(int line=0;line<=N;line++){
            int start=-1;
            for(int k=0;k<=N;k++){
                bool face=false;
                if(k<N){
                    int sx=orientation<2?k:line+(orientation==2?-1:0),sz=orientation<2?line+(orientation==0?-1:0):k;
                    int ax=orientation<2?sx:sx+(orientation==2?1:-1),az=orientation<2?sz+(orientation==0?1:-1):sz;
                    face=sx>=0&&sx<N&&sz>=0&&sz<N&&ax>=0&&ax<N&&az>=0&&az<N&&!walk[sx,sz]&&walk[ax,az];
                }
                if(face&&start<0)start=k;
                if(!face&&start>=0){
                    float length=k-start;var p=orientation<2?new Vector3(start-48+length/2,0,line-48):new Vector3(line-48,0,start-48+length/2);
                    var along=orientation<2?Vector3.right:Vector3.forward;var normal=orientation==0?Vector3.forward:orientation==1?Vector3.back:orientation==2?Vector3.right:Vector3.left;
                    bool east=p.x>12;var accent=east?blue:p.x<-15?amber:steel;
                    Vector3 Span(float y,float thick)=>orientation<2?new Vector3(length,y,thick):new Vector3(thick,y,length);
                    Box("WallCoping",p+Vector3.up*5.36f,Span(.12f,.26f),steel,detail,false);
                    Box("LowerWallCourse",p+Vector3.up*.45f+normal*.025f,Span(.9f,.06f),ConstructionMapArtPass.Cream,detail,false);
                    Box("SectorStripe",p+Vector3.up*3.3f+normal*.04f,Span(.15f,.09f),accent,detail,false);
                    for(float offset=-length/2+.22f;offset<length/2;offset+=5){
                        var q=p+along*offset+normal*.05f;
                        Box("PrecastJoint",q+Vector3.up*2.65f,orientation<2?new Vector3(.16f,5.3f,.12f):new Vector3(.12f,5.3f,.16f),white,detail,false);
                    }
                    if(length>=10&&length<28&&p.z>-32&&p.z<34){
                        float yaw=Quaternion.LookRotation(-normal).eulerAngles.y;
                        ConstructionMapArtPass.ServiceBay(p+normal*.09f,yaw,Mathf.Min(4,length*.4f),accent,east?"LOADING":"SERVICE");
                    }
                    start=-1;
                }
            }
        }
    }
    static void Cover(string name,Vector3 p,Vector3 size,Material color)
    {
        Box(name,p+Vector3.up*size.y/2,size,color);
        foreach(float sign in new[]{-1f,1f}){
            for(float x=-size.x/2+.12f;x<size.x/2;x+=1)Box("ShutteringRib",p+new Vector3(x,size.y/2,sign*(size.z/2+.025f)),new Vector3(.06f,size.y,.05f),steel,detail,false);
            Box("ShutteringTop",p+new Vector3(0,size.y-.08f,sign*(size.z/2+.025f)),new Vector3(size.x,.08f,.05f),steel,detail,false);
        }
    }
    static void EnclosedScaffold(Vector3 feet,Material color)
    {
        var scaffold=Prop("Props/Scaffolding_01",feet,1.1f,90,true);
        var b=ConstructionMapSurvey.BoundsOf(scaffold);
        // The authored broad collider must never block apparently open gaps.
        // Plywood hoarding fills the under-platform volume, behind its steel frame.
        Box("ScaffoldStorageHoarding",b.center,b.size-Vector3.one*.02f,color,detail,false);
    }
    static void ServiceRoom()
    {
        // A real, two-door interior on the same route, not a new camping dead end.
        Box("A_ServiceRaisedFloor",new Vector3(-20,.45f,-3),new Vector3(8,.9f,6),ConstructionMapArtPass.Cream);
        void Ramp(string name,Vector3 from,Vector3 to)
        {
            var d=to-from;var rotation=Quaternion.Euler(-Mathf.Atan2(d.y,d.z)*Mathf.Rad2Deg,0,0);
            var ramp=Box(name,(from+to)/2-rotation*Vector3.up*.08f,new Vector3(4,.16f,d.magnitude),steel);
            ramp.transform.rotation=rotation;
        }
        Ramp("A_ServiceEntryRamp",P(-20,-11),new Vector3(-20,.9f,-6));
        Ramp("A_ServiceExitRamp",new Vector3(-20,.9f,0),P(-20,5));
        var roof=Box("A_ServiceRoof",new Vector3(-20,4.15f,-3),new Vector3(8,.25f,6),concrete);
        // Gameplay cameras see the ceiling; the existing layer-3-only minimap sees the floor.
        // The physical roof still blocks bullets and rejects top-down aerial utility.
        roof.layer=0;roof.AddComponent<MapUtilityPlacementBlocker>();
        foreach(float z in new[]{-6f,0f})
        {
            Box("ServiceDoorHeader",new Vector3(-20,3.8f,z),new Vector3(4.25f,.3f,.24f),amber);
            foreach(float x in new[]{-22f,-18f})Box("ServiceDoorJamb",new Vector3(x,2.3f,z),new Vector3(.12f,2.8f,.24f),steel,detail,false);
        }
        Cover("ServiceToolCabinet",new Vector3(-23.25f,.9f,-3),new Vector3(1,2.1f,3.5f),steel);
        Box("ServiceWallBoard",new Vector3(-16.05f,2.45f,-3),new Vector3(.06f,1.2f,2.4f),white,detail,false);
        for(int i=0;i<3;i++)Box("ServiceBoardLine",new Vector3(-16.09f,2.2f+i*.22f,-3),new Vector3(.015f,.04f,1.7f-i*.2f),blue,detail,false);
        var lampMaterial=Mat("ServiceLamp",new Color(.85f,.86f,.75f));lampMaterial.EnableKeyword("_EMISSION");lampMaterial.SetColor("_EmissionColor",new Color(1.3f,1.35f,1.1f));
        foreach(float x in new[]{-21.5f,-18.5f}){
            var lamp=Box("ServiceCeilingStrip",new Vector3(x,3.96f,-3),new Vector3(.12f,.04f,2.8f),lampMaterial,detail,false);lamp.layer=0;
        }
        var lightObject=new GameObject("ServiceInteriorLight");lightObject.transform.SetParent(detail,false);lightObject.transform.position=new Vector3(-20,3,-3);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=1.3f;light.color=new Color(1,.94f,.82f);light.shadows=LightShadows.None;
        Label("A SERVICE / 02",new Vector3(-20,3.8f,-6.15f),0,2.4f,white.color);
        Label("A SITE",new Vector3(-20,3.8f,.15f),180,2.4f,white.color);
    }
    public static void Build()
    {
        Array.Clear(walk,0,walk.Length);ConstructionMapArtPass.InitializeMaterials();
        Rectangle(0,-40,22,12);Rectangle(0,41,16,12);
        Corridor(6,P(0,-38),P(-28,-38),P(-28,-30),P(-40,-30),P(-40,-22));
        Corridor(6,P(0,-38),P(29,-38),P(29,-22));
        Corridor(6,P(0,-38),P(0,-30),P(-4,-30),P(-4,-22));
        // One controlled 36m contest lane; the crane stops the continuation and
        // the separate mid connector offers a close-range route into the site.
        Corridor(6,P(-40,-22),P(-40,8));
        Corridor(6,P(29,-22),P(29,-10),P(21,-10),P(21,2),P(35,2),P(35,17));
        Corridor(6,P(-4,-22),P(-4,-12),P(4,-12),P(4,4));
        Polygon(new Vector2(-43,5),new Vector2(-24,5),new Vector2(-24,9),new Vector2(-17,9),new Vector2(-17,25),new Vector2(-23,25),new Vector2(-23,31),new Vector2(-43,31));
        Polygon(new Vector2(26,16),new Vector2(43,16),new Vector2(43,31),new Vector2(39,31),new Vector2(39,36),new Vector2(17,36),new Vector2(17,20),new Vector2(26,20));
        Rectangle(2.5f,4,19,20);
        // Attacker-side lower connector from the reviewed green-line proposal.
        // Replace the old north-mid detour; do not leave a fourth A entrance.
        // The last left turn keeps CT rear from watching the entire new lane.
        Corridor(4,P(-4,-12),P(-20,-12),P(-20,6),P(-26,6));
        Rectangle(-20,-3,8,6);
        Corridor(4,P(9,10),P(16,10),P(16,24),P(22,24));
        // Protected, elbowed retake route, without the old ~96m perimeter detour.
        Corridor(4,P(-20,23),P(-20,35),P(20,35),P(20,33));
        Corridor(5,P(0,40),P(0,27),P(7,27),P(7,12));
        Box("Foundation_96x96",new Vector3(0,-.35f,0),new Vector3(96,.7f,96),concrete);
        Masses();
        Pavement("AttackSupplyRoute",dark,5.2f,P(-28,-38),P(29,-38));
        Pavement("MidServiceRoute",dark,4.7f,P(-4,-28),P(-4,-12),P(4,-12),P(4,-4));
        Pavement("AServiceApproach",dark,3.5f,P(-4,-12),P(-20,-12),P(-20,6),P(-26,6));
        Pavement("BWorkshopRoute",dark,5.1f,P(29,-28),P(29,-10),P(21,-10),P(21,2),P(35,2),P(35,17));
        Pavement("RearServiceLane",dark,3.5f,P(-20,23),P(-20,35),P(20,35),P(20,33));
        ServiceRoom();
        Site("A",APlantCenter,amber,APlantSize);Site("B",P(29,26),blue);
        Cover("A_DefaultCover",P(-27,23),new Vector3(4.5f,2.4f,2.3f),amber);
        Cover("B_DefaultCover",P(28,30),new Vector3(4,2.7f,2),blue);
        // Offset full-height shuttering gives entry/plant cover without sealing the site.
        Cover("A_EntryCover",P(-31,15),new Vector3(3.4f,2.2f,2.4f),amber);
        Cover("B_EntryCover",P(26.5f,23),new Vector3(3.2f,2.2f,2.4f),blue);
        Cover("Mid_Transformer",P(2,4),new Vector3(5,3.4f,7),steel);
        for(int i=-1;i<=1;i++){
            Box("TransformerDoor",new Vector3(2+i*1.5f,1.7f,.46f),new Vector3(1.3f,2.7f,.08f),concrete,detail,false);
            for(int j=0;j<4;j++)Box("TransformerVent",new Vector3(2+i*1.5f,2+j*.14f,.40f),new Vector3(.95f,.045f,.03f),dark,detail,false);
        }
        Label("TRANSFORMER",new Vector3(2,3.1f,.39f),0,2.5f,amber.color);
        Box("A_CraneFooting",new Vector3(-39,.4f,24),new Vector3(5.5f,.8f,5.5f),concrete);
        var crane=Prop("Props/TowerCrane_01",new Vector3(-39,.8f,24),1.2f,-25,false);crane.name="A_Crane";
        crane.transform.position+=new Vector3(-39,.8f,24)-LowestFootprint(crane);
        // Opaque service housing is the low cover. Above it the open mast stays
        // open: only visible supplemental posts/rails have matching colliders.
        Cover("CraneBaseHousing",new Vector3(-39,.8f,24),new Vector3(2.1f,3.2f,2.1f),steel);
        foreach(float dx in new[]{-.96f,.96f})foreach(float dz in new[]{-.96f,.96f})
            Box("CraneMastPost",new Vector3(-39+dx,14.4f,24+dz),new Vector3(.18f,20.8f,.18f),steel);
        for(float y=4;y<=24.8f;y+=3.2f)foreach(float sign in new[]{-1f,1f}){
            Box("CraneMastRail",new Vector3(-39,y,24+sign*.96f),new Vector3(2.1f,.18f,.18f),steel);
            Box("CraneMastRail",new Vector3(-39+sign*.96f,y,24),new Vector3(.18f,.18f,2.1f),steel);
        }
        Box("B_LoadingDeck",new Vector3(36.5f,.6f,30),new Vector3(4,1.2f,6),concrete);
        var ramp=Box("B_LoadingRamp",new Vector3(36.5f,.5f,24),new Vector3(4,.25f,6.12f),steel);ramp.transform.rotation=Quaternion.Euler(-11.31f,0,0);
        // An open-roof loading arcade: aerial smoke and the top-down map still target the floor.
        foreach(float z in new[]{20f,30f}){
            Box("B_DockColumn",new Vector3(42.5f,3,z),new Vector3(.5f,6,.5f),steel);
            Beam("B_DockBracket",new Vector3(42.5f,4.3f,z),new Vector3(39.5f,6,z),.14f,steel);
            Box("B_DockCrossBeam",new Vector3(40.5f,6,z),new Vector3(4.5f,.25f,.3f),blue,detail,false);
        }
        // Light, non-occluding roofline structure above the B corridor, not a minimap-hiding ceiling.
        foreach(float z in new[]{-24f,-18f}){
            foreach(float x in new[]{26.2f,31.8f})Box("WorkshopPortalColumn",new Vector3(x,3,z),new Vector3(.22f,6,.22f),steel,detail,false);
            Box("WorkshopPortalBeam",new Vector3(29,6,z),new Vector3(6,.24f,.24f),steel,detail,false);
        }
        // Correct-height dressing in dead wall pockets, not random head-glitch boxes in lanes.
        Prop("Props/Pallet_02",new Vector3(-41.5f,0,10),1.2f,0);
        Prop("Props/CableReel_01",new Vector3(-40.5f,0,29),1,0);
        Prop("Props/ConstructionMat_03",new Vector3(40.8f,0,18.1f),.8f,90);
        Prop("Props/Generator_01",new Vector3(18.4f,0,32.5f),1.25f,0);
        EnclosedScaffold(new Vector3(-37.8f,0,-14),ConstructionMapArtPass.Cream);
        EnclosedScaffold(new Vector3(26.8f,0,-18),ConstructionMapArtPass.Cream);
        Label("A / CRANE",new Vector3(-33,4.5f,30.93f),0,6,amber.color);
        Label("B / LOADING",new Vector3(24,4.5f,35.93f),0,6,blue.color);
        Label("A MAIN",new Vector3(-36.93f,3.7f,-15),270,3,amber.color);
        Label("WORKSHOP",new Vector3(29,5.75f,-24.14f),0,3,white.color);
        Label("A CONNECTOR",new Vector3(-12,3.4f,-10.08f),0,2.8f,amber.color);
        Label("B CONNECTOR",new Vector3(13.93f,3.6f,21),90,2.8f,blue.color);
        // Keep a full walking aisle between the screen and courtyard throat.
        Box("AttackSpawnScreen",new Vector3(0,1.6f,-37),new Vector3(6,3.2f,.7f),steel);
        Box("DefenseSpawnScreen",new Vector3(0,1.6f,38),new Vector3(6,3.2f,.7f),steel);
        Label("ATTACK / SUPPLY",new Vector3(0,2,-37.36f),0,3.2f,white.color);
        Label("DEFENSE / CONTROL",new Vector3(0,2,38.36f),180,3.2f,white.color);
        Label("A",new Vector3(-7,.04f,-39),0,10,amber.color,true);Label("B",new Vector3(9,.04f,-39),0,10,blue.color,true);
        // Unfinished structures are tied into selected roofs; avoid a second row of identical boxes.
        foreach(var span in new[]{new Vector3(-19,0,-21),new Vector3(17,0,-29),new Vector3(-7,0,29)}){
            foreach(float dx in new[]{-3f,3f})foreach(float dz in new[]{-3f,3f})Box("RoofColumn",span+new Vector3(dx,6.9f,dz),new Vector3(.4f,3.2f,.4f),concrete,detail,false);
            foreach(float dz in new[]{-3f,3f})Box("RoofCrossBeam",span+new Vector3(0,8.4f,dz),new Vector3(6.4f,.3f,.4f),concrete,detail,false);
        }
        Background();ConstructionMapArtPass.FinishMaterials();
    }
    static void Background()
    {
        var bg=new GameObject("03_Background_NotPlayable").transform;bg.SetParent(root,false);
        Box("OffsiteGround",new Vector3(0,-.65f,0),new Vector3(180,.4f,180),concrete,bg,false);
        for(int k=0;k<3;k++){
            float x=-58+k*57;
            for(int level=0;level<3;level++){
                Box("OffsiteSlab",new Vector3(x,level*4+4,62),new Vector3(21,.4f,14),concrete,bg,false);
                foreach(float a in new[]{-9f,0,9f})foreach(float b in new[]{-6f,6f})Box("OffsiteColumn",new Vector3(x+a,level*4+2,62+b),new Vector3(.6f,4,.6f),concrete,bg,false);
            }
        }
        var crane=Prop("Props/TowerCrane_02",new Vector3(59,-.45f,56),1.1f,100,false,bg);crane.transform.position+=new Vector3(59,-.45f,56)-LowestFootprint(crane);
        foreach(var t in bg.GetComponentsInChildren<Transform>())t.gameObject.layer=0;
    }
    public static void Barriers(Transform parent)
    {
        foreach(float x in new[]{-40f,-4f})Box("AttackBarrier",new Vector3(x,2.8f,-20),new Vector3(6,5.6f,.25f),blue,parent);
        // B's extra workshop elbows are buy-phase staging, not a 7s attack tax.
        Box("AttackBarrier_B_Workshop",new Vector3(21,2.8f,-2),new Vector3(6,5.6f,.25f),blue,parent);
        foreach(var p in new[]{new Vector3(-40,0,3),new Vector3(35,0,14)})Box("DefenderMainGate",p+Vector3.up*2.8f,new Vector3(6,5.6f,.25f),blue,parent);
        Box("DefenderMidGate",new Vector3(7,2.8f,16),new Vector3(6,5.6f,.25f),blue,parent);
        foreach(var p in new[]{new Vector3(-20,0,2),new Vector3(16,0,17)})Box("DefenderConnectorGate",p+Vector3.up*2.8f,new Vector3(4,5.6f,.25f),blue,parent);
    }
}
#endif
