using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VectorSettingsAuthoring
{
    const string Folder="Assets/ProjectVector/Shared/UI/Settings";
    const string Prefab=Folder+"/VectorSettings.prefab";
    static TMP_FontAsset font;
    static Color Dark=new Color(.12f,.13f,.15f),Control=new Color(.25f,.28f,.32f),Red=new Color(.76f,.13f,.2f),Paper=new Color(.76f,.79f,.82f),Ink=new Color(.1f,.11f,.13f);
    static VectorSettingsPanel panel;
    static List<VectorSettingRow> rows;

    [MenuItem("Project Vector/Authoring/Install shared settings and lobby utilities")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before authoring.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"/SettingsFont.asset");
        if(font==null){font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/Pretendard-Regular.otf"));font.name="SettingsFont";font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(font,Folder+"/SettingsFont.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var tex in font.atlasTextures)AssetDatabase.AddObjectToAsset(tex,font);}
        var root=new GameObject("VectorSettings",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(VectorSettingsPanel));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
        var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        panel=root.GetComponent<VectorSettingsPanel>();rows=new List<VectorSettingRow>();
        panel.backdrop=Rect(root.transform,"Blurred lobby",0,0,1920,1080).gameObject.AddComponent<RawImage>();panel.backdrop.color=Color.white;
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Backdrop.mat");if(!mat){mat=new Material(Shader.Find("ProjectVector/SettingsBackdrop"));AssetDatabase.CreateAsset(mat,Folder+"/Backdrop.mat");}panel.blurMaterial=mat;
        BuildSettings(root.transform);BuildMenu(root.transform);BuildNotices(root.transform);BuildForms(root.transform);BuildConfirmation(root.transform);
        panel.utilityStatus=Text(root.transform,"Utility status","",150,930,1620,60,24,Color.white);panel.utilityStatus.alignment=TextAlignmentOptions.Center;
        panel.performance=Text(root.transform,"Performance","",1540,7,360,30,18,Color.white);panel.performance.alignment=TextAlignmentOptions.MidlineRight;
        panel.rows=rows.ToArray();
        foreach(var g in new[]{panel.settings,panel.menu,panel.notices,panel.redeem,panel.support,panel.displayConfirm,panel.backdrop.gameObject,panel.utilityStatus.gameObject,panel.performance.gameObject})g.SetActive(false);
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Prefab);UnityEngine.Object.DestroyImmediate(root);
        foreach(var scene in EditorBuildSettings.scenes.Where(s=>s.enabled)) {
            var loaded=EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);
            var existing=loaded.GetRootGameObjects().FirstOrDefault(g=>g.GetComponent<VectorSettingsPanel>()!=null);
            if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,loaded);
            if(scene.path.EndsWith("/Lobby.unity"))BindLobby(loaded,instance.GetComponent<VectorSettingsPanel>());
            foreach(var g in loaded.GetRootGameObjects())foreach(var t in g.GetComponentsInChildren<Transform>(true))if(t.name=="CrossHair") {
                var graphic=t.GetComponent<Graphic>();if(graphic!=null)graphic.enabled=false;
                var child=t.Find("Vector Crosshair");if(child==null){var rt=Rect(t,"Vector Crosshair",0,0,200,200);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.gameObject.AddComponent<VectorCrosshairGraphic>();}else if(!child.GetComponent<CanvasRenderer>())child.gameObject.AddComponent<CanvasRenderer>();
            }
            EditorSceneManager.SaveScene(loaded);
        }
        AssetDatabase.SaveAssets();Debug.Log("VECTOR_SETTINGS_AUTHORING_OK");
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c){var r=Rect(p,n,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=c;return i;}
    static TMP_Text Text(Transform p,string n,string s,float x,float y,float w,float h,int size,Color c)
    {var r=Rect(p,n,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=s;t.fontSize=size;t.color=c;t.raycastTarget=false;t.richText=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
    static Button Button(Transform p,string n,string label,float x,float y,float w,float h,Color color,UnityAction action=null)
    {var i=Box(p,n,x,y,w,h,color);var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;var cb=b.colors;cb.highlightedColor=new Color(1.13f,1.13f,1.13f);cb.pressedColor=new Color(.8f,.8f,.8f);b.colors=cb;var t=Text(i.transform,"Label",label,12,3,w-24,h-6,24,Color.white);t.alignment=TextAlignmentOptions.Center;if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
    static TMP_InputField Input(Transform p,string n,string hint,float x,float y,float w,float h,int limit,bool multiline=false)
    {
        var bg=Box(p,n,x,y,w,h,Control);var f=bg.gameObject.AddComponent<TMP_InputField>();var viewport=Rect(bg.transform,"Text viewport",12,8,w-24,h-16);viewport.gameObject.AddComponent<RectMask2D>();
        var text=Text(viewport,"Text","",0,0,w-24,h-16,24,Color.white);text.overflowMode=TextOverflowModes.Overflow;text.alignment=multiline?TextAlignmentOptions.TopLeft:TextAlignmentOptions.MidlineLeft;
        var placeholder=Text(viewport,"Placeholder",hint,0,0,w-24,h-16,22,new Color(.75f,.77f,.8f));placeholder.alignment=text.alignment;
        f.textViewport=viewport;f.textComponent=(TextMeshProUGUI)text;f.placeholder=placeholder;f.characterLimit=limit;f.lineType=multiline?TMP_InputField.LineType.MultiLineNewline:TMP_InputField.LineType.SingleLine;f.targetGraphic=bg;return f;
    }
    static ScrollRect Scroll(Transform p,string name,float x,float y,float w,float h,float contentHeight,out RectTransform content)
    {
        var r=Rect(p,name,x,y,w,h);var scroll=r.gameObject.AddComponent<ScrollRect>();var viewport=Box(r,"Viewport",0,0,w-16,h,new Color(.5f,.55f,.6f,.04f));viewport.gameObject.AddComponent<RectMask2D>();content=Rect(viewport.transform,"Content",0,0,w-16,contentHeight);
        scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=48;scroll.movementType=ScrollRect.MovementType.Clamped;
        var rail=Box(r,"Scrollbar",w-8,0,6,h,new Color(.25f,.28f,.32f,.25f));var bar=rail.gameObject.AddComponent<Scrollbar>();var handle=Box(rail.transform,"Handle",0,0,6,h,Control);handle.rectTransform.anchorMin=Vector2.zero;handle.rectTransform.anchorMax=Vector2.one;handle.rectTransform.offsetMin=Vector2.zero;handle.rectTransform.offsetMax=Vector2.zero;bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;return scroll;
    }
    static void Section(Transform parent,string name,float y,float width){Box(parent,"Accent",0,y+11,6,30,Red);Text(parent,"Section",name,20,y,width-20,52,32,Ink);}
    static void BuildSettings(Transform root)
    {
        panel.settings=Box(root,"Settings",0,0,1920,1080,Paper).gameObject;
        Box(panel.settings.transform,"Header",0,0,1920,82,Dark);Text(panel.settings.transform,"Title","설정",70,10,160,60,36,Color.white);
        panel.tabs=new Button[6];panel.pages=new GameObject[6];
        for(int tab=0;tab<6;tab++){
            panel.tabs[tab]=Button(panel.settings.transform,"Tab "+tab,VectorSettingsData.Tabs[tab],260+tab*225,0,220,82,Dark);
            float height=0;string section="";var definitions=VectorSettingsData.All.Where(d=>d.tab==tab).ToArray();
            if(tab==1)height=72*VectorInput.Actions.Length+120;else foreach(var d in definitions){if(d.section!=section){height+=62;section=d.section;}height+=76;}
            if(tab==5)height+=200;
            var scroll=Scroll(panel.settings.transform,"Page "+VectorSettingsData.Tabs[tab],70,145,1090,815,height,out var content);panel.pages[tab]=scroll.gameObject;
            float y=0;section="";
            if(tab==1){Section(content,"이동 · 전투 · 스킬 · 음성",0,1060);y=62;for(int i=0;i<VectorInput.Actions.Length;i++){BindingRow(content,i,y);y+=72;}}
            else foreach(var d in definitions){if(section!=d.section){Section(content,d.section,y,1060);y+=62;section=d.section;}SettingRow(content,d,y);y+=76;}
            if(tab==5){panel.voiceDevices=Text(content,"Voice devices","음성 연결 대기",10,y,1040,90,23,Ink);panel.inputDevice=Button(content,"Input device","입력 장치 변경",10,y+100,490,54,Control);panel.outputDevice=Button(content,"Output device","출력 장치 변경",530,y+100,490,54,Control);}
        }
        var help=Box(panel.settings.transform,"Description",1215,145,635,815,new Color(.64f,.68f,.72f,.35f));
        Box(help.transform,"Rule",30,32,48,4,Red);panel.descriptionTitle=Text(help.transform,"Title","",30,60,575,100,39,Ink);
        panel.descriptionBody=Text(help.transform,"Body","",30,185,575,550,25,Ink);panel.descriptionBody.alignment=TextAlignmentOptions.TopLeft;
        // The preview and share controls belong to the crosshair page but remain on the description side.
        var page3=panel.pages[3].transform;
        var preview=Box(page3,"Crosshair preview",1175,440,595,205,Dark);var caption=Text(preview.transform,"Caption","조준선 미리보기",18,12,560,35,22,Color.white);
        var graphic=Rect(preview.transform,"Crosshair",198,20,200,180).gameObject.AddComponent<VectorCrosshairGraphic>();graphic.preview=true;
        panel.crosshairCode=Input(page3,"Crosshair code","PV1- 조준선 코드를 붙여 넣으세요",1175,660,595,52,160);
        panel.copyCode=Button(page3,"Copy code","코드 복사",1175,727,287,54,Control);panel.importCode=Button(page3,"Import code","가져오기",1483,727,287,54,Red);
        // Draw the crosshair preview above the description surface without creating a runtime view.
        help.transform.SetSiblingIndex(2);
        panel.status=Text(panel.settings.transform,"Status","",70,990,1070,55,19,Ink);
        panel.apply=Button(panel.settings.transform,"Apply","적용",1215,995,190,55,Dark);panel.defaults=Button(panel.settings.transform,"Defaults","현재 탭 기본값",1420,995,215,55,Control);panel.back=Button(panel.settings.transform,"Back","ESC  뒤로",1650,995,200,55,Dark);
    }
    static void BindingRow(Transform parent,int index,float y)
    {
        var bg=Box(parent,"Binding "+VectorInput.Actions[index],0,y,1065,67,new Color(.45f,.5f,.55f,.13f));var row=bg.gameObject.AddComponent<VectorSettingRow>();row.key=VectorInput.Actions[index];row.binding=true;row.selected=bg;
        row.title=Text(bg.transform,"Title",VectorInput.Labels[index],12,8,450,50,25,Ink);row.capture=Button(bg.transform,"Bind","",475,8,575,50,Control);row.value=row.capture.GetComponentInChildren<TMP_Text>();rows.Add(row);
    }
    static void SettingRow(Transform parent,VectorSetting d,float y)
    {
        var bg=Box(parent,d.key,0,y,1065,71,new Color(.45f,.5f,.55f,.13f));var row=bg.gameObject.AddComponent<VectorSettingRow>();row.key=d.key;row.selected=bg;
        row.title=Text(bg.transform,"Title",d.title,12,8,440,55,25,Ink);
        row.previous=Button(bg.transform,"Previous",d.choices!=null?"‹":"−",470,10,48,50,new Color(.37f,.41f,.46f));
        row.next=Button(bg.transform,"Next",d.choices!=null?"›":"+",1005,10,48,50,new Color(.37f,.41f,.46f));
        if(d.choices!=null){
            var box=Box(bg.transform,"Value",530,10,463,50,Control);row.value=Text(box.transform,"Label",d.choices[(int)d.initial],8,0,447,48,25,Color.white);row.value.alignment=TextAlignmentOptions.Center;
            row.choiceMarks=new Image[d.choices.Length];float segment=(463f-3*(d.choices.Length-1))/d.choices.Length;
            for(int i=0;i<d.choices.Length;i++)row.choiceMarks[i]=Box(bg.transform,"Choice "+i,530+i*(segment+3),64,segment,4,i==(int)d.initial?new Color(.94f,.23f,.32f):Control);
        }
        else {
            var track=Box(bg.transform,"Slider",530,10,350,50,Control);track.gameObject.AddComponent<RectMask2D>();var slider=track.gameObject.AddComponent<Slider>();var fill=Box(track.transform,"Fill",0,0,350,50,Red);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=Vector2.zero;fill.rectTransform.offsetMax=Vector2.zero;slider.fillRect=fill.rectTransform;var handle=Box(track.transform,"Handle",0,0,4,50,Color.white);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=d.min;slider.maxValue=d.max;slider.value=d.initial;slider.direction=Slider.Direction.LeftToRight;row.slider=slider;
            row.number=Input(bg.transform,"Number","",890,10,103,50,12);row.number.contentType=TMP_InputField.ContentType.DecimalNumber;row.number.text=d.initial.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        rows.Add(row);
    }
    static GameObject Window(Transform root,string name,string title,float x,float y,float w,float h)
    {
        var bg=Box(root,name,x,y,w,h,Paper);Box(bg.transform,"Header",0,0,w,86,Dark);Box(bg.transform,"Accent",0,84,w,4,Red);Text(bg.transform,"Title",title,32,12,w-160,62,34,Color.white);Button(bg.transform,"Close","×",w-85,10,65,62,Dark,panel.Close);return bg.gameObject;
    }
    static void BuildMenu(Transform root)
    {
        panel.menu=Box(root,"Lobby menu",1410,0,510,1080,new Color(.08f,.09f,.12f,.98f)).gameObject;
        Text(panel.menu.transform,"Title","PROJECT VECTOR",46,92,410,55,30,Color.white);Box(panel.menu.transform,"Accent",46,164,70,5,Red);
        string[] names={"시스템 설정","공지","교환 코드","고객센터","게임 종료"};UnityAction[] actions={panel.OpenSettings,panel.OpenNotices,panel.OpenRedeem,panel.OpenSupport,panel.QuitGame};
        for(int i=0;i<names.Length;i++)Button(panel.menu.transform,"Menu "+i,names[i],46,235+i*116,418,78,i==4?new Color(.45f,.11f,.16f):Control,actions[i]);
        Button(panel.menu.transform,"Back","ESC  뒤로",46,940,418,60,Dark,panel.Close);
    }
    static void BuildNotices(Transform root)
    {
        panel.notices=Window(root,"Notices","공지",90,60,1740,930);
        Scroll(panel.notices.transform,"Notice list",28,115,450,750,16*118,out var list);
        // Layout group collapses authored unused slots when the server returns fewer entries.
        var layout=list.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=16;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=true;
        var fit=list.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        panel.noticeButtons=new Button[16];for(int i=0;i<16;i++){var b=Button(list,"Notice "+i,"공지",0,i*118,425,102,Control);var e=b.gameObject.AddComponent<LayoutElement>();e.preferredHeight=102;panel.noticeButtons[i]=b;}
        var body=Box(panel.notices.transform,"Article",505,115,1205,750,new Color(.91f,.92f,.93f));
        var header=Box(body.transform,"Article heading",20,20,1165,92,Control);panel.noticeTitle=Text(header.transform,"Title","공지",22,10,1120,72,30,Color.white);
        panel.noticeScroll=Scroll(body.transform,"Article scroll",28,140,1148,585,585,out var content);
        panel.noticeBody=Text(content,"Body","",0,0,1115,580,27,Ink);panel.noticeBody.alignment=TextAlignmentOptions.TopLeft;panel.noticeBody.overflowMode=TextOverflowModes.Overflow;
        var textFit=panel.noticeBody.gameObject.AddComponent<ContentSizeFitter>();textFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var bodyLayout=content.gameObject.AddComponent<VerticalLayoutGroup>();bodyLayout.childControlHeight=true;bodyLayout.childForceExpandHeight=false;bodyLayout.childControlWidth=true;content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
    }
    static void BuildForms(Transform root)
    {
        panel.redeem=Window(root,"Redeem","교환 코드",460,290,1000,410);Text(panel.redeem.transform,"Help","코드를 입력하면 보상이 계정에 지급됩니다.",44,115,912,65,27,Ink);
        panel.redeemCode=Input(panel.redeem.transform,"Code","교환 코드 입력",44,203,912,64,48);panel.redeemSubmit=Button(panel.redeem.transform,"Submit","교환",710,300,246,62,Red);
        panel.support=Window(root,"Support","고객센터",360,130,1200,760);Text(panel.support.transform,"Help","오류 제보와 의견을 보내 주세요.",40,110,1120,50,26,Ink);
        panel.supportTitle=Input(panel.support.transform,"Title","제목 (최대 80자)",40,182,1120,62,80);panel.supportBody=Input(panel.support.transform,"Body","발생 상황, 재현 방법, 기대한 동작을 알려 주세요. (최대 1,500자)",40,265,1120,345,1500,true);
        panel.supportSubmit=Button(panel.support.transform,"Submit","문의 보내기",850,650,310,62,Red);
    }
    static void BuildConfirmation(Transform root)
    {
        panel.displayConfirm=Box(root,"Confirm display",0,0,1920,1080,new Color(0,0,0,.8f)).gameObject;var box=Box(panel.displayConfirm.transform,"Dialog",510,360,900,320,Dark);
        panel.confirmText=Text(box.transform,"Question","",40,44,820,110,30,Color.white);panel.keepDisplay=Button(box.transform,"Keep","유지",45,210,390,65,Red);panel.revertDisplay=Button(box.transform,"Revert","복원",465,210,390,65,Control);
    }
    static Sprite Sprite(string name)
    {
        string path=Folder+"/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)return null;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void BindLobby(Scene scene,VectorSettingsPanel instance)
    {
        Transform Find(string path){foreach(var root in scene.GetRootGameObjects()){if(path==root.name)return root.transform;if(path.StartsWith(root.name+"/")){var t=root.transform.Find(path.Substring(root.name.Length+1));if(t)return t;}}return null;}
        const string nav="Canvas/Profile and Navigation/NavigationPanel/LobbyMainButtons/";
        foreach(var pair in new[]{("SettingButton","menu"),("FriendButton","friend"),("MailButton","mail")}){var t=Find(nav+pair.Item1);if(!t)throw new Exception("Missing lobby button "+pair.Item1);t.GetComponent<Image>().sprite=Sprite(pair.Item2);t.GetComponent<Image>().preserveAspect=true;if(pair.Item1=="SettingButton"){var b=t.GetComponent<Button>();for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)if(b.onClick.GetPersistentMethodName(i)=="OpenMenu")UnityEventTools.RemovePersistentListener(b.onClick,i);var binder=t.GetComponent<VectorLobbyMenuButton>();if(!binder)binder=t.gameObject.AddComponent<VectorLobbyMenuButton>();UnityEventTools.AddPersistentListener(b.onClick,binder.OpenMenu);}}
        foreach(var path in new[]{"Canvas/LobbyPanel/CharacterSelectPopup","Canvas/LobbyPanel/BackgroundSelectPopup"}){var t=Find(path);if(!t)throw new Exception("Missing popup "+path);var image=t.GetComponent<Image>();image.sprite=Sprite("popup");image.type=Image.Type.Simple;image.color=Color.white;}
        for(int i=0;i<2;i++){var t=Find("Canvas/Store Panel/Money Group/Money "+i);if(!t)throw new Exception("Missing wallet text");if(t.Find("Currency icon")==null){var img=Box(t,"Currency icon",-40,0,34,34,Color.white);img.sprite=Sprite(i==0?"coin":"ticket");img.preserveAspect=true;}}
    }
}
