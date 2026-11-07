#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PracticeUpgradeEditor
{
    const string Scene="Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity";
    static TMP_FontAsset font;
    static readonly Color Paper=new Color32(229,227,213,249),Ink=new Color32(43,49,44,255),Dark=new Color32(58,63,56,255);
    public static void Author()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene or Play mode");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var manager=UnityEngine.Object.FindFirstObjectByType<MatchManager>();
        var refs=new SerializedObject(manager).FindProperty("characterPrefabs");
        var prefabs=Enumerable.Range(0,refs.arraySize).Select(i=>(GameObject)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var pick=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include)).FindProperty("characters");
        var portraits=new Sprite[4];var names=new string[4];
        for(int i=0;i<pick.arraySize;i++)
        {
            var entry=pick.GetArrayElementAtIndex(i);int id=entry.FindPropertyRelative("characterId").intValue;
            if(id>=4)continue; portraits[id]=entry.FindPropertyRelative("characterImage").objectReferenceValue as Sprite;
            names[id]=entry.FindPropertyRelative("characterName").stringValue;
        }
        var scene=EditorSceneManager.OpenScene(Scene);
        var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        if(range.loadout!=null)throw new Exception("Upgrade already authored; use Verify");
        File.Copy(Scene,Path.Combine(Path.GetTempPath(),"WeaponTest-before-loadout.unity"),true);
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var loadout=range.gameObject.AddComponent<PracticeLoadout>();range.loadout=loadout;loadout.range=range;
        loadout.characterNames=names;
        var legacyInput=range.player.GetComponent<PracticeWeaponInput>();
        loadout.players=new PracticePlayerContext[4];
        for(int i=0;i<4;i++)
        {
            var go=i==0?range.player.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);
            if(i!=0)go.name="PracticeGamePlayer_"+i;
            var context=go.GetComponent<PracticePlayerContext>() ?? go.AddComponent<PracticePlayerContext>();context.range=range;
            loadout.players[i]=context;
            foreach(var skill in go.GetComponentsInChildren<CharacterSkill>(true))skill.enabled=false;
            var spike=go.GetComponent<PlayerSpike>();if(spike!=null)spike.enabled=false;
            var input=go.GetComponent<PracticeWeaponInput>() ?? go.AddComponent<PracticeWeaponInput>();
            input.inventory=go.GetComponent<PlayerItem>();input.range=range;input.aimCamera=go.GetComponentInChildren<Camera>(true);
            input.ammo=legacyInput.ammo;input.hitMarker=legacyInput.hitMarker;
            go.transform.position=range.distanceStation.position;
            if(i!=0)go.SetActive(false);
        }
        loadout.weapons=AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/ProjectVector/Shared/Weapons/Assets"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(w=>w!=null && w.weaponPrefab!=null).OrderBy(w=>w.weaponId).ToArray();
        if(loadout.weapons.Length!=14 || loadout.weapons.Select(w=>w.weaponId).Distinct().Count()!=14)throw new Exception("Expected 14 canonical weapons");
        // Snapshot the actual authoritative damage table into this training-only component.
        string server=Path.GetFullPath("../ProjectVectorServer/ProjectVectorServer/ProjectVectorServer/ServerWeaponData.cpp");
        string source=File.ReadAllText(server);
        var damage=Regex.Matches(source,@"ServerWeaponData\s*\{\s*static_cast<int>\(ServerWeaponId::\w+\),\s*ServerWeaponType::\w+,\s*(\d+)");
        if(damage.Count!=14)throw new Exception("Server weapon table layout changed; inspect before authoring");
        loadout.serverDamage=damage.Cast<Match>().Select(m=>int.Parse(m.Groups[1].Value)).ToArray();
        for(int i=0;i<14;i++)if(loadout.weapons[i].weaponId!=i)throw new Exception("Nonsequential canonical weapon IDs");
        range.spawner.realisticHealth=true;range.spawner.targetArmor=50;range.spawner.sessionKind=EnemySpawner.SessionKind.Free;
        foreach(var target in range.spawner.characterTargets)
        {
            var body=target.GetComponentsInChildren<PracticeTargetZone>(true).Single(z=>!z.isHead);
            var box=body.GetComponent<BoxCollider>();
            var legs=new GameObject("LegsHitZone",typeof(BoxCollider),typeof(PracticeTargetZone));legs.transform.SetParent(target.transform,false);
            float height=box.size.y, bottom=body.transform.localPosition.y-height*.5f;
            legs.transform.localPosition=new Vector3(body.transform.localPosition.x,bottom+height*.225f,body.transform.localPosition.z);
            legs.GetComponent<BoxCollider>().size=new Vector3(box.size.x,height*.45f,box.size.z);
            var zone=legs.GetComponent<PracticeTargetZone>();zone.isLegs=true;zone.spawner=range.spawner;
            body.transform.localPosition+=Vector3.up*height*.225f;box.size=new Vector3(box.size.x,height*.55f,box.size.z);
        }
        var panel=Panel(range.menu.transform,"LoadoutAndPracticeSettings",new Vector2(470,0),new Vector2(670,886),Paper);
        loadout.panel=panel.gameObject;
        Panel(panel,"Accent",Vector2.zero,new Vector2(670,5),new Color32(178,128,43,255));
        Label(panel,"Title",new Vector2(22,-20),new Vector2(628,35),"훈련 준비  /  CHARACTER & ARMORY",26);
        loadout.characterButtons=new Button[4];
        for(int i=0;i<4;i++)
        {
            var b=Button(panel,"Character"+i,new Vector2(22+i*158,-68),new Vector2(152,132),"");
            var icon=Panel(b.transform,"Portrait",new Vector2(22,-5),new Vector2(108,92),Color.white).GetComponent<Image>();
            icon.sprite=portraits[i];icon.preserveAspect=true;icon.raycastTarget=false;
            Label(b.transform,"Name",new Vector2(5,-101),new Vector2(142,25),names[i],19).color=Color.white;
            UnityEventTools.AddIntPersistentListener(b.onClick,loadout.SelectCharacter,i);loadout.characterButtons[i]=b;
        }
        loadout.state=Label(panel,"SelectionState",new Vector2(22,-214),new Vector2(628,58),"캐릭터를 선택하세요",20);
        Label(panel,"WeaponsTitle",new Vector2(22,-273),new Vector2(628,26),"무기 선택  ·  1 주무기 / 2 보조무기 / 3 근접무기",18);
        loadout.weaponButtons=new Button[loadout.weapons.Length];
        for(int i=0;i<loadout.weapons.Length;i++)
        {
            var weapon=loadout.weapons[i];
            var b=Button(panel,"Weapon"+weapon.weaponId,new Vector2(22+i%3*211,-308-i/3*43),new Vector2(204,37),weapon.name);
            UnityEventTools.AddIntPersistentListener(b.onClick,loadout.EquipWeapon,i);loadout.weaponButtons[i]=b;
        }
        loadout.targetState=Label(panel,"TargetSettings",new Vector2(22,-538),new Vector2(628,56),"체력 100 / 보호막 50",18);
        ActionButton(panel,"Health",22,602,204,"체력 / 한 발 명중",loadout.ToggleHealth);
        ActionButton(panel,"Armor",233,602,204,"보호막 0 / 50 / 100",loadout.CycleArmor);
        ActionButton(panel,"Target",444,602,204,"표적 캐릭터 변경",loadout.CycleTarget);
        ActionButton(panel,"Speed",22,648,204,"이동 속도 변경",loadout.CycleSpeed);
        ActionButton(panel,"Duration",233,648,204,"30 / 60 / 120초",loadout.CycleDuration);
        ActionButton(panel,"AutoReload",444,648,204,"자동 재장전 전환",loadout.ToggleAutoReload);
        ActionButton(panel,"Reset",22,702,204,"표적 초기화  F4",loadout.ResetTargets);
        ActionButton(panel,"Refill",233,702,204,"탄약 보충  F5",loadout.Refill);
        ActionButton(panel,"Station",444,702,204,"사격 위치 복귀",loadout.ReturnToLine);
        var confirm=ActionButton(panel,"Confirm",22,766,626,"선택 완료 · 훈련 시작",loadout.Confirm);confirm.image.color=new Color32(161,105,36,255);
        Label(panel,"Hint",new Vector2(22,-827),new Vector2(628,40),"설정 변경 시 현재 도전은 종료됩니다. 스킬은 이 훈련에서 사용하지 않습니다.",17);
        var hud=range.menu.transform.parent;
        loadout.feedback=Label(hud,"TargetDamageFeedback",new Vector2(24,-162),new Vector2(370,88),"",17);
        loadout.feedback.color=new Color32(245,238,207,255);
        range.menu.SetActive(true);
        AddDetails(range);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Verify();
    }
    static RectTransform Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;
        go.GetComponent<Image>().color=color;return r;
    }
    static TMP_Text Label(Transform parent,string name,Vector2 pos,Vector2 size,string text,int fs)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;
        var t=go.GetComponent<TMP_Text>();t.font=font;t.text=text;t.fontSize=fs;t.color=Ink;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMin=fs-2;t.fontSizeMax=fs;return t;
    }
    static Button Button(Transform parent,string name,Vector2 pos,Vector2 size,string text)
    {
        var r=Panel(parent,name,pos,size,Dark);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var t=Label(r,"Label",new Vector2(6,-4),size-new Vector2(12,8),text,19);t.alignment=TextAlignmentOptions.Center;t.color=Color.white;
        return b;
    }
    static Button ActionButton(Transform parent,string name,float x,float y,float w,string label,UnityEngine.Events.UnityAction action)
    {var b=Button(parent,name,new Vector2(x,-y),new Vector2(w,40),label);UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
    static void AddDetails(PracticeRangeController range)
    {
        var campus=GameObject.Find("HydroTrainingCampus").transform;
        var root=new GameObject("AuthoredArmoryAndWayfinding").transform;root.SetParent(campus,false);
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectVector/Modules/Practice/Assets/FinishCharcoal.mat");
        var ochre=AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectVector/Modules/Practice/Assets/CampusSafetyOchre.mat");
        for(int i=0;i<4;i++)
        {
            float x=200+i*32;
            Prop(root,"Assets/ThirdParty/FPS_Bundle/FPS_Map4/Prefabs/Props/Bench_01.prefab",new Vector3(x-10,0,-3),90);
            Prop(root,"Assets/ThirdParty/FPS_Bundle/FPS_Map3/Prefabs/Props/Box_01.prefab",new Vector3(x-10,0,-.8f),0);
            for(int n=0;n<5;n++) Box(root,"PromenadeSafetyStripe",new Vector3(x-2+n,.014f,-5.7f),new Vector3(.6f,.015f,.22f),ochre);
            var board=Box(root,"BayDirectory",new Vector3(x+9,1.55f,-2.4f),new Vector3(2.4f,1.25f,.10f),steel);
            var sign=new GameObject("BayInstructions",typeof(TextMeshPro));sign.transform.SetParent(board.transform,false);
            sign.transform.localScale=new Vector3(1/2.4f,1/1.25f,10);sign.transform.localPosition=new Vector3(0,0,-.6f);
            var t=sign.GetComponent<TextMeshPro>();t.font=font;t.fontSize=1.7f;t.color=new Color32(243,225,166,255);t.alignment=TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta=new Vector2(2.15f,1.0f);
            t.text=$"0{i+1}  {range.trainingZones[i].displayName}\nF  훈련 설정\nF4  표적 초기화   /   F5  탄약 보충";
        }
        // Saved weapon display: mesh-only copies, no gameplay scripts, colliders or new runtime UI.
        Box(root,"ArmoryDisplay",new Vector3(190,1.65f,-7),new Vector3(.2f,2.8f,5),steel);
        for(int i=0;i<range.loadout.weapons.Length;i++)
        {
            if(range.loadout.weapons[i].dropPrefab==null)continue;
            var visual=UnityEngine.Object.Instantiate(range.loadout.weapons[i].dropPrefab,root);
            if(visual==null)continue;visual.name="Display_"+range.loadout.weapons[i].name;
            foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var rb in visual.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
            foreach(var c in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var renderers=visual.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(longest>0)visual.transform.localScale*=.55f/longest;
            visual.transform.position=new Vector3(189.8f,2.6f-(i/7)*.8f,-9+(i%7)*.65f);visual.transform.rotation=Quaternion.Euler(0,90,0);
        }
    }
    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material mat)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
    static void Prop(Transform parent,string path,Vector3 pos,float yaw)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new Exception(path);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);obj.transform.position=pos;obj.transform.rotation=Quaternion.Euler(0,yaw,0);
    }
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene(Scene);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var l=range.loadout;if(l==null || l.players.Length!=4 || l.weapons.Length!=14)throw new Exception("Loadout missing");
        foreach(var rig in l.players)
        {
            if(rig.GetComponentsInChildren<CharacterSkill>(true).Any(s=>s.enabled))throw new Exception("Skill enabled in training");
            if(rig.transform.localScale!=PrefabUtility.GetCorrespondingObjectFromSource(rig.gameObject).transform.localScale)throw new Exception("Player scale changed");
        }
        foreach(var target in range.spawner.characterTargets)if(target.GetComponentsInChildren<PracticeTargetZone>(true).Length!=3)throw new Exception("Target regions missing");
        foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new Exception("Missing script "+t.name);
        foreach(var b in l.panel.GetComponentsInChildren<Button>(true))if(b.onClick.GetPersistentEventCount()==0)throw new Exception("Unwired button "+b.name);
        PracticeCampusEditor.Validate();
        Debug.Log("PRACTICE_UPGRADE_AUTHOR_PASS: saved 4 exact-scale characters, 14 weapons, disabled skills, 3 target regions, authored controls and campus detail");
    }
    public static void Refine()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
        var scene=EditorSceneManager.OpenScene(Scene);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var loadout=range.loadout;font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        loadout.timedLabel=range.menu.transform.Find("Timed/Label").GetComponent<TMP_Text>();
        range.result.text="설정 후 훈련 시작을 눌러주세요.\n탄약 보충·무기 변경을 사용한 도전은\n최고 기록에 반영되지 않습니다.";
        for(int i=0;i<loadout.weapons.Length;i++)
        {
            var b=loadout.weaponButtons[i];
            var existing=b.transform.Find("WeaponIcon");
            var icon=existing!=null?existing.GetComponent<Image>():Panel(b.transform,"WeaponIcon",new Vector2(8,-4),new Vector2(44,29),Color.white).GetComponent<Image>();
            icon.sprite=loadout.weapons[i].weaponIcon;icon.preserveAspect=true;icon.raycastTarget=false;icon.enabled=icon.sprite!=null;
            var label=b.transform.Find("Label").GetComponent<RectTransform>();label.anchoredPosition=new Vector2(56,-4);label.sizeDelta=new Vector2(142,29);
        }
        loadout.feedback.color=Color.white;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
    }
    public static void FinishArmory()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
        var scene=EditorSceneManager.OpenScene(Scene);var range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();
        var root=GameObject.Find("HydroTrainingCampus").transform.Find("AuthoredArmoryAndWayfinding");
        for(int i=0;i<range.loadout.weapons.Length;i++)
        {
            var visual=root.Find("Display_"+range.loadout.weapons[i].name);if(visual==null)continue;
            var renderers=visual.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            visual.position+=new Vector3(190.3f,2.4f-(i/7)*.85f,-8.9f+(i%7)*.63f)-bounds.center;
        }
        if(root.Find("ArmoryTitle")==null)
        {
            var ochre=AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectVector/Modules/Practice/Assets/CampusSafetyOchre.mat");
            Box(root,"ArmoryTrim",new Vector3(190,3.02f,-7),new Vector3(.23f,.08f,5.05f),ochre);
            foreach(float z in new[]{-9f,-5f})Box(root,"ArmoryFoot",new Vector3(190,.18f,z),new Vector3(.6f,.36f,.25f),ochre);
            var go=new GameObject("ArmoryTitle",typeof(TextMeshPro));go.transform.SetParent(root,false);
            go.transform.position=new Vector3(190.12f,2.8f,-7);go.transform.rotation=Quaternion.Euler(0,-90,0);
            var t=go.GetComponent<TextMeshPro>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
            t.text="ARMORY  /  14 WEAPONS";t.fontSize=2.4f;t.color=new Color32(242,217,164,255);t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(4.5f,.38f);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();PracticeCampusPolish.Capture();
    }
}
#endif
