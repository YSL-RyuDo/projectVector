#if UNITY_EDITOR
using System;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SentinelPlacementVerification
{
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch only; never replace an interactive scene.");
        string gameScene=File.ReadAllText("Assets/Scenes/Game_TestVersion.unity");
        foreach(string field in new[]{"trapPrefab","qTrapPrefab","eTrapPrefab"}){
            var match=Regex.Match(gameScene,@"\b"+field+@": \{fileID: (-?\d+), guid: ([a-f0-9]+), type: 3\}");
            if(!match.Success)throw new Exception("Game scene device reference missing: "+field);
            string path=AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab||!prefab.GetComponent<SentinelDeviceVfx>())throw new Exception("Actual game scene uses unpolished device: "+field+" -> "+path);
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab,out string guid,out long id)||id.ToString()!=match.Groups[1].Value)throw new Exception("Game scene device file ID does not resolve: "+field);
        }
        Debug.Log("SENTINEL_GAME_SCENE_REFERENCES_PASS: actual C/Q/E spawn prefabs resolve to saved device presentation");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var actor=new GameObject("Caster");var skill=actor.AddComponent<SentinelSkill_Q>();
        var wallSkill=actor.AddComponent<SentinelSkill_E>();
        typeof(SentinelSkill_E).GetField("wallLayer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(wallSkill,(LayerMask)(~0));
        var arm=GameObject.CreatePrimitive(PrimitiveType.Cube);arm.name="OwnArm";arm.transform.SetParent(actor.transform);arm.transform.position=new Vector3(0,1.5f,1);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="WorldFloor";floor.transform.position=new Vector3(0,-.25f,4);floor.transform.localScale=new Vector3(10,.5f,10);
        var ray=new Ray(new Vector3(0,2,0),new Vector3(0,-.5f,1));Physics.SyncTransforms();
        if(!Physics.Raycast(ray,out var unfiltered,10)||unfiltered.collider.gameObject!=arm)throw new Exception("Fixture must reproduce self obstruction");
        var method=typeof(SentinelSkill_Q).GetMethod("TryGetPlacementHit",BindingFlags.Instance|BindingFlags.NonPublic);
        object[] args={ray,default(RaycastHit)};
        if(!(bool)method.Invoke(skill,args)||((RaycastHit)args[1]).collider.gameObject!=floor)throw new Exception("Own arm not excluded");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="ForeignObstacle";wall.transform.position=new Vector3(0,1,2);Physics.SyncTransforms();
        args=new object[]{ray,default(RaycastHit)};
        if(!(bool)method.Invoke(skill,args)||((RaycastHit)args[1]).collider.gameObject!=wall||((RaycastHit)args[1]).normal.y>.9f)throw new Exception("Foreign blocker/slope was bypassed");
        args=new object[]{ray,default(RaycastHit)};
        var wallMethod=typeof(SentinelSkill_E).GetMethod("TryGetPlacementHit",BindingFlags.Instance|BindingFlags.NonPublic);
        if(!(bool)wallMethod.Invoke(wallSkill,args)||((RaycastHit)args[1]).collider.gameObject!=wall)throw new Exception("E own-body exclusion or foreign wall selection failed");
        var identity=actor.AddComponent<PlayerIdentity>();identity.PlayerId=17;
        var qDevice=new GameObject("EnemyOnlySlowZone");var qLogic=qDevice.AddComponent<Sentinel_SlowTrap>();
        var qFlags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(Sentinel_SlowTrap).GetField("_ownerPlayerId",qFlags).SetValue(qLogic,(uint)17);
        typeof(Sentinel_SlowTrap).GetField("_ownerTeam",qFlags).SetValue(qLogic,0);
        typeof(Sentinel_SlowTrap).GetField("_zoneRadius",qFlags).SetValue(qLogic,10f);
        var qFilter=typeof(Sentinel_SlowTrap).GetMethod("CanSlowPlayer",qFlags);
        identity.Team=0;
        if((bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q slows its caster");
        identity.PlayerId=18;
        if((bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q slows an ally");
        identity.Team=1;
        if(!(bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q no longer slows an enemy");
        actor.transform.position=Vector3.right*11;
        if((bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q slows an enemy outside the zone");
        actor.transform.position=Vector3.zero;identity.Team=2;
        if((bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q slows an observer");
        identity.Team=1;typeof(Sentinel_SlowTrap).GetField("_ownerTeam",qFlags).SetValue(qLogic,-1);
        if((bool)qFilter.Invoke(qLogic,new object[]{identity}))throw new Exception("Q applies before its team is known");
        identity.PlayerId=17;identity.Team=0;Object.DestroyImmediate(qDevice);
        var trap=new GameObject("ThrownDevice");var rootCollider=trap.AddComponent<SphereCollider>();
        var shell=GameObject.CreatePrimitive(PrimitiveType.Cube);shell.transform.SetParent(trap.transform);var childCollider=shell.GetComponent<Collider>();
        var logic=trap.AddComponent<Sentinel_Trap>();logic.InitNetwork(1,17,0,true,40);
        if(!Physics.GetIgnoreCollision(rootCollider,arm.GetComponent<Collider>())||!Physics.GetIgnoreCollision(childCollider,arm.GetComponent<Collider>()))throw new Exception("Caster child hitboxes still obstruct thrown device");
        typeof(Sentinel_Trap).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(logic,new object[]{arm.GetComponent<Collider>()});
        if(logic.IsPlaced)throw new Exception("Untagged caster child incorrectly anchors C");
        var sightMethod=typeof(Sentinel_Trap).GetMethod("HasLineOfSight",BindingFlags.Instance|BindingFlags.NonPublic);
        // Edit-mode AddComponent does not run this behaviour's Awake.
        typeof(Sentinel_Trap).GetField("visibilityMask",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(logic,-1);
        object[] sightArgs={identity,new Vector3(0,1,0),Vector3.forward,4f};
        if((bool)sightMethod.Invoke(logic,sightArgs))throw new Exception("C wave passes through world cover");
        wall.SetActive(false);Physics.SyncTransforms();
        if(!(bool)sightMethod.Invoke(logic,sightArgs))throw new Exception("Player body incorrectly blocks C wave");
        wall.SetActive(true);Physics.SyncTransforms();
        var outline=wall.AddComponent<Outline>();outline.OutlineMode=Outline.Mode.OutlineVisible;
        outline.OutlineColor=Color.red;outline.OutlineWidth=2;outline.enabled=false;
        var outlineFlags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(Sentinel_Trap).GetMethod("BackupOutline",outlineFlags).Invoke(logic,new object[]{outline});
        outline.enabled=true;outline.OutlineMode=Outline.Mode.OutlineAndSilhouette;outline.OutlineColor=Color.yellow;outline.OutlineWidth=5;
        typeof(Sentinel_Trap).GetMethod("RestoreAllOutlines",outlineFlags).Invoke(logic,null);
        if(outline.enabled||outline.OutlineMode!=Outline.Mode.OutlineVisible||outline.OutlineColor!=Color.red||outline.OutlineWidth!=2)throw new Exception("C reveal did not restore original outline state");
        var model=GameObject.CreatePrimitive(PrimitiveType.Cube);model.name="Model";model.transform.SetParent(actor.transform);
        var cameraObject=new GameObject("FirstPersonCamera");cameraObject.transform.SetParent(actor.transform);var camera=cameraObject.AddComponent<Camera>();
        var view=actor.AddComponent<PlayerCameraController>();typeof(PlayerCameraController).GetField("playerCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,camera);
        var presentation=actor.AddComponent<SentinelCastMotion>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(SentinelCastMotion).GetMethod("Awake",flags).Invoke(presentation,null);
        typeof(SentinelCastMotion).GetMethod("LateUpdate",flags).Invoke(presentation,null);
        if(!model.GetComponent<Renderer>().forceRenderingOff)throw new Exception("Own body leaks into first-person camera");
        camera.enabled=false;typeof(SentinelCastMotion).GetMethod("LateUpdate",flags).Invoke(presentation,null);
        if(model.GetComponent<Renderer>().forceRenderingOff)throw new Exception("Remote/preview body visibility was not restored");
        Object.DestroyImmediate(trap);
        Object.DestroyImmediate(wall);Object.DestroyImmediate(actor);Object.DestroyImmediate(floor);
        Debug.Log("SENTINEL_PLACEMENT_PASS: own arm ignored; world ground retained; foreign obstruction and slope preserved");
        Debug.Log("SENTINEL_VIEW_COLLISION_PASS: root/child projectile pairs ignore caster; untagged player hitbox cannot anchor C; first-person body hidden; remote/preview restored");
        Debug.Log("SENTINEL_Q_TEAM_PASS: caster/ally/observer/unknown team excluded; enemy slowed only within range");
        Debug.Log("SENTINEL_REVEAL_RESTORE_PASS: mode, color, width and enabled state restored after reveal");
        Debug.Log("SENTINEL_WAVE_OCCLUSION_PASS: world cover blocks new hits; player bodies do not");
    }
    public static void VerifyAndBuild(){Run();RankedCaptureTestEditor.Build();}
}
#endif
