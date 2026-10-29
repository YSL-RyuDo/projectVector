using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static partial class VectorMatchLayoutEditor
{
    static TMP_FontAsset font;
    static Color dark=new Color32(53,60,71,255),red=new Color32(194,39,57,255),blue=new Color32(79,111,170,255),ink=new Color32(36,42,53,255);
    static T Ref<T>(UnityEngine.Object owner,string name) where T:UnityEngine.Object=>new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.localScale=Vector3.one;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Stretch(Transform t){var r=(RectTransform)t;r.localScale=Vector3.one;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static RectTransform Rect(Transform p,string n,float x,float y,float w,float h){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Place(r,x,y,w,h);return r;}
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c){var r=Rect(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
    static TMP_Text Text(Transform p,string n,string s,float x,float y,float w,float h,int size,Color c){var r=Rect(p,n,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.text=s;t.color=c;t.raycastTarget=false;t.richText=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.overflowMode=TextOverflowModes.Ellipsis;return t;}
    static void Style(Button b,float x,float y,float w,float h,string title,Color c){Place(b.transform,x,y,w,h);b.image.sprite=null;b.image.color=c;b.transition=Selectable.Transition.ColorTint;var colors=ColorBlock.defaultColorBlock;colors.disabledColor=new Color(.7f,.7f,.7f,.65f);b.colors=colors;var t=b.GetComponentInChildren<TMP_Text>(true);Stretch(t.transform);t.fontSize=26;t.color=Color.white;t.text=title;t.alignment=TextAlignmentOptions.Center;}
    static Button Button(Transform p,string n,string s,float x,float y,float w,float h,Color c,UnityEngine.Events.UnityAction action){var im=Box(p,n,x,y,w,h,c);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;Text(b.transform,"Label",s,0,0,w,h,26,Color.white).alignment=TextAlignmentOptions.Center;if(action!=null)UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
    [MenuItem("Project Vector/UI/Upgrade Match Lobby")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play mode first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");if(scene.IsValid()&&scene.isDirty)throw new Exception("Save existing scene edits first");
        if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GamePanelController>(true)).Single();
        var game=Ref<GameObject>(controller,"gamePanel").transform;
        if(game.GetComponentInChildren<VectorRoomBrowser>(true)!=null)throw new Exception("Already authored. Edit saved hierarchy.");
        System.IO.File.Copy("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity",System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Lobby-before-match-rework-"+DateTime.Now.ToString("yyyyMMddHHmmss")+".unity"));
        font=Ref<Button>(controller,"normalGameButton").GetComponentInChildren<TMP_Text>().font;
        Stretch(game);
        var so=new SerializedObject(controller);so.FindProperty("selectedModeButtonColor").colorValue=red;so.FindProperty("unselectedModeButtonColor").colorValue=dark;so.ApplyModifiedPropertiesWithoutUndo();
        string[] modes={"normalGameButton","rankGameButton","customRoomButton"};for(int i=0;i<3;i++)Style(Ref<Button>(controller,modes[i]),620+i*225,100,218,44,new[]{"일반 · 3대3","랭크 · 3대3","사용자 지정"}[i],i==0?red:dark);
        BuildBrowser(controller);BuildRoom(Ref<RoomPanelUI>(controller,"roomPanelUI"));BuildReady(controller);
        foreach(var popup in new[]{"slotSwapRequestPanel","slotChangeConfirmPanel"}){var p=Ref<GameObject>(controller,popup).transform;Place(p,690,370,540,270);p.GetComponent<Image>().color=dark;var t=p.GetComponentInChildren<TMP_Text>(true);Place(t.transform,35,25,470,120);t.fontSize=28;t.color=Color.white;var buttons=p.GetComponentsInChildren<Button>(true);for(int i=0;i<buttons.Length;i++)Style(buttons[i],35+i*240,185,220,55,i==0?"확인":"취소",i==0?red:blue);}
        var message=Ref<GameObject>(controller,"roomMessagePanel");Place(message.transform,535,840,850,90);message.GetComponent<Image>().color=dark;Stretch(Ref<TMP_Text>(controller,"roomMessageText").transform);
        game.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");Debug.Log("MATCH_UI_SAVED");
    }
    static void BuildBrowser(GamePanelController c)
    {
        var p=Ref<GameObject>(c,"roomListPanel").transform;Stretch(p);var browser=p.gameObject.AddComponent<VectorRoomBrowser>();
        p.Find("RoomDescription").gameObject.SetActive(false);p.Find("RoomListScrollView").gameObject.SetActive(false);
        Text(p,"Heading","사설방",80,182,300,55,38,ink);
        Style(Ref<Button>(c,"roomRefreshButton"),1060,184,180,48,"새로고침",dark);
        Style(p.Find("RoomCreateButton").GetComponent<Button>(),1630,184,210,48,"+ 방 생성",red);
        browser.waitingFilter=Button(p,"WaitingFilter","모든 방 보기",1255,184,200,48,dark,browser.ToggleWaiting);
        var searchBox=Box(p,"RoomSearch",440,184,600,48,Color.white);searchBox.raycastTarget=true;var input=searchBox.gameObject.AddComponent<TMP_InputField>();
        var viewport=Rect(searchBox.transform,"Text Area",16,4,568,40);viewport.gameObject.AddComponent<RectMask2D>();input.textViewport=viewport;
        input.textComponent=Text(viewport,"Text","",0,0,568,40,23,ink) as TextMeshProUGUI;input.placeholder=Text(viewport,"Placeholder","방 이름 · 방장 · 방 번호 검색",0,0,568,40,23,new Color(.4f,.44f,.5f));input.characterLimit=60;browser.search=input;
        Box(p,"TableHeader",80,254,1760,60,dark);float[] xs={100,650,900,1210,1460,1660};string[] heads={"방 이름","모드","맵","방장","인원","상태"};for(int i=0;i<6;i++)Text(p,"Column"+i,heads[i],xs[i],258,i==0?510:190,52,26,Color.white);
        browser.rows=new RoomSlotUI[8];
        for(int i=0;i<8;i++){var b=Button(p,"RoomRow"+i,"",80,324+i*69,1760,62,dark,null);var row=b.gameObject.AddComponent<RoomSlotUI>();row.roomNameText=Text(b.transform,"RoomName","",20,0,530,62,24,Color.white);Text(b.transform,"Mode","3대3",570,0,230,62,23,Color.white);row.mapNameText=Text(b.transform,"Map","",820,0,280,62,23,Color.white);row.hostNameText=Text(b.transform,"Host","",1130,0,230,62,23,Color.white);row.playerCountText=Text(b.transform,"Players","",1380,0,180,62,23,Color.white);row.stateText=Text(b.transform,"State","",1580,0,160,62,23,Color.white);browser.rows[i]=row;b.gameObject.SetActive(false);}
        browser.status=Text(p,"ListStatus","새로고침하면 방 목록이 표시됩니다.",80,895,1100,45,23,ink);
        Button(p,"Previous","이전",680,960,110,48,dark,browser.Previous);browser.pageLabel=Text(p,"Page","1 / 1",820,960,160,48,24,ink);Button(p,"Next","다음",1000,960,110,48,dark,browser.Next);
        browser.join=Button(p,"JoinSelectedRoom","참가하기",1500,947,340,68,red,browser.Join);browser.join.interactable=false;
    }
    static void BuildRoom(RoomPanelUI ui)
    {
        var p=ui.transform;Stretch(p);var info=p.Find("RoomInfo");Place(info,110,180,1700,80);info.GetComponent<Image>().color=dark;
        Place(info.Find("MapImage"),0,0,260,80);Place(info.Find("RoomNameText"),285,5,920,40);Place(info.Find("MapNameText"),285,44,900,30);
        foreach(var t in info.GetComponentsInChildren<TMP_Text>(true)){t.color=Color.white;t.fontSize=26;}
        Style(Ref<Button>(ui,"settingButton"),1440,12,240,56,"맵 · 규칙 설정",blue);
        string[] groups={"aTeamSlots","bTeamSlots","observerSlots"};string[] old={"ATeamList","BTeamList","ObserverTeamList"};string[] headers={"ATeamImage","BTeamImage","ObserverTeamImage"};
        for(int team=0;team<3;team++){
            float x=new[]{110,705,1300}[team];float w=team==2?510:550;var head=p.Find(headers[team]);Place(head,x,320,w,45);head.GetComponent<Image>().color=team==0?red:team==1?blue:dark;var title=head.GetComponentInChildren<TMP_Text>();Stretch(title.transform);title.text=team==0?"A팀  /  3명":team==1?"B팀  /  3명":"관전자  /  2명";title.fontSize=28;title.color=Color.white;
            var arr=new SerializedObject(ui).FindProperty(groups[team]);if(arr.arraySize!=(team==2?2:3))throw new Exception("Unexpected team capacity");
            for(int i=0;i<arr.arraySize;i++){var slot=(RoomPlayerSlotUI)arr.GetArrayElementAtIndex(i).objectReferenceValue;slot.transform.SetParent(p,false);Place(slot.transform,x,380+i*155,w,140);var layout=slot.GetComponent<LayoutElement>();if(layout!=null)layout.ignoreLayout=true;var bg=slot.GetComponent<Image>();if(bg!=null){bg.sprite=null;bg.color=new Color32(95,107,122,215);}Place(Ref<Image>(slot,"image").transform,12,12,116,116);Place(Ref<TMP_Text>(slot,"nicknameText").transform,155,30,w-220,44);Place(Ref<TMP_Text>(slot,"levelText").transform,155,83,230,30);var nick=Ref<TMP_Text>(slot,"nicknameText");nick.fontSize=30;nick.richText=false;nick.color=Color.white;Ref<TMP_Text>(slot,"levelText").fontSize=23;Place(Ref<GameObject>(slot,"leaderImage").transform,w-52,10,36,36);slot.emptyLabel=Text(slot.transform,"EmptySeat","+    빈 자리",28,35,w-56,60,30,new Color(.86f,.9f,.95f)).gameObject;slot.Clear();}
            p.Find(old[team]).gameObject.SetActive(false);
        }
        ui.readinessText=Text(p,"Readiness","A팀 3명 · B팀 3명이 모이면 시작할 수 있습니다.",460,866,1000,42,24,ink);ui.readinessText.alignment=TextAlignmentOptions.Center;
        Text(p,"SlotHint","빈 자리 선택: 이동   ·   플레이어 우클릭: 방장 메뉴",110,805,1250,40,21,ink);
        Style(Ref<Button>(ui,"gameStartButton"),810,942,300,70,"경기 시작",red);Style(Ref<Button>(ui,"leaveButton"),1570,956,240,55,"방 나가기",dark);
        var confirm=Ref<GameObject>(ui,"leaveConfirmPanel").transform;Place(confirm,690,370,540,270);confirm.GetComponent<Image>().color=dark;Text(confirm,"LeaveTitle","사설방에서 나가시겠습니까?",30,30,480,100,28,Color.white);Style(Ref<Button>(ui,"leaveConfirmButton"),35,185,220,55,"나가기",red);Style(Ref<Button>(ui,"leaveCancelButton"),285,185,220,55,"취소",blue);
        foreach(var popup in new[]{"exitImage","SwapRequestImage","RoomSlotChangePanel","SlotAdminMenuPanel","RoomRulesPanel"})p.Find(popup).SetAsLastSibling();
    }
    static void BuildReady(GamePanelController c)
    {
        var p=Ref<GameObject>(c,"tempGameRoomPanel").transform;Stretch(p);
        var arr=new SerializedObject(c).FindProperty("tempGamePlayerSlots");
        for(int i=0;i<3;i++){
            float x=new[]{810,450,1170}[i];var frame=Box(p,"TeamPosition"+i,x,255,300,620,new Color32(86,98,115,120));Text(frame.transform,"Waiting","팀원 매칭 대기",30,270,240,55,26,Color.white).alignment=TextAlignmentOptions.Center;
            var slot=(TempGamePlayerSlotUI)arr.GetArrayElementAtIndex(i).objectReferenceValue;slot.transform.SetParent(p,false);Place(slot.transform,x,245,300,650);slot.gameObject.SetActive(i==0);
        }
        p.Find("Scroll View").gameObject.SetActive(false);
        Text(p,"ModeDetail","3대3 팀 전투",110,190,530,65,42,ink);Text(p,"MatchHint","매칭이 완료되면 전장으로 이동합니다.",110,262,540,75,24,ink);
        Style(Ref<Button>(c,"matchmakingStartButton"),810,946,300,70,"매칭 시작",red);Style(p.Find("TrainingButton").GetComponent<Button>(),1170,959,240,52,"훈련",dark);
    }
    public static void VerifyAndCapture()
    {
        // Verification is read-only; never replace the user's saved layout with defaults.
        ValidatePolish();
        CaptureSaved();
    }
    public static void CaptureSaved()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GamePanelController>(true)).Single();
        var game=Ref<GameObject>(c,"gamePanel");
        var host=new GameObject("PreviewCanvas",typeof(Canvas));host.layer=5;var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;host.GetComponent<RectTransform>().sizeDelta=new Vector2(1920,1080);
        var cam=new GameObject("PreviewCamera",typeof(Camera)).GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=540;cam.aspect=1920f/1080;cam.transform.position=new Vector3(0,0,-10);cam.cullingMask=1<<5;cam.farClipPlane=100;canvas.worldCamera=cam;
        var clone=UnityEngine.Object.Instantiate(game,host.transform,false);clone.SetActive(true);foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
        foreach(var name in new[]{"Profile and Navigation","ChatButton"})
        {
            var source=scene.GetRootGameObjects().Where(r=>r!=host&&r!=cam.gameObject).SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name==name);
            var shared=UnityEngine.Object.Instantiate(source.gameObject,host.transform,false);shared.SetActive(true);
            foreach(var t in shared.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
        }
        foreach(var root in scene.GetRootGameObjects())if(root!=host&&root!=cam.gameObject)root.SetActive(false);
        var list=clone.GetComponentInChildren<VectorRoomBrowser>(true);var lobby=clone.GetComponentInChildren<RoomPanelUI>(true);var ready=clone.transform.Find("TempRoomPanel").gameObject;
        clone.transform.Find("NormalButton").GetComponent<Image>().color=dark;clone.transform.Find("UserRoom").GetComponent<Image>().color=red;
        ready.SetActive(false);lobby.gameObject.SetActive(false);list.gameObject.SetActive(true);
        typeof(VectorRoomBrowser).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(list,null);
        var fixture=Enumerable.Range(1,11).Select(i=>new RoomListData{roomId=(uint)(100+i),roomName=$"VECTOR 함께하는 작전 {i}",mapName=i%2==0?"Desert":"TestMap",hostName=$"플레이어{i}",currentPlayerCount=(ushort)(i%2==0?8:3),maxPlayerCount=8,state=(byte)(i%3==0?1:0)}).ToList();
        list.Receive(fixture);list.Next();if(list.pageLabel.text!="2 / 2")throw new Exception("Pagination failed");list.Previous();list.rows[0].GetComponent<Button>().onClick.Invoke();if(!list.join.interactable)throw new Exception("Join selection failed");list.rows[1].GetComponent<Button>().onClick.Invoke();if(list.join.interactable)throw new Exception("Full room join allowed");list.search.text="플레이어11";if(list.rows.Count(r=>r.gameObject.activeSelf)!=1)throw new Exception("Search failed");list.search.text="";list.Receive(fixture.Take(5).ToList());
        Capture(cam,host,"VectorRoomList.png");
        list.gameObject.SetActive(false);lobby.gameObject.SetActive(true);lobby.ApplyRoomState(new RoomStateData{roomId=101,roomName="VECTOR 함께하는 작전",mapName="Desert",isLeader=true,canStart=false,members=new System.Collections.Generic.List<RoomMemberData>{new RoomMemberData{playerId=1,nickname="LYS",level=1,playerIconId=0,team=RoomSlotTeam.A,slotIndex=0,isLeader=true},new RoomMemberData{playerId=2,nickname="플레이어2",level=12,playerIconId=1,team=RoomSlotTeam.B,slotIndex=0}}});
        if(Ref<Button>(lobby,"gameStartButton").interactable)throw new Exception("Incomplete team can start");Capture(cam,host,"VectorPrivateRoom.png");
        lobby.gameObject.SetActive(false);ready.SetActive(true);clone.transform.Find("NormalButton").GetComponent<Image>().color=red;clone.transform.Find("UserRoom").GetComponent<Image>().color=dark;var slots=ready.GetComponentsInChildren<TempGamePlayerSlotUI>(true);foreach(var s in slots)s.Hide();slots.First(s=>s.name=="PlayerBorder0").SetData(Resources.Load<Sprite>("IconCard/icon0_card"),Resources.Load<Sprite>("IconBorder/border"),Resources.Load<Sprite>("Rank/Bronze"),"LYS");Capture(cam,host,"VectorMatchReady.png");
        int count=clone.GetComponentsInChildren<Transform>(true).Length;
        for(int i=0;i<slots.Length;i++)slots[i].SetData(Resources.Load<Sprite>("IconCard/icon"+i+"_card"),Resources.Load<Sprite>("IconBorder/border"),Resources.Load<Sprite>("Rank/Gold"),"플레이어 "+(i+1));
        slots[1].SetChipCraftPreset(new ChipCraftPresetData{medals=new System.Collections.Generic.List<ChipCraftMedalData>{new ChipCraftMedalData{medalId=2,x=.2f,y=-.3f,rotation=15,scale=.65f,flipped=true}}});
        if(ready.GetComponentsInChildren<VectorCardReflection>().Length!=3)throw new Exception("Returning teammate reflection is hidden");
        foreach(var reflection in ready.GetComponentsInChildren<VectorCardReflection>())
        {
            reflection.Refresh();
            for(int i=0;i<reflection.sources.Length;i++)
            {
                if(reflection.sources[i].sprite!=reflection.reflections[i].sprite)throw new Exception("Reflection content mismatch");
                if(i>0&&reflection.sources[i].rectTransform.localRotation!=reflection.reflections[i].rectTransform.localRotation)throw new Exception("Reflection medal rotation mismatch");
            }
        }
        if(clone.GetComponentsInChildren<Transform>(true).Length!=count)throw new Exception("Updating players created UI objects");
        Capture(cam,host,"VectorMatchThreePlayers.png");
        Debug.Log("MATCH_UI_VERIFIED: saved rows, pagination, search, join eligibility, 3v3 start gate. Offline fixture only.");
    }
    public static void ValidateSaved()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GamePanelController>(true)).Single();
        var browser=Ref<GameObject>(c,"roomListPanel").GetComponent<VectorRoomBrowser>();if(browser.rows.Length!=8||browser.rows.Any(r=>r==null)||browser.search==null||browser.join==null)throw new Exception("Saved browser missing references");
        var room=Ref<RoomPanelUI>(c,"roomPanelUI");if(room.GetComponentsInChildren<RoomPlayerSlotUI>(true).Any(s=>s.emptyLabel==null))throw new Exception("Saved empty seat reference missing");
        foreach(var key in new[]{"aTeamSlots","bTeamSlots","observerSlots"})if(new SerializedObject(room).FindProperty(key).arraySize!=(key=="observerSlots"?2:3))throw new Exception("Saved team capacity mismatch");
        if(new SerializedObject(c).FindProperty("tempGamePlayerSlots").arraySize!=3)throw new Exception("Saved ready room capacity mismatch");
        if(scene.isDirty)throw new Exception("Validation unexpectedly modified scene");Debug.Log("MATCH_UI_SAVED_REFERENCES_OK");
    }
    static void Capture(Camera cam,GameObject root,string file)
    {
        var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;var old=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;cam.targetTexture=rt;foreach(var reflection in root.GetComponentsInChildren<VectorCardReflection>())reflection.Refresh();Canvas.ForceUpdateCanvases();foreach(var g in root.GetComponentsInChildren<Graphic>()){g.canvasRenderer.SetColor(Color.white);g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);}foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),file),tex.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static void Inspect()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var report=new StringBuilder();
        foreach(var c in all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null&&(c is GamePanelController||c is RoomPanelUI||c is RoomRulesPanelUI||c is RoomCreateButton)))
        {
            report.AppendLine(c.GetType().Name+" "+Path(c.transform));var so=new SerializedObject(c);var it=so.GetIterator();
            while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)report.AppendLine(it.propertyPath+" = "+(it.objectReferenceValue is Component co?Path(co.transform):it.objectReferenceValue is GameObject go?Path(go.transform):AssetDatabase.GetAssetPath(it.objectReferenceValue)));
        }
        var game=all.First(t=>t.name=="Game Panel");
        foreach(var t in game.GetComponentsInChildren<Transform>(true)){var r=t as RectTransform;report.AppendLine(Path(t)+" active="+t.gameObject.activeSelf+" rect="+(r!=null?r.sizeDelta.ToString():"")+" text="+t.GetComponent<TMP_Text>()?.text);}
        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"VectorMatchLayout.txt"),report.ToString());
    }
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
}
