using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Explicit offline authoring only. All runtime screens use these saved objects.
public static class VectorMatchFlowEditor
{
    static TMP_FontAsset font;
    static readonly Color ink=new Color32(21,25,33,255), gold=new Color32(233,191,98,255), muted=new Color32(178,189,204,255);
    static T Ensure<T>(GameObject go) where T:Component { var c=go.GetComponent<T>();return c==null?go.AddComponent<T>():c; }
    static T Ref<T>(Object owner,string field) where T:Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static void Set(Object owner,string field,Object value) { var s=new SerializedObject(owner);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
    static RectTransform Rect(Transform p,string n,float x,float y,float w,float h)
    {
        var r=p.Find(n) as RectTransform;
        if(r==null) { r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false); }
        r.gameObject.layer=5;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;return r;
    }
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c)
    { var i=Ensure<Image>(Rect(p,n,x,y,w,h).gameObject);i.color=c;i.raycastTarget=false;return i; }
    static TMP_Text Text(Transform p,string n,string value,float x,float y,float w,float h,float size,Color? color=null)
    {
        var t=Ensure<TextMeshProUGUI>(Rect(p,n,x,y,w,h).gameObject);t.font=font;t.fontSize=size;t.text=value;t.color=color??Color.white;
        t.alignment=TextAlignmentOptions.Center;t.richText=false;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    static Transform Child(Transform p,string n)
    { var t=p.Find(n);if(t!=null)return t; var g=new GameObject(n);g.transform.SetParent(p,false);return g.transform; }
    static void Save(UnityEngine.SceneManagement.Scene scene,VectorExperiencePolishEditor.Preservation preservation)
    {
        preservation.Check();
        foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)))
            if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
    }
    static MatchIdentityCard Card(Transform parent,string name,float x,float y,float w,float h)
    {
        var root=Rect(parent,name,x,y,w,h);var view=Ensure<MatchIdentityCard>(root.gameObject);
        Box(root,"Plate",0,0,w,h,ink);
        var identity=Rect(root,"Identity",0,0,w,h);view.identity=Ensure<CanvasGroup>(identity.gameObject);
        view.card=Box(identity,"EquippedCard",0,0,w-12,h-12,Color.white);
        view.icon=Box(identity,"Icon",-w*.31f,h*.39f,34,34,Color.white);
        view.medals=new Image[3];for(int i=0;i<3;i++)view.medals[i]=Box(view.card.transform,"Medal"+i,0,0,40,40,Color.white);
        view.border=Box(root,"EquippedFrame",0,0,w,h,Color.white);view.border.sprite=Resources.Load<Sprite>("IconBorder/border");
        view.border.enabled=false;
        Box(root,"MetalLeft",-w*.5f+4,0,8,h,new Color32(109,140,163,255)).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MatchAuthored/ReferenceFrameLeft.asset");
        Box(root,"MetalRight",w*.5f-4,0,8,h,new Color32(109,140,163,255)).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MatchAuthored/ReferenceFrameRight.asset");
        Box(root,"MetalTop",0,h*.5f-12,w,24,Color.white).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/VideoReference/CardCrown.png");
        Box(root,"MetalFoot",0,-h*.5f+7,w,14,Color.white).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MatchAuthored/ReferenceFrameFoot.asset");
        var concealed=Box(root,"Concealed",0,0,w-14,h-14,new Color32(52,60,72,255));view.concealed=concealed.gameObject;
        Text(concealed.transform,"WaitingMark","◇",0,15,w,70,48,muted);
        Box(root,"NameBand",0,-h*.29f,w-18,48,new Color(0,0,0,.8f));
        view.nickname=Text(root,"Nickname","",0,-h*.29f,w-26,46,25);
        Box(root,"StatusBand",0,-h*.40f,w-18,32,new Color32(24,32,42,245));
        view.caption=Text(root,"Status","수락 대기",0,-h*.40f,w-18,32,19,muted);
        view.nickname.transform.SetAsLastSibling();view.caption.transform.SetAsLastSibling();
        return view;
    }
    [MenuItem("Project Vector/UI/Author Match Entry and MVP Flow")]
    public static void InstallAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene refused");
        foreach(string name in new[]{"Lobby","Game_TestVersion","Result"})
            System.IO.File.Copy("Assets/Scenes/"+name+".unity",System.IO.Path.Combine(System.IO.Path.GetTempPath(),name+"-before-flow-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".unity"));
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");var preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var social=Object.FindFirstObjectByType<LobbySocialController>(FindObjectsInactive.Include);font=social.readyProgress.font;
        var ready=Ensure<ReadyMatchPresentation>(social.readyPanel);social.readyPresentation=ready;ready.cards=new MatchIdentityCard[6];
        var modalCanvas=Ensure<Canvas>(social.readyPanel);modalCanvas.overrideSorting=true;
        modalCanvas.sortingOrder=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Where(c=>c!=modalCanvas).Max(c=>c.sortingOrder)+10;
        Ensure<GraphicRaycaster>(social.readyPanel);
        for(int i=0;i<6;i++)
        {
            var legacy=social.readyCards[i];
            foreach(Transform child in legacy.transform) if(child.name!="IdentityCard") child.gameObject.SetActive(false);
            ready.cards[i]=Card(legacy.transform,"IdentityCard",0,0,180,370);
        }
        Save(scene,preserved);

        scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var pick=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var backdrop=Ref<PickMapBackdrop>(pick,"mapBackdrop");var pickPanel=backdrop.transform;var canvas=pickPanel.GetComponentInParent<Canvas>();
        var intro=Ensure<MapIntroPresentation>(pick.gameObject);Set(pick,"mapIntro",intro);
        var overlay=Rect(canvas.transform,"AuthoredMapIntroduction",0,0,1920,1080);intro.overlay=overlay.gameObject;
        var cameraRoot=Child(backdrop.stageRoot.transform,"MapOverviewCamera");intro.overviewCamera=Ensure<Camera>(cameraRoot.gameObject);
        var camera=intro.overviewCamera;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=62;camera.nearClipPlane=.1f;camera.farClipPlane=500;camera.allowHDR=false;
        const string rtPath="Assets/UI/ResultSequence/MapIntroduction.renderTexture";
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if(rt==null){rt=new RenderTexture(1920,1080,24){name="MapIntroduction",antiAliasing=2};AssetDatabase.CreateAsset(rt,rtPath);}
        camera.targetTexture=rt;camera.enabled=false;
        var raw=Ensure<RawImage>(Rect(overlay,"MapView",0,0,1920,1080).gameObject);raw.texture=rt;raw.raycastTarget=true;
        Box(overlay,"TopLetterbox",0,500,1920,80,ink);
        var band=Box(overlay,"MapCaptionBand",0,-400,1920,280,new Color(.035f,.045f,.06f,.88f));
        intro.mapTitle=Text(band.transform,"MapName","TEST MAP",-475,15,780,100,76);intro.mapTitle.alignment=TextAlignmentOptions.Left;
        intro.modeTitle=Text(band.transform,"Mode","팀 전투  /  3 대 3",-475,-69,780,46,27,muted);intro.modeTitle.alignment=TextAlignmentOptions.Left;
        Text(band.transform,"Entry","작전 지역 진입",660,-55,420,48,28);
        Box(band.transform,"Accent",-887,5,5,145,gold);
        intro.startPoses=new Transform[2];intro.endPoses=new Transform[2];
        for(int i=0;i<2;i++)
        {
            var map=backdrop.maps[i];var info=map.GetComponent<MapInfo>();var spawn=info.attackSpawnPoints[0];
            var start=Child(map.transform,"IntroductionCameraStart");var end=Child(map.transform,"IntroductionCameraEnd");
            start.position=spawn.position-spawn.forward*7+Vector3.up*10;
            end.position=spawn.position+spawn.forward*3+Vector3.up*7;
            var focus=spawn.position+spawn.forward*20+Vector3.up*1;
            start.rotation=Quaternion.LookRotation(focus-start.position);end.rotation=Quaternion.LookRotation(focus-end.position);
            intro.startPoses[i]=start;intro.endPoses[i]=end;
        }
        intro.overlay.SetActive(false);
        var loading=Ref<MatchLoadingPanelUI>(pick,"matchLoadingPanelUI");
        // Old graphic references remain bound; the new opaque authored composition covers them.
        var loadingView=Ensure<MatchLoadingPresentation>(loading.gameObject);loading.presentation=loadingView;
        var panel=Box(loading.transform,"AuthoredVersus",0,0,1920,1080,ink);panel.transform.SetAsLastSibling();
        Box(panel.transform,"AttackField",-590,160,1200,750,new Color32(26,35,48,255));
        Box(panel.transform,"DefenceField",590,-160,1200,750,new Color32(51,27,36,255));
        loadingView.mapTitle=Text(panel.transform,"MapName","TEST MAP  /  3 대 3",0,470,1400,58,35,muted);
        Text(panel.transform,"AttackLabel","선 공격",-770,280,230,45,25,new Color32(102,190,234,255));
        Text(panel.transform,"DefenceLabel","선 수비",770,-260,230,45,25,new Color32(241,106,120,255));
        Box(panel.transform,"VersusRule",0,0,1500,2,new Color32(106,119,136,255));
        Box(panel.transform,"VersusBacking",0,0,150,82,ink);Text(panel.transform,"VS","VS",0,0,150,80,57,gold);
        loadingView.cards=new MatchIdentityCard[6];loadingView.portraits=new Image[6];loadingView.roles=new TMP_Text[6];
        for(int i=0;i<6;i++)
        {
            float x=-400+(i%3)*400+(i<3?-110:110),y=i<3?228:-228;
            var card=Card(panel.transform,"Combatant"+i,x,y,275,350);loadingView.cards[i]=card;
            var portrait=Box(card.transform,"CharacterPortrait",0,25,250,270,Color.white);portrait.preserveAspect=true;
            portrait.transform.SetSiblingIndex(card.concealed.transform.GetSiblingIndex());loadingView.portraits[i]=portrait;
            card.caption.gameObject.SetActive(false);
            loadingView.roles[i]=Text(card.transform,"Role","",0,-141,250,36,20,muted);
            loadingView.roles[i].transform.SetAsLastSibling();
        }
        var s=new SerializedObject(pick);var chars=s.FindProperty("characters");loadingView.characterArt=new Sprite[chars.arraySize];
        for(int i=0;i<chars.arraySize;i++)loadingView.characterArt[i]=chars.GetArrayElementAtIndex(i).FindPropertyRelative("characterImage").objectReferenceValue as Sprite;
        loadingView.status=Text(panel.transform,"Status","전투 정보 확인 중",0,-487,1400,42,25,muted);
        var loader=loading.GetComponentInChildren<LoadingSlider>(true);if(loader==null)throw new Exception("Missing existing load-ready sender");
        loader.useMatchBriefing=true;loader.briefingStatus=loadingView.status;
        Save(scene,preserved);

        scene=EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var result=Object.FindFirstObjectByType<ResultVictorySequence>(FindObjectsInactive.Include);
        var mvp=Rect(result.overlay.transform,"AuthoredMvpSpotlight",0,0,1920,1080);result.mvpSpotlight=mvp.gameObject;result.mvpReveal=Ensure<CanvasGroup>(mvp.gameObject);
        mvp.SetSiblingIndex(result.next.transform.GetSiblingIndex());
        Box(mvp,"SpotlightPanel",520,20,720,650,new Color(.055f,.065f,.09f,.94f));
        Box(mvp,"GoldRule",173,20,6,650,gold);
        Text(mvp,"Eyebrow","MATCH  MOST VALUABLE PLAYER",510,271,590,45,21,gold);
        Text(mvp,"Award","MVP",510,160,620,180,140,gold);
        result.mvpSpotlightName=Text(mvp,"PlayerName","",510,18,610,70,48);
        result.mvpSpotlightKda=Text(mvp,"Record","",510,-90,590,75,52,gold);
        Text(mvp,"RecordLabels","처치          데스          도움",510,-151,590,40,21,muted);
        result.mvpSpotlightCaption=Text(mvp,"Caption","이번 경기 최고의 플레이어",510,-230,610,45,26);
        result.mvpSpotlight.SetActive(false);
        Save(scene,preserved);
        Verify();
    }
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var social=Object.FindFirstObjectByType<LobbySocialController>(FindObjectsInactive.Include);
        int Count()=>scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
        int before=Count();var ids=new uint[]{1,2,3,4,5,6};var names=new[]{"LYS","KSW","JJY","KSH","KDH","CJU"};
        var appearances=ids.ToDictionary(id=>id,id=>new MatchAppearanceData{playerIconId=(int)(id%3),level=10,preset=new ChipCraftPresetData()});
        social.ReceiveReady(12,0,20,6,3,true);social.readyPresentation.Present(12,names,ids,appearances,21,1);
        for(int i=0;i<6;i++)if(social.readyPresentation.cards[i].concealed.activeSelf==((21&(1<<i))!=0))throw new Exception("Accepted card mismatch");
        var canvas=social.readyPanel.GetComponentInParent<Canvas>();
        VectorExperiencePolishEditor.CaptureCanvas(canvas,"VectorFlow-Ready.png");
        social.ReceiveReady(12,2,0,6,3,true);if(social.readyPanel.activeSelf)throw new Exception("Cancelled readiness stayed open");
        if(before!=Count())throw new Exception("Runtime constructed readiness objects");
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var pick=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);var intro=Ref<MapIntroPresentation>(pick,"mapIntro");var backdrop=Ref<PickMapBackdrop>(pick,"mapBackdrop");
        canvas=backdrop.GetComponentInParent<Canvas>();
        for(int i=0;i<2;i++)
        {
            var map=i==0?"TestMap":"Desert";backdrop.ApplyMap(map);intro.Begin(new PickStateData{roomId=(uint)i+1,mapName=map});intro.Evaluate(.5f);
            var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
            try{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;intro.overviewCamera.Render();}
            finally{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;}
            VectorExperiencePolishEditor.CaptureCanvas(canvas,"VectorFlow-Intro-"+map+".png");intro.Finish();
        }
        var loading=Ref<MatchLoadingPanelUI>(pick,"matchLoadingPanelUI");var data=new MatchLoadingInfoData();
        for(int i=0;i<6;i++)(i<3?data.attackMembers:data.defenseMembers).Add(new MatchLoadingMemberData{playerId=ids[i],nickname=names[i],characterId=i%4});
        loading.Show(data);VectorExperiencePolishEditor.CaptureCanvas(canvas,"VectorFlow-Loading.png");
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
        var result=Object.FindFirstObjectByType<ResultVictorySequence>(FindObjectsInactive.Include);
        MatchResultCache.Clear();MatchResultCache.mapName="TestMap";MatchResultCache.matchType=1;MatchResultCache.winnerTeam=0;MatchResultCache.scoreA=6;MatchResultCache.scoreB=3;
        for(int i=0;i<6;i++)MatchResultCache.members.Add(new MatchResultMemberData{playerId=ids[i],nickname=names[i],characterId=i%4,team=(byte)(i/3),isMvp=i==1,kills=i==1?18:3,appearance=appearances[ids[i]]});
        result.BindControls();result.Show(4);result.Advance();result.Tick(.8f);
        result.characterStage.animators[result.characterStage.SelectedCharacter].Update(2.5f);
        if(result.CurrentPage!=ResultVictorySequence.Page.Mvp || result.FeaturedPlayerId!=2)throw new Exception("Opponent MVP not spotlighted");
        var rp=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var qp=QualitySettings.renderPipeline;
        try{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;result.characterStage.stageCamera.Render();}
        finally{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.renderPipeline=qp;}
        VectorExperiencePolishEditor.CaptureCanvas(result.GetComponentInParent<Canvas>(),"VectorFlow-Mvp.png");
        result.Advance();if(result.FeaturedPlayerId!=4 || result.CurrentPage!=ResultVictorySequence.Page.Rank)throw new Exception("MVP overwrote local progression");
        result.ShowDetails();if(result.overlay.activeSelf)throw new Exception("Score table inaccessible");
        foreach(var m in MatchResultCache.members)m.isMvp=false;
        result.Show(4);result.Advance();if(result.CurrentPage==ResultVictorySequence.Page.Mvp)throw new Exception("Invented MVP");
        MatchResultCache.Clear();
        Debug.Log("MATCH_FLOW_VERIFIED: accepted identities, cancellation, TestMap/Desert intro, 3v3 loading, opponent MVP, local progression, existing score table.");
    }
    public static void VerifyAndBuild()
    {
        Verify();
        VectorVictorySequenceEditor.Verify();
        RankedCaptureTestEditor.Build();
    }
}
