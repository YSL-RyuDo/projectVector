#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CombatReportAuthoring
{
    public const string Output="C:/Users/GCT/Documents/Vector-Report-20261029";
    static TMP_FontAsset font; static Material fontMaterial,ink; static EquipmentIconCatalog icons;
    static readonly Color Back=new(.065f,.095f,.13f,.96f), Surface=new(.12f,.16f,.205f,.97f), Muted=new(.68f,.76f,.82f), Ice=new(.64f,.88f,1), Red=new(1,.56f,.53f);
    static T Get<T>(UnityEngine.Object o,string name) where T:UnityEngine.Object => ShopReadabilityAuthoring.Get<T>(o,name);
    static void Ref(UnityEngine.Object o,string name,UnityEngine.Object value)=>ShopReadabilityAuthoring.Ref(o,name,value);
    static GameObject Node(Transform p,string name){var t=p.Find(name);if(t)return t.gameObject;var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);return g;}
    static void At(Transform t,Transform p,float x,float y,float w,float h)
    {
        t.SetParent(p,false);foreach(var l in t.GetComponents<LayoutGroup>())l.enabled=false;
        foreach(var f in t.GetComponents<ContentSizeFitter>())f.enabled=false;
        var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition3D=new Vector3(x,-y,0);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
    }
    static Image Box(Transform p,string name,float x,float y,float w,float h,Color color)
    {
        var g=Node(p,name);At(g.transform,p,x,y,w,h);var i=g.GetComponent<Image>()??g.AddComponent<Image>();i.sprite=null;i.material=null;i.color=color;i.raycastTarget=false;return i;
    }
    static TMP_Text Style(TMP_Text t,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {t.font=font;t.fontSharedMaterial=fontMaterial;t.fontSize=size;t.enableAutoSizing=false;t.color=color;t.alignment=align;t.richText=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;return t;}
    static TMP_Text Text(Transform p,string name,string value,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {var g=Node(p,name);var t=g.GetComponent<TMP_Text>()??g.AddComponent<TextMeshProUGUI>();At(t.transform,p,x,y,w,h);Style(t,size,color,align);t.text=value;return t;}
    static void MoveText(TMP_Text t,Transform p,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {At(t.transform,p,x,y,w,h);Style(t,size,color,align);}
    static Button Button(Transform p,string name,string label,float x,float y,float w,float h)
    {var i=Box(p,name,x,y,w,h,new Color(.19f,.26f,.33f));i.raycastTarget=true;var b=i.GetComponent<Button>()??i.gameObject.AddComponent<Button>();b.targetGraphic=i;Text(i.transform,"Label",label,0,0,w,h,15,Color.white,TextAlignmentOptions.Center);return b;}
    static void Transparent(GameObject obj){var i=obj.GetComponent<Image>();if(i){i.color=Color.clear;i.raycastTarget=false;}foreach(var l in obj.GetComponents<LayoutGroup>())l.enabled=false;foreach(var f in obj.GetComponents<ContentSizeFitter>())f.enabled=false;}

    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor batch only");
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var source=UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.font);
        font=source.font;fontMaterial=source.fontSharedMaterial;icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        ink=AssetDatabase.LoadAssetAtPath<Material>(EquipmentIconsAuthoring.Folder+"/EquipmentInk.mat");
        var report=UnityEngine.Object.FindFirstObjectByType<ReportUI>(FindObjectsInactive.Include);
        if(!report)throw new Exception("Existing report controller missing");
        AuthorReport(report);AuthorSpectator(report);
        var top=UnityEngine.Object.FindFirstObjectByType<TopInfoUI>(FindObjectsInactive.Include);
        foreach(string name in new[]{"OurTeamListRoot/Viewport","EnemyTeamListRoot/Viewport"}) {
            var viewport=top.transform.Find(name);if(!viewport)continue;
            var graphic=viewport.GetComponent<Graphic>();if(graphic){graphic.color=Color.clear;graphic.raycastTarget=false;}
            var mask=viewport.GetComponent<Mask>();if(mask)mask.showMaskGraphic=false;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("REPORT_AUTHORING_PASS");
        CombatReportVerification.Run();
    }
    static void AuthorReport(ReportUI report)
    {
        var root=Get<GameObject>(report,"rootPanel");Transparent(root);
        var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition3D=new Vector3(56,180,0);rect.sizeDelta=new Vector2(400,280);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
        report.presentation=root.GetComponent<CanvasGroup>()??root.AddComponent<CanvasGroup>();
        var header=Box(root.transform,"ReportHeader",0,0,400,54,Back);
        Box(header.transform,"Accent",0,0,3,54,Ice);
        Text(header.transform,"Title","전투 리포트",16,10,210,32,23,Color.white);
        var toggle=Get<Button>(report,"toggleExpandButton");At(toggle.transform,header.transform,282,12,104,30);
        var toggleLabel=Get<TMP_Text>(report,"toggleButtonText");MoveText(toggleLabel,toggle.transform,0,0,104,30,15,Ice,TextAlignmentOptions.Center);
        toggle.GetComponent<Image>().sprite=null;toggle.GetComponent<Image>().color=new Color(.19f,.26f,.33f);toggle.colors=ColorBlock.defaultColorBlock;
        var summary=Get<GameObject>(report,"summaryView");At(summary.transform,root.transform,0,58,400,222);summary.GetComponent<Image>().color=Back;summary.GetComponent<Image>().sprite=null;
        MoveText(Get<TMP_Text>(report,"opponentInfoText"),summary.transform,16,10,260,30,22,Color.white);
        report.killerLabel=Text(summary.transform,"KillerLabel","나를 처치한 상대",16,42,260,21,14,Red);
        report.previousOpponent=Button(summary.transform,"PreviousOpponent","[",286,17,27,29);
        report.opponentCounter=Text(summary.transform,"OpponentCount","1 / 3",315,17,48,29,14,Muted,TextAlignmentOptions.Center);
        report.nextOpponent=Button(summary.transform,"NextOpponent","]",365,17,27,29);
        Box(summary.transform,"GivenPanel",12,76,184,114,Surface);Box(summary.transform,"TakenPanel",204,76,184,114,Surface);
        Text(summary.transform,"GivenHeading","준 피해",24,84,148,22,15,Ice);Text(summary.transform,"TakenHeading","받은 피해",216,84,148,22,15,Red);
        report.dealtValue=Text(summary.transform,"GivenValue","0",24,106,148,42,35,Color.white);
        report.takenValue=Text(summary.transform,"TakenValue","0",216,106,148,42,35,Color.white);
        Text(summary.transform,"GivenRegions","머리 / 몸통 / 다리",24,149,158,18,12,Muted);
        Text(summary.transform,"TakenRegions","머리 / 몸통 / 다리",216,149,158,18,12,Muted);
        MoveText(Get<TMP_Text>(report,"hitsDealtText"),summary.transform,24,168,158,20,16,Color.white);
        MoveText(Get<TMP_Text>(report,"hitsTakenText"),summary.transform,216,168,158,20,16,Color.white);
        MoveText(Get<TMP_Text>(report,"damageSummaryText"),summary.transform,16,197,368,18,12,Muted);
        var detail=Get<GameObject>(report,"detailView");At(detail.transform,root.transform,0,-334,400,330);Transparent(detail);
        Box(detail.transform,"DetailBacking",0,0,400,330,Back).transform.SetAsFirstSibling();
        Text(detail.transform,"DetailHeading","무기 / 스킬별 기록",16,8,248,26,16,Color.white);
        var container=Get<Transform>(report,"detailItemContainer");
        // Preserve the old scroll viewport and prefab references; re-use its content as a saved row pool.
        foreach(var scroll in detail.GetComponentsInChildren<ScrollRect>(true))scroll.enabled=false;
        foreach(var mask in detail.GetComponentsInChildren<Mask>(true))mask.enabled=false;
        foreach(var mask in detail.GetComponentsInChildren<RectMask2D>(true))mask.enabled=false;
        At(container,detail.transform,12,36,376,248);Transparent(container.gameObject);
        report.detailRows=new CombatReportDetailItemUI[4];
        for(int i=0;i<4;i++){
            string name="SavedReportRow"+i;var t=container.Find(name);
            var g=t?t.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(Get<GameObject>(report,"detailItemPrefab"),container);
            g.name=name;At(g.transform,container,0,i*62,376,58);
            var row=g.GetComponent<CombatReportDetailItemUI>();report.detailRows[i]=row;row.icons=icons;
            Box(g.transform,"RowBackground",0,0,376,58,Surface).transform.SetAsFirstSibling();
            var so=new SerializedObject(row);
            foreach(string kind in new[]{"weapon","skill"}) {
                GameObject group=(GameObject)so.FindProperty(kind+".root").objectReferenceValue;At(group.transform,g.transform,0,0,376,58);Transparent(group);
                TMP_Text Read(string field)=>(TMP_Text)so.FindProperty(kind+"."+field).objectReferenceValue;
                MoveText(Read("sourceNameText"),group.transform,12,3,218,22,17,Color.white);
                MoveText(Read("totalDamageText"),group.transform,292,18,70,32,26,Color.white,TextAlignmentOptions.MidlineRight);
                MoveText(Read("damageBreakdownText"),group.transform,12,25,268,18,13,Muted);
                MoveText(Read("hitCountText"),group.transform,12,41,270,17,12,Muted);
            }
            row.directionText=(TextMeshProUGUI)Text(g.transform,"Direction","",282,0,80,20,12,Ice,TextAlignmentOptions.MidlineRight);
            g.SetActive(false);
        }
        // Retire only this report's old preview chrome, preserving its original objects and references.
        foreach(var preview in root.GetComponentsInChildren<CombatReportDetailItemUI>(true))
            if(!report.detailRows.Contains(preview))preview.gameObject.SetActive(false);
        foreach(var scroll in detail.GetComponentsInChildren<ScrollRect>(true))scroll.gameObject.SetActive(false);
        foreach(var title in root.GetComponentsInChildren<TMP_Text>(true))if(title.text=="Report")title.gameObject.SetActive(false);
        report.previousPage=Button(detail.transform,"PreviousPage","<",12,292,28,26);
        report.pageCounter=Text(detail.transform,"PageCount","1 / 1",44,292,60,26,14,Muted,TextAlignmentOptions.Center);
        report.nextPage=Button(detail.transform,"NextPage",">",108,292,28,26);
        Text(detail.transform,"WheelHint","휠  상세 넘기기",160,292,224,26,13,Muted,TextAlignmentOptions.MidlineRight);
        foreach(var p in new[]{("emptyNotice","이번 생명에서 교전 기록이 없습니다"),("failedNotice","기록을 불러오지 못했습니다")}){
            var notice=Get<GameObject>(report,p.Item1);At(notice.transform,root.transform,0,58,400,108);Transparent(notice);
            var ts=notice.GetComponentsInChildren<TMP_Text>(true);foreach(var t in ts){MoveText(t,notice.transform,16,16,368,54,16,Muted);t.text=p.Item2;}
            Box(notice.transform,"NoticeBacking",0,0,400,108,Back).transform.SetAsFirstSibling();notice.SetActive(false);
        }
        report.loadingText=Text(root.transform,"Loading","전투 기록 확인 중",16,78,368,30,16,Muted);
        report.retryButton=Button(root.transform,"Retry","R  다시 불러오기",16,120,180,28);
        var warning=Text(root.transform,"ReportLimit","기록이 많아 일부 항목만 표시됩니다",8,282,384,22,12,Muted);Ref(report,"limitWarningNotice",warning.gameObject);
        var visibility=root.GetComponent<CanvasGroup>();if(!visibility)visibility=root.AddComponent<CanvasGroup>();
        Ref(report,"presentation",visibility);
        report.CloseAndReset();EditorUtility.SetDirty(report);
    }
    static void AuthorSpectator(ReportUI report)
    {
        var canvas=report.GetComponentInParent<Canvas>();if(!canvas)throw new Exception("Report canvas missing");
        var owner=Node(canvas.transform,"SpectatorHUD");At(owner.transform,canvas.transform,0,0,1920,1080);
        var ownerRect=(RectTransform)owner.transform;ownerRect.anchorMin=Vector2.zero;ownerRect.anchorMax=Vector2.one;ownerRect.offsetMin=ownerRect.offsetMax=Vector2.zero;
        var ui=owner.GetComponent<SpectatorHUD>()??owner.AddComponent<SpectatorHUD>();
        // Skills end at y=168, reload notice begins at y=304 (1080 reference).
        // Keep the whole spectator card between them, including its controls.
        var panel=Box(owner.transform,"SpectatedAlly",0,0,540,94,Back);var r=panel.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,196);
        ui.root=panel.gameObject;ui.presentation=panel.GetComponent<CanvasGroup>()??panel.gameObject.AddComponent<CanvasGroup>();ui.icons=icons;
        Box(panel.transform,"Accent",0,0,3,94,Ice);
        ui.portrait=Box(panel.transform,"Portrait",14,14,56,66,Color.white);ui.portrait.preserveAspect=true;
        ui.state=Text(panel.transform,"State","아군 관전",82,5,174,20,14,Ice);
        ui.playerName=Text(panel.transform,"PlayerName","",82,27,296,32,26,Color.white);
        ui.weapon=Box(panel.transform,"Weapon",390,27,130,32,Color.white);ui.weapon.material=ink;ui.weapon.preserveAspect=true;
        ui.weaponName=Text(panel.transform,"WeaponName","",390,63,130,20,14,Muted,TextAlignmentOptions.MidlineRight);
        ui.ultimate=Text(panel.transform,"Ultimate","",82,63,296,20,14,Muted);
        ui.controls=Text(panel.transform,"Controls","",260,5,266,20,13,Muted,TextAlignmentOptions.MidlineRight);
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        ui.portraits=Enumerable.Range(0,4).Select(id=>pick.GetCharacterData(id).characterImage).ToArray();
        ui.root.SetActive(false);EditorUtility.SetDirty(ui);
    }
    public static void RefineSpectatorAndBuild()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor batch only");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var source=UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.font);
        font=source.font;fontMaterial=source.fontSharedMaterial;icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        ink=AssetDatabase.LoadAssetAtPath<Material>(EquipmentIconsAuthoring.Folder+"/EquipmentInk.mat");
        var pingPrefab=PrefabUtility.LoadPrefabContents(PingAuthoring.PrefabPath);
        var hint=pingPrefab.GetComponent<Game.Pings.PingManager>().inputHint.GetComponent<RectTransform>();
        hint.anchoredPosition=new Vector2(38,-310);
        PrefabUtility.SaveAsPrefabAsset(pingPrefab,PingAuthoring.PrefabPath);PrefabUtility.UnloadPrefabContents(pingPrefab);
        AuthorSpectator(UnityEngine.Object.FindFirstObjectByType<ReportUI>(FindObjectsInactive.Include));
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CombatReportVerification.Run();RankedCaptureTestEditor.Build();
    }
}
#endif
