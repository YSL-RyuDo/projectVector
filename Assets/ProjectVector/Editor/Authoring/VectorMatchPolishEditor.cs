using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static partial class VectorMatchLayoutEditor
{
    static Sprite Art(string path)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/"+path+".png");
        if(sprite==null)
        {
            EnsureArtFolder();
            string copy="Assets/ProjectVector/Modules/Match/UI/MatchAuthored/"+System.IO.Path.GetFileName(path)+".png";
            if(!System.IO.File.Exists(copy))System.IO.File.Copy("Assets/"+path+".png",copy);
            AssetDatabase.ImportAsset(copy,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(copy);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            sprite=AssetDatabase.LoadAssetAtPath<Sprite>(copy);
        }
        if(sprite==null)throw new Exception("Missing UI sprite: "+path);
        return sprite;
    }
    static void EnsureArtFolder()
    {

        ProjectEditorPaths.EnsureFolder("Assets/ProjectVector/Modules/Match/UI/MatchAuthored");
    }
    static Sprite ReferenceSlice(string name,float x,float y,float w,float h)
    {
        EnsureArtFolder();
        const string image="Assets/ProjectVector/Modules/Match/UI/MatchAuthored/StrinovaMatchReference.png";
        if(!System.IO.File.Exists(image))System.IO.File.Copy("C:/Users/GCT/AppData/Local/Temp/codex-clipboard-31ccbb3c-fcd7-41ca-86cf-a4cecbc5e969.png",image);
        AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);
        string asset="Assets/ProjectVector/Modules/Match/UI/MatchAuthored/"+name+".asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(sprite!=null)return sprite;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(image);
        // Sprite rectangles retain the untouched reference screenshot; replace this asset later.
        var rect=new UnityEngine.Rect(x/2048*texture.width,(1-(y+h)/1152)*texture.height,w/2048*texture.width,h/1152*texture.height);
        sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,asset);return sprite;
    }
    static Image Decor(Transform p,string name,float x,float y,float w,float h,string asset,Color color)
    {
        var image=Box(p,name,x,y,w,h,color);image.sprite=Art(asset);return image;
    }
    static void AssetButton(Button button)
    {
        // LeaveButton is a dark texture: use it as subtle trim, not multiplied into red.
        button.image.sprite=null;
        RemoveChild(button.transform,"AngledAssetTrim");
        var trim=Box(button.transform,"AngledAssetTrim",0,0,1,1,new Color(1,1,1,.22f));
        trim.sprite=Art("ProjectVector/Modules/Results/UI/Sprites/LeaveButton");Stretch(trim.transform);trim.transform.SetAsFirstSibling();
    }
    static void RemoveChild(Transform parent,string name)
    {
        var child=parent.Find(name);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);
    }
    [MenuItem("Project Vector/UI/Polish Saved Match UI")]
    public static void Polish()
    {
        var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        if(EditorApplication.isPlayingOrWillChangePlaymode||(existing.IsValid()&&existing.isDirty))throw new Exception("Save and stop editing first");
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        System.IO.File.Copy("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity",System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Lobby-before-match-polish-"+DateTime.Now.ToString("yyyyMMddHHmmss")+".unity"));
        var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GamePanelController>(true)).Single();
        var game=Ref<GameObject>(c,"gamePanel").transform;
        font=Ref<Button>(c,"normalGameButton").GetComponentInChildren<TMP_Text>().font;

        // Keep the existing art at low contrast; no large circuit lines through labels.
        var bg=game.Find("GameImage").GetComponent<Image>();
        bg.color=new Color32(185,196,209,255);
        RemoveChild(game,"MatchBackdrop");
        var backdrop=Box(game,"MatchBackdrop",0,0,1920,1080,Color.white);
        backdrop.transform.SetSiblingIndex(bg.transform.GetSiblingIndex()+1);
        var gradient=backdrop.gameObject.AddComponent<VectorUIFade>();
        gradient.top=new Color(.73f,.78f,.83f,.93f);gradient.bottom=new Color(.9f,.93f,.95f,.97f);

        var ready=Ref<GameObject>(c,"tempGameRoomPanel").transform;
        var browser=Ref<GameObject>(c,"roomListPanel").transform;
        RemoveChild(ready,"BackToLobby");RemoveChild(browser,"BackToLobby");
        PolishReady(c,ready);
        PolishRoom(Ref<RoomPanelUI>(c,"roomPanelUI"));
        foreach(var b in browser.GetComponentsInChildren<Button>(true))
            if(!b.name.StartsWith("RoomRow"))AssetButton(b);
        var table=browser.Find("TableHeader").GetComponent<Image>();table.sprite=Art("ProjectVector/Modules/Results/UI/Sprites/TabImage");table.color=dark;
        var rows=browser.GetComponent<VectorRoomBrowser>().rows;
        for(int i=0;i<rows.Length;i++)
        {
            var row=rows[i];row.GetComponent<Image>().sprite=Art("ProjectVector/Modules/Results/UI/Sprites/TabImage");
            RemoveChild(row.transform,"RowAccent");Box(row.transform,"RowAccent",0,8,3,46,new Color32(92,183,206,255));
        }
        foreach(var mode in new[]{"normalGameButton","rankGameButton","customRoomButton"})AssetButton(Ref<Button>(c,mode));
        foreach(var component in game.GetComponentsInChildren<Component>(true))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        Debug.Log("MATCH_POLISH_SAVED");
        ValidatePolish();CaptureSaved();
    }
    static void PolishReady(GamePanelController c,Transform p)
    {
        RemoveChild(p,"ReflectiveFloor");
        var floor=Box(p,"ReflectiveFloor",0,824,1920,256,Color.white);floor.transform.SetAsFirstSibling();
        var fade=floor.gameObject.AddComponent<VectorUIFade>();fade.top=new Color(.54f,.63f,.7f,.45f);fade.bottom=new Color(.87f,.92f,.96f,.15f);
        Box(floor.transform,"Horizon",230,0,1460,2,new Color(1,1,1,.65f));
        for(int i=0;i<7;i++)
        {
            float bottomX=-800+i*590;
            float startX=960+(bottomX-960)*.57f;
            var line=Box(floor.transform,"FloorSeam"+i,startX,2,1,1,new Color(1,1,1,.16f));
            var rect=line.rectTransform;var direction=new Vector2(bottomX-startX,-254);
            rect.pivot=new Vector2(0,.5f);rect.sizeDelta=new Vector2(direction.magnitude,1);
            rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
        var heading=p.Find("ModeDetail").GetComponent<TMP_Text>();Place(heading.transform,110,194,480,55);heading.fontSize=34;
        p.Find("MatchHint").gameObject.SetActive(false);
        var arr=new SerializedObject(c).FindProperty("tempGamePlayerSlots");
        for(int i=0;i<3;i++)
        {
            float x=new[]{810,430,1190}[i];
            var dock=p.Find("TeamPosition"+i);Place(dock,x-15,766,330,64);dock.GetComponent<Image>().sprite=Art("ProjectVector/Modules/Match/UI/Sprites/CharacterSelect");dock.GetComponent<Image>().color=new Color32(103,124,143,255);
            var wait=dock.Find("Waiting").GetComponent<TMP_Text>();Place(wait.transform,25,5,280,48);wait.text=i==0?"":"팀원 매칭 대기";wait.fontSize=23;
            RemoveChild(dock,"DockEdge");Box(dock,"DockEdge",18,61,294,3,new Color32(101,191,217,255));
            RemoveChild(dock,"DockReflection");
            var dockReflection=Decor(dock,"DockReflection",0,92,330,28,"ProjectVector/Modules/Match/UI/Sprites/CharacterSelect",new Color(1,1,1,.16f));
            dockReflection.transform.localScale=new Vector3(1,-1,1);
            var dockFade=dockReflection.gameObject.AddComponent<VectorUIFade>();dockFade.top=new Color(1,1,1,0);dockFade.bottom=Color.white;
            var slot=(TempGamePlayerSlotUI)arr.GetArrayElementAtIndex(i).objectReferenceValue;
            if(PrefabUtility.IsPartOfPrefabInstance(slot)&&PrefabUtility.GetOutermostPrefabInstanceRoot(slot.gameObject)==slot.gameObject)
                PrefabUtility.UnpackPrefabInstance(slot.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            Place(slot.transform,x,230,300,594);
            RemoveChild(slot.transform,"FloorReflection");RemoveChild(slot.transform,"CardMetalwork");RemoveChild(slot.transform,"CardFoot");
            var frame=Ref<Image>(slot,"borderImage");frame.color=new Color32(33,43,55,255);frame.sprite=Art("ProjectVector/Shared/Resources/IconBorder/border");
            var card=Ref<Image>(slot,"iconImage");Place(card.transform,18,35,264,536);card.sprite=Art("ProjectVector/Shared/Resources/IconCard/icon"+i+"_card");card.color=Color.white;card.enabled=true;
            var metal=Rect(slot.transform,"CardMetalwork",0,0,300,594);metal.SetAsFirstSibling();
            Box(metal,"OuterBezel",0,0,300,594,new Color32(40,51,65,255));
            Box(metal,"SilverInset",5,5,290,584,new Color32(160,180,195,255));
            Box(metal,"InnerBezel",12,12,276,570,new Color32(40,51,65,255));
            Decor(metal,"TopCap",0,0,300,32,"ProjectVector/Modules/Match/UI/Sprites/CharacterSelect",Color.white);
            Box(metal,"LeftEdge",4,34,3,534,new Color32(109,185,209,255));
            Box(metal,"RightEdge",293,34,3,534,new Color32(109,185,209,255));
            var left=Box(metal,"ReferenceFrameLeft",0,28,18,545,Color.white);left.sprite=ReferenceSlice("ReferenceFrameLeft",870,307,19,637);
            var right=Box(metal,"ReferenceFrameRight",282,28,18,545,Color.white);right.sprite=ReferenceSlice("ReferenceFrameRight",1170,307,19,637);
            var foot=Decor(slot.transform,"CardFoot",0,570,300,24,"ProjectVector/Modules/Match/UI/Sprites/CharacterSelect",Color.white);foot.sprite=ReferenceSlice("ReferenceFrameFoot",870,929,319,24);
            Box(slot.transform.Find("CardFoot"),"LightStrip",30,17,240,3,new Color32(96,199,225,255));
            var nick=Ref<TMP_Text>(slot,"nicknameText");var label=nick.transform.parent;Place(label,14,53,272,42);label.GetComponent<Image>().color=new Color32(22,30,39,242);Stretch(nick.transform);nick.text="닉네임";nick.fontSize=25;nick.color=new Color32(255,220,121,255);
            var leader=label.Find("LeaderImage");Place(leader,78,-104,116,30);leader.GetComponent<Image>().sprite=Art("ProjectVector/Modules/Match/UI/Sprites/Timer");leader.GetComponent<Image>().color=new Color32(44,55,67,255);var title=leader.GetComponentInChildren<TMP_Text>(true);Stretch(title.transform);title.text="팀 리더";title.fontSize=20;
            var rank=Ref<Image>(slot,"rankImage");Place(rank.transform,120,-22,60,60);rank.sprite=Art("ProjectVector/Shared/Resources/Rank/Bronze");rank.color=Color.white;rank.preserveAspect=true;
            AddReflection(slot.transform);
            slot.gameObject.SetActive(i==0);
        }
        Style(Ref<Button>(c,"matchmakingStartButton"),810,967,300,66,"매칭 시작",red);AssetButton(Ref<Button>(c,"matchmakingStartButton"));
        var training=p.Find("TrainingButton").GetComponent<Button>();Style(training,1190,975,240,50,"훈련",dark);AssetButton(training);
    }
    static void AddReflection(Transform source)
    {
        var root=Rect(source,"FloorReflection",0,594*1.23f+6,300,594);root.localScale=new Vector3(1,-.23f,1);
        var group=root.gameObject.AddComponent<CanvasGroup>();group.alpha=.38f;group.blocksRaycasts=false;group.interactable=false;
        var sources=new List<Image>();var targets=new List<Image>();
        // Copy only authored visual transforms, never behaviours or event handlers.
        void Copy(Transform from,Transform to,bool isRoot)
        {
            var im=from.GetComponent<Image>();
            if(im!=null){var target=to.gameObject.AddComponent<Image>();EditorUtility.CopySerialized(im,target);target.raycastTarget=false;var fade=to.gameObject.AddComponent<VectorUIFade>();fade.bounds=root;fade.top=new Color(1,1,1,0);fade.bottom=Color.white;sources.Add(im);targets.Add(target);}
            foreach(Transform child in from)
            {
                if(child==root||child.GetComponent<TMP_Text>()!=null)continue;
                var rect=child as RectTransform;if(rect==null)continue;
                var copy=Rect(to,child.name,0,0,1,1);
                copy.anchorMin=rect.anchorMin;copy.anchorMax=rect.anchorMax;copy.pivot=rect.pivot;copy.sizeDelta=rect.sizeDelta;copy.anchoredPosition=rect.anchoredPosition;copy.localScale=rect.localScale;copy.localRotation=rect.localRotation;
                Copy(child,copy,false);copy.gameObject.SetActive(child.gameObject.activeSelf);
            }
        }
        Copy(source,root,true);
        var sync=root.gameObject.AddComponent<VectorCardReflection>();sync.sources=sources.ToArray();sync.reflections=targets.ToArray();sync.Refresh();
    }
    static void PolishRoom(RoomPanelUI ui)
    {
        var p=ui.transform;var info=p.Find("RoomInfo");Place(info,110,188,1700,94);
        info.GetComponent<Image>().sprite=Art("ProjectVector/Modules/Results/UI/Sprites/TabImage");info.GetComponent<Image>().color=dark;
        var mode=info.Find("MapImage");Place(mode,0,0,260,94);mode.GetComponent<Image>().sprite=Art("ProjectVector/Modules/Match/UI/Sprites/OurTeamList");mode.GetComponent<Image>().color=Color.white;
        var modeText=mode.GetComponentInChildren<TMP_Text>();Stretch(modeText.transform);modeText.text="CUSTOM / 3대3";modeText.fontSize=25;
        Place(info.Find("RoomNameText"),290,12,670,36);Place(info.Find("MapNameText"),290,51,670,30);
        ui.roomCodeText.transform.SetParent(info,false);Place(ui.roomCodeText.transform,1000,20,390,54);ui.roomCodeText.color=new Color32(255,221,141,255);ui.roomCodeText.fontSize=27;ui.roomCodeText.alignment=TextAlignmentOptions.Center;ui.roomCodeText.text="방 번호  —";
        Style(Ref<Button>(ui,"settingButton"),1430,20,250,54,"맵 · 규칙 설정",blue);AssetButton(Ref<Button>(ui,"settingButton"));
        string[] groups={"aTeamSlots","bTeamSlots","observerSlots"};string[] heads={"ATeamImage","BTeamImage","ObserverTeamImage"};
        for(int team=0;team<3;team++)
        {
            float x=new[]{110,705,1300}[team],w=team==2?510:550;
            var head=p.Find(heads[team]);Place(head,x,328,w,50);head.GetComponent<Image>().sprite=Art(team==0?"ProjectVector/Modules/Match/UI/Sprites/EnemyTeamList":team==1?"ProjectVector/Modules/Match/UI/Sprites/OurTeamList":"ProjectVector/Modules/Results/UI/Sprites/TabImage");head.GetComponent<Image>().color=team==2?dark:Color.white;
            var label=head.GetComponentInChildren<TMP_Text>();Place(label.transform,22,0,w-44,50);label.alignment=TextAlignmentOptions.MidlineLeft;label.fontSize=25;
            var arr=new SerializedObject(ui).FindProperty(groups[team]);
            for(int i=0;i<arr.arraySize;i++)
            {
                var slot=(RoomPlayerSlotUI)arr.GetArrayElementAtIndex(i).objectReferenceValue;Place(slot.transform,x,394+i*150,w,134);
                var bg=slot.GetComponent<Image>();bg.sprite=Art("ProjectVector/Modules/Results/UI/Sprites/TabImage");bg.color=new Color32(100,115,133,235);
                RemoveChild(slot.transform,"SeatTrim");var trim=Rect(slot.transform,"SeatTrim",0,0,w,134);trim.SetAsFirstSibling();
                var accent=team==0?new Color32(206,64,79,255):team==1?new Color32(85,138,215,255):new Color32(173,190,205,255);
                Box(trim,"TeamStripe",0,0,5,134,accent);Box(trim,"TopEdge",8,0,w-16,1,new Color(1,1,1,.35f));
                Box(trim,"BottomEdge",8,133,w-16,1,new Color(0,0,0,.16f));
                var socket=Decor(trim,"PortraitSocket",15,12,110,110,"ProjectVector/Modules/Match/UI/Sprites/CharacterSelect",new Color32(87,101,119,180));
                Text(socket.transform,"SeatNumber",(i+1).ToString("00"),0,0,110,110,38,new Color(1,1,1,.27f)).alignment=TextAlignmentOptions.Center;
                Place(Ref<Image>(slot,"image").transform,19,16,102,102);
                Place(Ref<TMP_Text>(slot,"nicknameText").transform,149,28,w-220,38);Place(Ref<TMP_Text>(slot,"levelText").transform,149,78,240,30);
                Place(slot.emptyLabel.transform,151,35,w-174,64);slot.emptyLabel.GetComponent<TMP_Text>().text="+  빈 자리";slot.emptyLabel.GetComponent<TMP_Text>().fontSize=26;
                slot.Clear();
            }
        }
        Place(p.Find("SlotHint"),110,868,1100,32);p.Find("SlotHint").GetComponent<TMP_Text>().fontSize=20;p.Find("SlotHint").GetComponent<TMP_Text>().color=new Color32(77,93,110,255);
        Place(ui.readinessText.transform,510,916,900,34);ui.readinessText.fontSize=23;
        Style(Ref<Button>(ui,"gameStartButton"),810,967,300,66,"경기 시작",red);AssetButton(Ref<Button>(ui,"gameStartButton"));
        Style(Ref<Button>(ui,"leaveButton"),1570,978,240,50,"방 나가기",dark);AssetButton(Ref<Button>(ui,"leaveButton"));
    }
    public static void ValidatePolish()
    {
        ValidateSaved();
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GamePanelController>(true)).Single();
        var p=Ref<GameObject>(c,"tempGameRoomPanel");var refs=p.GetComponentsInChildren<VectorCardReflection>(true);
        if(refs.Length!=3||refs.Any(r=>r.sources.Length==0||r.sources.Length!=r.reflections.Length||r.sources.Any(i=>i==null)||r.reflections.Any(i=>i==null)))throw new Exception("Missing saved reflection references");
        if(p.transform.Find("BackToLobby")!=null||Ref<GameObject>(c,"roomListPanel").transform.Find("BackToLobby")!=null)throw new Exception("Redundant lobby button remains");
        var room=Ref<RoomPanelUI>(c,"roomPanelUI");if(room.roomCodeText.transform.parent!=room.transform.Find("RoomInfo"))throw new Exception("Room number is not in the header");
        Debug.Log("MATCH_POLISH_REFERENCES_OK: 3 saved reflections, visible header room code, no duplicate lobby actions");
    }
}
