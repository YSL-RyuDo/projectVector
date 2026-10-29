using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class VectorEventPassEditor
{
    static TMP_FontAsset font;
    public static void PolishAndCapture()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var pass=all.Select(t=>t.GetComponent<VectorEventPass>()).First(p=>p!=null);
        font=pass.levelLabel.font;
        pass.rewardsNavigation=pass.transform.Find("RewardsNavigation").GetComponent<Button>();pass.questsNavigation=pass.transform.Find("QuestsNavigation").GetComponent<Button>();
        foreach(var label in pass.rewardLabels)label.color=Ink;
        var promo=all.Select(t=>t.GetComponent<VectorLobbyPromotion>()).First(p=>p!=null);
        var store=all.First(t=>t.name=="Store Panel");
        var hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Modules/Login/UI/Sprites/Wallpaper_Summer_Music_Festival.jpg");
        var profile=Resources.Load<Sprite>("Sprites/InitiatorSprite");
        if(hero==null)throw new InvalidOperationException("Shop hero artwork missing");
        pass.GetComponent<Image>().sprite=hero;
        promo.banners=new[]{hero,profile??hero};promo.titles=new[]{"스킨 보급","나만의 프로필"};promo.shopPages=new[]{1,2};promo.shop=store.GetComponent<VectorShopNavigation>();
        promo.art.sprite=hero;promo.indicators=promo.indicators.Take(2).ToArray();
        var extra=promo.transform.Find("Page2");if(extra!=null)extra.gameObject.SetActive(false);
        pass.rewardLevels=new TMP_Text[5];pass.rewardStates=new TMP_Text[5];
        for(int i=0;i<5;i++){
            var b=pass.rewardButtons[i];var label=pass.rewardLabels[i];Place(label.rectTransform,8,212,120,65);label.fontSize=23;label.text=i==4?"재화 1\n×3":"재화 0\n×300";
            pass.rewardLevels[i]=SavedLabel(b.transform,"Level",$"Lv.{i+1}",8,16,120,42,26);
            pass.rewardStates[i]=SavedLabel(b.transform,"State","잠김",8,285,120,36,21);
        }
        pass.questStates=new TMP_Text[3];pass.questProgress=new Image[3];
        for(int i=0;i<3;i++){
            Place(pass.questLabels[i].rectTransform,16,10,550,62);pass.questLabels[i].fontSize=23;
            pass.questStates[i]=SavedLabel(pass.questButtons[i].transform,"ClaimState","진행 중",585,15,130,50,24);
            var track=pass.questButtons[i].transform.Find("ProgressTrack") as RectTransform;
            if(track==null)track=Box(pass.questButtons[i].transform,"ProgressTrack",16,73,530,4,new Color32(155,166,183,255));
            var fill=track.Find("Fill") as RectTransform;if(fill==null)fill=Box(track,"Fill",0,0,530,4,new Color32(233,179,54,255));
            var image=fill.GetComponent<Image>();image.sprite=pass.progressBar.GetComponent<Image>().sprite;
            // Filled images need a sprite; use the authored default UI sprite if the track has none.
            if(image.sprite==null)image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=0;image.raycastTarget=false;pass.questProgress[i]=image;
        }
        pass.rewardsPanel.transform.Find("Previous").GetComponentInChildren<TMP_Text>().text="이전";
        pass.rewardsPanel.transform.Find("Next").GetComponentInChildren<TMP_Text>().text="다음";
        Stretch(pass.progressBar.fillRect);pass.progressBar.value=.5f;pass.progressBar.value=0;
        promo.bannerVisibility=promo.GetComponent<CanvasGroup>()??promo.gameObject.AddComponent<CanvasGroup>();
        var entry=all.First(t=>t.name=="GamePassButton");promo.entryVisibility=entry.GetComponent<CanvasGroup>()??entry.gameObject.AddComponent<CanvasGroup>();
        promo.transform.SetSiblingIndex(entry.GetSiblingIndex());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var host=new GameObject("OfflineCapture",typeof(Canvas));host.layer=5;var canvas=host.GetComponent<Canvas>();
        var cam=new GameObject("OfflineCamera",typeof(Camera)).GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=540;cam.aspect=1920f/1080;cam.transform.position=new Vector3(0,0,-10);cam.cullingMask=1<<5;cam.farClipPlane=100;
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cam;host.GetComponent<RectTransform>().sizeDelta=new Vector2(1920,1080);
        var clone=UnityEngine.Object.Instantiate(pass.gameObject,host.transform,false);clone.SetActive(true);
        foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
        foreach(var obj in scene.GetRootGameObjects())if(obj!=host&&obj!=cam.gameObject)obj.SetActive(false);
        var view=clone.GetComponent<VectorEventPass>();view.enabled=false;
        var check=new byte[60];BitConverter.GetBytes(30).CopyTo(check,4);BitConverter.GetBytes(2900).CopyTo(check,8);BitConverter.GetBytes(3).CopyTo(check,36);
        view.ApplyState(check);view.ShowQuests();if(!view.questButtons[0].interactable)throw new Exception("Completed quest not claimable");
        for(int i=0;i<6;i++)view.Next();if(view.pageLabel.text!="6 / 6"||view.rewardLevels[4].text!="Lv.30")throw new Exception("Last reward page mismatch");
        BitConverter.GetBytes(0x3fffffff).CopyTo(check,12);BitConverter.GetBytes(63).CopyTo(check,16);view.ApplyState(check);
        if(view.rewardButtons.Any(b=>b.interactable)||view.questButtons.Any(b=>b.interactable))throw new Exception("Claimed rewards still interactable");
        for(int i=0;i<6;i++)view.Previous();
        var body=new byte[60];BitConverter.GetBytes(1).CopyTo(body,4);BitConverter.GetBytes(1).CopyTo(body,36);view.ApplyState(body);view.ShowRewards();
        Capture(cam,clone,"VectorPassRewards.png");view.ShowQuests();Capture(cam,clone,"VectorPassQuests.png");
        Debug.Log("EVENT_PASS_CAPTURED offline fixture; no account writes.");
    }
    static TMP_Text SavedLabel(Transform parent,string name,string value,float x,float y,float w,float h,int size){var old=parent.Find(name);var label=old!=null?old.GetComponent<TMP_Text>():Label(parent,name,value,x,y,w,h,size,Ink);Place(label.rectTransform,x,y,w,h);label.alignment=TextAlignmentOptions.Center;label.color=Ink;return label;}
    static void Capture(Camera cam,GameObject root,string file)
    {
        var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
        var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;cam.targetTexture=rt;
            Canvas.ForceUpdateCanvases();foreach(var g in root.GetComponentsInChildren<Graphic>()){g.canvasRenderer.SetColor(Color.white);g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);}foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),file),tex.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    static readonly Color Ink=new Color32(32,37,48,255), Red=new Color32(195,35,61,255), Blue=new Color32(60,79,115,255);
    [MenuItem("Project Vector/UI/Upgrade Saved Event Pass")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit play mode first.");
        var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        if(existing.IsValid() && existing.isDirty)throw new InvalidOperationException("Save the Lobby scene first.");
        var scene=existing.IsValid()&&existing.isLoaded?existing:EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var root=all.First(t=>t.name=="Game Pass Panel");
        if(root.GetComponent<VectorEventPass>()!=null)throw new InvalidOperationException("Already installed; edit the saved objects directly.");
        font=root.GetComponentsInChildren<TMP_Text>(true).First(t=>t.font!=null).font;
        var store=all.First(t=>t.name=="Store Panel");
        var arts=store.GetComponentsInChildren<Image>(true).Where(i=>i.sprite!=null && i.sprite.rect.width>500 && i.sprite.rect.width>i.sprite.rect.height*1.2f).Select(i=>i.sprite).Distinct().Take(3).ToArray();
        if(arts.Length==0)throw new InvalidOperationException("Missing existing shop artwork.");
        // The old pass controls are replaced only within the requested pass panel.
        // Keep the panel itself so existing serialized SetActive references remain valid.
        foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(var old in root.GetComponents<GamePassManager>())UnityEngine.Object.DestroyImmediate(old);
        root.localScale=Vector3.one;Stretch((RectTransform)root);
        var screen=root.GetComponent<Image>();screen.sprite=arts[0];screen.color=Color.white;
        var pass=root.gameObject.AddComponent<VectorEventPass>();
        Box(root,"Readability",0,0,1920,1080,new Color(.08f,.12f,.19f,.35f));
        Box(root,"TopBar",0,0,1920,78,Ink);
        Label(root,"Title","이벤트 센터",40,15,260,55,34,Color.white);
        Button(root,"EventTab","VECTOR 성장 패스",300,8,310,62,Red,null);
        Button(root,"Close","ESC  뒤로",1690,18,180,45,Blue,pass.Close);
        Label(root,"PassTitle","함께 만드는 다음 작전",870,175,960,90,64,Color.white);
        Label(root,"PassSubtitle","VECTOR 성장 패스 · 모든 보상 무료",1110,280,700,45,27,Color.white);
        Button(root,"RewardsNavigation","성장 패스 보상",45,170,290,85,Red,pass.ShowRewards);
        Button(root,"QuestsNavigation","주간 · 누적 퀘스트",45,270,290,85,Blue,pass.ShowQuests);
        Box(root,"PassContentBackdrop",1040,415,820,610,new Color(.94f,.95f,.98f,.92f));
        pass.levelLabel=Label(root,"Level","Lv.— / 30",1090,435,365,62,46,Ink);
        var bar=Box(root,"PassProgress",1090,505,720,9,new Color32(158,168,183,255));
        var fill=Box(bar,"Fill",0,0,720,9,new Color32(246,193,67,255));
        pass.progressBar=bar.gameObject.AddComponent<Slider>();pass.progressBar.fillRect=fill;pass.progressBar.interactable=false;pass.progressBar.value=0;
        pass.rewardTab=Button(root,"RewardTab","보상",1470,437,165,52,Red,pass.ShowRewards);
        pass.questTab=Button(root,"QuestTab","퀘스트",1645,437,165,52,Blue,pass.ShowQuests);
        pass.rewardsPanel=Rect(root,"Rewards",1080,555,750,390).gameObject;
        pass.rewardButtons=new Button[5];pass.rewardLabels=new TMP_Text[5];pass.rewardIcons=new Image[5];
        pass.currency0Icon=Resources.Load<Sprite>("LevelRewards/Currency0Ticket");pass.currency1Icon=Resources.Load<Sprite>("LevelRewards/Currency1Crystal");
        for(int i=0;i<5;i++)
        {
            var b=Button(pass.rewardsPanel.transform,"RewardSlot"+i,"",i*148,0,136,335,new Color32(240,230,215,255),null);
            pass.rewardButtons[i]=b;pass.rewardLabels[i]=b.GetComponentInChildren<TMP_Text>();pass.rewardLabels[i].fontSize=25;
            pass.rewardLabels[i].text=$"Lv.{i+1}\n\n\n\n{(i==4?"재화 1 ×3":"재화 0 ×300")}\n잠김";
            var icon=Box(b.transform,"RewardIcon",18,90,100,110,Color.white).GetComponent<Image>();icon.sprite=i==4?pass.currency1Icon:pass.currency0Icon;icon.preserveAspect=true;icon.raycastTarget=false;pass.rewardIcons[i]=icon;
        }
        Button(pass.rewardsPanel.transform,"Previous","〈",180,347,80,42,Blue,pass.Previous);
        pass.pageLabel=Label(pass.rewardsPanel.transform,"Page","1 / 6",280,349,140,40,23,Ink);pass.pageLabel.alignment=TextAlignmentOptions.Center;
        Button(pass.rewardsPanel.transform,"Next","〉",440,347,80,42,Blue,pass.Next);
        pass.questsPanel=Rect(root,"Quests",1080,540,750,415).gameObject;
        pass.weeklyTab=Button(pass.questsPanel.transform,"WeeklyTab","주간 챌린지",0,0,360,48,Red,pass.ShowWeekly);
        pass.cumulativeTab=Button(pass.questsPanel.transform,"CumulativeTab","누적 퀘스트",370,0,360,48,Blue,pass.ShowCumulative);
        pass.questButtons=new Button[3];pass.questLabels=new TMP_Text[3];
        for(int i=0;i<3;i++){var b=Button(pass.questsPanel.transform,"Quest"+i,"퀘스트 불러오는 중",0,65+i*95,730,82,new Color32(216,222,232,255),null);pass.questButtons[i]=b;var label=b.GetComponentInChildren<TMP_Text>();label.color=Ink;label.fontSize=25;label.alignment=TextAlignmentOptions.Left;pass.questLabels[i]=label;}
        pass.questGroupLabel=Label(pass.questsPanel.transform,"Reset","매주 월요일 09:00 갱신 (한국 시간)",0,360,730,40,21,Ink);
        pass.questsPanel.SetActive(false);
        pass.statusLabel=Label(root,"Status","퀘스트를 완료하고 패스 레벨을 올려 보세요.",1080,962,575,45,20,Ink);
        Button(root,"Reload","새로고침",1670,957,155,45,Blue,pass.Reload);
        // Reuse the old entry button and its root, rather than adding a competing lobby entry.
        var entry=all.First(t=>t.name=="GamePassButton");
        Place((RectTransform)entry,35,285,325,46);entry.localScale=Vector3.one;
        entry.GetComponentInChildren<TMP_Text>(true).text="이벤트 개요";
        var entryButton=entry.GetComponent<Button>();entryButton.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(entryButton.onClick,pass.Open);entryButton.image.color=Red;
        var promo=Rect(entry.parent,"LobbyShopPromotion",35,95,325,183);
        var promotion=promo.gameObject.AddComponent<VectorLobbyPromotion>();promotion.pass=pass;promotion.navigation=all.Select(t=>t.GetComponent<NavigationPanelController>()).First(c=>c!=null);
        var hero=Button(promo,"ShopBanner","",0,0,325,183,Color.white,promotion.OpenShop);hero.image.sprite=arts[0];promotion.art=hero.image;
        Box(promo,"CaptionStrip",0,0,325,34,Ink);promotion.title=Label(promo,"BannerTitle","상점 추천",10,4,305,29,20,Color.white);
        promotion.banners=arts;promotion.titles=arts.Select((s,i)=>new[]{"스킨 보급","스킨 컬렉션","추천 상점"}[i]).ToArray();
        promotion.indicators=new Image[arts.Length];for(int i=0;i<arts.Length;i++){var dot=Button(promo,"Page"+i,"",125+i*25,160,12,12,Color.white,null);UnityEventTools.AddIntPersistentListener(dot.onClick,promotion.Show,i);promotion.indicators[i]=dot.image;}
        root.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("EVENT_PASS_SAVED: Lobby panel, authored slots, quests, banner, entry links.");
    }
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Place(r,x,y,w,h);return r;}
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static RectTransform Box(Transform p,string n,float x,float y,float w,float h,Color c){var r=Rect(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;return r;}
    static TMP_Text Label(Transform p,string n,string value,float x,float y,float w,float h,int size,Color c){var r=Rect(p,n,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=c;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string n,string value,float x,float y,float w,float h,Color c,UnityAction action){var r=Box(p,n,x,y,w,h,c);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.navigation=new Navigation{mode=Navigation.Mode.None};var t=Label(r,"Label",value,8,6,w-16,h-12,26,Color.white);t.alignment=TextAlignmentOptions.Center;if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
}
