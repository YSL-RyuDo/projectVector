#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only measurements of the authored map and authoritative server weapon sources.
// Clear/blocked/invalid samples are all written, including unsuccessful sightlines.
public static class ConstructionMapEngagementAudit
{
    sealed class Weapon
    {
        public int id,damage;
        public string name,type;
        public double body,start,end,minimum,range;
        public double Scale(double distance)=>end<=start||distance<=start?1:distance>=end?minimum:1-(1-minimum)*(distance-start)/(end-start);
        public int BodyDamage(double distance)=>(int)Math.Floor(damage*Scale(distance)*body+.000001);
    }
    sealed class Segment
    {
        public string name,purpose;
        public Vector3 from,to;
        public Segment(string name,string purpose,Vector3 from,Vector3 to){this.name=name;this.purpose=purpose;this.from=from;this.to=to;}
    }
    static string F(double value)=>value.ToString("F4",CultureInfo.InvariantCulture);
    static string Q(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
    static string Position(Vector3 p)=>F(p.x)+","+F(p.y)+","+F(p.z);
    static string Hash(string path)
    {
        using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
    }
    static List<Weapon> LoadWeapons(string serverRoot,out string profilePath,out string basePath)
    {
        profilePath=Path.Combine(serverRoot,"Config","weapon_damage_profiles.csv");
        basePath=Path.Combine(serverRoot,"ProjectVectorServer","ProjectVectorServer","ServerWeaponData.cpp");
        var weapons=new List<Weapon>();
        foreach(string line in File.ReadAllLines(profilePath).Skip(1).Where(l=>!string.IsNullOrWhiteSpace(l)))
        {
            var c=line.Split(',');if(c.Length!=8)throw new Exception("Unexpected damage profile schema: "+line);
            int id=int.Parse(c[0],CultureInfo.InvariantCulture);if(id==13)continue; // Thirteen firearms; knife is not a ranged comparison.
            weapons.Add(new Weapon{id=id,name=c[1],body=double.Parse(c[3],CultureInfo.InvariantCulture),start=double.Parse(c[5],CultureInfo.InvariantCulture),end=double.Parse(c[6],CultureInfo.InvariantCulture),minimum=double.Parse(c[7],CultureInfo.InvariantCulture)});
        }
        string source=File.ReadAllText(basePath);
        var matches=Regex.Matches(source,@"ServerWeaponData\s*\{\s*static_cast<int>\(ServerWeaponId::(?<name>\w+)\)\s*,\s*ServerWeaponType::(?<type>\w+)\s*,\s*(?<damage>\d+)\s*,\s*(?<range>[\d.]+)f?\s*,",RegexOptions.CultureInvariant);
        foreach(var w in weapons)
        {
            var row=matches.Cast<Match>().Single(m=>m.Groups["name"].Value==w.name);
            w.damage=int.Parse(row.Groups["damage"].Value,CultureInfo.InvariantCulture);w.range=double.Parse(row.Groups["range"].Value,CultureInfo.InvariantCulture);w.type=row.Groups["type"].Value;
        }
        if(weapons.Count!=13||weapons.Select(w=>w.id).Distinct().Count()!=13||weapons.Any(w=>w.damage<=0))throw new Exception("Expected thirteen complete firearm profiles, not guessed damage data");
        return weapons.OrderBy(w=>w.id).ToList();
    }
    static bool Ground(Vector3 authored,out Vector3 feet)
    {
        feet=authored;
        if(!Physics.Raycast(new Vector3(authored.x,2.5f,authored.z),Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f)return false;
        feet=hit.point;
        // Do not silently reinterpret a ground-level probe as a player standing on an inaccessible crate.
        return Mathf.Abs(feet.y-authored.y)<=.5f;
    }
    static bool BodyClear(Vector3 feet)=>!Physics.CheckCapsule(feet+Vector3.up*.35f,feet+Vector3.up*1.45f,.3f,~0,QueryTriggerInteraction.Ignore);
    static string Hierarchy(Transform t)
    {
        string name=t.name;while(t.parent!=null){t=t.parent;name=t.name+"/"+name;}return name;
    }
    static IEnumerable<Segment> Samples(MapInfo info)
    {
        // All successive route legs AND all non-successive chords: retain blocked chords,
        // rather than selecting only attractive clear shots from the route graph.
        foreach(var route in ConstructionMapLayout.Routes)
        {
            for(int a=0;a<route.Value.Length-1;a++)for(int b=a+1;b<route.Value.Length;b++)
                yield return new Segment(route.Key+"_"+a+"_"+b,b==a+1?"authored_route_leg":"route_chord_not_a_walk_path",route.Value[a],route.Value[b]);
        }
        var sites=info.GetComponentsInChildren<SpikePlantZone>();
        foreach(var site in sites)
        {
            var box=site.GetComponent<BoxCollider>();var center=box.transform.TransformPoint(box.center);
            Vector3 At(float x,float z)=>box.transform.TransformPoint(box.center+new Vector3(box.size.x*x,0,box.size.z*z));
            yield return new Segment(site.name+"_west_east","site_trade_sample",At(-.4f,0),At(.4f,0));
            yield return new Segment(site.name+"_south_north","site_trade_sample",At(0,-.4f),At(0,.4f));
            yield return new Segment(site.name+"_diagonal","site_trade_sample",At(-.4f,-.4f),At(.4f,.4f));
            yield return new Segment("mid_hub_to_"+site.name,"protected_should_block",new Vector3(7,0,7),center);
        }
        if(sites.Length==2)
            yield return new Segment("A_to_B_plant_centers","protected_should_block",sites[0].transform.position,sites[1].transform.position);
        // Transformer exits and opposing cover edges are sampled independently of route averages.
        var transformer=info.transform.Find("01_Architecture/Mid_Transformer");
        if(transformer)
        {
            var box=transformer.GetComponent<BoxCollider>();var bounds=box.bounds;
            var southLeft=new Vector3(bounds.min.x-1,0,bounds.min.z-1);var southRight=new Vector3(bounds.max.x+1,0,bounds.min.z-1);
            var northLeft=new Vector3(bounds.min.x-1,0,bounds.max.z+1);var northRight=new Vector3(bounds.max.x+1,0,bounds.max.z+1);
            yield return new Segment("transformer_left_exit","mid_exit_trade",southLeft,northLeft);
            yield return new Segment("transformer_right_exit","mid_exit_trade",southRight,northRight);
            yield return new Segment("transformer_cross_cover","cover_should_block",southLeft,northRight);
        }
        foreach(int index in new[]{0,2})
            yield return new Segment((index==0?"A":"B")+"_entry_to_CT_rear","protected_should_block",ConstructionMapLayout.ContactPoints[index],new Vector3(0,0,35));
        yield return new Segment("A_long_36m","designed_long_sightline",new Vector3(-40,0,-18),new Vector3(-40,0,18));
        yield return new Segment("A_long_past_crane","crane_should_block",new Vector3(-40,0,-18),new Vector3(-40,0,29));
        yield return new Segment("A_long_past_crane_clear_floor","crane_should_block",new Vector3(-40,0,-18),new Vector3(-40,0,30.5f));
        foreach(var a in info.attackSpawnPoints)foreach(var b in info.defendSpawnPoints)
            yield return new Segment(a.name+"_to_"+b.name,"spawn_should_block",a.position,b.position);
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; never replace an open user scene");
        string serverRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","ProjectVectorServer"));
        var weapons=LoadWeapons(serverRoot,out var profilePath,out var basePath);
        var output=ConstructionMapSurvey.Output;Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
        var rows=new List<string>{"sample,purpose,eye_height_m,from_x,from_y,from_z,to_x,to_y,to_z,planar_distance_m,eye_to_eye_distance_m,from_ground_valid,to_ground_valid,from_body_clear,to_body_clear,from_eye_embedded,to_eye_embedded,ray_blocked,status,blocker"};
        var damageRows=new List<string>{"sample,eye_height_m,sightline_status,weapon_id,weapon,distance_m,falloff_scale,body_damage_before_armor,nominal_server_range_m,within_nominal_range,unit"};
        int clear=0,blocked=0,invalid=0,protectedLeaks=0;
        foreach(var sample in Samples(info))foreach(float eyeHeight in new[]{1.2f,1.45f})
        {
            bool groundA=Ground(sample.from,out var a),groundB=Ground(sample.to,out var b);
            bool bodyA=groundA&&BodyClear(a),bodyB=groundB&&BodyClear(b);
            var eyeA=a+Vector3.up*eyeHeight;var eyeB=b+Vector3.up*eyeHeight;
            bool embeddedA=Physics.CheckSphere(eyeA,.01f,~0,QueryTriggerInteraction.Ignore),embeddedB=Physics.CheckSphere(eyeB,.01f,~0,QueryTriggerInteraction.Ignore);
            bool rayBlocked=Physics.Linecast(eyeA,eyeB,out var hit,~0,QueryTriggerInteraction.Ignore);
            string status=!groundA||!groundB||!bodyA||!bodyB||embeddedA||embeddedB?"invalid_endpoint":rayBlocked?"blocked":"clear";
            if(status=="clear")clear++;else if(status=="blocked")blocked++;else invalid++;
            if(status=="clear"&&sample.purpose.EndsWith("should_block",StringComparison.Ordinal))protectedLeaks++;
            float planar=Vector2.Distance(new(a.x,a.z),new(b.x,b.z)),distance=Vector3.Distance(eyeA,eyeB);
            rows.Add(Q(sample.name)+","+sample.purpose+","+F(eyeHeight)+","+Position(eyeA)+","+Position(eyeB)+","+F(planar)+","+F(distance)+","+groundA+","+groundB+","+bodyA+","+bodyB+","+embeddedA+","+embeddedB+","+rayBlocked+","+status+","+Q(rayBlocked?Hierarchy(hit.collider.transform):""));
            foreach(var w in weapons)
                damageRows.Add(Q(sample.name)+","+F(eyeHeight)+","+status+","+w.id+","+w.name+","+F(distance)+","+F(w.Scale(distance))+","+w.BodyDamage(distance)+","+F(w.range)+","+(distance<=w.range)+","+(w.type=="Shotgun"?"one_pellet":"one_bullet"));
        }
        var comparison=new List<string>{"distance_m,weapon_id,weapon,base_damage,falloff_start_m,falloff_end_m,min_scale,current_scale,body_damage_before_armor,nominal_server_range_m,within_nominal_range,unit"};
        foreach(double distance in new double[]{5,10,15,20,25,30,40,45})foreach(var w in weapons)
            comparison.Add(F(distance)+","+w.id+","+w.name+","+w.damage+","+F(w.start)+","+F(w.end)+","+F(w.minimum)+","+F(w.Scale(distance))+","+w.BodyDamage(distance)+","+F(w.range)+","+(distance<=w.range)+","+(w.type=="Shotgun"?"one_pellet":"one_bullet"));
        File.WriteAllLines(Path.Combine(output,"engagements.csv"),rows);
        File.WriteAllLines(Path.Combine(output,"engagement-weapon-body.csv"),damageRows);
        File.WriteAllLines(Path.Combine(output,"weapon-range-comparison.csv"),comparison);
        File.WriteAllLines(Path.Combine(output,"engagement-audit-notes.txt"),new[]{
            "Authoritative profile: "+profilePath,"Profile SHA256: "+Hash(profilePath),"Authoritative base damage and nominal range: "+basePath,"Base table SHA256: "+Hash(basePath),
            "Saved prefab: "+ConstructionMapAuthoring.MapPath,"Prefab SHA256: "+Hash(Path.Combine(Application.dataPath,ConstructionMapAuthoring.MapPath.Substring("Assets/".Length))),
            "All authored route waypoint pairs, site trades, transformer edges and protected negatives are reported, including blocked and invalid endpoints. This is not an exhaustive visibility graph or a balance approval.",
            "Eyes 1.2m and 1.45m sample production standing-camera range. Grounded endpoints use the current single-floor fixture, with a separate conservative body-clearance test. A ground height differing from the authored waypoint by over .5m is invalid, rather than silently standing on a crate. Embedded endpoints are invalid because Unity rays may ignore a containing collider.",
            "Damage = floor(authoritative base * linear distance scale * body multiplier + 0.000001), before armor. Shotguns are PER PELLET; no guarantee that all pellets hit. Knife excluded from thirteen firearm comparisons.",
            "Blocked/invalid sightline rows retain hypothetical distance damage for comparison; they are NOT successful shots. Beyond nominal server range, profile damage is mathematical only, not a claim that an actual shot can hit.",
            "Eye-to-eye centerline distance is a layout comparison, not a replay of muzzle-to-hit server measurements. No damage, movement, range, recoil or weapon profile is modified.",
            "Samples clear="+clear+", blocked="+blocked+", invalid="+invalid+", protected negative samples unexpectedly clear="+protectedLeaks});
        if(protectedLeaks>0)Debug.LogWarning("CONSTRUCTION_ENGAGEMENT_PROTECTED_LEAKS "+protectedLeaks+"; inspect engagements.csv, not hidden as successes");
        Debug.Log("CONSTRUCTION_ENGAGEMENT_AUDIT_DONE clear="+clear+" blocked="+blocked+" invalid="+invalid+" protectedLeaks="+protectedLeaks+" firearmProfiles="+weapons.Count);
    }
}
#endif
