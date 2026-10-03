using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit, offline authoring. Never executes during Play or against dirty scenes.
public static class VectorVictorySequenceEditor
{
    const string Folder = "Assets/UI/ResultSequence";
    static readonly Color red = new Color32(186, 24, 55, 255);
    static TMP_FontAsset font;
    static T Ensure<T>(GameObject go) where T:Component { var c=go.GetComponent<T>(); return c==null?go.AddComponent<T>():c; }
    static T Ref<T>(Object o,string p) where T:Object => new SerializedObject(o).FindProperty(p).objectReferenceValue as T;
    static Transform Find(Transform p,string n) => p.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
    static RectTransform Rect(Transform p,string n,float x,float y,float w,float h)
    {
        var t=p.Find(n) as RectTransform;
        if(t==null) { t=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>(); t.SetParent(p,false); }
        Pose(t,x,y,w,h); t.gameObject.layer=5; return t;
    }
    static void Pose(Transform t,float x,float y,float w,float h)
    {
        var r=(RectTransform)t; r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); r.localScale=Vector3.one;
    }
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c)
    { var image=Ensure<Image>(Rect(p,n,x,y,w,h).gameObject); image.color=c; image.raycastTarget=false; return image; }
    static TMP_Text Text(Transform p,string n,string text,float x,float y,float w,float h,float size)
    {
        var t=Ensure<TextMeshProUGUI>(Rect(p,n,x,y,w,h).gameObject); t.font=font; t.text=text; t.fontSize=size;
        t.color=Color.white; t.richText=false; t.raycastTarget=false; t.alignment=TextAlignmentOptions.MidlineLeft;
        t.overflowMode=TextOverflowModes.Ellipsis; return t;
    }
    static Button Button(Transform p,string n,string label,float x,float y,float w)
    {
        var image=Box(p,n,x,y,w,60,red); image.raycastTarget=true;
        var b=Ensure<Button>(image.gameObject); b.targetGraphic=image;
        Text(p.Find(n),"Label",label,0,0,w-30,60,26).alignment=TextAlignmentOptions.Center; return b;
    }
    static GameObject Child(Transform p,string name)
    { var t=p.Find(name); if(t!=null) return t.gameObject; var g=new GameObject(name); g.transform.SetParent(p,false); return g; }
    static CanvasGroup Group(Transform p,string name) => Ensure<CanvasGroup>(Rect(p,name,0,0,1920,1080).gameObject);
    static RawImage View(Transform p,string n,RenderTexture texture)
    { var r=Ensure<RawImage>(Rect(p,n,0,0,1920,1080).gameObject); r.texture=texture; r.raycastTarget=false; return r; }

    [MenuItem("Project Vector/UI/Install Video 2 Victory Sequence")]
    public static void InstallAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused");
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/UI","ResultSequence");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
        var preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var result=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResultUI>(true)).Single();
        var canvas=result.GetComponent<Canvas>();
        var legacy=result.GetComponentInChildren<MatchResultPresentation>(true);
        legacy.enabled=false; legacy.gameObject.SetActive(false); // Only the rejected, newly authored draft.
        var root=Rect(canvas.transform,"VictorySequence",0,0,1920,1080);
        var ui=Ensure<ResultVictorySequence>(root.gameObject);
        ui.identityBinder=legacy;
        ui.characterStage=AuthorStage(scene);
        var texture=ui.characterStage.stageCamera.targetTexture;
        var underlay=View(canvas.transform,"ResultStageBackground",texture); underlay.transform.SetAsFirstSibling();
        var defocus=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/TableDefocus.mat");
        if(defocus==null) { defocus=new Material(Shader.Find("UI/ResultDefocus")); AssetDatabase.CreateAsset(defocus,Folder+"/TableDefocus.mat"); }
        underlay.material=defocus;
        // Preserve the original table and all of its buttons; only make its background translucent.
        foreach(var image in canvas.GetComponentsInChildren<Image>(true))
            if(image.name=="BackgroundImage") image.color=new Color(1,1,1,0);
        Pose(Find(canvas.transform,"ResultScore"),515,365,590,110);
        Pose(Ref<TMP_Text>(result,"resultTitleText").transform,0,0,250,100);
        Ref<TMP_Text>(result,"resultTitleText").fontSize=54;
        Pose(Ref<TMP_Text>(result,"blueTeamScoreText").transform,-172,0,100,90);
        Pose(Ref<TMP_Text>(result,"redTeamScoreText").transform,172,0,100,90);
        Ref<TMP_Text>(result,"blueTeamScoreText").color=new Color32(106,177,255,255);
        Ref<TMP_Text>(result,"redTeamScoreText").color=new Color32(235,78,91,255);
        foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="MvpText"))
        {
            text.color=new Color32(255,217,85,255);
            var motion=Ensure<AuthoredPanelMotion>(text.gameObject); motion.content=text.rectTransform;
            motion.entryOffset=Vector2.zero; motion.initialScale=1.7f; motion.duration=.28f; motion.delay=.35f;
        }
        ui.replay=Button(root,"Replay","연출 다시 보기",730,474,300);
        var overlay=Box(root,"Cinematic",0,0,1920,1080,Color.clear); overlay.raycastTarget=true;
        ui.overlay=overlay.gameObject;
        View(overlay.transform,"Live3DStage",texture);
        var content=Group(overlay.transform,"ProgressionPages"); ui.pageFade=content;
        var shade=Box(content.transform,"RightReadability",585,0,750,1080,Color.white);
        var fade=Ensure<VectorUIFade>(shade.gameObject); fade.top=new Color(.10f,.08f,.07f,.1f); fade.bottom=new Color(.10f,.08f,.07f,.60f);
        var header=Rect(content.transform,"Header",515,365,590,110); ui.header=header.gameObject;
        Box(header,"ScoreBand",0,0,590,100,new Color(.16f,.12f,.11f,.34f));
        ui.scoreA=Text(header,"TeamAScore","0",-172,0,115,90,66); ui.scoreA.color=new Color32(106,177,255,255); ui.scoreA.alignment=TextAlignmentOptions.Center;
        ui.outcome=Text(header,"Outcome","승리",0,0,180,90,64); ui.outcome.alignment=TextAlignmentOptions.Center;
        ui.scoreB=Text(header,"TeamBScore","0",172,0,115,90,66); ui.scoreB.color=new Color32(235,78,91,255); ui.scoreB.alignment=TextAlignmentOptions.Center;
        var rankRoot=Rect(content.transform,"RankPage",0,0,1920,1080); ui.rankPage=rankRoot.gameObject;
        ui.rank=rankRoot.GetComponentInChildren<RankResultController>(true);
        if(ui.rank==null)
        {
            var rankScene=EditorSceneManager.OpenScene("Assets/Scenes/RankResult.unity",OpenSceneMode.Additive);
            var source=rankScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RankResultController>(true)).Single();
            ui.rank=Object.Instantiate(source.gameObject,rankRoot,false).GetComponent<RankResultController>();
            ui.rank.name="AuthoredRankPresentation";
            EditorSceneManager.CloseScene(rankScene,true);
        }
        AuthorRank(ui.rank);
        var rewards=Rect(content.transform,"RewardsPage",0,0,1920,1080); ui.rewardPage=rewards.gameObject;
        Text(rewards,"Title","경기 보상",515,243,590,58,42);
        ui.profileIcon=Box(rewards,"ProfileIcon",267,147,90,90,Color.white); ui.profileIcon.preserveAspect=true;
        ui.card=Box(rewards,"PairedCard",765,87,94,188,Color.white);
        ui.localName=Text(rewards,"LocalName","",500,165,320,45,29);
        ui.level=Text(rewards,"AccountLevel","",500,122,320,36,22);
        Box(rewards,"RecordBand",475,16,510,70,new Color(0,0,0,.30f));
        ui.kda=Text(rewards,"KDA","",475,16,470,60,39);
        Text(rewards,"KDALabel","처치   /   데스   /   도움",475,-39,510,30,19);
        Box(rewards,"RewardBand",515,-139,590,110,new Color(0,0,0,.35f));
        Text(rewards,"CombatCurrencyLabel","전투 재화",382,-119,260,32,21);
        Text(rewards,"WinCurrencyLabel","승리 재화",676,-119,240,32,21);
        ui.combatReward=Text(rewards,"CombatCurrency","",382,-162,260,48,38);
        ui.winReward=Text(rewards,"WinCurrency","",676,-162,240,48,38);
        ui.balance=Text(rewards,"Balances","",515,-224,590,38,20);
        ui.mvpName=Text(rewards,"AuthoritativeMVP","",515,-287,590,45,23); ui.mvpName.color=new Color32(255,219,112,255);
        ui.viewMvp=Button(rewards,"ViewMVP","MVP 보기",715,-358,190);
        ui.featuredName=Text(rewards,"FeaturedName","",-432,-416,700,65,39);
        var identity=Rect(content.transform,"EquippedIdentitySlot",765,87,220,470); ui.identityRoot=identity.gameObject;
        legacy.equippedIdentity.transform.SetParent(identity,false); Pose(legacy.equippedIdentity.transform,0,0,190,430);
        legacy.equippedIdentity.transform.localScale=Vector3.one*.55f;
        legacy.profileIcon.gameObject.SetActive(false); legacy.equippedLevel.gameObject.SetActive(false);
        ui.card.gameObject.SetActive(false); // Matching card is already shown with the equipped case and medals.
        foreach(var t in identity.GetComponentsInChildren<TMP_Text>(true)) t.color=Color.white;
        ui.next=Button(overlay.transform,"Next","다음",515,-450,320); ui.nextLabel=ui.next.GetComponentInChildren<TMP_Text>();
        Text(overlay.transform,"InputHint","Space  계속     Esc  전체 결과",515,-502,590,28,17).alignment=TextAlignmentOptions.Center;
        ui.banner=Group(overlay.transform,"VictoryBanner");
        Box(ui.banner.transform,"Dim",0,0,1920,1080,new Color(0,0,0,.46f));
        Box(ui.banner.transform,"Band",0,0,1920,185,new Color(.93f,.86f,.84f,.94f));
        Box(ui.banner.transform,"AccentTop",0,96,1920,3,red); Box(ui.banner.transform,"AccentBottom",0,-96,1920,3,red);
        ui.bannerTitle=Text(ui.banner.transform,"Title","승리",0,0,900,155,112); ui.bannerTitle.color=red; ui.bannerTitle.alignment=TextAlignmentOptions.Center;
        ui.curtain=Group(overlay.transform,"TransitionBlack"); Box(ui.curtain.transform,"Black",0,0,1920,1080,Color.black); ui.curtain.blocksRaycasts=false; ui.curtain.alpha=0;
        // Default preview is inspectable without Play; runtime only changes visibility/data/animation.
        ui.banner.gameObject.SetActive(false); ui.rankPage.SetActive(false); ui.rewardPage.SetActive(true); content.alpha=1;
        ui.replay.gameObject.SetActive(false); ui.characterStage.Select(0,false); ui.characterStage.SetFraming(1);
        preserved.Check();
        EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Result save failed");
        AssetDatabase.SaveAssets(); Verify();
    }
    static void AuthorRank(RankResultController rank)
    {
        rank.embedded=true; rank.enabled=true; Pose(rank.transform,0,0,1920,1080);
        foreach(var motion in rank.GetComponentsInChildren<AuthoredPanelMotion>(true)) motion.enabled=false;
        foreach(var text in rank.GetComponentsInChildren<TMP_Text>(true)) text.color=Color.white;
        Find(rank.transform,"Header").gameObject.SetActive(false);
        Find(rank.transform,"Continue Hint").gameObject.SetActive(false);
        Ref<Button>(rank,"continueButton").gameObject.SetActive(false);
        Pose(Find(rank.transform,"Rank Frame"),515,125,340,340);
        Pose(Ref<Image>(rank,"rankImage").transform,0,0,340,340);
        Pose(Ref<TMP_Text>(rank,"rankNameText").transform,515,-75,590,65); Ref<TMP_Text>(rank,"rankNameText").fontSize=47;
        Pose(Ref<GameObject>(rank,"rankProgressRoot").transform,515,-138,535,12);
        Pose(Ref<TMP_Text>(rank,"rankPointText").transform,515,-177,535,40);
        Pose(Ref<TMP_Text>(rank,"rankDeltaText").transform,515,-228,535,48); Ref<TMP_Text>(rank,"rankDeltaText").fontSize=37;
        Pose(Ref<TMP_Text>(rank,"rankChangeText").transform,515,-313,650,100); Ref<TMP_Text>(rank,"rankChangeText").fontSize=29;
        var so=new SerializedObject(rank); so.FindProperty("increaseColor").colorValue=new Color32(255,220,117,255);
        so.FindProperty("neutralColor").colorValue=Color.white; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Material Material(Material source)
    {
        string key=source==null?"Floor":AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_"+source.name.Replace('/','_');
        string path=Folder+"/Stage_"+key+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material!=null) return material;
        material=new Material(Shader.Find("ProjectVector/ResultStage"));
        if(source!=null)
        {
            foreach(string p in new[]{"_BaseMap","_MainTex","_BaseColorMap"}) if(source.HasProperty(p)&&source.GetTexture(p)!=null)
            { material.SetTexture("_BaseMap",source.GetTexture(p)); material.SetTextureScale("_BaseMap",source.GetTextureScale(p)); material.SetTextureOffset("_BaseMap",source.GetTextureOffset(p)); break; }
            material.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
        }
        else material.SetColor("_BaseColor",new Color(.39f,.36f,.33f));
        AssetDatabase.CreateAsset(material,path); return material;
    }
    internal static void Isolate(GameObject go)
    {
        foreach(var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
        foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled=false;
        foreach(var c in go.GetComponentsInChildren<Collider>(true)) c.enabled=false;
        foreach(var b in go.GetComponentsInChildren<Rigidbody>(true)) b.isKinematic=true;
        foreach(var c in go.GetComponentsInChildren<Camera>(true)) c.enabled=false;
        foreach(var a in go.GetComponentsInChildren<AudioSource>(true)) a.enabled=false;
        foreach(var light in go.GetComponentsInChildren<Light>(true)) light.enabled=false;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials=r.sharedMaterials.Select(Material).ToArray();
    }
    static Bounds Bounds(GameObject go)
    {
        var renderers=go.GetComponentsInChildren<Renderer>(true); var result=new Bounds(); bool first=true;
        foreach(var r in renderers)
        {
            var b=r.bounds;
            if(r is SkinnedMeshRenderer s && s.sharedMesh!=null)
            {
                var baked=new Mesh(); s.BakeMesh(baked,false); var local=baked.bounds;
                b=new Bounds(s.transform.TransformPoint(local.center),Vector3.zero);
                for(int i=0;i<8;i++) b.Encapsulate(s.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                Object.DestroyImmediate(baked);
            }
            if(first) {result=b;first=false;} else result.Encapsulate(b);
        }
        return result;
    }
    static Bounds SkeletonBounds(GameObject go)
    {
        var bones=go.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.bones).Where(t=>t!=null).Distinct().ToArray();
        if(bones.Length==0) throw new Exception("Missing model skeleton");
        var b=new Bounds(bones[0].position,Vector3.zero); foreach(var bone in bones) b.Encapsulate(bone.position);
        return b;
    }
    static ResultCharacterStage AuthorStage(UnityEngine.SceneManagement.Scene scene)
    {
        var root=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Result3DStage") ?? new GameObject("Result3DStage");
        var stage=Ensure<ResultCharacterStage>(root);
        root.transform.position=new Vector3(10000,10000,10000);
        var modelRoot=Child(root.transform,"AuthoredCharacters").transform;
        stage.models=new GameObject[4]; stage.animators=new Animator[4];
        string[] names={"unitychan","UnityChanSSU","Casual1","PicoChan"};
        string[] roles={"Initiator","Controller","Sentinel","Entry"};
        for(int i=0;i<4;i++)
        {
            string name="Character_"+i+"_"+roles[i]; var parent=Child(modelRoot,name); parent.SetActive(true);
            var model=parent.transform.childCount>0?parent.transform.GetChild(0).gameObject:null;
            if(model==null)
            {
                model=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("LobbyCharacter/"+names[i]),parent.transform);
                Isolate(model); model.transform.localRotation=Quaternion.Euler(0,180,0);
                var bounds=Bounds(model); parent.transform.localScale=Vector3.one*(1.8f/Mathf.Max(.1f,bounds.size.y));
                bounds=Bounds(model); parent.transform.position+=root.transform.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            }
            var animator=model.GetComponentInChildren<Animator>(true); stage.models[i]=parent; stage.animators[i]=animator;
            string controllerPath=Folder+"/Result_"+roles[i]+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null || controller.layers.Length==0)
            {
                if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                if(controller.layers.Length==0) controller.AddLayer("Base Layer");
                string prefix="Assets/Character Animation/"+roles[i]+"/Lobby/"+roles[i]+(i==2?"_Lobby":"");
                var idle=controller.layers[0].stateMachine.AddState("Idle"); idle.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix+"_Idle.anim");
                var celebrate=controller.layers[0].stateMachine.AddState("Celebrate");
                celebrate.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix+"_Greeting.anim");
                var transition=celebrate.AddTransition(idle); transition.hasExitTime=true; transition.exitTime=1; transition.duration=.22f;
                if(idle.motion==null||celebrate.motion==null) throw new Exception("Missing animation: "+roles[i]);
                controller.layers[0].stateMachine.defaultState=idle;
                foreach(var asset in new Object[]{controller,controller.layers[0].stateMachine,idle,celebrate,transition}) EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.updateMode=AnimatorUpdateMode.UnscaledTime; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            // The lobby rigs are a mix of generic and humanoid. Use their matching clips,
            // not a humanoid-only victory clip on a generic avatar.
            if(i==0)
            {
                var state=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Celebrate").state;
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character Animation/Initiator/Lobby/Initiator_Greeting.anim");
                EditorUtility.SetDirty(state); AssetDatabase.SaveAssets();
            }
            parent.transform.localPosition=Vector3.zero; parent.transform.localScale=Vector3.one;
            animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
            var posed=SkeletonBounds(model); parent.transform.localScale=Vector3.one*(1.65f/Mathf.Max(.1f,posed.size.y));
            posed=SkeletonBounds(model); parent.transform.position+=root.transform.position-new Vector3(posed.center.x,posed.min.y,posed.center.z);
            animator.Update(0);
            Debug.Log($"RESULT_POSED_BOUNDS: {i} {SkeletonBounds(model)} parent={parent.transform.localPosition} scale={parent.transform.localScale}");
            Debug.Log($"RESULT_MODEL_MAPPING: {i} = {names[i]}, avatar={animator.avatar?.name}, human={animator.isHuman}");
            parent.SetActive(false);
        }
        var rejected=root.transform.Find("AuthoredEnvironment"); if(rejected!=null) rejected.gameObject.SetActive(false);
        var environments=Child(root.transform,"AuthoredMatchMaps"); stage.maps=new GameObject[2];
        for(int mapId=0;mapId<2;mapId++)
        {
            string mapName=mapId==0?"TestMap":"Desert";
            var map=environments.transform.Find(mapName)?.gameObject;
            if(map==null)
            {
                map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/"+mapName+".prefab"),environments.transform);
                map.name=mapName; Isolate(map);
                var info=map.GetComponent<MapInfo>();
                var point=info.attackSpawnPoints[0];
                // This is an actual playable-map location, looking inward from the attack spawn.
                map.transform.localRotation=Quaternion.Inverse(point.rotation);
                map.transform.position+=root.transform.position-(point.position+point.forward*4);
                if(info.barriers!=null) info.barriers.SetActive(false);
                Debug.Log($"RESULT_MAP_LOCATION: id={mapId} {mapName} spawn={point.position-root.transform.position} bounds={Bounds(map)}");
            }
            stage.maps[mapId]=map;
        }
        stage.SelectMap("TestMap");
        stage.closePose=Child(root.transform,"CloseUpCameraPose").transform; stage.closePose.localPosition=new Vector3(0,1.23f,-1.9f);
        stage.settledPose=Child(root.transform,"ProgressionCameraPose").transform; stage.settledPose.localPosition=new Vector3(.68f,1.20f,-2.3f);
        stage.closePose.localRotation=stage.settledPose.localRotation=Quaternion.identity;
        stage.stageCamera=Ensure<Camera>(Child(root.transform,"ResultCamera"));
        var camera=stage.stageCamera; camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.Skybox; camera.backgroundColor=new Color(.61f,.54f,.48f);
        camera.nearClipPlane=.03f; camera.farClipPlane=500; camera.allowHDR=false; camera.depth=-10;
        string rtPath=Folder+"/ResultStage.renderTexture"; var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if(rt==null) {rt=new RenderTexture(1920,1080,24){name="ResultStage",antiAliasing=2}; AssetDatabase.CreateAsset(rt,rtPath);}
        camera.targetTexture=rt; camera.aspect=16f/9; return stage;
    }
    static void Fixture()
    {
        MatchResultCache.Clear(); MatchResultCache.scoreA=7; MatchResultCache.scoreB=4; MatchResultCache.winnerTeam=0;
        MatchResultCache.freeCurrencyReward=120; MatchResultCache.victoryCurrencyReward=35;
        MatchResultCache.rankPointsBefore=990; MatchResultCache.rankPointsAfter=1015; MatchResultCache.rankPointDelta=25;
        MatchResultCache.rankTierIdBefore=9; MatchResultCache.rankTierIdAfter=10;
        for(int i=0;i<6;i++) MatchResultCache.members.Add(new MatchResultMemberData {playerId=(uint)i+1,characterId=i%4,team=(byte)(i/3),nickname="플레이어 "+(i+1),kills=12-i,deaths=3+i,assists=4+i,isMvp=i==1,
            appearance=new MatchAppearanceData {playerIconId=i%3,level=23,rankImageId=2,preset=new ChipCraftPresetData {cardId=i%3,caseId=0}}});
    }
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
        var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResultVictorySequence>(true)).Single();
        var result=ui.GetComponentInParent<ResultUI>();
        int count=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
        Fixture(); ui.BindControls(); ui.Show(1);
        if(ui.FeaturedPlayerId!=1 || ui.characterStage.SelectedCharacter!=0) throw new Exception("Local character replaced by MVP");
        Capture(ui,"VectorVictory-Banner.png");
        ui.Tick(1.7f); ui.Tick(1.5f); Sample(ui.characterStage,1.5f); Capture(ui,"VectorVictory-Character.png");
        ui.Tick(3); if(ui.CurrentPage!=ResultVictorySequence.Page.Rewards) throw new Exception("Normal match showed rank page");
        ui.Tick(.3f); Capture(ui,"VectorVictory-Rewards.png");
        ui.viewMvp.onClick.Invoke(); if(ui.FeaturedPlayerId!=2||ui.characterStage.SelectedCharacter!=1) throw new Exception("MVP model selection failed");
        ui.viewMvp.onClick.Invoke(); if(ui.FeaturedPlayerId!=1) throw new Exception("Return to local model failed");
        for(int i=0;i<4;i++) {ui.characterStage.Select(i,true); ui.characterStage.SetFraming(1); Sample(ui.characterStage,.7f); Capture(ui,"VectorVictory-Model"+i+".png");}
        MatchResultCache.matchType=1; ui.Show(1); ui.Advance(); ui.Advance();
        if(ui.CurrentPage!=ResultVictorySequence.Page.Rank) throw new Exception("Rank sequence not entered");
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(RankResultController).GetMethod("ApplyRankPresentation",flags).Invoke(ui.rank,new object[]{1015,10});
        typeof(RankResultController).GetMethod("ApplyChangeText",flags).Invoke(ui.rank,new object[]{9,10});
        Ref<TMP_Text>(ui.rank,"rankDeltaText").text="+25"; ui.Tick(.3f); Sample(ui.characterStage,.7f); Capture(ui,"VectorVictory-Rank.png");
        ui.characterStage.SelectMap("Desert");
        if(ui.characterStage.SelectedMap!=1 || ui.characterStage.maps[0].activeSelf || !ui.characterStage.maps[1].activeSelf) throw new Exception("Map 1 selection failed");
        Capture(ui,"VectorVictory-DesertRank.png");
        ui.characterStage.SelectMap("TestMap");
        if(ui.characterStage.SelectedMap!=0 || !ui.characterStage.maps[0].activeSelf || ui.characterStage.maps[1].activeSelf) throw new Exception("Map 0 selection failed");
        for(int i=0;i<4;i++)
        {
            ui.characterStage.Select(i,false); ui.characterStage.SetFraming(1); Sample(ui.characterStage,.2f);
            Capture(ui,"VectorVictory-Idle"+i+".png");
        }
        ui.next.onClick.Invoke(); ui.next.onClick.Invoke(); if(ui.CurrentPage!=ResultVictorySequence.Page.Rewards) throw new Exception("Rank continue event failed");
        ui.ShowDetails(); result.OnClickTotal(); Capture(ui,"VectorVictory-Details.png");
        for(int i=0;i<3;i++) {MatchResultCache.members[0].appearance.playerIconId=i;ui.Show(1);ui.ShowRewards();if(ui.profileIcon.sprite!=Resources.Load<Sprite>($"PlayerIcon/{i}")||ui.card.sprite!=Resources.Load<Sprite>($"IconCard/icon{i}_card")) throw new Exception("Icon/card pairing diverged");}
        ui.Show(4); if(ui.outcome.text!="패배") throw new Exception("Defeat wrong");
        MatchResultCache.scoreB=7;ui.Show(1);if(ui.outcome.text!="무승부") throw new Exception("Draw wrong");
        ui.Show(999);ui.Advance();ui.Advance();if(ui.CurrentPage!=ResultVictorySequence.Page.Rewards||ui.combatReward.text!="—") throw new Exception("Spectator received personal rank/reward");
        foreach(var m in MatchResultCache.members) m.isMvp=false;
        ui.Show(999);if(ui.FeaturedPlayerId!=0||ui.characterStage.SelectedCharacter!=-1||ui.mvpName.text!="") throw new Exception("Invented MVP/model");
        for(int i=0;i<5;i++){ui.Show(1);ui.ShowDetails();result.OnClickFirstHalf();result.OnClickSecondHalf();result.OnClickArrayButton();result.OnClickTotal();}
        if(count!=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Length)) throw new Exception("Runtime created hierarchy");
        if(MatchResultCache.freeCurrencyReward!=120||MatchResultCache.rankPointsAfter!=1015) throw new Exception("Presentation mutated settlement");
        MatchResultCache.Clear();ui.Show(1);if(ui.overlay.activeSelf) throw new Exception("Empty result not handled");
        Debug.Log($"VIDEO2_SEQUENCE_VERIFIED: {count} saved objects unchanged; 4 models, local != MVP, normal/rank/defeat/draw/observer/skip/replay/icon pairing, immutable settlement.");
    }
    static void Sample(ResultCharacterStage stage,float time)
    {
        if(stage.SelectedCharacter<0)return;
        var animator=stage.animators[stage.SelectedCharacter]; animator.Update(time);
    }
    static void Capture(ResultVictorySequence ui,string file)
    {
        var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
        try {UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;ui.characterStage.stageCamera.Render();}
        finally {UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;}
        VectorExperiencePolishEditor.CaptureCanvas(ui.GetComponentInParent<Canvas>(),file);
    }
}
