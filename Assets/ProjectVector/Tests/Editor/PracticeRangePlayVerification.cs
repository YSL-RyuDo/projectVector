#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Opt-in batch Play test. Does not save scenes or account data.
[InitializeOnLoad]
public static class PracticeRangePlayVerification
{
    const string Active = "Vector.Practice.PlayVerification";
    static double deadline, next;
    static int phase;
    static PracticeRangeController range;
    static PracticeWeaponInput input;
    static bool failed;
    static int runtimeErrors;
    static PracticeRangePlayVerification()
    {
        EditorApplication.playModeStateChanged += State;
        Application.logMessageReceived += (message,stack,type) => {if(EditorApplication.isPlaying && (type==LogType.Exception || type==LogType.Error))++runtimeErrors;};
        if (SessionState.GetBool(Active,false)) { deadline=EditorApplication.timeSinceStartup+90; EditorApplication.update+=Tick; }
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene first");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");
        SessionState.SetBool(Active,true);deadline=EditorApplication.timeSinceStartup+90;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    public static void AuditSize()
    {
        SessionState.SetBool("Vector.Practice.SizeAudit",true);
        Run();
    }
    static void LogSize(Transform root,string label)
    {
        var model=root.Find("Model");
        bool found=false;var bounds=new Bounds();
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b=renderer.bounds;
            if(renderer is SkinnedMeshRenderer skinned)
            {
                var mesh=new Mesh();skinned.BakeMesh(mesh,true);
                var vertices=mesh.vertices;
                if(vertices.Length>0){b=new Bounds(renderer.transform.TransformPoint(vertices[0]),Vector3.zero);foreach(var v in vertices)b.Encapsulate(renderer.transform.TransformPoint(v));}
                UnityEngine.Object.Destroy(mesh);
            }
            if(!found){bounds=b;found=true;}else bounds.Encapsulate(b);
        }
        var cc=root.GetComponent<CharacterController>();
        Debug.Log($"SIZE_AUDIT {label}: root={root.position:F4}, worldScale={root.lossyScale:F4}, modelScale={model.lossyScale:F4}, meshHeight={bounds.size.y:F4}, meshBottom={bounds.min.y:F4}, meshTop={bounds.max.y:F4}, capsuleHeight={(cc!=null?cc.height:0):F4}");
        foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))Debug.Log($"SIZE_RENDERER {label} {r.name} scale={r.transform.lossyScale:F4} bounds={r.bounds}");
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Active,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){phase=0;next=EditorApplication.timeSinceStartup+2;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Active,false);EditorApplication.Exit(failed?1:0);}
    }
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Play verification timed out");
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next)return;
            if(phase==0)
            {
                range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();input=range.player.GetComponent<PracticeWeaponInput>();range.persistRecords=false;
                if(range.loadout!=null){range.spawner.realisticHealth=false;range.loadout.Confirm();}
                var sceneRender=UnityEngine.Object.FindFirstObjectByType<PracticeSceneRendering>();
                if(sceneRender!=null)
                {
                    Assert(QualitySettings.renderPipeline==sceneRender.trainingPipeline,"Training lighting not applied");
                    sceneRender.enabled=false;
                    Assert(QualitySettings.renderPipeline!=sceneRender.trainingPipeline,"Training pipeline leaked after disable");
                    sceneRender.enabled=true;
                    Assert(QualitySettings.renderPipeline==sceneRender.trainingPipeline,"Training lighting not restored on reenable");
                }
                if(SessionState.GetBool("Vector.Practice.SizeAudit",false))
                {
                    LogSize(range.player,"PLAYER");LogSize(range.spawner.enemyObject.transform,"TARGET");
                    Debug.Log($"SIZE_AUDIT CAMERA world={input.aimCamera.transform.position:F4} local={input.aimCamera.transform.localPosition:F4} fov={input.aimCamera.fieldOfView} ground={range.player.GetComponent<CharacterController>().isGrounded}");
                    SessionState.SetBool("Vector.Practice.SizeAudit",false);EditorApplication.ExitPlaymode();phase=4;return;
                }
                Assert(!range.MenuOpen && range.inventory.GetCurrentWeapon()?.Data==range.rifle,"Unobstructed start/rifle");
                var header=range.title.transform.parent.GetComponent<RectTransform>();
                Assert(header.sizeDelta.x<=350 && header.sizeDelta.y<=140 && header.anchorMin==new Vector2(0,1),"Status HUD is not compact/top-left");
                Assert(range.inventory.GetCurrentWeapon().gameObject.activeInHierarchy,"Rifle not visible");
                var canvas=range.menu.GetComponentInParent<Canvas>();
                Assert(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight==1,"Ultrawide settings can be clipped");
                Assert(canvas.transform.Find("CrossHair").GetComponent<RectTransform>().anchorMin==new Vector2(.5f,.5f),"Crosshair not centered across aspect ratios");
                range.EquipRifle();Assert(range.inventory.GetCurrentWeapon().gameObject.activeInHierarchy,"Same-slot rifle re-equip");
                range.SetMode(1);range.SetDistance(0);range.spawner.headOnly=false;range.infiniteAmmo=false;range.Normal();
                next=EditorApplication.timeSinceStartup+1.1;phase=1;
            }
            else if(phase==1)
            {
                var sp=range.spawner;sp.StopTraining();sp.sessionKind=EnemySpawner.SessionKind.Free;range.FreePractice();sp.Advance(1);
                Assert(range.player.position.y>-.5f,"Player fell through floor");
                var zone=sp.enemyObject.GetComponentsInChildren<PracticeTargetZone>().Single(z=>!z.isHead&&!z.isLegs);
                // Move camera close for a deterministic physical hit even with authored weapon spread.
                input.aimCamera.transform.position=zone.transform.position+Vector3.back;
                input.aimCamera.transform.LookAt(zone.transform.position);Physics.SyncTransforms();
                int ammo=range.inventory.GetCurrentWeapon().CurrentAmmo;
                Assert(input.TryFire(),"Fire blocked");Assert(sp.Shots==1,"Shared Weapon did not count shot");
                Assert(range.inventory.GetCurrentWeapon().CurrentAmmo==ammo-1,"Ammo not consumed");
                Assert(!input.TryFire(),"Cooldown bypass");
                next=EditorApplication.timeSinceStartup+.2;phase=10;
            }
            else if(phase==10)
            {
                var sp=range.spawner;
                Assert(sp.Score==1,"Shared Bullet physical hit was not recorded");
                range.OpenMenu();float elapsed=sp.Elapsed;sp.Advance(100);Assert(sp.Elapsed==elapsed && !input.TryFire(),"Menu did not pause");
                range.EquipPistol();Assert(range.inventory.GetCurrentWeapon()?.Data==range.pistol,"Pistol selection");
                Assert(range.inventory.Items.Count(i=>i!=null&&i.gameObject.activeSelf)==1,"Multiple held weapons visible");
                range.Resume();range.inventory.GetCurrentWeapon().SetAmmo(0,range.pistol.maxReserveAmmo);
                range.inventory.SendMessage("TryStartReload");Assert(range.inventory.IsReloading,"Reload not started");
                next=EditorApplication.timeSinceStartup+range.pistol.reloadTime+.5;phase=2;
            }
            else if(phase==2)
            {
                Assert(!range.inventory.IsReloading && range.inventory.GetCurrentWeapon().CurrentAmmo==range.pistol.magSize,"Reload did not finish");
                var sp=range.spawner;range.SetMode(2);sp.irregularMovement=true;sp.sessionKind=EnemySpawner.SessionKind.Free;range.FreePractice();sp.Advance(1);
                Vector3 before=sp.enemyObject.transform.position;sp.Advance(.2f);Assert(Vector3.Distance(before,sp.enemyObject.transform.position)>.1f,"Strafing target stationary");
                sp.headOnly=true;sp.RecordShot();Assert(!sp.RegisterHit(false)&&sp.Score==0,"Body awarded head-only score");
                sp.RecordShot();Assert(sp.RegisterHit(true)&&sp.Score==1&&sp.HeadHits==1,"Head hit not awarded");Assert(!sp.RegisterHit(true)&&sp.Score==1,"Duplicate head credit");
                sp.StopTraining();range.TimedChallenge();sp.Advance(60);Assert(!sp.Running && range.MenuOpen,"Timed completion");
                range.FreePractice();sp.Advance(1000);Assert(sp.Running,"Free practice ended at time limit");
                range.OpenMenu();range.infiniteAmmo=true;range.Resume();range.inventory.GetCurrentWeapon().SetAmmo(0,0);
                Assert(input.TryFire()&&range.inventory.GetCurrentWeapon().CurrentAmmo==0,"Infinite ammo empty-mag handling");
                range.SetMode(3);sp.headOnly=false;range.FreePractice();sp.Advance(1);
                Assert(!sp.RegisterHit(false) && sp.enemyObject.activeSelf,"Tracking target disappeared on hit");
                float tracked=sp.TrackingDuration;sp.Advance(.1f);Assert(sp.TrackingDuration>tracked,"Tracking timer did not advance");
                var trackingZone=sp.enemyObject.GetComponentsInChildren<PracticeTargetZone>().Single(z=>!z.isHead&&!z.isLegs);
                input.aimCamera.transform.position=trackingZone.transform.position+Vector3.back;input.aimCamera.transform.LookAt(trackingZone.transform.position);Physics.SyncTransforms();
                sp.Advance(.001f);Assert(sp.TrackingTime>0,"On-target aim not counted");
                range.OpenMenu();tracked=sp.TrackingDuration;sp.Advance(5);Assert(sp.TrackingDuration==tracked,"Paused tracking advanced");
                range.SetMode(4);range.FreePractice();sp.Advance(1);Physics.SyncTransforms();
                if(range.HasCampus)
                {
                    Assert(Physics.Raycast(range.distanceStation.position+Vector3.up*1.25f,Vector3.forward,out var machineHit,5,~0,QueryTriggerInteraction.Ignore) && machineHit.collider.transform.IsChildOf(GameObject.Find("HydroTrainingCampus").transform),"Campus machinery does not block fire");
                    VerifyCampus();
                }
                else
                {
                    Assert(range.peekCover.activeSelf,"Peek cover absent");
                    Assert(Physics.Raycast(new Vector3(40,1.35f,0),Vector3.forward,out var coverHit,4) && coverHit.collider.transform.IsChildOf(range.peekCover.transform),"Peek cover does not block fire");
                }
                range.SetMode(1);Assert(!range.peekCover.activeSelf,"Peek cover leaked into other modes");
                var targetModel=sp.enemyObject.transform.Find("Model");Assert(targetModel!=null && targetModel.localScale==Vector3.one,"Target model scale changed");
                Assert(Vector3.Dot(targetModel.up,Vector3.up)>.999f,"Target model is not upright");
                var boxes=sp.enemyObject.GetComponentsInChildren<BoxCollider>(true);Assert(Mathf.Abs(boxes.Sum(b=>b.size.y)-1.5f)<.001f && boxes.All(b=>Mathf.Abs(b.size.x-.4f)<.001f),"Target dimensions differ from game character");
                Assert(sp.characterTargets.Length==4,"Not all gameplay character sizes are authored");
                for(int i=0;i<sp.characterTargets.Length;i++)
                {
                    sp.targetCharacterIndex=i;range.FreePractice();sp.Advance(1);
                    var profile=sp.characterTargets[i];var source=profile.gameplayPrefab;var cc=source.GetComponent<CharacterController>();
                    Assert(sp.enemyObject==profile.gameObject && sp.characterTargets.Count(t=>t.gameObject.activeSelf)==1,"Target selection left another collider active");
                    Assert((profile.transform.lossyScale-source.transform.lossyScale).sqrMagnitude<1e-8f,"Per-character root scale lost");
                    Assert((profile.model.localScale-source.transform.Find("Model").localScale).sqrMagnitude<1e-8f,"Per-character model scale lost");
                    Assert(Mathf.Abs(profile.worldCapsuleHeight-cc.height*source.transform.lossyScale.y)<.0001f,"Per-character world height lost");
                    Assert(Mathf.Abs(profile.transform.position.y-sp.distanceTargets[0].position.y-profile.groundedRootOffset)<.0001f,"Target ground alignment lost");
                    LogSize(profile.transform,"PROFILE_"+i);
                    sp.RecordShot();Assert(sp.RegisterHit(false) && sp.Score==1,"Character target cannot be hit");
                }
                sp.targetCharacterIndex=0;
                range.EquipRifle();range.SetMode(1);range.SetDistance(0);sp.irregularMovement=false;sp.headOnly=false;range.FreePractice();sp.Advance(1);range.OpenMenu();
                Assert(Mathf.Abs(range.player.position.y-range.spawner.enemyObject.transform.position.y)<.001f,"Same-character feet baseline differs");
                Assert(input.aimCamera.transform.position.y<1.8f,"Menu/restart left player camera floating above target");
                // Fixture uses live Start/Update data and actual held weapon, no scene is saved.
                next=EditorApplication.timeSinceStartup+.5;phase=3;
            }
            else if(phase==3)
            {
                Capture("practice-play-menu.png");range.Resume();Capture("practice-play-range.png");
                if(range.HasCampus)
                {
                    for(int i=0;i<4;i++)
                    {
                        range.SetMode(range.trainingZones[i].defaultMode);range.FreePractice();range.spawner.Advance(1);
                        if(i==3)range.player.GetComponent<CharacterController>().Move(Vector3.left*3.2f);
                        Capture("campus-play-"+i+".png");
                    }
                    range.SetMode(1);range.FreePractice();range.spawner.Advance(1);
                }
                for(int i=0;i<range.spawner.characterTargets.Length;i++)
                {
                    range.spawner.targetCharacterIndex=i;range.FreePractice();range.spawner.Advance(1);
                    var target=range.spawner.characterTargets[i];if(target.animator!=null)target.animator.Update(1f);
                    Capture("practice-character-"+i+".png");
                }
                range.spawner.targetCharacterIndex=0;
                range.SetMode(4);range.FreePractice();range.spawner.Advance(1);Capture("practice-peek.png");
                range.SetMode(3);range.TimedChallenge();range.spawner.Advance(1);Capture("practice-tracking.png");
                var weapon=range.inventory.GetCurrentWeapon();var bounds=weapon.GetComponentsInChildren<Renderer>().First().bounds;
                foreach(var r in weapon.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                Debug.Log($"PRACTICE_WEAPON_POSE {weapon.transform.position} {weapon.transform.eulerAngles} {weapon.transform.lossyScale} bounds={bounds} cam={input.aimCamera.transform.position}");
                Assert(runtimeErrors==0,"Runtime errors during Play: "+runtimeErrors);
                Debug.Log("PRACTICE_PLAY_PASS: character dimensions, tracking persistence/timer/pause, peek cover isolation/blocking, aspect-ratio anchors, visible weapon re-equip, physics hit, cooldown, ammo, reload, menu pause, moving/head targets, timed/free completion, infinite ammo. No account writes.");
                EditorApplication.ExitPlaymode();phase=4;
            }
        }
        catch(Exception error){failed=true;Debug.LogException(error);if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Active,false);EditorApplication.Exit(1);}}
    }
    static void VerifyCampus()
    {
        var zones=range.trainingZones;var sp=range.spawner;
        Assert(zones.Length==4,"Campus missing zones");
        var capsule=range.player.GetComponent<CharacterController>();
        var root=GameObject.Find("HydroTrainingCampus").transform;
        foreach(var zone in zones)
        {
            range.SetMode(zone.defaultMode);range.FreePractice();sp.Advance(1);Physics.SyncTransforms();
            Assert(range.distanceStation==zone.station && sp.distanceTargets==zone.distanceTargets,"Wrong zone/station binding");
            Assert(Vector3.Distance(range.player.position,zone.station.position)<.2f,"Quick travel did not use zone station");
            Assert(sp.enemyObject.transform.position.x>180,"Target spawned in legacy lane");
            foreach(var point in zone.spawnPoints)
            {
                Assert(zone.walkInVolume.bounds.Contains(point.position+Vector3.up),"Target outside bay");
                // Reaction/moving targets must have clear sightlines. Cover targets intentionally occluded.
                if(zone.defaultMode==4)continue;
                Vector3 origin=zone.station.position+Vector3.up*1.3f;
                Vector3 aim=point.position+Vector3.up*.8f;
                foreach(var hit in Physics.RaycastAll(origin,(aim-origin).normalized,Vector3.Distance(origin,aim)-.3f,~0,QueryTriggerInteraction.Ignore))
                    Assert(!hit.collider.transform.IsChildOf(root),"Architecture blocks authored target: "+point.name+" / "+hit.collider.name);
            }
        }
        range.SetMode(0);range.FreePractice();
        var previous=new Vector3(float.MaxValue,0,0);
        for(int i=0;i<24;i++)
        {
            sp.Advance(1);var pos=sp.enemyObject.transform.position;
            Assert(pos!=previous,"Random spawn repeated immediately");previous=pos;
            Assert(zones[1].spawnPoints.Any(p=>(p.position+Vector3.up*sp.enemyObject.GetComponent<PracticeTargetProfile>().groundedRootOffset-pos).sqrMagnitude<.001f),"Random target off socket");
            sp.RegisterHit(false);sp.enemyObject.SetActive(false);sp.Advance(.01f);
        }
        // Walk the shared promenade with the real CharacterController, not transform teleports.
        sp.StopTraining();capsule.enabled=false;range.player.position=new Vector3(200,.1f,-8);capsule.enabled=true;
        Physics.SyncTransforms();
        for(int i=0;i<480;i++)capsule.Move(new Vector3(.2f,-.05f,0));
        Assert(Mathf.Abs(range.player.position.x-296)<.5f,"Promenade walking path obstructed");
        range.SetMode(1);range.FreePractice();
        capsule.enabled=false;range.player.position=zones[1].station.position;capsule.enabled=true;Physics.SyncTransforms();
        Vector3 before=range.player.position;range.SendMessage("CheckWalkInZone");
        Assert(range.activeZone==1 && sp.mode==0 && sp.Running,"Walking into bay does not select its training");
        Assert(range.player.position==before,"Walk-in unexpectedly teleported player");
        range.SetMode(3);range.FreePractice();
        Assert(range.activeZone==2 && sp.mode==3,"Tracking not mapped to moving yard");
        sp.Advance(1);
        for(int i=0;i<50;i++){sp.Advance(.1f);Assert(zones[2].walkInVolume.bounds.Contains(sp.enemyObject.transform.position+Vector3.up),"Moving target left its yard");}
        range.SetMode(4);range.FreePractice();sp.Advance(1);Physics.SyncTransforms();
        foreach(var point in zones[3].spawnPoints)
        {
            Vector3 origin=zones[3].station.position+new Vector3(point.position.x<zones[3].station.position.x?-3.2f:3.2f,1.3f,0);
            Vector3 aim=point.position+Vector3.up*.8f;
            foreach(var hit in Physics.RaycastAll(origin,(aim-origin).normalized,Vector3.Distance(origin,aim)-.3f,~0,QueryTriggerInteraction.Ignore))
                Assert(!hit.collider.transform.IsChildOf(root),"Cover target cannot be peeked: "+point.name+" / "+hit.collider.name);
        }
        Debug.Log("CAMPUS_PLAY_PASS: four quick-travel stations, clear random/moving sightlines, nonrepeating authored random spawns, live controller promenade traversal, walk-in without teleport, tracking yard and permanent machinery cover. No account writes.");
    }
    static void Capture(string name)
    {
        var cam=input.aimCamera;cam.transform.localPosition=Vector3.zero;cam.transform.localRotation=Quaternion.identity;
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=cam.nearClipPlane+1f;
        range.SendMessage("Refresh");Canvas.ForceUpdateCanvases();
        var rt=new RenderTexture(1920,1080,24);cam.targetTexture=rt;var old=RenderTexture.active;RenderTexture.active=rt;cam.Render();
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        string dir=Path.Combine(Path.GetTempPath(),"VectorPracticeVerification");Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,name),image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
    }
}
#endif
