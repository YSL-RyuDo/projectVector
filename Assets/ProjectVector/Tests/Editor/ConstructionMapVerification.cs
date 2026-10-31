#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Offline collision/topology evidence, not a substitute for multiplayer balance playtesting.
public static class ConstructionMapVerification
{
    static int N=95;
    static float gridStep=1,navRadius=.35f,navBottom=.45f,navTop=1.6f,stepBottom=.5f,stepTop=1.65f;
    static float productionRadius,productionStandingHeight,productionCrouchHeight,productionJump,productionGravity;
    // Baseline knife speed. Timing rows also use audited production armed/ADS/crouch speeds.
    const float ReferenceSpeed=5.5f;
    static bool[,] free;
    static Vector3[,] floor;
    static StringBuilder report;
    static int checks;
    static readonly Vector3[] NeutralBuyProbes={new(-40,0,1),new(7,0,0),new(35,0,12),new(-20,0,-8)};

    static void Check(bool value,string name)
    {
        if(!value)throw new Exception("CONSTRUCTION_VERIFY_FAIL "+name);
        checks++;report.AppendLine("PASS "+name);
    }
    static string F(float value)=>value.ToString("F3",CultureInfo.InvariantCulture);
    static string TimingRow(string name,List<Vector3> path,string context)
    {
        float length=Length(path);
        return name+","+F(length)+","+F(length/4.5f)+","+F(length/5.5f)+","+F(length/3f)+","+F(length/2.25f)+","+context;
    }
    static Vector2Int Cell(Vector3 p)
    {
        int half=(N-1)/2;int x=Mathf.RoundToInt(p.x/gridStep)+half,z=Mathf.RoundToInt(p.z/gridStep)+half;
        if(x<0||z<0||x>=N||z>=N)throw new Exception("Probe outside navigation grid: "+p);
        return new Vector2Int(x,z);
    }
    static void SampleNavigation(bool fineCrouch=false)
    {
        gridStep=fineCrouch?.25f:1f;N=Mathf.RoundToInt(94/gridStep)+1;
        navRadius=fineCrouch?productionRadius:.35f;
        navBottom=fineCrouch?productionRadius+.03f:.45f;navTop=fineCrouch?productionCrouchHeight-productionRadius+.03f:1.6f;
        stepBottom=fineCrouch?navBottom:.5f;stepTop=fineCrouch?navTop:1.65f;
        Physics.SyncTransforms();free=new bool[N,N];floor=new Vector3[N,N];
        // One walkable floor per column. New stacked floors require a multilayer fixture.
        for(int x=0;x<N;x++)for(int z=0;z<N;z++)
        {
            var p=new Vector3(x*gridStep-47,2.5f,z*gridStep-47);
            if(!Physics.Raycast(p,Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f)continue;
            if(Physics.CheckCapsule(hit.point+Vector3.up*navBottom,hit.point+Vector3.up*navTop,navRadius,~0,QueryTriggerInteraction.Ignore))continue;
            free[x,z]=true;floor[x,z]=hit.point;
        }
    }
    static bool CanStep(Vector2Int a,int x,int z)
    {
        if(x<0||z<0||x>=N||z>=N||!free[x,z])return false;
        if(x!=a.x&&z!=a.y&&(!free[a.x,z]||!free[x,a.y]))return false;
        var delta=floor[x,z]-floor[a.x,a.y];
        if(Mathf.Abs(delta.y)>.4f)return false;
        var feet=floor[a.x,a.y];
        return !Physics.CapsuleCast(feet+Vector3.up*stepBottom,feet+Vector3.up*stepTop,navRadius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
    }
    static List<Vector3> FindPath(Vector3 start,Vector3 end)
    {
        var a=Cell(start);var b=Cell(end);
        if(!free[a.x,a.y]||!free[b.x,b.y])throw new Exception("Blocked route endpoint "+start+" / "+end);
        var distance=new float[N,N];var prev=new Vector2Int[N,N];
        for(int x=0;x<N;x++)for(int z=0;z<N;z++)distance[x,z]=float.PositiveInfinity;
        var pending=new SortedSet<(float,int,int)>();distance[a.x,a.y]=0;pending.Add((0,a.x,a.y));
        while(pending.Count>0)
        {
            var item=pending.Min;pending.Remove(item);var p=new Vector2Int(item.Item2,item.Item3);if(p==b)break;
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
            {
                if(dx==0&&dz==0)continue;int x=p.x+dx,z=p.y+dz;if(!CanStep(p,x,z))continue;
                float next=item.Item1+Vector3.Distance(floor[x,z],floor[p.x,p.y]);if(next>=distance[x,z])continue;
                pending.Remove((distance[x,z],x,z));distance[x,z]=next;prev[x,z]=p;pending.Add((next,x,z));
            }
        }
        if(float.IsInfinity(distance[b.x,b.y]))throw new Exception("No route "+start+" -> "+end);
        var path=new List<Vector3>();var step=b;
        for(int i=0;i<N*N;i++){path.Add(floor[step.x,step.y]);if(step==a)break;step=prev[step.x,step.y];}
        path.Reverse();return path;
    }
    static List<Vector3> ForcedRoute(Vector3[] waypoints)
    {
        if(waypoints==null||waypoints.Length<2)throw new Exception("Named route needs at least two authored waypoints");
        var result=new List<Vector3>();
        for(int i=1;i<waypoints.Length;i++)
        {
            var leg=FindPath(waypoints[i-1],waypoints[i]);result.AddRange(i==1?leg:leg.Skip(1));
        }
        return result;
    }
    static bool[,] Reachable(Vector3 start)
    {
        var seed=Cell(start);Check(free[seed.x,seed.y],"buy-phase spawn has navigable floor "+start);
        var reached=new bool[N,N];var queue=new Queue<Vector2Int>();queue.Enqueue(seed);reached[seed.x,seed.y]=true;
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
            {
                if(dx==0&&dz==0)continue;int x=p.x+dx,z=p.y+dz;
                if(x<0||z<0||x>=N||z>=N||reached[x,z]||!CanStep(p,x,z))continue;
                reached[x,z]=true;queue.Enqueue(new Vector2Int(x,z));
            }
        }
        return reached;
    }
    static float Length(List<Vector3> p)
    {
        float result=0;for(int i=1;i<p.Count;i++)result+=Vector3.Distance(p[i-1],p[i]);return result;
    }
    static void Walk(List<Vector3> path,string name)
    {
        var actor=new GameObject("CharacterControllerProbe");var expectedStart=path[0]+Vector3.up*.04f;
        // Native CharacterController must be created at the intended pose when autoSyncTransforms is false.
        actor.transform.position=expectedStart;var cc=actor.AddComponent<CharacterController>();
        cc.height=1.8f;cc.radius=.3f;cc.center=Vector3.up*.9f;cc.skinWidth=.03f;cc.stepOffset=.25f;cc.slopeLimit=45;
        Physics.SyncTransforms();
        try
        {
            Check(Vector3.Distance(actor.transform.position,expectedStart)<.05f&&Vector3.Distance(cc.bounds.center,expectedStart+cc.center)<.05f,name+" controller native start pose synchronized");
            for(int n=1;n<path.Count;n++)
            {
                for(int i=0;i<45;i++)
                {
                    var delta=path[n]-actor.transform.position;delta.y=0;if(delta.magnitude<.12f)break;
                    cc.Move(delta.normalized*.08f+Vector3.down*.12f);
                }
                // A failed leg must not pass merely because a later shortcut reaches the destination.
                if(Vector2.Distance(new(actor.transform.position.x,actor.transform.position.z),new(path[n].x,path[n].z))>=.35f)
                    throw new Exception("CONSTRUCTION_VERIFY_FAIL "+name+" stuck near "+path[n]+" actual="+actor.transform.position);
            }
            Check(true,name+" physically traversable");
        }
        finally{Object.DestroyImmediate(actor);}
    }
    static void WritePath(string name,List<Vector3> path)
    {
        File.WriteAllLines(Path.Combine(ConstructionMapSurvey.Output,name+"-path.csv"),
            new[]{"x,y,z"}.Concat(path.Select(p=>F(p.x)+","+F(p.y)+","+F(p.z))));
    }
    static Vector3 ApproachPoint(List<Vector3> path,float backDistance)
    {
        float length=0;
        for(int i=path.Count-1;i>0;i--){length+=Vector3.Distance(path[i],path[i-1]);if(length>=backDistance)return path[i-1];}
        return path[0];
    }
    static void CheckDistinctApproaches(string site,List<Vector3> main,List<Vector3> alternate)
    {
        var a=ApproachPoint(main,12);var b=ApproachPoint(alternate,12);
        float separation=Vector2.Distance(new(a.x,a.z),new(b.x,b.z));
        Check(separation>=6,site+" main/mid approaches separated by "+F(separation)+"m at 12m before plant");
        var mainCells=new HashSet<Vector2Int>(main.Select(Cell));var midCells=new HashSet<Vector2Int>(alternate.Select(Cell));
        float overlap=(float)mainCells.Intersect(midCells).Count()/Mathf.Max(1,Mathf.Min(mainCells.Count,midCells.Count));
        report.AppendLine("MEASURE "+site+" main/mid overlap of shorter path="+F(overlap)+" (not a balance score)");
    }
    static void BuyPhaseIsolation(MapInfo info)
    {
        info.SetBarriersActive(true);SampleNavigation(true);
        for(int team=0;team<2;team++)
        {
            var own=team==0?info.attackSpawnPoints:info.defendSpawnPoints;
            var other=team==0?info.defendSpawnPoints:info.attackSpawnPoints;
            var reachable=Reachable(own[0].position);
            foreach(var spawn in own)
            {
                var spawnCell=Cell(spawn.position);Check(free[spawnCell.x,spawnCell.y]&&reachable[spawnCell.x,spawnCell.y],spawn.name+" shares own team's buy connected component");
                foreach(var stage in team==0?ConstructionMapLayout.AttackStages:ConstructionMapLayout.DefenseStages)
                {
                    var c=Cell(stage);Check(reachable[c.x,c.y],spawn.name+" can reach own legal buy staging position "+stage);
                }
                foreach(var target in NeutralBuyProbes)
                {
                    var c=Cell(target);Check(free[c.x,c.y],"neutral probe is playable "+target);
                    Check(!reachable[c.x,c.y],spawn.name+" cannot walk/diagonally bypass into neutral zone "+target);
                }
                foreach(var target in other)
                {
                    var c=Cell(target.position);Check(!reachable[c.x,c.y],spawn.name+" cannot reach opposing spawn "+target.name+" during buy");
                }
            }
        }
        foreach(var box in info.barriers.GetComponentsInChildren<BoxCollider>(true))
        {
            var center=box.transform.TransformPoint(box.center);var axis=box.transform.forward;
            float GroundHeight(Vector3 point)
            {
                Check(Physics.Raycast(new Vector3(point.x,2.5f,point.z),Vector3.down,out var ground,4,~0,QueryTriggerInteraction.Ignore),box.name+" probe has actual floor");
                return ground.point.y+.05f;
            }
            var start=center-axis*1.5f;start.y=GroundHeight(start);
            Check(!Physics.CheckCapsule(start+Vector3.up*productionRadius,start+Vector3.up*(productionStandingHeight-productionRadius),productionRadius,~0,QueryTriggerInteraction.Ignore),box.name+" probe starts above actual ramp/floor, not embedded");
            foreach(float height in new[]{productionStandingHeight,productionCrouchHeight})
                Check(Physics.CapsuleCast(start+Vector3.up*productionRadius,start+Vector3.up*(height-productionRadius),productionRadius,axis,3,~0,QueryTriggerInteraction.Ignore),box.name+" blocks production-radius capsule height="+F(height));
            // Production prefab maximum knife jump; guns are lower.
            // This is not a skill/prop-climb exploit test.
            foreach(float direction in new[]{-1f,1f})
            {
                bool blocked=false;var previous=center-axis*(direction*1.5f);float startHeight=GroundHeight(previous);previous.y=startHeight;
                float duration=3f/ReferenceSpeed;
                for(int step=1;step<=Mathf.CeilToInt(duration/.02f);step++)
                {
                    float time=Mathf.Min(duration,step*.02f);
                    var feet=center+axis*(direction*(-1.5f+Mathf.Min(3,time*ReferenceSpeed)));feet.y=startHeight+productionJump*time-.5f*productionGravity*time*time;
                    var delta=feet-previous;
                    if(Physics.CapsuleCast(previous+Vector3.up*productionRadius,previous+Vector3.up*(productionStandingHeight-productionRadius),productionRadius,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore)){blocked=hit.collider==box;break;}
                    previous=feet;
                }
                Check(blocked,box.name+" blocks baseline jump sweep direction="+direction);
            }
            info.SetBarriersActive(false);Physics.SyncTransforms();
            Check(!Physics.CapsuleCast(start+Vector3.up*productionRadius,start+Vector3.up*(productionStandingHeight-productionRadius),productionRadius,axis,3,~0,QueryTriggerInteraction.Ignore),box.name+" opening releases after buy");
            info.SetBarriersActive(true);Physics.SyncTransforms();
        }
        info.SetBarriersActive(false);SampleNavigation();
    }
    static void ReadProductionCapsules()
    {
        productionRadius=productionStandingHeight=productionCrouchHeight=productionGravity=float.PositiveInfinity;productionJump=0;
        foreach(string name in new[]{"Entry","Initiator","Sentinel","Controller"})
        {
            string path="Assets/ProjectVector/Shared/Characters/Prefabs/"+name+"/"+name+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(prefab!=null,"production capsule source exists "+path);
            var cc=prefab.GetComponentInChildren<CharacterController>(true);Check(cc!=null,name+" production CharacterController found");
            var movement=cc.GetComponent<PlayerMovement>();Check(movement!=null,name+" production movement source found");
            var movementData=new SerializedObject(movement);float drop=movementData.FindProperty("_crouchHeightOffset").floatValue;
            var scale=cc.transform.lossyScale;float radius=cc.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float standing=cc.height*Mathf.Abs(scale.y),crouch=Mathf.Max(radius*2,standing-drop);
            productionRadius=Mathf.Min(productionRadius,radius);productionStandingHeight=Mathf.Min(productionStandingHeight,standing);productionCrouchHeight=Mathf.Min(productionCrouchHeight,crouch);
            productionJump=Mathf.Max(productionJump,movementData.FindProperty("_jumpPower").floatValue);productionGravity=Mathf.Min(productionGravity,Mathf.Abs(movementData.FindProperty("_gravity").floatValue));
            report.AppendLine("SOURCE "+path+" world radius="+F(radius)+" standing="+F(standing)+" crouched="+F(crouch)+" world skin="+F(cc.skinWidth*Mathf.Abs(scale.y)));
        }
        Check(productionRadius>0&&productionCrouchHeight>=2*productionRadius&&productionGravity>0,"production min capsule and jump values valid");
        report.AppendLine("MEASURE buy flood uses .25m grid, min production nominal radius="+F(productionRadius)+", min crouched height="+F(productionCrouchHeight)+". Travel remains conservative .3m-radius CharacterController. Discrete grid is not a formal proof of arbitrary subcell or skill exploits.");
    }
    static void VerifyUtilityPlacement(GameObject map)
    {
        var masses=map.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("ComplexMass_",StringComparison.Ordinal)).ToArray();
        Check(masses.Length>0,"sealed construction masses found for utility placement audit");
        foreach(var mass in masses)
        {
            Check(mass.GetComponentInParent<MapUtilityPlacementBlocker>()!=null,mass.name+" inaccessible roof has utility blocker marker");
            var center=mass.bounds.center;
            Check(Physics.Raycast(new Vector3(center.x,100,center.z),Vector3.down,out var roof,150,~0,QueryTriggerInteraction.Ignore)&&roof.collider.GetComponentInParent<MapUtilityPlacementBlocker>()!=null,mass.name+" top-down first surface rejects utility without passing through");
        }
        foreach(var point in new[]{new Vector3(-40,0,-18),new Vector3(35,0,8),new Vector3(-29,0,18),new Vector3(29,0,26),new Vector3(36.5f,.6f,24)})
        {
            Check(Physics.Raycast(new Vector3(point.x,100,point.z),Vector3.down,out var ground,150,~0,QueryTriggerInteraction.Ignore),"utility ray reaches authored playable point "+point);
            Check(ground.normal.y>=.45f&&ground.collider.GetComponentInParent<MapUtilityPlacementBlocker>()==null&&Mathf.Abs(ground.point.y-point.y)<.3f,"utility point remains legal on first surface "+point);
        }
    }
    static void AlternateWithMainClosed(Dictionary<string,List<Vector3>> routes)
    {
        var closures=new List<GameObject>();
        try
        {
            foreach(var point in new[]{new Vector3(-40,2.5f,3),new Vector3(35,2.5f,14)})
            {
                var g=new GameObject("VerificationOnly_MainClosure");g.transform.position=point;g.AddComponent<BoxCollider>().size=new Vector3(6,5,.4f);closures.Add(g);
            }
            SampleNavigation();
            foreach(string name in new[]{"midA","midB"})
            {
                var alternate=ForcedRoute(ConstructionMapLayout.Routes[name]);Walk(alternate,name+" while both main entrances blocked");
                Check(Length(alternate)<=Length(routes[name])+3,name+" does not secretly depend on main entrance");WritePath(name+"-main-closed",alternate);
            }
        }
        finally{foreach(var g in closures)Object.DestroyImmediate(g);SampleNavigation();}
    }
    static void VerifyAttackerARevision(GameObject map,Dictionary<string,List<Vector3>> routes)
    {
        // Compare with the previous measured route, not the new path's straight-line distance.
        Check(Length(routes["midA"])<66,"attacker-side mid A route under 66m (previous forced route 86.314m)");
        Walk(ForcedRoute(new[]{new Vector3(-4,0,-12),new Vector3(-20,0,-12),new Vector3(-20,0,6),new Vector3(-26,0,6),new Vector3(-29,0,18)}),"new lower A connector");
        var roomFloor=map.transform.Find("01_Architecture/A_ServiceRaisedFloor");
        var roomRoof=map.transform.Find("01_Architecture/A_ServiceRoof");
        Check(roomFloor!=null&&roomRoof!=null,"raised A service room saved with actual floor and roof");
        Check(Mathf.Abs(roomFloor.GetComponent<BoxCollider>().bounds.max.y-.9f)<.02f,"A service room floor is 0.9m high");
        Check(roomRoof.gameObject.layer!=LayerMask.NameToLayer("MiniMap")&&roomFloor.gameObject.layer==LayerMask.NameToLayer("MiniMap"),"minimap shows interior floor rather than roof");
        Check(roomRoof.GetComponent<MapUtilityPlacementBlocker>()!=null,"service roof rejects top-down utility placement");
        Check(Physics.Linecast(new Vector3(-20,2.25f,-3),new Vector3(-20,7,-3),out var ceilingHit,~0,QueryTriggerInteraction.Ignore)&&ceilingHit.collider.transform==roomRoof,"service ceiling physically blocks upward fire");
        foreach(bool reverse in new[]{false,true}){
            var rampRoute=new[]{new Vector3(-20,0,-12),new Vector3(-20,.9f,-3),new Vector3(-20,0,6)};
            if(reverse)Array.Reverse(rampRoute);
            Walk(ForcedRoute(rampRoute),"service room both ramps traversal reverse="+reverse);
        }
        foreach(float eye in new[]{.75f,1.2f,1.45f})
        {
            var up=Vector3.up*eye;
            Check(!Physics.Linecast(new Vector3(-4,0,-12)+up,new Vector3(-20,0,-12)+up,~0,QueryTriggerInteraction.Ignore),"new attacker-side cross passage open eye="+F(eye));
            Check(Physics.Linecast(new Vector3(-6,0,10)+up,new Vector3(-16,0,10)+up,~0,QueryTriggerInteraction.Ignore),"old defender-side mid A connector closed eye="+F(eye));
            Check(Physics.Linecast(new Vector3(-20,0,-8)+up,new Vector3(-20,0,35)+up,~0,QueryTriggerInteraction.Ignore),"A connector dogleg blocks CT rear long angle eye="+F(eye));
        }
        foreach(var name in new[]{"A_EntryCover","B_EntryCover"})
        {
            var cover=map.transform.Find("01_Architecture/"+name);Check(cover!=null,name+" saved in gameplay hierarchy");
            var box=cover.GetComponent<BoxCollider>();Check(box!=null&&box.enabled&&!box.isTrigger,name+" has solid collider");
            var b=box.bounds;Check(Mathf.Abs(b.min.y)<.02f,name+" grounded, not floating");
            foreach(float eye in new[]{.75f,1.2f,1.45f})
                Check(Physics.Linecast(new Vector3(b.center.x,eye,b.min.z-1),new Vector3(b.center.x,eye,b.max.z+1),out var hit,~0,QueryTriggerInteraction.Ignore)&&hit.collider==box,name+" blocks bullet/sight height="+F(eye));
            var ring=new[]{new Vector3(b.min.x-1,0,b.min.z-1),new Vector3(b.max.x+1,0,b.min.z-1),new Vector3(b.max.x+1,0,b.max.z+1),new Vector3(b.min.x-1,0,b.max.z+1),new Vector3(b.min.x-1,0,b.min.z-1)};
            Walk(ForcedRoute(ring),name+" all four sides have circulation");
        }
    }
    static void VerifyAPlantExpansion(GameObject map)
    {
        var a=map.transform.Find("SpikePlantZoneA").GetComponent<BoxCollider>();
        var b=map.transform.Find("SpikePlantZoneB").GetComponent<BoxCollider>();
        Check(a.size==new Vector3(14,.5f,14)&&a.transform.position==new Vector3(-29,.25f,17),"A plant expanded to 14x14, north boundary retained at z=24");
        Check(b.size==new Vector3(10,.5f,12)&&b.transform.position==new Vector3(29,.25f,26),"B plant size and center unchanged");
        foreach(var p in new[]{new Vector3(-35,0,18),new Vector3(-23,0,18),new Vector3(-29,0,11),new Vector3(-35,0,11),new Vector3(-23,0,23)})
            Check(SpikePlantZone.Contains(p),"A newly added strip accepts "+p);
        foreach(float x in new[]{-34f,-24f})foreach(float z in new[]{12f,24f})
            Check(SpikePlantZone.Contains(new Vector3(x,0,z)),"original A boundary still accepted "+x+","+z);
        foreach(var p in new[]{new Vector3(-36.05f,0,17),new Vector3(-21.95f,0,17),new Vector3(-29,0,9.95f),new Vector3(-29,0,24.05f),new Vector3(-20,0,6)})
            Check(!SpikePlantZone.Contains(p),"outside expanded A and entry passage rejected "+p);
        // Sample the added floor, not the old area already occupied by authored cover.
        int addedSamples=0;
        for(float x=-35.75f;x<-22;x+=.5f)for(float z=10.25f;z<24;z+=.5f){
            if(x>=-34&&x<=-24&&z>=12)continue;
            var p=new Vector3(x,0,z);
            Check(Physics.Raycast(p+Vector3.up*.3f,Vector3.down,out var hit,.5f,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y)<.03f,"A added floor grounded "+p);
            Check(!Physics.CheckCapsule(p+Vector3.up*.45f,p+Vector3.up*1.6f,.35f,~0,QueryTriggerInteraction.Ignore),"A added floor clear of walls and crane "+p);
            addedSamples++;
        }
        var edges=map.transform.Find("02_SetDressing").Cast<Transform>().Where(t=>t.name=="A_SiteEdge").ToArray();
        Check(edges.Length==4,"four A floor boundary strips retained");
        foreach(var edge in edges){
            var p=edge.position;var s=edge.localScale;
            Check(s.x>s.z ? Mathf.Abs(p.x+29)<.001f&&Mathf.Abs(Mathf.Abs(p.z-17)-7)<.001f&&Mathf.Abs(s.x-14)<.001f : Mathf.Abs(p.z-17)<.001f&&Mathf.Abs(Mathf.Abs(p.x+29)-7)<.001f&&Mathf.Abs(s.z-14)<.001f,"A visual boundary matches trigger "+p);
        }
        // Export actual client acceptance, including tolerance edges, for the server regression.
        var parity=new List<string>{"map,x,y,z,accepted"};
        foreach(float y in new[]{-.76f,0f,1.25f,1.26f})
            for(float x=-38;x<=-20;x+=.25f)for(float z=8;z<=26;z+=.25f)
                parity.Add("Construction,"+F(x)+","+F(y)+","+F(z)+","+(SpikePlantZone.Contains(new Vector3(x,y,z))?1:0));
        File.WriteAllLines(Path.Combine(ConstructionMapSurvey.Output,"A-plant-client-parity.csv"),parity);
        report.AppendLine("A expansion: "+addedSamples+" added-floor samples, "+(parity.Count-1)+" client boundary acceptance samples exported.");
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");report=new StringBuilder();checks=0;Directory.CreateDirectory(ConstructionMapSurvey.Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
        try
        {
            Check(info.attackSpawnPoints.Length==4&&info.defendSpawnPoints.Length==4,"4 attack + 4 defend slots");
            Check(info.spikeSpawnPoint!=null,"dropped spike spawn bound");
            foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
            {
                Check(Physics.Raycast(spawn.position+Vector3.up,Vector3.down,out var h,3,~0,QueryTriggerInteraction.Ignore),spawn.name+" ground");
                Check(!Physics.CheckCapsule(h.point+Vector3.up*.4f,h.point+Vector3.up*1.5f,.3f,~0,QueryTriggerInteraction.Ignore),spawn.name+" body clearance");
            }
            Check(Vector3.Dot(info.attackSpawnPoints[0].forward,Vector3.forward)>.99f&&Vector3.Dot(info.defendSpawnPoints[0].forward,Vector3.back)>.99f,"both teams face into map");
            foreach(var c in map.GetComponentsInChildren<Collider>())Check(c is BoxCollider,"server-exportable collider: "+c.name);
            var sites=map.GetComponentsInChildren<SpikePlantZone>();Check(sites.Length==2,"exactly two plant sites");
            foreach(var site in sites)
            {
                typeof(SpikePlantZone).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(site,null);
                Check(site.GetComponent<BoxCollider>().isTrigger,site.name+" trigger");Check(SpikePlantZone.Contains(site.transform.position),site.name+" accepts planting");
            }
            Check(SpikePlantZone.Contains(new Vector3(-29,0,18))&&SpikePlantZone.Contains(new Vector3(29,0,26)),"original A/B plant reference points accepted");
            Check(!SpikePlantZone.Contains(Vector3.zero)&&!SpikePlantZone.Contains(new Vector3(-29,0,25))&&!SpikePlantZone.Contains(new Vector3(29,0,33)),"outside authored plant boxes rejected");
            VerifyAPlantExpansion(map);
            ReadProductionCapsules();BuyPhaseIsolation(info);VerifyUtilityPlacement(map);
            var csv=new List<string>{"route,length_m,seconds_gun_4_5,seconds_knife_5_5,seconds_ads_3,seconds_crouch_2_25,context"};
            foreach(var site in sites)foreach(int team in new[]{0,1})
            {
                var spawns=team==0?info.attackSpawnPoints:info.defendSpawnPoints;
                for(int i=0;i<spawns.Length;i++)
                {
                    string name=(team==0?"Attack":"Defend")+i+"_"+site.name;var path=FindPath(spawns[i].position,site.transform.position);
                    Walk(path,name);csv.Add(TimingRow(name,path,"spawn baseline NOT round-start contact timing"));
                }
            }
            var routes=new Dictionary<string,List<Vector3>>();
            foreach(string required in new[]{"mainA","mainB","midA","midB","rearRotation"})Check(ConstructionMapLayout.Routes.ContainsKey(required),"authored tactical route "+required);
            foreach(var route in ConstructionMapLayout.Routes)
            {
                var path=ForcedRoute(route.Value);Walk(path,route.Key);routes.Add(route.Key,path);WritePath(route.Key,path);
                csv.Add(TimingRow(route.Key,path,"forced authored route with barriers released"));
            }
            CheckDistinctApproaches("A",routes["mainA"],routes["midA"]);CheckDistinctApproaches("B",routes["mainB"],routes["midB"]);AlternateWithMainClosed(routes);
            VerifyAttackerARevision(map,routes);
            Check(ConstructionMapLayout.AttackStages.Length==3&&ConstructionMapLayout.DefenseStages.Length==3&&ConstructionMapLayout.ContactPoints.Length==3,"three authored staging/contact comparisons");
            for(int i=0;i<3;i++)foreach(int team in new[]{0,1})
            {
                var stage=team==0?ConstructionMapLayout.AttackStages[i]:ConstructionMapLayout.DefenseStages[i];var target=ConstructionMapLayout.ContactPoints[i];
                var path=FindPath(stage,target);string name=(team==0?"AttackGate":"DefenderPreposition")+"_"+new[]{"A","Mid","B"}[i];
                Walk(path,name);WritePath(name,path);csv.Add(TimingRow(name,path,"staging to authored checkpoint NOT earliest visual contact"));
            }
            foreach(float eye in new[]{.75f,1.2f,1.45f})
            {
                var up=Vector3.up*eye;
                Check(Physics.Linecast(new Vector3(7,0,7)+up,new Vector3(-29,0,18)+up,~0,QueryTriggerInteraction.Ignore),"mid hub cannot see A plant eye="+F(eye));
                Check(Physics.Linecast(new Vector3(7,0,7)+up,new Vector3(29,0,26)+up,~0,QueryTriggerInteraction.Ignore),"mid hub cannot see B plant eye="+F(eye));
                foreach(var entry in new[]{new Vector3(-40,0,7),new Vector3(35,0,17)})
                    Check(Physics.Linecast(entry+up,new Vector3(0,0,35)+up,~0,QueryTriggerInteraction.Ignore),"main entry "+entry+" cannot see protected CT rear eye="+F(eye));
                foreach(var a in info.attackSpawnPoints)foreach(var b in info.defendSpawnPoints)
                    Check(Physics.Linecast(a.position+up,b.position+up,~0,QueryTriggerInteraction.Ignore),a.name+" to "+b.name+" sight blocked eye="+F(eye));
            }
            var pipelineBefore=QualitySettings.renderPipeline;int aaBefore=QualitySettings.antiAliasing;var env=map.GetComponent<ConstructionMapEnvironment>();
            env.Apply();Check(QualitySettings.renderPipeline==env.pipeline,"map-only shadow profile applied");env.Restore();Check(QualitySettings.renderPipeline==pipelineBefore&&QualitySettings.antiAliasing==aaBefore,"previous quality profile and AA restored");
            var crane=map.transform.Find("02_SetDressing/A_Crane");var contact=ConstructionMapAuthoring.LowestFootprint(crane.gameObject);
            Check(Vector2.Distance(new(contact.x,contact.z),new(-39,24))<.05f,"crane mast centered on footing, not boom bounds");
            Check(Physics.Raycast(contact+Vector3.up*.02f,Vector3.down,out var support,.1f,~0,QueryTriggerInteraction.Ignore)&&support.collider.name=="A_CraneFooting","crane base touches load-bearing concrete");
            report.AppendLine("Times are distance / audited rounded speeds: gun 4.5m/s (raw gun weights produce 4.5001-4.5011), knife 5.5, gun ADS 3.0, gun crouch 2.25. Source: production prefab speed 5.5; PlayerMovement.CalculateMoveSpeed weight formula, ADS /1.5, crouch multiplier .5. No invented sprint mode.");
            report.AppendLine("Timing assumes instantaneous constant speed, without acceleration, collision delay, combat or reactions. Not observed runtime timing or a balance score. Defenders may preposition behind their own gates during buy.");
            report.AppendLine("Staging-to-checkpoint times are movement comparisons, NOT earliest visual contact: an A-long peek can see opponents before reaching the site-entry checkpoint.");
            report.AppendLine("Buy isolation sampled actual minimum nominal-radius crouch capsule on a .25m single-floor grid. It does not model CharacterController skin penetration, all subcell routes or skill exploits. Gate jump sweeps are baseline production jump tests, not exhaustive player-ability simulation.");
            report.AppendLine("Offline authoring/physics checks, not a six-player combat or win-rate test. Checks: "+checks);
            File.WriteAllLines(Path.Combine(ConstructionMapSurvey.Output,"routes.csv"),csv);Debug.Log("CONSTRUCTION_VERIFY_PASS checks="+checks);
        }
        catch(Exception error){report.AppendLine(error.ToString());throw;}
        finally{File.WriteAllText(Path.Combine(ConstructionMapSurvey.Output,"verification.txt"),report.ToString());}
    }
}
#endif
