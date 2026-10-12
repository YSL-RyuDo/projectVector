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
using UnityEngine.Events;
using UnityEditor.Build.Reporting;

public static class PracticeRangeEditor
{
    const string ScenePath = "Assets/Scenes/WeaponTest.unity";
    static TMP_FontAsset font;
    static Color Ink = new Color32(35, 38, 37, 255), Paper = new Color32(236, 233, 221, 248);
    public static void Install()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new Exception("Save/close the current scene before authoring.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (GameObject.Find("PracticeExpansion") != null) throw new Exception("Expansion already authored; use Verify, not reinstall.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var root = new GameObject("PracticeExpansion");
        var range = root.AddComponent<PracticeRangeController>();
        range.spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        range.inventory = UnityEngine.Object.FindFirstObjectByType<PlayerItem>();
        range.player = range.inventory.transform;
        range.originalStation = Point(root.transform, "OriginalStation", range.player.position);
        range.distanceStation = Point(root.transform, "DistanceStation", new Vector3(40,1,0));
        range.rifle = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Prefab/Weapon/AR/M416/M416.asset");
        range.pistol = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Prefab/Weapon/Pistol/P92/P92.asset");
        var inv = new SerializedObject(range.inventory); inv.FindProperty("secondary").objectReferenceValue = range.pistol; inv.ApplyModifiedPropertiesWithoutUndo();
        range.spawner.distanceTargets = new Transform[3];
        for (int i=0;i<3;++i) range.spawner.distanceTargets[i] = Point(root.transform, $"Distance{(i+1)*10}m", new Vector3(40,1,(i+1)*10));
        range.spawner.mode = 1;
        range.player.position = range.distanceStation.position;
        const string floor = "Assets/FPS_Bundle/FPS_Map4/Prefabs/Buildings/Floor_01.prefab";
        for (int x=0;x<3;++x) for (int z=0;z<9;++z) Prop(floor,root.transform,new Vector3(35+x*5,-.1f,-5+z*5),Quaternion.identity);
        for (int z=0;z<9;++z) foreach(float x in new[]{32.5f,47.5f}) Prop(floor,root.transform,new Vector3(x,2.4f,-5+z*5),Quaternion.Euler(0,0,90));
        for(int x=0;x<3;++x) Prop(floor,root.transform,new Vector3(35+x*5,2.4f,37.5f),Quaternion.Euler(90,0,0));
        foreach(float x in new[]{35f,45f})
        {
            Prop("Assets/FPS_Bundle/FPS_Map0/Prefabs/Barrier_01.prefab",root.transform,new Vector3(x,0,2),Quaternion.identity);
            Prop("Assets/FPS_Bundle/FPS_Map0/Prefabs/Box_01.prefab",root.transform,new Vector3(x,.46f,-4),Quaternion.identity);
        }
        var target = range.spawner.enemyObject;
        foreach (var r in target.GetComponents<Renderer>()) r.enabled=false;
        Prop("Assets/FPS_Bundle/FPS_Map3/Prefabs/Props/ShootingTarget_01.prefab",target.transform,Vector3.zero,Quaternion.identity).transform.localPosition = new Vector3(0,-1,0);
        target.transform.position = range.spawner.distanceTargets[0].position;
        for(int i=0;i<3;++i)
        {
            var sign = new GameObject($"DistanceSign{(i+1)*10}m", typeof(TextMeshPro)); sign.transform.SetParent(root.transform);
            sign.transform.position=new Vector3(46,1.4f,(i+1)*10); sign.transform.rotation=Quaternion.Euler(0,0,0);
            var text=sign.GetComponent<TextMeshPro>();text.font=font;text.text=$"{(i+1)*10} m";text.fontSize=6;text.color=Ink;text.alignment=TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta=new Vector2(3,1);
        }
        var canvasGO = new GameObject("PracticeHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root.transform); var canvas=canvasGO.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var header=Panel(canvasGO.transform,"TrainingHeader",new Vector2(35,-28),new Vector2(1850,106),Paper);
        range.title=Label(header,"Title",new Vector2(22,-12),new Vector2(1500,30),"VECTOR  /  TRAINING",25);
        range.stats=Label(header,"Stats",new Vector2(22,-47),new Vector2(1700,54),"명중 00 / 30",20);
        var cross=Label(canvasGO.transform,"CrossHair",new Vector2(945,-522),new Vector2(30,30),"+",28);cross.alignment=TextAlignmentOptions.Center;
        var hit=Label(canvasGO.transform,"PracticeHitMarker",new Vector2(930,-510),new Vector2(60,60),"×",44);hit.alignment=TextAlignmentOptions.Center;hit.color=new Color32(184,47,42,255);hit.gameObject.SetActive(false);
        var ammo=Label(canvasGO.transform,"AmmoText",new Vector2(1420,-1000),new Vector2(450,55),"M416   30 / 30",27);
        range.menu=Panel(canvasGO.transform,"PracticeMenu",new Vector2(35,-155),new Vector2(530,865),Paper).gameObject;
        var menu=range.menu.transform;
        Label(menu,"Heading",new Vector2(26,-22),new Vector2(475,52),"사격 훈련",38);
        range.selection=Label(menu,"Selection",new Vector2(26,-86),new Vector2(475,58),"고정 거리 · 10m",22);
        Label(menu,"ModeHeading",new Vector2(26,-158),new Vector2(475,28),"훈련 방식",21);
        for(int i=0;i<3;++i) {var b=Button(menu,"Mode"+i,new Vector2(26+i*162,-195),new Vector2(150,55),new[]{"30개 표적","고정 거리","이동 표적"}[i]);UnityEventTools.AddIntPersistentListener(b.onClick,range.SetMode,i);}
        Label(menu,"DistanceHeading",new Vector2(26,-270),new Vector2(475,28),"사격 거리",21);
        for(int i=0;i<3;++i) {var b=Button(menu,"Distance"+i,new Vector2(26+i*162,-307),new Vector2(150,55),$"{(i+1)*10} m");UnityEventTools.AddIntPersistentListener(b.onClick,range.SetDistance,i);}
        AddButton(menu,"Easy",26,390,150,"쉬움",range.Easy); AddButton(menu,"Normal",188,390,150,"보통",range.Normal);AddButton(menu,"Hard",350,390,150,"어려움",range.Hard);
        AddButton(menu,"Rifle",26,465,230,"M416",range.EquipRifle); AddButton(menu,"Pistol",270,465,230,"P92",range.EquipPistol);
        range.result=Label(menu,"Result",new Vector2(26,-548),new Vector2(478,150),"난이도를 선택하면 훈련을 시작합니다.\n표적 30개 · 중간 취소는 기록하지 않습니다.\n최고 기록은 계정별로 이 기기에 저장됩니다.",21);
        AddButton(menu,"Resume",26,708,230,"계속 / 자유 사격",range.Resume);AddButton(menu,"Retry",270,708,230,"다시 시작",range.Retry);
        AddButton(menu,"Return",26,786,474,"로비로 돌아가기",range.ReturnToLobby);
        var input=range.player.gameObject.AddComponent<PracticeWeaponInput>();input.inventory=range.inventory;input.range=range;input.aimCamera=range.player.GetComponentInChildren<Camera>();input.ammo=ammo;input.hitMarker=hit;input.sound=range.player.gameObject.AddComponent<AudioSource>();input.sound.playOnAwake=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath)) EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        var lobby=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var training=lobby.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Button>(true)).Single(b=>b.name=="TrainingButton");
        var entry=training.gameObject.AddComponent<PracticeEntry>();entry.button=training;UnityEventTools.AddPersistentListener(training.onClick,entry.Enter);
        EditorSceneManager.MarkSceneDirty(lobby);EditorSceneManager.SaveScene(lobby);
        Verify();
    }
    static Transform Point(Transform root,string name,Vector3 p){var o=new GameObject(name);o.transform.SetParent(root);o.transform.position=p;return o.transform;}
    static GameObject Prop(string path,Transform root,Vector3 p,Quaternion rotation)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new Exception(path);
        var o=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);o.transform.position=p;o.transform.rotation=rotation;
        if(o.GetComponentsInChildren<Collider>().Length==0){var b=o.AddComponent<BoxCollider>();var rs=o.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);b.center=o.transform.InverseTransformPoint(bounds.center);b.size=bounds.size;}
        return o;
    }
    static RectTransform Panel(Transform p,string name,Vector2 position,Vector2 size,Color color)
    {
        var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(p,false);o.layer=5;
        var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;o.GetComponent<Image>().color=color;return r;
    }
    static TMP_Text Label(Transform p,string name,Vector2 position,Vector2 size,string value,int fontSize)
    {
        var o=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));o.transform.SetParent(p,false);o.layer=5;
        var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;
        var t=o.GetComponent<TextMeshProUGUI>();t.font=font;t.fontSize=fontSize;t.color=Ink;t.text=value;t.raycastTarget=false;return t;
    }
    static Button Button(Transform p,string name,Vector2 pos,Vector2 size,string text)
    {
        var r=Panel(p,name,pos,size,new Color32(55,58,54,255));var b=r.gameObject.AddComponent<Button>();var l=Label(r,"Label",Vector2.zero,size,text,21);l.alignment=TextAlignmentOptions.Center;l.color=Color.white;return b;
    }
    static void AddButton(Transform p,string name,float x,float y,float width,string text,UnityAction action){UnityEventTools.AddPersistentListener(Button(p,name,new Vector2(x,-y),new Vector2(width,55),text).onClick,action);}
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        if(range==null||range.spawner.distanceTargets.Length!=3||range.rifle==null||range.pistol==null)throw new Exception("Practice references missing");
        foreach(var r in scene.GetRootGameObjects())foreach(var t in r.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
        var sp=range.spawner; int completed=0;sp.Ended+=(ok)=>{if(ok)++completed;};
        sp.mode=1;sp.StartTrainingByDifficulty("Normal");sp.StartTrainingByDifficulty("invalid");if(!sp.Running)throw new Exception("Invalid difficulty aborted session");
        for(int i=0;i<30;++i){sp.Advance(1);sp.RecordShot();sp.Advance(.1f);sp.AddScore();sp.AddScore();sp.enemyObject.SetActive(false);sp.Advance(.1f);}
        if(sp.Score!=30||sp.Shots!=30||sp.Accuracy!=100||sp.Running||completed!=1)throw new Exception("Score/completion regression");
        sp.StartTrainingByDifficulty("Easy");sp.Paused=true;float elapsed=sp.Elapsed;sp.Advance(100);if(sp.Elapsed!=elapsed)throw new Exception("Pause advanced training");sp.StopTraining();if(completed!=1)throw new Exception("Cancelled run saved");
        Debug.Log("PRACTICE_VERIFY_PASS: serialized refs, 30 targets, duplicate hits, completion, invalid difficulty, pause, cancel. No account writes.");
        Capture(range);
    }
    public static void Polish()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var root=GameObject.Find("PracticeExpansion").transform;
        foreach(var renderer in range.player.GetComponents<Renderer>())renderer.enabled=false;
        // Repair only colliders authored by this installer, never bundled source prefabs.
        foreach(var box in root.GetComponentsInChildren<BoxCollider>(true))
            if(PrefabUtility.GetCorrespondingObjectFromSource(box)==null && box.GetComponent<MeshFilter>()!=null)
            {var mesh=box.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(box);var c=mesh.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mesh.sharedMesh;}
        if(root.Find("LaneMarkings")==null)
        {
            var marks=Point(root,"LaneMarkings",Vector3.zero);
            var yellow=new Material(Shader.Find("Universal Render Pipeline/Lit"));yellow.color=new Color32(232,170,43,255);
            AssetDatabase.CreateAsset(yellow,"Assets/Prefab/WeaponTest/PracticeLaneYellow.mat");
            foreach(float x in new[]{37.5f,42.5f}) Mark(marks,new Vector3(x,.012f,16),new Vector3(.07f,.025f,38),yellow);
            foreach(int z in new[]{0,10,20,30})Mark(marks,new Vector3(40,.012f,z),new Vector3(5,.025f,.10f),yellow);
        }
        var playerCam=range.player.GetComponentInChildren<Camera>();
        var urp=playerCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(urp!=null)urp.renderPostProcessing=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
    }
    static void Mark(Transform root,Vector3 p,Vector3 scale,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="LaneMark";o.transform.SetParent(root);o.transform.position=p;o.transform.localScale=scale;
        o.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    public static void Enhance()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var root=range.transform;
        if(root.Find("RangeArchitecture")!=null)throw new Exception("Enhancement already authored");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var architecture=Point(root,"RangeArchitecture",Vector3.zero);
        var concrete=RangeMaterial("Concrete",new Color32(172,163,142,255));
        var wall=RangeMaterial("Wall",new Color32(111,115,101,255));
        var dark=RangeMaterial("Steel",new Color32(55,60,54,255));
        var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefab/WeaponTest/PracticeLaneYellow.mat");
        foreach(Transform t in root) if(t.name=="Floor_01")
            foreach(var renderer in t.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=t.position.y<0 ? concrete : wall;
        foreach(float x in new[]{33f,47f})foreach(float z in new[]{-5f,5f})
            Solid(architecture,"CanopyColumn",new Vector3(x,2.2f,z),new Vector3(.3f,4.4f,.3f),dark);
        Solid(architecture,"Canopy",new Vector3(40,4.5f,-.5f),new Vector3(15,.22f,12),dark);
        foreach(float z in new[]{10f,20f,30f})
        {
            Solid(architecture,"DistanceRail",new Vector3(40,.06f,z),new Vector3(6,.10f,.18f),dark);
            foreach(float x in new[]{33f,47f})Solid(architecture,"WallRib",new Vector3(x,2.1f,z),new Vector3(.2f,4.2f,.4f),dark);
        }
        foreach(float x in new[]{34.5f,45.5f})
            Prop("Assets/FPS_Bundle/FPS_Map3/Prefabs/Props/Sand_01.prefab",architecture,new Vector3(x,0,33),Quaternion.identity);
        Prop("Assets/FPS_Bundle/FPS_Map1/Prefabs/Props/Table_01.prefab",architecture,new Vector3(43,0,-1.5f),Quaternion.Euler(0,90,0));
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)
        {light.intensity=1.6f;light.color=new Color(1,.94f,.84f);light.transform.rotation=Quaternion.Euler(48,-35,0);}
        UnityEngine.RenderSettings.ambientLight=new Color(.65f,.64f,.58f);
        // These boxes match the silhouette, rather than the original invisible capsule.
        var enemy=range.spawner.enemyObject;
        foreach(var c in enemy.GetComponentsInChildren<Collider>(true))c.enabled=false;
        Zone(enemy.transform,"Head",new Vector3(0,.66f,0),new Vector3(.24f,.24f,.25f),true,range.spawner);
        Zone(enemy.transform,"Body",new Vector3(0,.13f,0),new Vector3(.55f,.80f,.25f),false,range.spawner);
        var input=range.player.GetComponent<PracticeWeaponInput>();
        input.recoil=range.player.GetComponentInChildren<FPSCameraLook>().gameObject.AddComponent<WeaponRecoil>();
        range.modeButtons=new Button[3];range.distanceButtons=new Button[3];
        for(int i=0;i<3;++i)
        {
            range.modeButtons[i]=range.menu.transform.Find("Mode"+i).GetComponent<Button>();
            range.distanceButtons[i]=range.menu.transform.Find("Distance"+i).GetComponent<Button>();
        }
        range.modeButtons[0].GetComponentInChildren<TMP_Text>().text="반응 표적";
        range.menu.transform.Find("Resume/Label").GetComponent<TMP_Text>().text="계속하기";
        range.result.text="난이도를 눌러 30표적 훈련을 시작하거나\n오른쪽에서 자유 사격 · 60초 도전을 선택하세요.\n기록은 훈련 조건별로 이 기기에 저장됩니다.";
        var settings=Panel(range.menu.transform,"TrainingOptions",new Vector2(1415,0),new Vector2(435,590),Paper);
        Panel(settings,"Accent",Vector2.zero,new Vector2(435,6),new Color32(212,151,53,255));
        Label(settings,"Title",new Vector2(24,-25),new Vector2(390,38),"TRAINING CONTROL",26);
        range.options=Label(settings,"State",new Vector2(24,-83),new Vector2(390,110),"탄약 무제한\n판정 전신 명중\n이동 일정한 좌우 이동",22);
        AddButton(settings,"InfiniteAmmo",24,210,386,"탄약 방식 변경",range.ToggleAmmo);
        AddButton(settings,"HeadOnly",24,275,386,"헤드샷 전용 전환",range.ToggleHeadOnly);
        AddButton(settings,"Movement",24,340,386,"이동 패턴 전환",range.ToggleMovement);
        AddButton(settings,"Free",24,415,185,"자유 사격",range.FreePractice);
        AddButton(settings,"Timed",224,415,186,"60초 도전",range.TimedChallenge);
        AddButton(settings,"Thirty",24,485,386,"30표적 도전",range.TargetChallenge);
        Label(settings,"Hint",new Vector2(24,-552),new Vector2(385,25),"설정 변경 시 진행 중인 훈련이 초기화됩니다.",16);
        // Small world-space console. Its layout, colliders and references are saved here, before Play.
        var terminal=new GameObject("RangeConsole",typeof(RectTransform),typeof(Canvas));terminal.transform.SetParent(architecture);
        terminal.transform.position=new Vector3(42.7f,1.65f,0);terminal.transform.rotation=Quaternion.Euler(12,0,0);terminal.transform.localScale=Vector3.one*.004f;
        terminal.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;terminal.GetComponent<RectTransform>().sizeDelta=new Vector2(460,240);
        var screen=Panel(terminal.transform,"Screen",Vector2.zero,new Vector2(460,240),new Color32(34,40,34,255));
        range.consoleStatus=Label(screen,"Status",new Vector2(24,-25),new Vector2(420,205),"VECTOR RANGE\nTRAINING CONTROL\n[F] 훈련 설정",31);range.consoleStatus.color=new Color32(240,212,139,255);
        range.console=terminal.transform;
        Solid(architecture,"ConsoleStand",new Vector3(42.7f,.65f,0),new Vector3(.35f,1.3f,.35f),dark);
        Solid(architecture,"StationThreshold",new Vector3(40,.03f,-.5f),new Vector3(5,.04f,.16f),amber);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
    }
    static Material RangeMaterial(string name,Color color)
    {
        string path="Assets/Prefab/WeaponTest/Practice"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Solid(Transform root,string name,Vector3 position,Vector3 size,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root);o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Zone(Transform root,string name,Vector3 position,Vector3 size,bool head,EnemySpawner spawner)
    {
        var o=new GameObject(name+"HitZone",typeof(BoxCollider),typeof(PracticeTargetZone));o.transform.SetParent(root,false);o.transform.localPosition=position;
        o.GetComponent<BoxCollider>().size=size;var zone=o.GetComponent<PracticeTargetZone>();zone.spawner=spawner;zone.isHead=head;
    }
    public static void Build()
    {
        var path=Path.Combine(Path.GetTempPath(),"VectorPracticeClient","Project Vector.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),path,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Practice client build failed");Debug.Log("PRACTICE_BUILD_PASS "+path);
    }
    public static void UseGamePlayer()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var game=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var manager=UnityEngine.Object.FindFirstObjectByType<MatchManager>();
        var source=new SerializedObject(manager).FindProperty("characterPrefabs").GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
        if(source==null)throw new Exception("Game character prefab missing");
        string sourcePath=AssetDatabase.GetAssetPath(source);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var old=GameObject.Find("LocalPlayer");var oldHud=old.GetComponent<PracticeWeaponInput>();
        var character=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);character.name="PracticeGamePlayer";
        character.transform.position=range.distanceStation.position;
        var context=character.AddComponent<PracticePlayerContext>();context.range=range;
        range.player=character.transform;range.inventory=character.GetComponent<PlayerItem>();
        var hud=character.AddComponent<PracticeWeaponInput>();hud.range=range;hud.inventory=range.inventory;
        hud.aimCamera=character.GetComponent<PlayerCameraController>().PlayerCamera ?? character.GetComponentInChildren<Camera>(true);
        hud.ammo=oldHud.ammo;hud.hitMarker=oldHud.hitMarker;
        old.SetActive(false); // Preserve the original test rig and all its serialized references.
        var body=character.transform.Find("Model");if(body!=null)foreach(var r in body.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var origin=new SerializedObject(range.inventory);Debug.Log("PRACTICE_GAME_PLAYER_SOURCE "+sourcePath+" holder="+origin.FindProperty("itemHolder").objectReferenceValue);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Verify();
    }
    public static void CompactHUD()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var header=range.title.transform.parent.GetComponent<RectTransform>();
        header.anchorMin=header.anchorMax=header.pivot=new Vector2(0,1);
        header.anchoredPosition=new Vector2(24,-24);header.sizeDelta=new Vector2(345,130);
        header.GetComponent<Image>().color=new Color32(236,233,221,215);
        range.title.rectTransform.anchoredPosition=new Vector2(14,-10);range.title.rectTransform.sizeDelta=new Vector2(317,26);range.title.fontSize=20;
        range.title.text="사격 훈련 · 고정 거리";
        range.stats.rectTransform.anchoredPosition=new Vector2(14,-39);range.stats.rectTransform.sizeDelta=new Vector2(317,85);range.stats.fontSize=17;
        range.stats.text="자유 사격\n성공 00    명중률 0%\n헤드샷 0    평균 반응 0.00초\nF3 / ESC  훈련 설정";
        range.menu.SetActive(false); // Start may open settings; gameplay leaves only this compact status block.
        // The added canopy also covered the top of the view. Raise only this new range prop, not the gameplay camera.
        var canopy=range.transform.Find("RangeArchitecture/Canopy");
        if(canopy!=null)canopy.position=new Vector3(canopy.position.x,6.5f,canopy.position.z);
        var architecture=range.transform.Find("RangeArchitecture");
        if(architecture!=null)foreach(Transform t in architecture)if(t.name=="CanopyColumn")
        {t.position=new Vector3(t.position.x,3.2f,t.position.z);t.localScale=new Vector3(.3f,6.4f,.3f);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Verify();
    }
    static void Capture(PracticeRangeController range)
    {
        range.spawner.enemyObject.SetActive(true);range.spawner.enemyObject.transform.position=range.spawner.distanceTargets[0].position;
        var cam=range.player.GetComponentInChildren<Camera>();cam.transform.position=new Vector3(41,3,-5);cam.transform.LookAt(new Vector3(40,1,15));
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        var rt=new RenderTexture(1920,1080,24);cam.targetTexture=rt;var old=RenderTexture.active;RenderTexture.active=rt;Canvas.ForceUpdateCanvases();cam.Render();
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        var dir=Path.Combine(Path.GetTempPath(),"VectorPracticeVerification");Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,"practice-range.png"),image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
    // Explicit editor-only migration: all target geometry and UI are saved before Play.
    public static void ReworkTraining()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var sp=range.spawner;
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        if(range.transform.Find("CharacterPracticeTarget")==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab");
            var cc=source.GetComponent<CharacterController>();
            var target=new GameObject("CharacterPracticeTarget");target.transform.SetParent(range.transform,false);
            var model=UnityEngine.Object.Instantiate(source.transform.Find("Model").gameObject,target.transform);
            model.name="Model";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character Animation/Initiator/InGame/Long Gun/Initiator_LongGun_Idle.anim");
            clip.SampleAnimation(model,0);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.enabled=true;renderer.gameObject.layer=0;}
            // Same capsule dimensions as the gameplay prefab, partitioned into body/head for training.
            float bottom=cc.center.y-cc.height*.5f, headHeight=cc.radius*1.5f;
            Zone(target.transform,"Head",new Vector3(cc.center.x,bottom+cc.height-headHeight*.5f,cc.center.z),new Vector3(cc.radius*2,headHeight,cc.radius*2),true,sp);
            Zone(target.transform,"Body",new Vector3(cc.center.x,bottom+(cc.height-headHeight)*.5f,cc.center.z),new Vector3(cc.radius*2,cc.height-headHeight,cc.radius*2),false,sp);
            sp.enemyObject.SetActive(false);sp.enemyObject=target;
            Debug.Log($"PRACTICE_TARGET_SOURCE {AssetDatabase.GetAssetPath(source)} height={cc.height} diameter={cc.radius*2} modelScale={model.transform.localScale}");
        }
        foreach(var point in sp.distanceTargets)point.position=new Vector3(point.position.x,.04f,point.position.z);
        sp.enemyObject.transform.position=sp.distanceTargets[0].position;sp.enemyObject.SetActive(false);
        sp.trackingCamera=range.player.GetComponentInChildren<Camera>(true);
        if(range.peekCover==null)
        {
            var cover=new GameObject("PeekTrainingCover");cover.transform.SetParent(range.transform,false);range.peekCover=cover;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefab/WeaponTest/PracticeSteel.mat");
            Solid(cover.transform,"CoverWall",new Vector3(40,1.2f,3),new Vector3(2.8f,2.4f,.35f),material);
            var yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefab/WeaponTest/PracticeLaneYellow.mat");
            foreach(float x in new[]{38.57f,41.43f})Solid(cover.transform,"CoverEdge",new Vector3(x,1.2f,2.80f),new Vector3(.06f,2.4f,.05f),yellow);
            cover.SetActive(false);
        }
        var header=range.title.transform.parent.GetComponent<RectTransform>();
        header.GetComponent<Image>().color=new Color32(27,29,27,205);
        range.title.color=new Color32(244,205,114,255);range.stats.color=new Color32(240,239,232,255);
        var menu=range.menu.transform;
        Place(menu,20,174,450,886);menu.GetComponent<Image>().color=new Color32(233,231,220,248);
        Place(menu.Find("Heading"),20,16,410,32);menu.Find("Heading").GetComponent<TMP_Text>().fontSize=26;
        Place(range.selection.transform,20,58,410,60);range.selection.fontSize=18;
        Place(menu.Find("ModeHeading"),20,125,410,25);
        var modeNames=new[]{"반응 사격","고정 사격","이동 사격","지속 추적","엄폐 사격"};
        range.modeButtons=new Button[5];
        for(int i=0;i<5;i++)
        {
            var child=menu.Find("Mode"+i);
            var button=child!=null?child.GetComponent<Button>():Button(menu,"Mode"+i,Vector2.zero,Vector2.one,modeNames[i]);
            if(child==null)UnityEventTools.AddIntPersistentListener(button.onClick,range.SetMode,i);
            range.modeButtons[i]=button;
            FitButton(button.transform,20+(i%3)*140,157+(i/3)*46,130,38,modeNames[i]);
        }
        Place(menu.Find("DistanceHeading"),20,256,410,25);
        for(int i=0;i<3;i++)FitButton(menu.Find("Distance"+i),20+i*140,290,130,38,$"{(i+1)*10} m");
        foreach(var pair in new[]{("Easy",0,"쉬움"),("Normal",1,"보통"),("Hard",2,"어려움")})FitButton(menu.Find(pair.Item1),20+pair.Item2*140,342,130,38,pair.Item3);
        FitButton(menu.Find("Rifle"),20,394,200,38,"M416  /  소총");FitButton(menu.Find("Pistol"),230,394,200,38,"P92  /  권총");
        var settings=menu.Find("TrainingOptions");Place(settings,20,449,410,173);settings.GetComponent<Image>().color=new Color32(215,214,201,255);
        Place(settings.Find("Accent"),0,0,410,2);
        Place(settings.Find("Title"),12,12,386,22);settings.Find("Title").GetComponent<TMP_Text>().text="훈련 조건";settings.Find("Title").GetComponent<TMP_Text>().fontSize=18;
        Place(range.options.transform,12,45,250,117);range.options.fontSize=16;range.options.lineSpacing=20;
        FitButton(settings.Find("InfiniteAmmo"),272,40,126,32,"탄약 변경");
        FitButton(settings.Find("HeadOnly"),272,82,126,32,"명중 조건");
        FitButton(settings.Find("Movement"),272,124,126,32,"이동 패턴");
        settings.Find("Hint").SetParent(menu,false);Place(menu.Find("Hint"),20,630,410,24);menu.Find("Hint").GetComponent<TMP_Text>().fontSize=14;
        string[] sessions={"Free","Timed","Thirty"},labels={"자유 사격","60초 도전","30표적 도전"};
        for(int i=0;i<3;i++){var button=settings.Find(sessions[i]);button.SetParent(menu,false);FitButton(button,20+i*140,664,130,40,labels[i]);}
        Place(range.result.transform,20,718,410,73);range.result.fontSize=16;
        FitButton(menu.Find("Resume"),20,798,200,36,"설정 닫기");FitButton(menu.Find("Retry"),230,798,200,36,"훈련 시작 / 재시작");
        FitButton(menu.Find("Return"),20,844,410,28,"로비로 돌아가기");
        var ammo=range.player.GetComponent<PracticeWeaponInput>().ammo;ammo.color=Color.white;
        Place(ammo.transform,1540,1010,350,42);ammo.fontSize=25;
        range.menu.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
    }
    static void Place(Transform t,float x,float y,float width,float height)
    {
        var r=t.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);
    }
    static void FitButton(Transform t,float x,float y,float width,float height,string label)
    {
        Place(t,x,y,width,height);var text=t.GetComponentInChildren<TMP_Text>();Place(text.transform,0,0,width,height);text.text=label;text.fontSize=18;
    }
    public static void FinishTrainingLayout()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        var scene=EditorSceneManager.OpenScene(ScenePath);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
        range.title.transform.parent.GetComponent<Image>().color=new Color32(26,27,25,245);range.title.color=new Color32(245,245,240,255);
        range.menu.transform.Find("TrainingOptions/Accent").GetComponent<Image>().color=new Color32(174,35,61,255);
        foreach(string name in new[]{"CrossHair","PracticeHitMarker"})
        {
            var r=canvas.transform.Find(name).GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;
        }
        var regular=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset");
        foreach(var t in new[]{range.stats,range.selection,range.options,range.result}){t.font=regular;t.fontStyle=FontStyles.Normal;}
        range.stats.fontSize=17;range.options.lineSpacing=12;
        var ammo=range.player.GetComponent<PracticeWeaponInput>().ammo.rectTransform;
        ammo.anchorMin=ammo.anchorMax=ammo.pivot=new Vector2(1,0);ammo.anchoredPosition=new Vector2(-30,24);
        // Face the firing station without altering the source model scale or its authored pose.
        var model=range.spawner.enemyObject.transform.Find("Model");
        model.localRotation=Quaternion.Euler(0,180,0);
        var face=model.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="EYE_DEF");
        var back=model.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="head_back");
        Vector3 Center(Renderer renderer)
        {
            if(!(renderer is SkinnedMeshRenderer skinned))return renderer.bounds.center;
            var mesh=new Mesh();skinned.BakeMesh(mesh);var p=renderer.transform.TransformPoint(mesh.bounds.center);UnityEngine.Object.DestroyImmediate(mesh);return p;
        }
        var facing=Center(face)-Center(back);facing.y=0;
        // This imported model's face/back renderer centers point opposite its visual forward axis.
        if(facing.sqrMagnitude>.00001f)model.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(facing.normalized,Vector3.forward,Vector3.up),Vector3.up)*model.rotation;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("PRACTICE_LAYOUT_PASS: height scaling, centered crosshair, bottom-right ammo, original-scale target faces station.");
    }
    public static void Inspect()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab");
        var sample=UnityEngine.Object.Instantiate(source);
        foreach(var t in sample.GetComponentsInChildren<Transform>(true))
            if(t.parent==sample.transform || t.GetComponent<Renderer>()!=null || t.GetComponent<Collider>()!=null)
                Debug.Log($"TARGET_INSPECT {t.name} pos={t.position} scale={t.lossyScale} renderer={(t.GetComponent<Renderer>() != null ? t.GetComponent<Renderer>().bounds.ToString() : "none")} collider={(t.GetComponent<Collider>() != null ? t.GetComponent<Collider>().GetType().Name : "none")}");
        UnityEngine.Object.DestroyImmediate(sample);
        EditorSceneManager.OpenScene("Assets/Scenes/WeaponTest.unity");
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.zero);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Debug.Log($"PRACTICE_ROOT {root.name} pos={root.transform.position} bounds={bounds} scripts={string.Join(",",root.GetComponents<MonoBehaviour>().Select(x=>x?x.GetType().Name:"MISSING"))}");
        }
        foreach (var path in new[] {"Assets/FPS_Bundle/FPS_Map3/Prefabs/Props/ShootingTarget_01.prefab", "Assets/FPS_Bundle/FPS_Map0/Prefabs/Barrier_01.prefab", "Assets/FPS_Bundle/FPS_Map0/Prefabs/Box_01.prefab", "Assets/FPS_Bundle/FPS_Map4/Prefabs/Buildings/Floor_01.prefab"})
        {
            var obj = PrefabUtility.LoadPrefabContents(path);
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Debug.Log($"PRACTICE_ASSET {path} bounds={bounds}");
            PrefabUtility.UnloadPrefabContents(obj);
        }
    }
    public static void InspectGameCharacterSizes()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var manager=UnityEngine.Object.FindFirstObjectByType<MatchManager>();
        var prefabs=new SerializedObject(manager).FindProperty("characterPrefabs");
        for(int i=0;i<prefabs.arraySize;i++)
        {
            var source=(GameObject)prefabs.GetArrayElementAtIndex(i).objectReferenceValue;
            var instance=UnityEngine.Object.Instantiate(source);var model=instance.transform.Find("Model");var cc=instance.GetComponent<CharacterController>();
            Debug.Log($"GAME_SIZE {i} source={AssetDatabase.GetAssetPath(source)} rootScale={source.transform.localScale:F5} modelScale={model.localScale:F5} modelPos={model.localPosition:F5} controllerHeight={cc.height} radius={cc.radius} center={cc.center:F5} skin={cc.skinWidth}");
            foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.parent==model || (t.localScale-Vector3.one).sqrMagnitude>.001f)Debug.Log($"GAME_SIZE_NODE {i} {AnimationUtility.CalculateTransformPath(t,model)} scale={t.localScale:F5}");
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))Debug.Log($"GAME_ANIM {i} {AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)}");
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
    public static void SyncPracticeCharacterSizes()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var manager=UnityEngine.Object.FindFirstObjectByType<MatchManager>();
        var refs=new SerializedObject(manager).FindProperty("characterPrefabs");
        var sources=Enumerable.Range(0,refs.arraySize).Select(i=>(GameObject)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var scene=EditorSceneManager.OpenScene(ScenePath);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();var sp=range.spawner;
        if(range.transform.Find("GameSizedTargets")!=null)throw new Exception("Targets already authored; verify without duplicating them.");
        var group=Point(range.transform,"GameSizedTargets",Vector3.zero);
        sp.enemyObject.SetActive(false); // Keep the old authored target intact, but no longer use its sampled pose/boxes.
        sp.characterTargets=new PracticeTargetProfile[sources.Length];
        for(int i=0;i<sources.Length;i++)
        {
            var source=sources[i];var cc=source.GetComponent<CharacterController>();
            var root=new GameObject($"GameTarget_{i}_{source.name}");root.transform.SetParent(group,false);
            root.transform.localScale=source.transform.localScale;root.transform.rotation=Quaternion.Euler(0,180,0);
            var sourceModel=source.transform.Find("Model");
            var model=UnityEngine.Object.Instantiate(sourceModel.gameObject,root.transform,false);model.name="Model";
            model.transform.localPosition=sourceModel.localPosition;model.transform.localRotation=sourceModel.localRotation;model.transform.localScale=sourceModel.localScale;
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.enabled=true;renderer.gameObject.layer=0;}
            var profile=root.AddComponent<PracticeTargetProfile>();profile.gameplayPrefab=source;profile.model=model.transform;profile.animator=model.GetComponent<Animator>();
            if(profile.animator!=null){profile.animator.enabled=true;profile.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;profile.animator.applyRootMotion=false;}
            float bottom=cc.center.y-cc.height*.5f,headHeight=cc.radius*1.5f;
            Zone(root.transform,"Head",new Vector3(cc.center.x,bottom+cc.height-headHeight*.5f,cc.center.z),new Vector3(cc.radius*2,headHeight,cc.radius*2),true,sp);
            Zone(root.transform,"Body",new Vector3(cc.center.x,bottom+(cc.height-headHeight)*.5f,cc.center.z),new Vector3(cc.radius*2,cc.height-headHeight,cc.radius*2),false,sp);
            profile.groundedRootOffset=(cc.skinWidth-bottom)*root.transform.lossyScale.y;
            profile.worldCapsuleHeight=cc.height*root.transform.lossyScale.y;
            sp.characterTargets[i]=profile;root.SetActive(false);
        }
        // Distance rails are painted guides, not steps underneath a target's feet.
        foreach(Transform t in range.transform.Find("RangeArchitecture"))if(t.name=="DistanceRail")foreach(var c in t.GetComponents<Collider>())c.enabled=false;
        Physics.SyncTransforms();
        foreach(var point in sp.distanceTargets)
        {
            if(!Physics.Raycast(new Vector3(point.position.x,3,point.position.z),Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore))throw new Exception("Target ground missing");
            point.position=new Vector3(point.position.x,hit.point.y,point.position.z);
        }
        var playerCC=range.player.GetComponent<CharacterController>();var station=range.distanceStation;
        if(!Physics.Raycast(new Vector3(station.position.x,3,station.position.z),Vector3.down,out var floor,5,~0,QueryTriggerInteraction.Ignore))throw new Exception("Station ground missing");
        // Temporarily exclude the player's own collider from the station ground query.
        bool enabled=playerCC.enabled;playerCC.enabled=false;Physics.SyncTransforms();
        if(!Physics.Raycast(new Vector3(station.position.x,3,station.position.z),Vector3.down,out floor,5,~0,QueryTriggerInteraction.Ignore))throw new Exception("Station floor missing");
        station.position=new Vector3(station.position.x,floor.point.y+(playerCC.skinWidth-(playerCC.center.y-playerCC.height*.5f))*range.player.lossyScale.y,station.position.z);
        range.player.position=station.position;playerCC.enabled=enabled;
        sp.targetCharacterIndex=0;sp.enemyObject=sp.characterTargets[0].gameObject;
        foreach(var target in sp.characterTargets)target.transform.position=sp.distanceTargets[0].position+Vector3.up*target.groundedRootOffset;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        VerifyCharacterSizes();
    }
    public static void VerifyCharacterSizes()
    {
        EditorSceneManager.OpenScene(ScenePath);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        foreach(var profile in range.spawner.characterTargets)
        {
            var source=UnityEngine.Object.Instantiate(profile.gameplayPrefab);source.transform.position=profile.transform.position;source.transform.rotation=profile.transform.rotation;
            var sourceModel=source.transform.Find("Model");
            if((source.transform.lossyScale-profile.transform.lossyScale).sqrMagnitude>1e-8f)throw new Exception("Root scale mismatch");
            foreach(var t in sourceModel.GetComponentsInChildren<Transform>(true))
            {
                string path=AnimationUtility.CalculateTransformPath(t,sourceModel);var other=string.IsNullOrEmpty(path)?profile.model:profile.model.Find(path);
                if(other==null || (t.localScale-other.localScale).sqrMagnitude>1e-8f)throw new Exception("Nested scale mismatch: "+path);
            }
            var animator=sourceModel.GetComponent<Animator>();
            var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.Contains("LongGun_Idle")) ?? animator.runtimeAnimatorController.animationClips.First();
            clip.SampleAnimation(sourceModel.gameObject,0);clip.SampleAnimation(profile.model.gameObject,0);
            var a=ModelBounds(sourceModel);var b=ModelBounds(profile.model);
            if(Mathf.Abs(a.size.y-b.size.y)>.0001f || Mathf.Abs(a.min.y-b.min.y)>.0001f)throw new Exception("Same-pose size/feet mismatch "+profile.name);
            Debug.Log($"CHARACTER_SIZE_PASS {profile.name} sourceHeight={a.size.y:F5} targetHeight={b.size.y:F5} sourceFeet={a.min.y:F5} targetFeet={b.min.y:F5} capsule={profile.worldCapsuleHeight:F3} rootScale={profile.transform.lossyScale:F3} modelScale={profile.model.lossyScale:F3}");
            UnityEngine.Object.DestroyImmediate(source);
        }
        // Comparison posed the in-memory models only. Never save verification changes.
        Debug.Log("CHARACTER_SIZES_PASS: all gameplay prefab scales, nested transforms and same-pose world-space bounds match. No gameplay assets changed.");
    }
    static Bounds ModelBounds(Transform model)
    {
        bool found=false;var result=new Bounds();
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var b=r.bounds;
            if(r is SkinnedMeshRenderer skinned)
            {
                var mesh=new Mesh();skinned.BakeMesh(mesh,true);var vertices=mesh.vertices;
                if(vertices.Length>0){b=new Bounds(r.transform.TransformPoint(vertices[0]),Vector3.zero);foreach(var v in vertices)b.Encapsulate(r.transform.TransformPoint(v));}
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            if(!found){result=b;found=true;}else result.Encapsulate(b);
        }
        return result;
    }
}
#endif
