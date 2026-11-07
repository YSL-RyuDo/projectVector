using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Additive authoring only. Never invoked at runtime; preserve all original references.
public static class VectorExperiencePolishEditor
{
    static readonly Color ink = new Color32(34, 35, 39, 255), panel = new Color32(111, 116, 124, 255);
    static readonly Color cyan = new Color32(228, 230, 233, 255), muted = new Color32(208, 210, 214, 255);
    static readonly Color paper = new Color32(201, 205, 210, 255);
    static readonly Color red = new Color32(193, 40, 60, 255);
    static TMP_FontAsset font;
    static T Ensure<T>(GameObject go) where T : Component { var c=go.GetComponent<T>(); return c==null?go.AddComponent<T>():c; }
    static Sprite VideoArt(string name)
    {
        string path="Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/"+name+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.textureType!=TextureImporterType.Sprite) { importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.mipmapEnabled=false; importer.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static T Ref<T>(Object owner, string name) where T : Object => new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;
    static Transform Find(Transform parent, string name) => parent.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null) { rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); }
        rect.gameObject.layer = 5; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h); return rect;
    }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static void Pose(Transform target,float x,float y,float width,float height)
    {
        var rect=(RectTransform)target; rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,height);
    }
    static void VideoBackdrop(Transform parent)
    {
        var image=Box(parent,"ReferenceBackdrop",0,0,1920,1080,Color.white);
        image.sprite=VideoArt("NeutralBackground"); Stretch(image.rectTransform);
        image.transform.SetAsFirstSibling();
    }
    static void VideoRails(Transform parent,float halfWidth,float height)
    {
        Box(parent,"ReferenceRailLeft",-halfWidth,0,20,height,Color.white).sprite=VideoArt("CardRailLeft");
        Box(parent,"ReferenceRailRight",halfWidth,0,20,height,Color.white).sprite=VideoArt("CardRailRight");
    }
    static void VideoSurface(Transform parent,string asset)
    {
        var surface=Box(parent,"ReferenceSurface",0,0,1,1,Color.white);
        surface.sprite=VideoArt(asset); Stretch(surface.rectTransform); surface.transform.SetAsFirstSibling();
    }
    static Image Box(Transform p, string n, float x, float y, float w, float h, Color c)
    {
        var rect = Rect(p,n,x,y,w,h); var image = Ensure<Image>(rect.gameObject);
        image.color = c; image.raycastTarget = false; return image;
    }
    static TMP_Text Text(Transform p, string n, string value, float x, float y, float w, float h, float size, Color c)
    {
        var rect=Rect(p,n,x,y,w,h); var text=Ensure<TextMeshProUGUI>(rect.gameObject);
        text.font=font; text.text=value; text.fontSize=size; text.color=c; text.raycastTarget=false; text.richText=false;
        text.alignment=TextAlignmentOptions.MidlineLeft; text.overflowMode=TextOverflowModes.Ellipsis; return text;
    }
    static Button Button(Transform p,string n,string value,float x,float y,float w,float h,Color c)
    {
        var image=Box(p,n,x,y,w,h,c); image.raycastTarget=true;
        var button=Ensure<Button>(image.gameObject); button.targetGraphic=image;
        Text(image.transform,"Label",value,0,0,w-36,h,24,Color.white).alignment=TextAlignmentOptions.Center;
        Hover(button); return button;
    }
    static void Hover(Button button)
    {
        var line=Box(button.transform,"HoverAccent",0,0,0,3,cyan);
        line.rectTransform.anchorMin=new Vector2(0,0); line.rectTransform.anchorMax=new Vector2(1,0);
        line.rectTransform.sizeDelta=new Vector2(0,3); line.rectTransform.anchoredPosition=new Vector2(0,2);
        var group=Ensure<CanvasGroup>(line.gameObject); group.alpha=0; group.blocksRaycasts=false; group.interactable=false;
        var hover=Ensure<AuthoredHoverAccent>(button.gameObject); hover.accent=group;
    }
    static AuthoredPanelMotion Motion(GameObject go,float delay=0)
    {
        var motion=Ensure<AuthoredPanelMotion>(go);
        motion.content=(RectTransform)go.transform; motion.entryOffset=new Vector2(0,-18); motion.initialScale=1; motion.duration=.4f; motion.delay=delay; return motion;
    }
    static CanvasGroup Stage(Transform parent,string name)
    {
        var rect=Rect(parent,name,0,0,1920,1080);
        return Ensure<CanvasGroup>(rect.gameObject);
    }
    public sealed class Preservation
    {
        readonly GameObject[] objects;
        readonly List<(Object owner,string field,Object target)> refs=new();
        public Preservation(UnityEngine.SceneManagement.Scene scene)
        {
            objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
            foreach(var script in objects.SelectMany(o=>o.GetComponents<MonoBehaviour>()).Where(s=>s!=null))
            {
                var p=new SerializedObject(script).GetIterator();
                while(p.NextVisible(true)) if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue!=null) refs.Add((script,p.propertyPath,p.objectReferenceValue));
            }
            File.Copy(scene.path,Path.Combine(Path.GetTempPath(),"Vector-before-polish-"+scene.name+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".unity"),true);
        }
        // For explicitly requested artwork replacement only; keep component/button references protected.
        public void AllowSpriteChange(Image image) => refs.RemoveAll(r => r.owner == image && r.field == "m_Sprite");
        public void Check()
        {
            if(objects.Any(o=>o==null)) throw new Exception("Original scene object removed");
            foreach(var r in refs) if(new SerializedObject(r.owner).FindProperty(r.field)?.objectReferenceValue!=r.target) throw new Exception("Reference changed: "+r.owner.name+"."+r.field);
            Debug.Log($"POLISH_PRESERVED: {objects.Length} objects / {refs.Count} references");
        }
    }
    [MenuItem("Project Vector/UI/Install Authored Experience Polish")]
    public static void InstallAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        VerifyAppearanceWire(); InstallLobby(); InstallResult(); InstallRank(); VerifyRank(); VerifyResult();
        VectorSocialPresentationEditor.VerifySaved();
        CaptureLobby();
        VectorVictorySequenceEditor.InstallAndVerify();
    }
    static void InstallLobby()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity"); var snapshot=new Preservation(scene);
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var profile=all.Select(t=>t.GetComponent<MyPagePanelController>()).First(c=>c!=null);
        VideoBackdrop(profile.transform);
        var sequence=Ensure<ProfileRevealSequence>(profile.gameObject);
        string[] sections={"ProfileSection","PlayerCardPreview","RankSection","FrequentCharactersSection","BattleDataSection","SkinCollectionSection"};
        sequence.sections=sections.Select((name,i)=>Motion(Find(profile.transform,name).gameObject,i*.065f)).ToArray();
        foreach(var image in profile.GetComponentsInChildren<Image>(true))
            if(new[]{"ProfileInfoBox","PersonalInfoBox","SocialBox","WeaponSkinSummary","CharacterSkinSummary"}.Contains(image.name) || image.name.StartsWith("RankBox") || image.name.StartsWith("CharacterCard") || (image.name.StartsWith("BattleSlot") && image.GetComponent<MyPageStatDropSlot>()!=null)) image.color=new Color32(117,120,126,255);
        var region=(RectTransform)Find(profile.transform,"RegionText"); region.anchoredPosition=new Vector2(100,58); region.sizeDelta=new Vector2(260,34);
        var cardPreview=Find(profile.transform,"PlayerCardPreview");
        Box(cardPreview,"ReferenceCrown",0,440,375,56,Color.white).sprite=VideoArt("CardCrown");
        Box(cardPreview,"ReferenceBase",0,-436,375,48,Color.white).sprite=VideoArt("CardBase");
        VideoRails(cardPreview,177,820);
        var profileSerialized=new SerializedObject(profile); var icons=profileSerialized.FindProperty("statIcons"); icons.arraySize=7;
        string[] iconNames={"StatWin","StatKills","StatDamage","StatMatches","StatMvp","StatHeadshot","StatAverage"};
        for(int i=0;i<7;i++)
        {
            var sprite=VideoArt(iconNames[i]); icons.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
            var image=Find(profile.transform,"BattleSlot"+i+"Icon").GetComponent<Image>(); image.sprite=sprite; image.color=Color.white; image.preserveAspect=true;
        }
        profileSerialized.ApplyModifiedPropertiesWithoutUndo();
        foreach(var button in profile.GetComponentsInChildren<Button>(true)) Hover(button);
        for(int i=0;i<3;i++)
        {
            var rank=Find(profile.transform,"RankBox"+i);
            Box(rank,"RankAccent",0,145,350,2,i==1?cyan:muted);
        }
        var game=all.Select(t=>t.GetComponent<GamePanelController>()).First(c=>c!=null);
        var social=all.Select(t=>t.GetComponent<LobbySocialController>()).First(c=>c!=null);
        // Reference palette: neutral silver / charcoal / red. No navy panels.
        social.readyPanel.GetComponent<Image>().color=new Color32(25,25,27,255);
        Find(social.readyPanel.transform,"Horizon").GetComponent<Image>().color=new Color32(86,88,91,255);
        Find(social.readyPanel.transform,"Subtitle").GetComponent<TMP_Text>().color=muted;
        social.readyProgress.color=Color.white;
        foreach(var image in social.readyCards)
        {
            image.color=new Color32(135,138,144,255);
            Find(image.transform,"TopLine").GetComponent<Image>().color=Color.white;
        }
        foreach(var popup in new[]{social.invitePanel,social.confirmationPanel})
        {
            popup.GetComponent<Image>().color=new Color32(12,12,14,205);
            Find(popup.transform,"Card").GetComponent<Image>().color=paper;
            Find(popup.transform,"Header").GetComponent<Image>().color=ink;
        }
        social.toastPanel.GetComponent<Image>().color=ink;
        foreach(var button in social.GetComponentsInChildren<Button>(true))
        {
            bool primary=button==social.acceptReady || button==social.acceptInvite || button==social.confirmButton;
            button.image.color=primary?red:ink;
            foreach(var accent in button.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Accent")) accent.color=Color.white;
        }
        var readyMotion=Motion(social.readyPanel); readyMotion.entryOffset=new Vector2(0,-1080); readyMotion.duration=.65f;
        for(int i=0;i<social.readyCards.Length;i++)
        {
            var motion=Motion(social.readyCards[i].gameObject,.2f+i*.055f); motion.entryOffset=new Vector2(0,120); motion.duration=.5f;
        }
        var ready=Ref<GameObject>(game,"tempGameRoomPanel");
        VideoBackdrop(ready.transform);
        foreach(var button in social.inviteSlots) button.image.color=ink;
        foreach(var button in Ref<GameObject>(game,"gamePanel").GetComponentsInChildren<Button>(true)) Hover(button);
        // Retire only our rejected draft panel; real queue/return status remains on the existing controls.
        var rejectedStatus=ready.transform.Find("OperationStatus");
        if(rejectedStatus!=null) rejectedStatus.gameObject.SetActive(false);
        var partyStatus=social.partyText.rectTransform;
        partyStatus.anchoredPosition=new Vector2(0,-366); partyStatus.sizeDelta=new Vector2(1100,36);
        social.partyText.text=""; social.partyText.fontSize=21; social.partyText.alignment=TextAlignmentOptions.Center;
        Find(ready.transform,"ModeDetail").GetComponent<TMP_Text>().fontSize=24;
        foreach(var slot in ready.GetComponentsInChildren<TempGamePlayerSlotUI>(true))
        {
            Motion(slot.gameObject,slot.name.EndsWith("0")?.08f:slot.name.EndsWith("1")?.15f:.22f);
            VideoRails(slot.transform,139,548);
            Box(slot.transform,"ReferenceCrown",0,283,298,40,Color.white).sprite=VideoArt("CardCrown");
            var rank=Ref<Image>(slot,"rankImage"); var badge=rank.rectTransform;
            badge.anchorMin=badge.anchorMax=new Vector2(.5f,1); badge.pivot=Vector2.one*.5f;
            badge.anchoredPosition=new Vector2(0,6); badge.sizeDelta=new Vector2(76,76);
            rank.transform.SetAsLastSibling(); // Above the extracted crown, never underneath it.
            var leader=(RectTransform)Find(slot.transform,"LeaderImage");
            leader.anchoredPosition=new Vector2(78,128); leader.sizeDelta=new Vector2(116,24);
            if(rank.transform.parent!=slot.transform || rank.transform.GetSiblingIndex()!=slot.transform.childCount-1)
                throw new Exception("Party rank badge is not in front of its frame");
        }
        var room=Ref<RoomPanelUI>(game,"roomPanelUI");
        VideoBackdrop(room.transform);
        VideoSurface(Find(room.transform,"RoomInfo"),"RoomHeaderSurface");
        VideoSurface(Find(Find(room.transform,"RoomInfo"),"MapImage"),"RoomHeaderSurface");
        VideoSurface(Find(room.transform,"ObserverTeamImage"),"RoomHeaderSurface");
        Ref<Button>(room,"settingButton").image.color=ink;
        Motion(Find(room.transform,"RoomInfo").gameObject);
        int seat=0;
        foreach(var slot in room.GetComponentsInChildren<RoomPlayerSlotUI>(true))
        {
            VideoSurface(slot.transform,"RoomRowSurface");
            Motion(slot.gameObject,.04f*(seat++%3));
            var number=Find(slot.transform,"SeatNumber").GetComponent<TMP_Text>(); number.text="";
            var socket=(RectTransform)number.transform.parent;
            VideoSurface(socket,"RoomRowSurface");
            var diamond=Box(socket,"SeatDiamond",0,0,67,67,Color.white); diamond.rectTransform.localRotation=Quaternion.identity;
            diamond.sprite=VideoArt("EmptySeat");
            diamond.transform.SetAsLastSibling();
        }
        snapshot.Check(); EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Lobby save failed");
    }
    static void VerifyAppearanceWire()
    {
        var body=new byte[467]; BitConverter.GetBytes((uint)42).CopyTo(body,0); body[4]=3;
        for(int i=0;i<3;i++)
        {
            int start=5+77*i;
            BitConverter.GetBytes((uint)i+1).CopyTo(body,start);
            BitConverter.GetBytes(i).CopyTo(body,start+4);
            BitConverter.GetBytes(2).CopyTo(body,start+12);
            BitConverter.GetBytes(23).CopyTo(body,start+16);
            body[start+20]=(byte)i; body[start+21]=2; body[start+22]=1;
            body[start+23]=2; BitConverter.GetBytes(.25f).CopyTo(body,start+24);
            BitConverter.GetBytes(-.2f).CopyTo(body,start+28);
            BitConverter.GetBytes(15f).CopyTo(body,start+32); BitConverter.GetBytes(1f).CopyTo(body,start+36);
        }
        if(!NetworkManager.TryReadResultAppearance(body,42,out var parsed) || parsed.Count!=3) throw new Exception("Appearance packet not parsed");
        for(uint i=1;i<=3;i++) if(parsed[i].playerIconId!=(int)i-1 || parsed[i].preset.cardId!=(int)i-1 || parsed[i].preset.medals[0].rotation!=15) throw new Exception("Appearance values changed");
        if(NetworkManager.TryReadResultAppearance(body,43,out _) || NetworkManager.TryReadResultAppearance(new byte[4],42,out _)) throw new Exception("Stale/truncated appearance accepted");
        body[25]=2; if(NetworkManager.TryReadResultAppearance(body,42,out _)) throw new Exception("Independent icon/card selection accepted"); body[25]=0;
        BitConverter.GetBytes(float.NaN).CopyTo(body,29); if(NetworkManager.TryReadResultAppearance(body,42,out _)) throw new Exception("Invalid medal accepted");
        Debug.Log("APPEARANCE_WIRE_VERIFIED: IDs 0/1/2 map to matching cards; wrong room, truncated body, mismatched ID and invalid medal rejected.");
    }
    static void InstallResult()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/Result.unity"); var snapshot=new Preservation(scene);
        var result=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResultUI>(true)).Single();
        var root=result.GetComponent<Canvas>().transform;
        var host=Rect(root,"MatchPresentation",0,0,1920,1080); Stretch(host);
        var ui=Ensure<MatchResultPresentation>(host.gameObject);
        ui.replayButton=Button(host,"Replay","하이라이트 다시 보기",660,475,290,45,panel);
        var overlay=Box(host,"ResultCinematic",0,0,1920,1080,paper); Stretch(overlay.rectTransform); overlay.raycastTarget=true; ui.overlay=overlay.gameObject;
        VideoBackdrop(overlay.transform);
        Box(overlay.transform,"TopRule",0,474,1760,2,new Color(0,0,0,.12f));
        Box(overlay.transform,"BottomRule",0,-402,1760,2,new Color(0,0,0,.25f));
        var wash=Box(overlay.transform,"LightWash",0,0,1920,1080,new Color32(247,247,248,50));
        var fade=Ensure<VectorUIFade>(wash.gameObject); fade.top=Color.white; fade.bottom=new Color(1,1,1,0);
        ui.sweep=Box(overlay.transform,"RevealSweep",0,294,1920,4,new Color(1,1,1,0));
        ui.stages=new[]{Stage(overlay.transform,"OutcomeStage"),Stage(overlay.transform,"SpotlightStage"),Stage(overlay.transform,"PersonalStage"),Stage(overlay.transform,"RewardStage")};
        var title=ui.stages[0].transform;
        ui.mode=Text(title,"Mode","경기 결과",450,415,700,40,23,ink);
        ui.outcome=Text(title,"Outcome","VICTORY",450,321,700,125,100,ink);
        ui.outcome.enableAutoSizing=true; ui.outcome.fontSizeMin=42; ui.outcome.fontSizeMax=100;
        ui.score=Text(title,"Score","00  :  00",450,222,700,90,66,ink); ui.score.alignment=TextAlignmentOptions.MidlineLeft;
        Text(title,"Teams","A TEAM  /  B TEAM",450,159,700,32,18,ink);
        ui.accent=Box(title,"OutcomeAccent",64,321,5,100,red);
        var star=ui.stages[1].transform;
        var hero=Box(star,"MvpCard",-440,0,900,1080,Color.clear);
        foreach(string name in new[]{"CardTop","ReferenceCrown","ReferenceBase","ReferenceRailLeft","ReferenceRailRight","IdentityBand"})
            if(hero.transform.Find(name)!=null) hero.transform.Find(name).gameObject.SetActive(false);
        ui.spotlightCaption=Text(hero.transform,"Caption","MATCH MVP",-240,391,340,38,27,red);
        ui.portrait=Box(hero.transform,"Portrait",90,-10,780,990,Color.white); ui.portrait.preserveAspect=true;
        ui.spotlightName=Text(hero.transform,"Nickname","",-175,-321,470,76,55,ink);
        ui.spotlightKda=Text(hero.transform,"KDA","",-175,-378,470,36,25,ink);
        ui.spotlightName.transform.SetAsLastSibling(); ui.spotlightKda.transform.SetAsLastSibling();
        var identity=Box(hero.transform,"EquippedIdentity",-285,45,190,430,Color.clear);
        ui.equippedIdentity=identity.gameObject;
        ui.profileIcon=Box(identity.transform,"ProfileIcon",-48,244,68,68,Color.white); ui.profileIcon.preserveAspect=true;
        ui.equippedLevel=Text(identity.transform,"Level","",48,244,95,38,20,ink);
        ui.playerCard=Box(identity.transform,"SelectedCard",0,0,168,340,Color.white);
        ui.playerCardBorder=Box(identity.transform,"SelectedCase",0,0,190,362,Color.white);
        ui.equippedMedals=Enumerable.Range(0,3).Select(i=>Box(ui.playerCard.transform,"EquippedMedal"+i,0,0,32,32,Color.white)).ToArray();
        foreach(var medal in ui.equippedMedals) medal.preserveAspect=true;
        ui.equippedRank=Box(identity.transform,"EquippedRank",0,185,52,52,Color.white); ui.equippedRank.preserveAspect=true;
        var personal=Box(ui.stages[2].transform,"PersonalCard",450,-37,700,260,Color.clear);
        Text(personal.transform,"Header","나의 전투 기록",0,94,700,36,23,ink);
        ui.personalName=Text(personal.transform,"Player","",0,47,700,35,21,ink);
        ui.personalKda=Text(personal.transform,"KDA","",0,-18,700,90,58,ink);
        Text(personal.transform,"Legend","처치      /      데스      /      도움",0,-88,700,32,19,ink);
        Box(personal.transform,"Rule",0,-127,700,1,new Color(0,0,0,.2f));
        var rewards=ui.stages[3].transform;
        Text(rewards,"RewardTitle","획득 보상",450,-216,700,36,23,ink);
        var coin=Box(rewards,"FreeCurrency",262,-298,324,100,Color.clear);
        Text(coin.transform,"Label","전투 재화",0,24,324,30,18,ink);
        ui.freeReward=Text(coin.transform,"Value","+0",0,-24,324,55,38,ink);
        var victory=Box(rewards,"VictoryCurrency",638,-298,324,100,Color.clear);
        Box(rewards,"RewardDivider",450,-298,1,85,new Color(0,0,0,.18f));
        Text(victory.transform,"Label","승리 재화",0,24,324,30,18,ink);
        ui.victoryReward=Text(victory.transform,"Value","+0",0,-24,324,55,38,red);
        Text(overlay.transform,"ExitHint","Enter  계속   ·   Esc  건너뛰기",450,-476,700,34,18,ink).alignment=TextAlignmentOptions.Center;
        ui.continueButton=Button(overlay.transform,"Continue","연출 건너뛰기  ›",450,-417,700,64,red);
        ui.continueLabel=ui.continueButton.GetComponentInChildren<TMP_Text>();
        var serialized=new SerializedObject(result); var sprites=serialized.FindProperty("characterSprites");
        ui.characters=Enumerable.Range(0,sprites.arraySize).Select(i=>(Sprite)sprites.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        ui.spotlightCharacters=Enumerable.Range(0,4).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectVector/Shared/Characters/Portraits/Image/Char{i}.png")).ToArray();
        var parent=Ref<Transform>(result,"allRowParent");
        foreach(string team in new[]{"Blue","Red"})
        {
            var rows=serialized.FindProperty("all"+team+"Rows"); rows.arraySize=6;
            var source=Ref<ResultUserRowUI>(result,team=="Blue"?"blueTeamRowPrefab":"redTeamRowPrefab");
            for(int i=0;i<6;i++)
            {
                string name="AuthoredKDA"+i+team;
                var row=parent.Find(name)?.GetComponent<ResultUserRowUI>();
                if(row==null) { row=Object.Instantiate(source,parent); row.name=name; }
                rows.GetArrayElementAtIndex(i).objectReferenceValue=row; row.gameObject.SetActive(false);
            }
        }
        // Interleave both color variants; the active variant at each saved index is used.
        for(int i=0;i<6;i++) foreach(string team in new[]{"Blue","Red"}) parent.Find("AuthoredKDA"+i+team).SetAsLastSibling();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach(var button in result.GetComponentsInChildren<Button>(true)) Hover(button);
        snapshot.Check(); EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Result save failed");
    }
    static void InstallRank()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/RankResult.unity"); var snapshot=new Preservation(scene);
        var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RankResultController>(true)).Single();
        var rankCanvas=controller.GetComponentInParent<Canvas>();
        Find(rankCanvas.transform,"Background").GetComponent<Image>().color=paper;
        VideoBackdrop(Find(rankCanvas.transform,"Background"));
        Find(rankCanvas.transform,"Center Glow").GetComponent<Image>().color=Color.clear;
        Find(rankCanvas.transform,"Top Accent").GetComponent<Image>().color=red;
        ((RectTransform)Find(rankCanvas.transform,"Top Accent")).anchoredPosition=new Vector2(0,480);
        var header=Find(controller.transform,"Header"); Pose(header,0,396,1300,55);
        header.GetComponent<TMP_Text>().text="랭크 결과"; header.GetComponent<TMP_Text>().fontSize=28;
        var change=Find(controller.transform,"Rank Change"); Pose(change,315,250,920,100);
        Find(rankCanvas.transform,"Bottom Accent").GetComponent<Image>().color=ink;
        foreach(var text in controller.GetComponentsInChildren<TMP_Text>(true)) text.color=ink;
        var rankSerialized=new SerializedObject(controller);
        rankSerialized.FindProperty("increaseColor").colorValue=new Color32(143,83,24,255);
        rankSerialized.FindProperty("decreaseColor").colorValue=red; rankSerialized.FindProperty("neutralColor").colorValue=ink;
        rankSerialized.ApplyModifiedPropertiesWithoutUndo();
        Ref<Slider>(controller,"rankProgressSlider").fillRect.GetComponent<Image>().color=red;
        var proceed=Ref<Button>(controller,"continueButton"); proceed.image.color=red; proceed.GetComponentInChildren<TMP_Text>().color=Color.white;
        Motion(Find(controller.transform,"Rank Frame").gameObject,.1f).initialScale=.92f;
        Motion(Find(controller.transform,"Rank Name").gameObject,.2f);
        Motion(Find(controller.transform,"Rank Change").gameObject,.3f);
        var frame=Find(controller.transform,"Rank Frame");
        frame.GetComponent<Image>().color=Color.clear; Pose(frame,-460,20,540,540);
        foreach(string name in new[]{"ReferenceCrown","ReferenceBase","ReferenceRailLeft","ReferenceRailRight"})
            if(frame.Find(name)!=null) frame.Find(name).gameObject.SetActive(false);
        Pose(Ref<Image>(controller,"rankImage").transform,0,0,520,520);
        var rankName=Ref<TMP_Text>(controller,"rankNameText"); Pose(rankName.transform,315,115,730,90); rankName.fontSize=62;
        Pose(Ref<GameObject>(controller,"rankProgressRoot").transform,315,5,680,18);
        Pose(Ref<TMP_Text>(controller,"rankPointText").transform,315,-57,730,48);
        var delta=Ref<TMP_Text>(controller,"rankDeltaText"); Pose(delta.transform,315,-145,730,88); delta.fontSize=60;
        Pose(proceed.transform,315,-332,480,64);
        Pose(Find(controller.transform,"Continue Hint"),315,-395,730,35);
        foreach(var button in controller.GetComponentsInChildren<Button>(true)) Hover(button);
        snapshot.Check(); EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Rank save failed");
    }
    static void Fixture()
    {
        MatchResultCache.Clear(); MatchResultCache.scoreA=7; MatchResultCache.scoreB=4; MatchResultCache.winnerTeam=0;
        MatchResultCache.freeCurrencyReward=120; MatchResultCache.victoryCurrencyReward=35;
        for(int i=0;i<6;i++) MatchResultCache.members.Add(new MatchResultMemberData{playerId=(uint)i+1,team=(byte)(i/3),slotIndex=(byte)(i%3),characterId=i%4,nickname=i==0?"VECTOR":$"플레이어 {i+1}",kills=18-i*2,deaths=4+i,assists=6+i,firstHalfKills=8-i,secondHalfKills=10-i,isMvp=i==0});
        foreach(var member in MatchResultCache.members) member.appearance=new MatchAppearanceData{playerIconId=(int)(member.playerId%3),rankImageId=2,level=23,preset=new ChipCraftPresetData{cardId=(int)(member.playerId%3),caseId=1,medals=new List<ChipCraftMedalData>{new(){medalId=2,x=.25f,y=-.27f,scale=1,rotation=15}}}};
    }
    static void VerifyRank()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/RankResult.unity");
        var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RankResultController>(true)).Single();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        MatchResultCache.rankPointDelta=25; MatchResultCache.rankExpectedWinRatePermille=420; MatchResultCache.rankModifierFlags=1;
        typeof(RankResultController).GetMethod("ApplyRankPresentation",flags).Invoke(controller,new object[]{1015,10});
        typeof(RankResultController).GetMethod("ApplyChangeText",flags).Invoke(controller,new object[]{9,10});
        Ref<TMP_Text>(controller,"rankDeltaText").text="+25"; Ref<TMP_Text>(controller,"rankDeltaText").color=red;
        if(Ref<TMP_Text>(controller,"rankNameText").text!="골드 5" || !Ref<TMP_Text>(controller,"rankChangeText").text.StartsWith("승급")) throw new Exception("Rank/tier presentation failed");
        CaptureCanvas(controller.GetComponentInParent<Canvas>(),"VectorRankResult.png");
        MatchResultCache.Clear(); Debug.Log("RANK_PRESENTATION_VERIFIED: server points/tier, promotion, modifiers; no new UI.");
    }
    public static void VerifyResult()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/Result.unity");
        var result=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResultUI>(true)).Single();
        var ui=result.GetComponentInChildren<MatchResultPresentation>(true);
        int Count()=>scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
        int before=Count(); Fixture();
        try
        {
            ui.Show(1);
            foreach(float moment in new[]{0f,.3f,.7f,1.1f,1.8f,2.8f})
            {
                ui.Evaluate(moment/ui.duration);
                CaptureCanvas(result.GetComponent<Canvas>(),"VectorResultMotion-"+Mathf.RoundToInt(moment*10).ToString("00")+".png");
            }
            ui.Continue();
            if(ui.outcome.text!="VICTORY" || ui.spotlightCaption.text!="MATCH MVP" || ui.freeReward.text!="+120") throw new Exception("Result fixture failed");
            for(int selected=0;selected<3;selected++)
            {
                MatchResultCache.members[0].appearance.playerIconId=selected;
                ui.Show(1); ui.Continue();
                if(ui.profileIcon.sprite!=Resources.Load<Sprite>($"PlayerIcon/{selected}") || ui.playerCard.sprite!=Resources.Load<Sprite>($"IconCard/icon{selected}_card")) throw new Exception("Profile icon/card ID mapping diverged");
            }
            MatchResultCache.members[0].appearance.playerIconId=1; ui.Show(1); ui.Continue();
            ui.personalKda.ForceMeshUpdate(); if(ui.personalKda.textInfo.characterCount<5) throw new Exception("KDA text clipped");
            CaptureCanvas(result.GetComponent<Canvas>(),"VectorResultHighlight.png");
            ui.Continue(); if(ui.overlay.activeSelf) throw new Exception("Skip/detail failed");
            for(int i=0;i<5;i++) { result.OnClickFirstHalf(); result.OnClickSecondHalf(); result.OnClickArrayButton(); result.OnClickTotal(); }
            if(Count()!=before) throw new Exception("Result sorting created UI objects");
            ui.Show(4); ui.Evaluate(1); if(ui.outcome.text!="DEFEAT") throw new Exception("Defeat not recognized");
            MatchResultCache.scoreA=4; ui.Show(1); if(ui.outcome.text!="DRAW") throw new Exception("Draw not recognized");
            foreach(var member in MatchResultCache.members) member.isMvp=false;
            ui.Show(1); if(ui.spotlightCaption.text=="MATCH MVP") throw new Exception("Invented MVP");
            ui.Show(999); ui.Evaluate(1); if(ui.freeReward.text!="—") throw new Exception("Spectator awarded reward");
            MatchResultCache.members.Add(new MatchResultMemberData{playerId=999,team=2,nickname="관전자"});
            ui.Show(999); ui.Evaluate(1);
            if(ui.outcome.text!="MATCH COMPLETE" || ui.personalKda.text!="—" || ui.freeReward.text!="—") throw new Exception("Observer treated as defeated player");
            if(ui.equippedIdentity.activeSelf) throw new Exception("Invented appearance for a missing result player");
            if(MatchResultCache.freeCurrencyReward!=120 || MatchResultCache.victoryCurrencyReward!=35) throw new Exception("Presentation mutated rewards");
            Fixture(); ui.Dismiss(); result.OnClickTotal(); CaptureCanvas(result.GetComponent<Canvas>(),"VectorResultDetails.png");
            MatchResultCache.Clear(); ui.Show(1); if(ui.overlay.activeSelf) throw new Exception("Empty cache overlay");
            Debug.Log($"RESULT_POLISH_VERIFIED: {before} objects unchanged; victory/defeat/draw/spectator, MVP absence, skip/replay, sort/half tabs, immutable rewards.");
        }
        finally { MatchResultCache.Clear(); }
    }
    static void CaptureLobby()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var profile=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MyPagePanelController>(true)).Single();
        var group=profile.GetComponent<CanvasGroup>(); group.alpha=1; group.interactable=group.blocksRaycasts=true; profile.gameObject.SetActive(true);
        foreach(var other in new[]{"BattleCustomPanel","SkinCustomPanel","RecentVisitorsPopup"}) Find(profile.transform,other).gameObject.SetActive(false);
        profile.GetComponent<MyPageTabController>().Select(MyPageTabController.Page.Profile);
        profile.ApplyProfile(new MyPageProfileData{isSelf=true,userId="preview",nickname="VECTOR",level=23,currentTierName="골드 3",currentRankImageId=2,seasonHighestRankImageId=2,allTimeHighestRankImageId=3,joinDate="2026-09-15",region="대한민국",birthday="09.15",playCount=25,wins=16,totalKills=186,totalAssists=84,totalDamage=42000,roundsPlayed=240,mvpCount=7,headshotKills=78});
        Canvas.ForceUpdateCanvases(); Find(profile.transform,"MainScrollView").GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        CaptureCanvas(profile.GetComponentInParent<Canvas>(),"VectorProfilePolish.png",profile.gameObject,1);
        Find(profile.transform,"MainScrollView").GetComponent<ScrollRect>().verticalNormalizedPosition=0;
        CaptureCanvas(profile.GetComponentInParent<Canvas>(),"VectorProfileStats.png",profile.gameObject,0);
    }
    public static void CaptureCanvas(Canvas source,string file,GameObject focus=null,float scrollPosition=1,Action<Transform,Camera> verify=null)
    {
        var host=new GameObject("DiagnosticCanvas",typeof(Canvas)).GetComponent<Canvas>(); host.gameObject.layer=5;
        host.renderMode=RenderMode.WorldSpace; ((RectTransform)host.transform).sizeDelta=new Vector2(1920,1080);
        var clone=Object.Instantiate(focus!=null?focus:source.gameObject,host.transform,false); clone.SetActive(true);
        var rect=(RectTransform)clone.transform; rect.localScale=Vector3.one; rect.localPosition=Vector3.zero; Stretch(rect);
        var canvas=clone.GetComponent<Canvas>(); if(canvas==null) canvas=host; canvas.renderMode=RenderMode.WorldSpace;
        var scaler=clone.GetComponent<CanvasScaler>(); if(scaler!=null) scaler.enabled=false;
        var camera=new GameObject("DiagnosticCamera",typeof(Camera)).GetComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=540; camera.aspect=1920f/1080; camera.transform.position=new Vector3(0,0,-10); camera.farClipPlane=100; camera.cullingMask=1<<5;
        host.worldCamera=canvas.worldCamera=camera;
        var originals=source.gameObject.scene.GetRootGameObjects().Where(r=>r!=host.gameObject&&r!=camera.gameObject).Select(r=>(r,r.activeSelf)).ToArray();
        foreach(var pair in originals) pair.r.SetActive(false);
        var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline; var quality=QualitySettings.renderPipeline; var old=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24); var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            foreach(var motion in clone.GetComponentsInChildren<AuthoredPanelMotion>(true)) { motion.ResetMotion(); }
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null; QualitySettings.renderPipeline=null; camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();
            if(focus!=null) Find(clone.transform,"MainScrollView").GetComponent<ScrollRect>().verticalNormalizedPosition=scrollPosition;
            foreach(var g in clone.GetComponentsInChildren<Graphic>()) { g.canvasRenderer.SetColor(Color.white); g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); }
            foreach(var text in clone.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target; texture.ReadPixels(new UnityEngine.Rect(0,0,1920,1080),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),file),texture.EncodeToPNG());
            verify?.Invoke(clone.transform,camera);
        }
        finally
        {
            camera.targetTexture=null; RenderTexture.active=old; UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=quality;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(target); Object.DestroyImmediate(host.gameObject); Object.DestroyImmediate(camera.gameObject);
            foreach(var pair in originals) pair.r.SetActive(pair.activeSelf);
        }
    }
}
