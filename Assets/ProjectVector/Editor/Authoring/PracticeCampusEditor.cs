#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

// Explicit, offline authoring only. Never runs when the game starts.
public static class PracticeCampusEditor
{
    const string ScenePath = "Assets/Scenes/WeaponTest.unity";
    const string Bundle = "Assets/FPS_Bundle/";
    static readonly string[] Samples = {
        "FPS_Map0/Prefabs/Rock_01.prefab", "FPS_Map0/Prefabs/Tree_01.prefab",
        "FPS_Map0/Prefabs/Tree_02.prefab", "FPS_Map3/Prefabs/Building/Wall_01.prefab",
        "FPS_Map3/Prefabs/Building/Wall_02.prefab", "FPS_Map3/Prefabs/Building/Roof_01.prefab",
        "FPS_Map3/Prefabs/Building/Floor_01.prefab", "FPS_Map4/Prefabs/Props/Generator_01.prefab",
        "FPS_Map5/Prefabs/Props/Pipe_01.prefab", "FPS_Map5/Prefabs/Props/Pipe_02.prefab",
        "FPS_Map3/Prefabs/Props/ShippingContainer_01.prefab", "FPS_Map0/Prefabs/Fence_01.prefab"
    };
    static Material concrete, steel, paint, green, water, white;
    static TMP_FontAsset font;
    static Transform campus;
    static PracticeRangeController range;
    public static void BuildCampus()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new Exception("Save/close the scene before offline authoring.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        if(GameObject.Find("HydroTrainingCampus")!=null)throw new Exception("Campus already exists; do not overwrite authored work.");
        range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        campus=new GameObject("HydroTrainingCampus").transform;
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        concrete=Mat("Concrete",new Color32(161,167,157,255));steel=Mat("Graphite",new Color32(47,58,54,255));
        paint=Mat("SafetyOchre",new Color32(231,172,61,255));green=Mat("Forest",new Color32(62,88,71,255));
        water=Mat("Reservoir",new Color32(69,128,118,255));white=Mat("Ivory",new Color32(230,230,211,255));
        Block(campus,"ContinuousCampusGround",new Vector3(248,-.3f,12),new Vector3(140,.5f,65),concrete);
        var promenade=Group(campus,"00_AccessPromenade");
        Block(promenade,"ContinuousWalkway",new Vector3(248,-.25f,-10),new Vector3(126,.5f,16),concrete);
        foreach(float z in new[]{-16.5f,-4.5f})Block(promenade,"RouteStripe",new Vector3(248,.012f,z),new Vector3(124,.024f,.10f),paint,false);
        for(int i=0;i<4;i++)
        {
            float x=200+i*32;
            Sign(promenade,"Route_"+i,new Vector3(x,0.025f,-10),$"0{i+1}",2.6f,3,paint.color,new Vector3(90,0,0));
            Prop("FPS_Map4/Prefabs/Props/Bench_01.prefab",promenade,new Vector3(x+8,0,-14),1,180);
        }
        var entry=Group(campus,"ArrivalSign");
        Block(entry,"SignBacking",new Vector3(248,2,-17.5f),new Vector3(14,3,.25f),steel);
        Sign(entry,"BaseName",new Vector3(248,2.65f,-17.32f),"VECTOR / HYDRO",1,12,white.color,new Vector3(0,180,0));
        Sign(entry,"BaseDescription",new Vector3(248,1.5f,-17.32f),"산악 훈련 기지   /   4개 훈련 구역",.42f,12,paint.color,new Vector3(0,180,0));
        range.trainingZones=new PracticeTrainingZone[4];
        string[] names={"거리 사격장","반응 훈련동","이동 훈련장","엄폐 훈련동"};
        int[] modes={1,0,2,4};
        for(int i=0;i<4;i++)
        {
            float x=200+i*32;
            var root=Group(campus,$"0{i+1}_{names[i]}");
            var zone=new PracticeTrainingZone{displayName=names[i],defaultMode=modes[i],movementWidth=i==2?5:2};
            range.trainingZones[i]=zone;
            zone.station=Point(root,"PlayerStation",new Vector3(x,.1f,0));
            var volume=Point(root,"WalkInVolume",new Vector3(x,2,18)).gameObject.AddComponent<BoxCollider>();
            volume.isTrigger=true;volume.size=new Vector3(27,6,42);zone.walkInVolume=volume;
            zone.distanceTargets=Enumerable.Range(1,3).Select(n=>Point(root,n*10+"m",new Vector3(x,0,n*10))).ToArray();
            Shell(root,x,i==0?38:33,i==1||i==3);
            Gate(root,x,i+1,names[i]);
            zone.console=Terminal(root,new Vector3(x+9,0,-1),i+1,names[i]);
            if(i==0)DistanceBay(root,x,zone);
            else if(i==1)ReactionBay(root,x,zone);
            else if(i==2)MovingBay(root,x,zone);
            else CoverBay(root,x,zone);
        }
        Landscape();
        // Existing old range geometry/serialized references remain intact; new campus is separate.
        if(range.peekCover!=null)range.peekCover.SetActive(false);
        range.activeZone=0;range.spawner.mode=1;
        range.distanceStation=range.trainingZones[0].station;
        range.spawner.distanceTargets=range.trainingZones[0].distanceTargets;
        range.spawner.authoredSpawnPoints=Array.Empty<Transform>();range.spawner.movementWidth=2;
        range.console=range.trainingZones[0].console;
        range.player.position=range.distanceStation.position;
        foreach(var profile in range.spawner.characterTargets)profile.gameObject.SetActive(false);
        UpdateMenu();
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)
        {
            light.transform.rotation=Quaternion.Euler(48,-32,0);light.intensity=1.25f;
            light.color=new Color(1,.96f,.88f);light.shadows=LightShadows.Soft;
        }
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.65f,.74f,.78f);
        RenderSettings.ambientEquatorColor=new Color(.55f,.58f,.51f);
        RenderSettings.ambientGroundColor=new Color(.3f,.32f,.27f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=290;RenderSettings.fogColor=new Color(.65f,.73f,.72f);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(ScenePath);Validate();CaptureCampus();
        Debug.Log("CAMPUS_AUTHOR_PASS: four saved training zones, continuous walkway, authored controls and bundled environment props. No existing map scene/prefab or account data changed.");
    }
    static void Shell(Transform root,float x,float depth,bool roof)
    {
        var structure=Group(root,"Architecture");
        Block(structure,"Foundation",new Vector3(x,-.3f,depth*.5f-1),new Vector3(28,.6f,depth+4),concrete);
        // Individual bundled wall modules, not a copy of any complete map.
        foreach(float side in new[]{-14f,14f})for(int z=3;z<depth;z+=10)
            Prop("FPS_Map3/Prefabs/Building/Wall_02.prefab",structure,new Vector3(x+side,0,z),2f/3,90);
        for(int j=-1;j<=1;j++)Prop("FPS_Map3/Prefabs/Building/Wall_02.prefab",structure,new Vector3(x+j*9.4f,0,depth),2f/3,0);
        foreach(float side in new[]{-13.1f,13.1f})
        {
            Block(structure,"ReinforcedPlinth",new Vector3(x+side,.4f,depth*.5f),new Vector3(.55f,.8f,depth),steel);
            for(int z=4;z<depth;z+=10)
            {
                Block(structure,"SteelColumn",new Vector3(x+side,3,z),new Vector3(.24f,6,.28f),steel);
                Prop("FPS_Map5/Prefabs/Props/Pipe_01.prefab",structure,new Vector3(x+side-(side>0?.3f:-.3f),1,z),1,90);
            }
        }
        for(int z=0;z<depth;z+=8)
            Block(structure,"FloorJoint",new Vector3(x,.006f,z),new Vector3(25,.01f,.035f),steel,false);
        if(roof)
        {
            // Roof strips leave a central daylight opening, so target silhouettes stay readable.
            foreach(float side in new[]{-7.8f,7.8f})for(int z=5;z<depth;z+=12)
            {
                var r=Prop("FPS_Map3/Prefabs/Building/Roof_01.prefab",structure,new Vector3(x+side,6.3f,z),.8f,90);
                r.transform.localScale=new Vector3(.4f,.8f,.8f);
                GroundAt(r,new Vector3(x+side,6.3f,z));
            }
            for(int z=3;z<depth;z+=10)Block(structure,"RoofTruss",new Vector3(x,6.15f,z),new Vector3(28,.24f,.3f),steel);
        }
        else
        {
            Block(structure,"FiringLineCanopy",new Vector3(x,4.2f,0),new Vector3(26,.25f,5),steel);
            foreach(float side in new[]{-11.5f,11.5f})Block(structure,"CanopyPost",new Vector3(x+side,2,0),new Vector3(.25f,4,.25f),steel);
        }
    }
    static void Gate(Transform root,float x,int number,string title)
    {
        Block(root,"EntryFascia",new Vector3(x,3.6f,-2.8f),new Vector3(17,1,.3f),steel);
        Sign(root,"ZoneTitle",new Vector3(x,3.63f,-2.98f),$"0{number}   /   {title}",.65f,16,white.color);
        foreach(float side in new[]{-8.5f,8.5f})Block(root,"EntryPost",new Vector3(x+side,1.7f,-2.8f),new Vector3(.15f,3.4f,.2f),paint);
        Block(root,"StartLine",new Vector3(x,.014f,0),new Vector3(15,.025f,.12f),paint,false);
        Sign(root,"FloorName",new Vector3(x,.026f,-2),$"TRAINING  /  0{number}",.55f,12,steel.color,new Vector3(90,0,0));
    }
    static void DistanceBay(Transform root,float x,PracticeTrainingZone zone)
    {
        zone.spawnPoints=Array.Empty<Transform>();
        foreach(int z in new[]{10,20,30})
        {
            Block(root,"DistanceLine_"+z,new Vector3(x,.012f,z),new Vector3(20,.02f,.08f),paint,false);
            Sign(root,"Distance_"+z,new Vector3(x+7,1.5f,z+.1f),z+" m",1,4,white.color);
            Block(root,"DistanceSignBacking",new Vector3(x+7,1.5f,z+.2f),new Vector3(3,1.5f,.08f),steel);
        }
        foreach(float side in new[]{-3f,3f})Block(root,"LaneStripe",new Vector3(x+side,.012f,17),new Vector3(.07f,.02f,35),paint,false);
        Block(root,"BallisticBackstop",new Vector3(x,2,36.8f),new Vector3(20,4,.4f),green);
        for(int i=-4;i<=4;i++)Block(root,"BackstopRib",new Vector3(x+i*2.2f,2,36.5f),new Vector3(.08f,4,.12f),steel);
        Prop("FPS_Map3/Prefabs/Props/Rack_01.prefab",root,new Vector3(x-10,0,1),1,90);
        Prop("FPS_Map3/Prefabs/Props/AmoBox_01.prefab",root,new Vector3(x-10,.1f,-1),1,0);
    }
    static void ReactionBay(Transform root,float x,PracticeTrainingZone zone)
    {
        var points=Group(root,"RandomSpawnSockets");
        zone.spawnPoints=(from z in new[]{12f,21f,28f} from dx in new[]{-6f,-2f,2f,6f}
            select Point(points,$"Spawn_{dx}_{z}",new Vector3(x+dx,0,z))).ToArray();
        for(int i=-2;i<=2;i++)
        {
            Block(root,"TargetBackdrop",new Vector3(x+i*4.8f,1.7f,31),new Vector3(4.4f,3.4f,.15f),green);
            Sign(root,"SectorNumber",new Vector3(x+i*4.8f,3,30.85f),(i+3).ToString("00"),.55f,2,paint.color);
        }
        foreach(float side in new[]{-11f,11f})
        {
            Prop("FPS_Map3/Prefabs/Props/Rack_02.prefab",root,new Vector3(x+side,0,11),1,90);
            Prop("FPS_Map3/Prefabs/Props/Pallet_01.prefab",root,new Vector3(x+side,0,25),1,0);
        }
        Sign(root,"TrainingInstruction",new Vector3(x,4.6f,30.8f),"위치가 바뀌는 표적에 대응하세요",.5f,22,white.color);
    }
    static void MovingBay(Transform root,float x,PracticeTrainingZone zone)
    {
        zone.spawnPoints=new[]{Point(root,"NearTraverse",new Vector3(x,0,15)),Point(root,"FarTraverse",new Vector3(x,0,25))};
        foreach(float side in new[]{-9f,9f})
        {
            Prop("FPS_Map3/Prefabs/Props/ShippingContainer_01.prefab",root,new Vector3(x+side,0,17),1,0);
            Prop("FPS_Map4/Prefabs/Props/Generator_01.prefab",root,new Vector3(x+side,0,28),1,side<0?90:270);
        }
        Block(root,"OverheadServiceBeam",new Vector3(x,5,19),new Vector3(23,.6f,.4f),paint);
        foreach(float side in new[]{-11f,11f})Block(root,"CraneSupport",new Vector3(x+side,2.5f,19),new Vector3(.35f,5,.4f),steel);
        foreach(int z in new[]{15,25})Block(root,"TraverseGuide",new Vector3(x,.012f,z),new Vector3(12,.02f,.08f),paint,false);
        Sign(root,"YardLabel",new Vector3(x,4.7f,32.7f),"SERVICE YARD / 이동 · 추적",.65f,24,white.color);
    }
    static void CoverBay(Transform root,float x,PracticeTrainingZone zone)
    {
        var machinery=Group(root,"PermanentGeneratorCover");
        Prop("FPS_Map4/Prefabs/Props/Generator_01.prefab",machinery,new Vector3(x,0,4),1,0);
        // Two real routes around equipment, then a doorway/corner junction.
        foreach(float side in new[]{-8.5f,8.5f})
        {
            Prop("FPS_Map3/Prefabs/Props/ShippingContainer_01.prefab",root,new Vector3(x+side,0,12),1,0);
            Prop("FPS_Map5/Prefabs/Props/Pipe_02.prefab",root,new Vector3(x+side,0,25),1,0);
        }
        foreach(float side in new[]{-8f,8f})
            Block(root,"ControlRoomPartition",new Vector3(x+side,1.7f,20),new Vector3(9,3.4f,.3f),green);
        Block(root,"DoorLintel",new Vector3(x,3.35f,20),new Vector3(7,1,.35f),steel);
        foreach(float side in new[]{-3.5f,3.5f})Block(root,"DoorTrim",new Vector3(x+side,1.4f,19.8f),new Vector3(.12f,2.8f,.12f),paint);
        Prop("FPS_Map4/Prefabs/Props/Generator_01.prefab",root,new Vector3(x+7,0,28),1,90);
        zone.spawnPoints=new[]{Point(root,"LeftEquipmentAngle",new Vector3(x-3.5f,0,11)),Point(root,"RightEquipmentAngle",new Vector3(x+3.5f,0,11)),Point(root,"DoorwayLeft",new Vector3(x-2,0,27)),Point(root,"DoorwayRight",new Vector3(x+2,0,27))};
        Sign(root,"ControlRoom",new Vector3(x,3.4f,19.75f),"CONTROL / 04",.48f,6,white.color);
    }
    static void Landscape()
    {
        var root=Group(campus,"05_ReservoirAndMountainBoundary");
        Block(root,"ReservoirBasin",new Vector3(248,-1.2f,-33),new Vector3(150,1,23),steel);
        Block(root,"StillWater",new Vector3(248,-.61f,-33),new Vector3(150,.03f,23),water,false);
        Block(root,"FrontRetainingWall",new Vector3(248,.65f,-19),new Vector3(127,1.3f,.45f),steel);
        for(int i=0;i<13;i++)
        {
            float x=182+i*11;
            var rock=Prop("FPS_Map0/Prefabs/Rock_01.prefab",root,new Vector3(x,-1,49+(i%3)*3),9+(i%4)*2,17*i);
            rock.transform.localScale=Vector3.Scale(rock.transform.localScale,new Vector3(1,1.5f,1));GroundAt(rock,new Vector3(x,-1,49+(i%3)*3));
            Prop("FPS_Map0/Prefabs/Tree_01.prefab",root,new Vector3(x+3,2,46),2.1f+(i%3)*.4f,41*i);
            Prop("FPS_Map0/Prefabs/Tree_02.prefab",root,new Vector3(x-2,5,56),2.7f,19*i);
        }
        foreach(float x in new[]{181f,315f})
        {
            Block(root,"CampusBoundary",new Vector3(x,1,9),new Vector3(.5f,2,58),steel);
            for(int z=-10;z<50;z+=10)
            {
                Prop("FPS_Map0/Prefabs/Rock_01.prefab",root,new Vector3(x+(x<200?-6:6),-2,z),8,z*11);
                Prop("FPS_Map0/Prefabs/Tree_02.prefab",root,new Vector3(x+(x<200?-9:9),0,z+3),2,z*5);
            }
        }
        // Intake towers beyond the live-fire backstops establish a hydro-facility silhouette.
        foreach(float x in new[]{210f,280f})
        {
            Block(root,"IntakeTower",new Vector3(x,8,43),new Vector3(7,16,6),concrete);
            Block(root,"IntakeCap",new Vector3(x,16.2f,43),new Vector3(8,.5f,7),steel);
            Prop("FPS_Map5/Prefabs/Props/Pipe_01.prefab",root,new Vector3(x-2,1,39.5f),3,0);
            Sign(root,"FacilityMark",new Vector3(x,12,39.8f),"HYDRO",.8f,6,steel.color);
        }
    }
    static void UpdateMenu()
    {
        range.menu.transform.Find("Heading").GetComponent<TMP_Text>().text="훈련 구역";
        range.menu.transform.Find("ModeHeading").GetComponent<TMP_Text>().text="구역으로 이동";
        string[] labels={"02 반응 훈련동","01 거리 사격장","03 이동 훈련장","03 지속 추적","04 엄폐 훈련동"};
        for(int i=0;i<range.modeButtons.Length;i++)range.modeButtons[i].GetComponentInChildren<TMP_Text>().text=labels[i];
        range.menu.transform.Find("Hint").GetComponent<TMP_Text>().text="각 구역까지 걸어서 이동할 수도 있습니다.";
        range.title.fontSize=19;range.title.enableAutoSizing=true;range.title.fontSizeMin=14;range.title.fontSizeMax=19;
        range.menu.SetActive(false);
    }
    static Transform Terminal(Transform root,Vector3 floor,int number,string title)
    {
        var p=Group(root,"AuthoredTrainingTerminal");
        Block(p,"Pedestal",floor+new Vector3(0,.6f,0),new Vector3(.55f,1.2f,.5f),steel);
        Block(p,"ConsoleHousing",floor+new Vector3(0,1.45f,0),new Vector3(1.6f,.8f,.18f),steel);
        Sign(p,"ConsoleLabel",floor+new Vector3(0,1.55f,-.11f),$"0{number}  {title}\n[F] 훈련 설정",.16f,1.5f,white.color);
        return Point(p,"InteractionPoint",floor+Vector3.up);
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/Prefab/WeaponTest/Campus"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};m.SetFloat("_Smoothness",.18f);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Transform Group(Transform parent,string name)=>Point(parent,name,Vector3.zero);
    static Transform Point(Transform parent,string name,Vector3 p)
    {
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.position=p;return o.transform;
    }
    static GameObject Block(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool collider=true)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);
        o.transform.position=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static GameObject Prop(string path,Transform parent,Vector3 ground,float scale,float yaw)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Bundle+path);if(asset==null)throw new Exception(path);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        obj.transform.localScale*=scale;obj.transform.rotation=Quaternion.Euler(0,yaw,0);GroundAt(obj,ground);return obj;
    }
    static void GroundAt(GameObject obj,Vector3 ground)
    {
        var b=BoundsOf(obj);obj.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);
    }
    static void Sign(Transform parent,string name,Vector3 p,string text,float height,float width,Color color,Vector3 rotation=default)
    {
        var t=Point(parent,name,p);t.rotation=Quaternion.Euler(rotation);
        var label=t.gameObject.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=height*10;
        label.color=color;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(width,height*2.6f);
        label.textWrappingMode=TextWrappingModes.NoWrap;label.enableAutoSizing=true;label.fontSizeMin=1;label.fontSizeMax=height*10;
    }
    public static void Validate()
    {
        range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        if(range.trainingZones?.Length!=4)throw new Exception("Four zones required");
        Physics.SyncTransforms();
        foreach(var zone in range.trainingZones)
        {
            if(zone.station==null||zone.console==null||zone.walkInVolume==null||zone.distanceTargets.Length!=3)throw new Exception("Missing zone reference");
            if(!Physics.Raycast(zone.station.position+Vector3.up,Vector3.down,out var floor,3,~0,QueryTriggerInteraction.Ignore))throw new Exception("Station has no floor");
            if(Mathf.Abs(floor.point.y)>.015f)throw new Exception("Station floor is not ground level: "+zone.displayName+" hit="+floor.collider.name+" point="+floor.point.ToString("F4"));
            foreach(var point in zone.spawnPoints.Concat(zone.distanceTargets))
            {
                if(!Physics.Raycast(point.position+Vector3.up*.3f,Vector3.down,out var ground,1,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(ground.point.y-point.position.y)>.02f)
                    throw new Exception("Invalid target floor "+point.name+" "+zone.displayName);
            }
            Debug.Log($"CAMPUS_ZONE_PASS {zone.displayName} station={zone.station.position} spawns={zone.spawnPoints.Length}");
        }
        // Confirm a continuous walking route along the promenade and into every station.
        for(float x=200;x<=296;x+=2)
            if(!Physics.Raycast(new Vector3(x,1,-8),Vector3.down,2,~0,QueryTriggerInteraction.Ignore))throw new Exception("Walkway gap");
        foreach(var t in GameObject.Find("HydroTrainingCampus").GetComponentsInChildren<Transform>(true))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
        Debug.Log("CAMPUS_VALIDATE_PASS: station/target floors, four serialized zones, continuous promenade, missing scripts.");
    }
    public static void CaptureCampus()
    {
        range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var cam=new GameObject("TemporaryReviewCamera").AddComponent<Camera>();cam.fieldOfView=65;cam.farClipPlane=400;
        for(int i=0;i<4;i++)
        {
            var p=range.trainingZones[i].station.position;cam.transform.position=p+new Vector3(-2,1.45f,-1);
            cam.transform.LookAt(p+new Vector3(1,2,19));Render(cam,"campus-zone-"+i+".png");
        }
        cam.farClipPlane=210;
        cam.transform.position=new Vector3(329,67,-64);cam.transform.LookAt(new Vector3(250,1,16));Render(cam,"campus-overview.png",1920,1080);
        cam.transform.position=new Vector3(214,1.65f,-11);cam.transform.LookAt(new Vector3(286,2,-4));Render(cam,"campus-promenade.png");
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
    }
    public static void Review()
    {
        EditorSceneManager.OpenScene(ScenePath);
        range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        Physics.SyncTransforms();
        foreach(var zone in range.trainingZones)
            foreach(var hit in Physics.RaycastAll(zone.station.position+Vector3.up,Vector3.down,4,~0,QueryTriggerInteraction.Ignore))
                Debug.Log($"CAMPUS_FLOOR {zone.displayName} {hit.collider.name} parent={hit.collider.transform.parent?.name} point={hit.point:F4}");
        CaptureCampus();
    }
    public static void FinishCampus()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene/Play");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var root=GameObject.Find("HydroTrainingCampus");
        if(root.transform.Find("ContinuousCampusGround")==null)
            Block(root.transform,"ContinuousCampusGround",new Vector3(248,-.3f,12),new Vector3(140,.5f,65),AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefab/WeaponTest/CampusConcrete.mat"));
        foreach(var text in root.GetComponentsInChildren<TextMeshPro>())
        {
            text.textWrappingMode=TextWrappingModes.NoWrap;text.enableAutoSizing=true;
            text.fontSizeMax=text.fontSize;text.fontSizeMin=1;
        }
        // Use conifers for the mountain treeline, rather than oversized round city trees.
        var trees=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Tree_02").ToArray();
        foreach(var t in trees)
        {
            var ground=BoundsOf(t.gameObject);float scale=t.localScale.x;
            Prop("FPS_Map0/Prefabs/Tree_01.prefab",t.parent,new Vector3(ground.center.x,ground.min.y,ground.center.z),scale*1.25f,t.eulerAngles.y);
            UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // Reload authored data before validation (also catches serialization mistakes).
        EditorSceneManager.OpenScene(ScenePath);Validate();CaptureCampus();
    }
    public static void BuildPlayer()
    {
        string output=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp","VectorHydroTrainingBuild","ProjectVector.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),output,BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Campus player build failed");
        }
        finally { PracticeCampusBuildSupport.Restore(); }
        Debug.Log("CAMPUS_BUILD_PASS "+output);
    }
    public static void InspectAssets()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var root = new GameObject("AssetInspectionOnly");
        for (int i=0;i<Samples.Length;i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Bundle+Samples[i]);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            var bounds = BoundsOf(obj);
            Debug.Log($"CAMPUS_ASSET {Samples[i]} size={bounds.size:F3} center={bounds.center:F3} colliders={obj.GetComponentsInChildren<Collider>().Length}");
            obj.transform.position += new Vector3(i*30,0,100)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            bounds=BoundsOf(obj);
            var camera=new GameObject("InspectionCamera").AddComponent<Camera>();
            camera.transform.position=bounds.center+new Vector3(1,.65f,-1)*Mathf.Max(bounds.size.magnitude,3);
            camera.transform.LookAt(bounds.center);camera.fieldOfView=40;camera.farClipPlane=200;
            Render(camera,"asset-"+i+".png",800,600);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);obj.SetActive(false);
        }
        UnityEngine.Object.DestroyImmediate(root);
    }
    static Bounds BoundsOf(GameObject obj)
    {
        var renderers=obj.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();
        if(renderers.Length==0)throw new Exception("No geometry: "+obj.name);
        var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
    }
    static void Render(Camera camera,string name,int width=1600,int height=900)
    {
        var rt=new RenderTexture(width,height,24);var old=RenderTexture.active;
        camera.targetTexture=rt;RenderTexture.active=rt;camera.Render();
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        var dir=Path.Combine(Path.GetTempPath(),"VectorPracticeCampus");Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir,name),texture.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=old;
        UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
    }
}
#endif
