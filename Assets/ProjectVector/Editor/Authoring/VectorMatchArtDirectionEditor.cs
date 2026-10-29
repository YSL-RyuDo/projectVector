using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Run explicitly while the project is closed. Never constructs UI during Play.
public static class VectorMatchArtDirectionEditor
{
    static TMP_FontAsset font;
    static VectorExperiencePolishEditor.Preservation guard;
    static readonly Color ink=new Color32(17,22,27,255), paper=new Color32(234,236,227,255),
        muted=new Color32(148,162,165,255), cyan=new Color32(110,208,218,255), red=new Color32(230,125,118,255), gold=new Color32(222,187,119,255);
    static T Ensure<T>(GameObject go) where T:Component { var t=go.GetComponent<T>();return t==null?go.AddComponent<T>():t; }
    static T Ref<T>(Object owner,string field) where T:Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static RectTransform R(Transform p,string n,float x,float y,float w,float h)
    {
        var r=p.Find(n) as RectTransform;
        if(r==null){r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);}
        r.gameObject.layer=5;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;return r;
    }
    static Image B(Transform p,string n,float x,float y,float w,float h,Color c)
    {
        var v=Ensure<Image>(R(p,n,x,y,w,h).gameObject);guard?.AllowSpriteChange(v);v.sprite=null;v.color=c;v.raycastTarget=false;return v;
    }
    static TMP_Text T(Transform p,string n,string text,float x,float y,float w,float h,float size,Color c,TextAlignmentOptions align=TextAlignmentOptions.Left)
    {
        var v=Ensure<TextMeshProUGUI>(R(p,n,x,y,w,h).gameObject);v.font=font;v.fontSize=size;v.color=c;v.text=text;
        v.alignment=align;v.raycastTarget=false;v.richText=false;v.overflowMode=TextOverflowModes.Ellipsis;return v;
    }
    static void Panel(Transform p,string n,float x,float y,float w,float h,Color fill,Color stroke,float corner=16)
    {
        var r=R(p,n,x,y,w,h);Ensure<CanvasRenderer>(r.gameObject);
        var v=Ensure<VectorCutPanel>(r.gameObject);v.color=fill;v.borderColor=stroke;v.corner=corner;v.borderWidth=1;v.raycastTarget=false;
        v.SetAllDirty();
    }
    static void Motion(GameObject go,float delay=0)
    {
        Ensure<CanvasGroup>(go);var m=Ensure<AuthoredPanelMotion>(go);m.content=go.transform as RectTransform;m.entryOffset=new Vector2(0,-22);m.duration=.38f;m.delay=delay;m.initialScale=1;
    }
    static void Off(Transform p,params string[] names){foreach(var n in names){var t=p.Find(n);if(t!=null)t.gameObject.SetActive(false);}}
    static void Save(UnityEngine.SceneManagement.Scene scene)
    {
        guard.Check();
        foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)))
            if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
    }
    static void RestyleCard(MatchIdentityCard v,float w,float h,Color accent,int index,bool combat)
    {
        var p=v.transform;R(p.parent,p.name,((RectTransform)p).anchoredPosition.x,((RectTransform)p).anchoredPosition.y,w,h);
        // Retain obsolete authored objects/references, but do not render copied frame slices.
        Off(p,"MetalLeft","MetalRight","MetalTop","MetalFoot","EquippedFrame","Plate");
        Panel(p,"VectorFrame",0,0,w,h,ink,new Color32(73,91,96,255));p.Find("VectorFrame").SetAsFirstSibling();
        R(p,"Identity",0,0,w,h);R(v.card.transform.parent,v.card.name,0,0,w-16,h-16);
        v.card.color=combat?new Color(.36f,.4f,.43f,.32f):Color.white;
        R(v.icon.transform.parent,v.icon.name,-w*.5f+31,h*.5f-46,34,34);
        v.icon.preserveAspect=true;
        B(p,"CrestLine",0,h*.5f-9,w-44,3,accent);
        T(p,"Serial",index.ToString("00"),w*.5f-37,h*.5f-44,44,28,18,accent,TextAlignmentOptions.Right);
        R(p,"Concealed",0,0,w-18,h-22);var conceal=v.concealed.GetComponent<Image>();conceal.color=new Color32(28,36,42,255);
        T(v.concealed.transform,"WaitingMark","+",0,22,w-30,90,56,new Color32(76,97,103,255),TextAlignmentOptions.Center);
        T(v.concealed.transform,"WaitingTitle","응답 대기",0,-50,w-30,34,19,muted,TextAlignmentOptions.Center);
        B(p,"NameBand",0,-h*.5f+82,w-18,64,new Color32(17,22,27,246));
        R(p,v.nickname.name,0,-h*.5f+83,w-34,52);v.nickname.fontSize=combat?28:25;
        B(p,"StatusBand",0,-h*.5f+31,w-18,38,new Color32(29,41,45,255));
        R(p,v.caption.name,0,-h*.5f+31,w-24,34);v.caption.fontSize=18;v.caption.color=accent;
        v.nickname.transform.SetAsLastSibling();v.caption.transform.SetAsLastSibling();
    }
    static Sprite Cutout(string name)
    {
        string path="Assets/ProjectVector/Modules/Match/UI/MatchFlow/Portraits/"+name+"Cutout.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Project Vector/UI/Polish Match Flow Original Art Direction")]
    public static void InstallAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene refused");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var sentinel=Cutout("Sentinel");var duelist=Cutout("Duelist");
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");guard=new(scene);
        var social=Object.FindFirstObjectByType<LobbySocialController>(FindObjectsInactive.Include);var p=social.readyPanel.transform;
        social.readyPanel.GetComponent<Image>().color=ink;Off(p,"Horizon");
        T(p,"Wordmark","VECTOR  /  MATCH",-620,449,460,32,19,muted);
        B(p,"HeaderRule",0,410,1700,1,new Color32(68,84,91,255));
        T(p,"Title","매칭 완료",0,344,1200,80,58,paper,TextAlignmentOptions.Center);
        T(p,"Subtitle","수락하면 플레이어 카드가 공개됩니다",0,275,1200,40,22,muted,TextAlignmentOptions.Center);
        for(int i=0;i<6;i++)
        {
            var image=social.readyCards[i];R(p,image.name,-615+i*246,8,220,430);
            guard.AllowSpriteChange(image);image.sprite=null;image.color=Color.clear;image.enabled=false;
            RestyleCard(social.readyPresentation.cards[i],220,430,cyan,i+1,false);Motion(image.gameObject,.08f+i*.045f);
        }
        R(p,social.readyProgress.name,0,-263,1000,42);social.readyProgress.fontSize=27;
        R(p,social.readyTimer.name,0,-310,1000,38);social.readyTimer.fontSize=21;social.readyTimer.color=muted;
        R(p,social.acceptReady.name,0,-401,360,68);social.acceptReady.image.color=new Color32(48,105,113,255);
        R(p,social.declineReady.name,0,-477,260,42);social.declineReady.image.color=Color.clear;
        Off(social.acceptReady.transform,"Accent");Off(social.declineReady.transform,"Accent");
        Motion(social.readyPanel);Save(scene);

        scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");guard=new(scene);
        var pick=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);var intro=Ref<MapIntroPresentation>(pick,"mapIntro");p=intro.overlay.transform;
        B(p,"TopLetterbox",0,505,1920,70,ink);
        T(p,"Wordmark","VECTOR  /  OPERATION",-628,505,530,38,19,paper);
        var band=B(p,"MapCaptionBand",0,-393,1920,294,new Color(.045f,.064f,.071f,.93f));intro.captionReveal=Ensure<CanvasGroup>(band.gameObject);
        R(band.transform,intro.mapTitle.name,-444,21,860,105);intro.mapTitle.fontSize=82;intro.mapTitle.color=paper;
        R(band.transform,intro.modeTitle.name,-442,-67,860,38);intro.modeTitle.fontSize=24;intro.modeTitle.color=muted;
        T(band.transform,"Entry","작전 지역",688,-65,320,38,23,cyan,TextAlignmentOptions.Right);
        T(band.transform,"Briefing","MAP BRIEFING",688,26,320,38,22,muted,TextAlignmentOptions.Right);
        B(band.transform,"Accent",-900,0,4,144,cyan);B(band.transform,"TopRule",0,146,1800,1,new Color32(78,113,119,255));
        var loading=Ref<MatchLoadingPanelUI>(pick,"matchLoadingPanelUI");var v=loading.presentation;p=loading.transform.Find("AuthoredVersus");p.GetComponent<Image>().color=ink;
        var mapImage=Ensure<RawImage>(R(p,"SelectedMapBackdrop",0,0,1920,1080).gameObject);mapImage.texture=intro.overviewCamera.targetTexture;mapImage.raycastTarget=false;
        mapImage.transform.SetAsFirstSibling();
        var veil=B(p,"MapBackdropVeil",0,0,1920,1080,new Color(.035f,.055f,.063f,.92f));veil.transform.SetSiblingIndex(1);
        Off(p,"AttackField","DefenceField","VersusRule","VersusBacking");
        T(p,"Wordmark","VECTOR  /  DEPLOYMENT",-630,470,530,35,19,muted);
        B(p,"HeaderRule",0,425,1720,1,new Color32(68,84,91,255));
        R(p,v.mapTitle.name,590,469,580,64);v.mapTitle.alignment=TextAlignmentOptions.Right;v.mapTitle.fontSize=29;v.mapTitle.color=paper;
        T(p,"AttackLabel","선 공격",-468,354,744,48,30,cyan);
        T(p,"DefenceLabel","선 수비",468,354,744,48,30,red,TextAlignmentOptions.Right);
        B(p,"AttackRule",-468,315,744,3,cyan);B(p,"DefenseRule",468,315,744,3,red);
        T(p,"VS","VS",0,5,120,90,41,paper,TextAlignmentOptions.Center);
        B(p,"CenterRuleTop",0,178,1,204,new Color32(60,76,82,255));B(p,"CenterRuleBottom",0,-169,1,204,new Color32(60,76,82,255));
        for(int i=0;i<6;i++)
        {
            float x=i<3?-720+i*252:216+(i-3)*252;var c=v.cards[i];R(p,c.name,x,-8,232,574);
            RestyleCard(c,232,574,i<3?cyan:red,i%3+1,true);
            var window=B(c.transform,"PortraitWindow",0,43,214,400,Color.black);
            Ensure<RectMask2D>(window.gameObject);
            window.transform.SetSiblingIndex(c.transform.Find("NameBand").GetSiblingIndex());
            var portrait=v.portraits[i];portrait.transform.SetParent(window.transform,false);
            R(window.transform,portrait.name,0,-4,354,390);portrait.preserveAspect=true;
            var shade=B(window.transform,"FootShade",0,-159,214,84,ink);
            var fade=Ensure<VectorUIFade>(shade.gameObject);fade.top=new Color(1,1,1,0);fade.bottom=Color.white;
            R(c.transform,v.roles[i].name,0,-256,214,36);v.roles[i].color=i<3?cyan:red;v.roles[i].fontSize=19;
            c.nickname.transform.SetAsLastSibling();v.roles[i].transform.SetAsLastSibling();Motion(c.gameObject,.10f+(i%3)*.075f);
        }
        v.portraitOverrides=new Sprite[]{null,null,sentinel,duelist};
        R(p,v.status.name,0,-425,1500,45);v.status.fontSize=22;v.status.color=muted;
        B(p,"FooterRule",0,-378,1720,1,new Color32(68,84,91,255));
        T(p,"Format","3  vs  3",-690,-475,300,34,18,muted);
        T(p,"FooterBrand","PROJECT VECTOR",675,-475,330,34,18,muted,TextAlignmentOptions.Right);
        Save(scene);

        scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/Result.unity");guard=new(scene);
        var result=Object.FindFirstObjectByType<ResultVictorySequence>(FindObjectsInactive.Include);p=result.mvpSpotlight.transform;
        B(p,"SpotlightPanel",560,20,672,738,new Color(.045f,.06f,.067f,.95f));
        B(p,"GoldRule",225,20,3,738,gold);
        T(p,"Eyebrow","MATCH HONORS",558,315,558,36,22,gold);
        T(p,"Award","MVP",548,181,560,195,150,paper);
        R(p,result.mvpSpotlightName.name,558,24,558,74);result.mvpSpotlightName.fontSize=49;result.mvpSpotlightName.alignment=TextAlignmentOptions.Left;
        B(p,"RecordRule",558,-50,558,1,new Color32(107,100,80,255));
        R(p,result.mvpSpotlightKda.name,558,-120,558,78);result.mvpSpotlightKda.fontSize=50;result.mvpSpotlightKda.color=gold;result.mvpSpotlightKda.alignment=TextAlignmentOptions.Center;
        T(p,"RecordLabels","처치          데스          도움",558,-182,558,34,20,muted,TextAlignmentOptions.Center);
        R(p,result.mvpSpotlightCaption.name,558,-281,558,48);result.mvpSpotlightCaption.fontSize=23;result.mvpSpotlightCaption.alignment=TextAlignmentOptions.Left;result.mvpSpotlightCaption.color=paper;
        B(p,"AwardUnderline",323,86,88,4,gold);
        Save(scene);guard=null;
        Verify();
    }
    public static void Verify()
    {
        VectorMatchFlowEditor.Verify();
        VectorVictorySequenceEditor.Verify();
        VerifyKeyframes();
        foreach(string path in new[]{"Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity","Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MatchIdentityCard>(true)))
                foreach(string name in new[]{"MetalLeft","MetalRight","MetalTop","MetalFoot"})
                    if(c.transform.Find(name).gameObject.activeSelf)throw new Exception("Copied frame still enabled");
        }
        Debug.Log("MATCH_ART_VERIFIED: original frames, authored hierarchy, 2D portraits, real maps, MVP and score-table regression.");
    }
    public static void VerifyAndBuild()
    {
        Verify();
        // Never bake the diagnostic fixture into the player build.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RankedCaptureTestEditor.Build();
    }
    static void VerifyKeyframes()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var pick=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var intro=Ref<MapIntroPresentation>(pick,"mapIntro");var backdrop=Ref<PickMapBackdrop>(pick,"mapBackdrop");
        var original=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var canvas=backdrop.GetComponentInParent<Canvas>();
        for(int map=0;map<2;map++)
        {
            string name=map==0?"TestMap":"Desert";backdrop.ApplyMap(name);intro.Begin(new PickStateData{roomId=(uint)map+51,mapName=name});
            foreach(int frame in new[]{0,12,36,102,194})
            {
                intro.Evaluate(frame/204f);
                if(frame==0 && intro.captionReveal.alpha!=0)throw new Exception("Map title obscures establishing frame");
                if(frame==102 && intro.captionReveal.alpha<.99f)throw new Exception("Map title not revealed");
                var rp=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var qp=QualitySettings.renderPipeline;
                try{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;intro.overviewCamera.Render();}
                finally{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.renderPipeline=qp;}
                VectorExperiencePolishEditor.CaptureCanvas(canvas,$"VectorFlow-{name}-Frame{frame:000}.png");
            }
            intro.Finish();if(intro.overviewCamera.enabled || intro.overlay.activeSelf)throw new Exception("Map overlay leaked into pick");
        }
        // TMP creates renderer submeshes for fallback glyphs; those are not new authored UI controls.
        var added=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Except(original)
            .Where(t=>t.GetComponent<TMP_SubMeshUI>()==null).ToArray();
        if(added.Length>0)throw new Exception("Presentation added objects: "+string.Join(",",added.Select(t=>t.name+" ["+string.Join("/",t.GetComponents<Component>().Select(c=>c.GetType().Name))+"]")));
        Debug.Log("MATCH_FLOW_KEYFRAMES_VERIFIED: both maps at frames 0/12/36/102/194, 60fps timing, no runtime objects.");
    }
}
