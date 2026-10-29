using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Explicit scene authoring only. Runtime scripts only switch these saved pages.
public static class VectorShopLayoutEditor
{
    static TMP_FontAsset font;
    static readonly Color Red=new Color32(194,39,57,255), Dark=new Color32(33,38,47,255), Gray=new Color32(65,71,81,255), Gold=new Color32(255,210,93,255);
    [MenuItem("Project Vector/UI/Apply Reference Shop Layout")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity"); bool opened=!scene.IsValid() || !scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity",OpenSceneMode.Additive);
        if(scene.isDirty) throw new InvalidOperationException("Save your unsaved Lobby edits first.");
        try
        {
            var shop=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VectorShopPanel>(true)).Single();
            var store=shop.transform;
            if(store.GetComponent<VectorShopNavigation>()!=null) { Validate(store); return; }
            font=shop.description.font;
            var nav=store.gameObject.AddComponent<VectorShopNavigation>(); nav.shop=shop;
            var main=store.Find("Main Panel"); var draw=store.Find("Weapon Skin Gacha Panel");
            var profile=store.Find("Profile Gacha Panel"); var exchange=store.Find("Exchange Store Panel");
            var catalog=Panel(store,"SkinCatalogPanel",new Color32(195,205,218,255));
            nav.pages=new[]{main.gameObject,draw.gameObject,profile.gameObject,exchange.gameObject,catalog.gameObject};
            nav.legacyMoney=store.Find("Money Group").gameObject;
            var header=Panel(store,"ShopTabBar",Color.clear); Box(header,60,101,1800,48);
            var canvas=header.gameObject.AddComponent<Canvas>(); header.gameObject.AddComponent<GraphicRaycaster>();
            var serialized=new SerializedObject(canvas); serialized.FindProperty("m_OverrideSorting").boolValue=true; serialized.FindProperty("m_SortingOrder").intValue=110; serialized.ApplyModifiedPropertiesWithoutUndo();
            nav.tabs=new Image[5];
            string[] labels={"인기 추천","스킨 보급","프로필 뽑기","교환 상점","스킨 상점"};
            for(int i=0;i<labels.Length;++i)
            {
                var tab=Button(header,"Tab_"+i,labels[i],i*224,0,216,42,null,Gray);
                UnityEventTools.AddIntPersistentListener(tab.onClick,nav.SelectPage,i); nav.tabs[i]=tab.GetComponent<Image>();
            }
            nav.wallet=Label(header,"ShopWallet","상점 재화 확인 중…",1130,0,630,42,19,Color.white);
            var walletBackground=Image(header,"WalletBackground",Dark); Box(walletBackground,1126,0,654,42); walletBackground.SetAsFirstSibling();
            RebuildRecommendation(main,nav,shop);
            BuildCatalog(catalog,nav,shop);
            RestyleDraw(draw,shop,nav);
            foreach(var page in new[]{profile,exchange,draw})
            {
                var back=page.Find("Back Button");
                if(back!=null && back.TryGetComponent<Button>(out var button)) UnityEventTools.AddIntPersistentListener(button.onClick,nav.SelectPage,0);
            }
            for(int i=0;i<nav.pages.Length;++i) nav.pages[i].SetActive(i==0);
            nav.tabs[0].color=Red; nav.legacyMoney.SetActive(false);
            EditorUtility.SetDirty(nav); EditorSceneManager.MarkSceneDirty(scene); Validate(store);
            if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Lobby save failed.");
            Debug.Log("[SHOP LAYOUT] Saved reference recommendation, tab navigation, 4-column catalog, filters/sort and redesigned draw page; no cash purchase UI.");
        }
        finally {if(opened) EditorSceneManager.CloseScene(scene,true);}
    }
    static void RebuildRecommendation(Transform main,VectorShopNavigation nav,VectorShopPanel shop)
    {
        var hero=main.Find("Weapon Skin Gacha Button"); var profile=main.Find("Profile Gacha Button"); var exchange=main.Find("Exchange Store Button");
        if(hero==null || profile==null || exchange==null) throw new InvalidOperationException("Original recommendation button names changed; refusing to replace them.");
        Label(main,"RecommendationHeading","RECOMMENDED  /  추천 콘텐츠",90,153,650,26,20,new Color32(179,105,40,255));
        var scroll=Scroll(main,"RecommendationScroll",90,192,1740,775,out var content); content.sizeDelta=new Vector2(0,1110);
        hero.SetParent(content,false); profile.SetParent(content,false); exchange.SetParent(content,false);
        Banner(hero,"스킨 보급",0,0,1040,590,nav,1);
        Banner(profile,"나만의 프로필",1060,0,665,282,nav,2);
        Banner(exchange,"교환 상점",1060,302,665,288,nav,3);
        // Use project-owned preview art instead of repeating the same promotional image three times.
        var profileArt=Resources.Load<Sprite>("Sprites/InitiatorSprite");
        if(profileArt!=null) {profile.GetComponent<Image>().sprite=profileArt; profile.GetComponent<Image>().preserveAspect=false;}
        var exchangeImage=exchange.GetComponent<Image>(); exchangeImage.sprite=null; exchangeImage.color=new Color32(33,53,74,255);
        var token=Image(exchange,"ExchangePreview",Color.white); Box(token,395,20,220,210); token.GetComponent<Image>().sprite=Resources.Load<Sprite>("LevelRewards/Currency1Crystal"); token.GetComponent<Image>().preserveAspect=true; token.GetComponent<Image>().raycastTarget=false;
        Label(exchange,"ExchangeHeading","게임에서 얻은 재화로\n원하는 보상을 선택하세요",38,62,360,120,28,Color.white);
        Label(content,"FeaturedHeading","COLLECTION  /  스킨 컬렉션",0,620,800,38,25,Dark);
        var featured=shop.rewardSlots.Where(s=>s.itemId>=1000).Take(4).ToArray();
        for(int i=0;i<featured.Length;++i)
        {
            var item=featured[i]; var card=Product(content,"Featured_"+item.itemId,item,412,380);
            Box((RectTransform)card.root.transform,i*437,676,412,380);
            UnityEventTools.AddIntPersistentListener(card.root.GetComponent<Button>().onClick,nav.OpenProduct,item.itemId);
        }
    }
    static void Banner(Transform target,string title,float x,float y,float w,float h,VectorShopNavigation nav,int page)
    {
        Box((RectTransform)target,x,y,w,h);
        foreach(var text in target.GetComponentsInChildren<TMP_Text>(true))
        { text.text=title; text.fontSize=29; text.alignment=TextAlignmentOptions.Center; Stretch(text.rectTransform,0,0,1,.13f); }
        var caption=Image(target,"CaptionStrip",new Color(0,0,0,.72f)); Stretch(caption,0,0,1,.13f); caption.SetAsFirstSibling(); caption.GetComponent<Image>().raycastTarget=false;
        UnityEventTools.AddIntPersistentListener(target.GetComponent<Button>().onClick,nav.SelectPage,page);
    }
    static void BuildCatalog(RectTransform root,VectorShopNavigation nav,VectorShopPanel shop)
    {
        nav.filters=new Image[3];
        for(int i=0;i<3;++i)
        {
            var button=Button(root,"Filter_"+i,new[]{"전체","캐릭터 외형","무기 스킨"}[i],90,230+i*72,240,58,null,i==0?Red:Gray);
            UnityEventTools.AddIntPersistentListener(button.onClick,nav.Filter,i-1); nav.filters[i]=button.GetComponent<Image>();
        }
        Label(root,"CurrencyNotice","현금 결제 없음\n게임 내 재화 전용",95,470,230,85,20,Gray);
        Button(root,"DefaultSort","기본",365,176,150,42,nav.DefaultOrder,Dark);
        var sort=Button(root,"PriceSort","가격순 ↕",525,176,210,42,nav.TogglePriceOrder,Gray); nav.sortLabel=sort.GetComponentInChildren<TMP_Text>(true);
        nav.catalogCount=Label(root,"ProductCount","70개 상품 · 게임 내 재화 전용",1170,176,650,42,20,Dark); nav.catalogCount.alignment=TextAlignmentOptions.Right;
        nav.catalogScroll=Scroll(root,"ProductScroll",365,236,1460,720,out var content);
        var grid=content.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=4; grid.cellSize=new Vector2(350,352); grid.spacing=new Vector2(14,18); grid.padding=new RectOffset(0,10,0,16);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var cards=new List<VectorShopSlot>();
        foreach(var reward in shop.rewardSlots.OrderBy(s=>s.itemId))
        {
            var card=Product(content,"Product_"+reward.itemId,reward,350,352); cards.Add(card);
            UnityEventTools.AddIntPersistentListener(card.root.GetComponent<Button>().onClick,nav.OpenProduct,reward.itemId);
        }
        nav.products=cards.ToArray();
    }
    static VectorShopSlot Product(Transform parent,string name,VectorShopSlot source,float w,float h)
    {
        var rect=Image(parent,name,Dark); Box(rect,0,0,w,h); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
        var colors=button.colors; colors.highlightedColor=new Color(1.18f,1.18f,1.18f,1); colors.pressedColor=new Color(.75f,.75f,.75f,1); button.colors=colors;
        var frame=Image(rect,"InnerFrame",new Color32(59,70,84,255)); Stretch(frame,.025f,.24f,.975f,.975f); frame.GetComponent<Image>().raycastTarget=false;
        var icon=Image(rect,"ModelArtwork",Color.white); Stretch(icon,.04f,.27f,.96f,.95f); icon.GetComponent<Image>().sprite=source.icon.sprite; icon.GetComponent<Image>().preserveAspect=true; icon.GetComponent<Image>().raycastTarget=false;
        var line=Image(rect,"RarityLine",source.rarity.color); Stretch(line,0,.22f,1,.228f); line.GetComponent<Image>().raycastTarget=false;
        var label=Label(rect,"NameAndPrice",source.label.text.Split('\n')[0]+"\n상세 보기",12,h*.79f,w-24,h*.20f,23,Color.white); label.alignment=TextAlignmentOptions.Center;
        return new VectorShopSlot {itemId=source.itemId,root=rect.gameObject,icon=icon.GetComponent<Image>(),rarity=line.GetComponent<Image>(),label=label};
    }
    static void RestyleDraw(Transform draw,VectorShopPanel shop,VectorShopNavigation nav)
    {
        var backdrop=Image(draw,"SupplyBackdrop",new Color32(21,35,67,255)); Stretch(backdrop,0,0,1,.85f); backdrop.SetAsFirstSibling();
        var band=Image(backdrop,"StageBand",new Color32(31,51,87,255)); Box(band,540,105,795,580);
        for(int i=0;i<6;++i)
        {
            var line=Image(backdrop,"StageLine"+i,new Color(.3f,.66f,.95f,.16f)); Box(line,650+i*70,40,2,760); line.localRotation=Quaternion.Euler(0,0,-25);
        }
        Label(draw,"SupplyTitle","VECTOR\n스킨 보급",80,212,440,155,54,Color.white);
        Label(draw,"SupplyDescription","캐릭터 외형 · 무기 스킨\n게임 내 재화로 획득하는 컬렉션",86,380,450,94,24,new Color32(178,209,242,255));
        foreach(var image in draw.GetComponentsInChildren<RawImage>(true).Where(r=>r.transform.parent==draw))
        { Box(image.rectTransform,470,190,950,660); image.raycastTarget=false; }
        var weaponName=draw.Find("Weapon Name Text"); if(weaponName!=null) {Box((RectTransform)weaponName,660,680,700,60); weaponName.GetComponent<TMP_Text>().color=Color.white;}
        var itemGroup=(RectTransform)draw.Find("Item Group"); itemGroup.localScale=Vector3.one*.5f; itemGroup.anchorMin=itemGroup.anchorMax=new Vector2(.5f,.5f); itemGroup.pivot=new Vector2(.5f,.5f); itemGroup.anchoredPosition=new Vector2(0,-305);
        for(int i=0;i<3;++i)
        {
            var grade=(RectTransform)draw.Find("Grade"+i+"Rewards"); Box(grade,80,580+i*108,390,92);
            grade.GetComponent<Image>().color=i==0?new Color32(161,118,47,255):new Color32(83,69,125,255);
            var title=grade.GetComponentInChildren<TMP_Text>(true); Box(title.rectTransform,14,3,365,32); title.fontSize=22; title.alignment=TextAlignmentOptions.Left;
            var samples=shop.rewardSlots.Where(s=>Grade(s.itemId)==i).Take(3).ToArray();
            for(int j=0;j<samples.Length;++j)
            {var icon=Image(grade,"RewardPreview"+j,Color.white); Box(icon,16+j*115,34,105,54); icon.GetComponent<Image>().sprite=samples[j].icon.sprite; icon.GetComponent<Image>().preserveAspect=true; icon.GetComponent<Image>().raycastTarget=false;}
        }
        RepositionButton(draw,"Gacha Button","1회 뽑기",1470,665,350,84,Gray);
        RepositionButton(draw,"DrawTenButton","10회 뽑기",1470,761,350,104,Red);
        RepositionButton(draw,"ChangeCurrencyButton","사용 재화 변경",1470,610,350,42,Gray);
        RepositionButton(draw,"RewardDetailsButton","보상 상세",1470,882,110,46,Gray);
        RepositionButton(draw,"HistoryButton","기록",1590,882,110,46,Gray);
        RepositionButton(draw,"ProbabilityButton","상세 확률",1710,882,110,46,Gray);
        Box(shop.balance.rectTransform,1350,510,490,58); shop.balance.fontSize=18; shop.balance.alignment=TextAlignmentOptions.Right;
        Box(shop.status.rectTransform,80,974,1120,35); shop.status.fontSize=18;
        RepositionButton(draw,"RetryButton","응답 재확인",1220,972,180,36,Gray);
        var back=draw.Find("Back Button"); if(back!=null) Box((RectTransform)back,1650,990,175,42);
        Label(draw,"DrawCost","1회 · 재화 0 ×300 또는 재화 1 ×1\n10회 · 재화 0 ×3,000 또는 재화 1 ×10",1360,555,460,54,18,new Color32(210,223,241,255));
    }
    static int Grade(int id) {int skin=(id-1000)%100; return id<1000||skin==1?0:skin<=3?1:2;}
    public static void RefinePresentation()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity",OpenSceneMode.Single);
        if(scene.isDirty) throw new InvalidOperationException("Save your Lobby edits first.");
        var shop=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VectorShopPanel>(true)).Single();
        var root=shop.transform; var draw=root.Find("Weapon Skin Gacha Panel");
        foreach(var path in new[]{"ShopTabBar/ShopWallet","ShopTabBar/WalletBackground","Money Group","SkinCatalogPanel/CurrencyNotice","Weapon Skin Gacha Panel/ShopBalance","Weapon Skin Gacha Panel/SupplyDescription"})
        {var item=root.Find(path); if(item!=null) item.gameObject.SetActive(false);}
        shop.status.text=""; Box(shop.status.rectTransform,1390,540,430,48); shop.status.fontSize=18;shop.status.alignment=TextAlignmentOptions.Right;
        shop.drawCost=draw.Find("DrawCost").GetComponent<TMP_Text>(); shop.drawCost.text="1회  300    ·    10회  3,000";
        Box(shop.drawCost.rectTransform,1470,562,350,36);shop.drawCost.alignment=TextAlignmentOptions.Center;
        shop.currencyButtonLabel=draw.Find("ChangeCurrencyButton").GetComponentInChildren<TMP_Text>(true);shop.currencyButtonLabel.text="재화 0 · 변경";
        shop.retryButton=draw.Find("RetryButton").GetComponent<Button>();shop.retryButton.gameObject.SetActive(false);
        var backdrop=draw.Find("SupplyBackdrop");
        if(backdrop!=null)
        {
            backdrop.GetComponent<Image>().color=new Color32(185,198,214,255);
            var band=backdrop.Find("StageBand");
            if(band!=null) band.GetComponent<Image>().color=new Color32(204,215,228,255);
            foreach(var line in backdrop.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("StageLine",StringComparison.Ordinal)))
                line.color=new Color(.18f,.52f,.86f,.24f);
        }
        var title=draw.Find("SupplyTitle");
        if(title!=null) title.GetComponent<TMP_Text>().color=new Color32(28,38,58,255);
        root.Find("Main Panel/RecommendationHeading").GetComponent<TMP_Text>().text="추천";
        root.Find("Main Panel/RecommendationScroll/Content/FeaturedHeading").GetComponent<TMP_Text>().text="스킨 컬렉션";
        root.Find("SkinCatalogPanel/ProductCount").GetComponent<TMP_Text>().text="70개 상품";
        var sequence=shop.sequence; font=shop.description.font;
        var background=sequence.overlay.transform.Find("RevealBackdrop") as RectTransform;
        if(background==null) background=Image(sequence.overlay.transform,"RevealBackdrop",new Color32(177,190,206,255));
        Stretch(background,0,0,1,1);background.SetAsFirstSibling();background.GetComponent<Image>().raycastTarget=false;
        sequence.revealBackdrop=background.GetComponent<Image>();background.gameObject.SetActive(false);
        sequence.label.text="";sequence.label.fontSize=32; Stretch(sequence.label.rectTransform,.1f,.06f,.9f,.14f);
        var skip=sequence.overlay.transform.Find("SkipButton");Box((RectTransform)skip,1650,990,215,45);
        skip.GetComponentInChildren<TMP_Text>(true).fontSize=20;
        // Reuse the existing global currency icons and labels; no second shop balance.
        var money=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MoneyManager>(true)).Single();
        var serialized=new SerializedObject(money);
        for(int i=0;i<2;++i)
        {
            var label=serialized.FindProperty("_money"+i+"Text").objectReferenceValue as TMP_Text;
            if(label==null) continue;
            label.enableAutoSizing=true;label.fontSizeMin=8;label.fontSizeMax=13;
            Box(label.rectTransform,1655,i==0?42:10,55,24);label.alignment=TextAlignmentOptions.Right;
            label.overflowMode=TextOverflowModes.Ellipsis;
            var icon=label.transform.parent.Find("Coin"+i+"Image");
            if(icon!=null && icon.TryGetComponent<Image>(out var graphic))
            {graphic.sprite=Resources.Load<Sprite>(i==0?"LevelRewards/Currency0Ticket":"LevelRewards/Currency1Crystal");graphic.preserveAspect=true;}
        }
        var money2=serialized.FindProperty("_money2Text").objectReferenceValue as TMP_Text;
        foreach(var text in root.Find("Money Group").GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(text==money2); // Keep the original exchange currency; remove only duplicate 0/1 labels.
        EditorUtility.SetDirty(shop);EditorUtility.SetDirty(sequence);EditorSceneManager.MarkSceneDirty(scene);
        Validate(root);if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Lobby save failed.");
        Debug.Log("[SHOP LAYOUT] Removed duplicate balances and developer copy; saved currency cost/reveal references.");
    }
    // Explicit follow-up authored correction, never executed on editor startup.
    public static void Polish()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity",OpenSceneMode.Single);
        if(scene.isDirty) throw new InvalidOperationException("Save your Lobby edits first.");
        var shop=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VectorShopPanel>(true)).Single();
        var content=shop.transform.Find("Main Panel/RecommendationScroll/Content");
        foreach(string name in new[]{"Weapon Skin Gacha Button","Profile Gacha Button","Exchange Store Button"})
        {
            var banner=content.Find(name);
            var artwork=banner.GetComponentsInChildren<Image>(true).FirstOrDefault(i=>i.transform!=banner && i.sprite!=null && i.name!="ExchangePreview");
            foreach(var title in banner.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name!="ExchangeHeading"))
            { title.transform.SetParent(banner,false); Stretch(title.rectTransform,0,0,1,.13f); title.transform.SetAsLastSibling(); }
            if(artwork!=null)
            {
                artwork.transform.SetParent(banner,false);
                Stretch(artwork.rectTransform,0,.13f,1,1); artwork.preserveAspect=false;
                if(name=="Profile Gacha Button")
                { artwork.sprite=Resources.Load<Sprite>("Sprites/InitiatorSprite"); artwork.preserveAspect=true; artwork.color=Color.white; }
                if(name=="Exchange Store Button") {artwork.sprite=null; artwork.color=new Color32(33,53,74,255);}
                artwork.raycastTarget=false;
            }
            foreach(var image in banner.GetComponentsInChildren<Image>(true))
                if(image.transform!=banner && image!=artwork && image.name!="CaptionStrip" && image.name!="ExchangePreview")
                {image.color=Color.clear; image.raycastTarget=false;}
            foreach(var shadow in banner.GetComponentsInChildren<Shadow>(true)) shadow.enabled=false;
            if(artwork!=null) artwork.transform.SetAsFirstSibling();
            if(name=="Profile Gacha Button") {banner.GetComponent<Image>().sprite=null; banner.GetComponent<Image>().color=new Color32(47,65,87,255);}
            var strip=banner.Find("CaptionStrip"); strip.SetAsLastSibling();
            foreach(var title in banner.GetComponentsInChildren<TMP_Text>(true)) title.transform.SetAsLastSibling();
        }
        var draw=shop.transform.Find("Weapon Skin Gacha Panel");
        foreach(var image in draw.GetComponentsInChildren<RawImage>(true).Where(r=>r.transform.parent==draw))
            Box(image.rectTransform,470,215,870,560);
        foreach(var label in draw.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.transform.parent==draw && t.text.Contains("상품")))
        {Box(label.rectTransform,80,521,390,42); label.fontSize=24; label.color=Color.white;}
        Box(shop.status.rectTransform,80,935,1100,35);
        EditorSceneManager.MarkSceneDirty(scene); Validate(shop.transform);
        if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Lobby save failed.");
        Debug.Log("[SHOP LAYOUT] Banner artwork/caption geometry and draw caption corrected and saved.");
    }
    static void RepositionButton(Transform root,string name,string title,float x,float y,float w,float h,Color color)
    {var rect=(RectTransform)root.Find(name); if(rect==null) return; Box(rect,x,y,w,h); rect.GetComponent<Image>().color=color; var text=rect.GetComponentInChildren<TMP_Text>(true); text.text=title; text.fontSize=h>70?34:20;}
    public static void Validate(Transform store)
    {
        var nav=store.GetComponent<VectorShopNavigation>();
        if(nav==null || nav.pages.Length!=5 || nav.products.Length!=70 || nav.tabs.Length!=5 || nav.filters.Length!=3) throw new InvalidOperationException("Shop navigation references incomplete.");
        foreach(var slot in nav.products) if(slot.root.GetComponent<Button>().onClick.GetPersistentEventCount()==0 || slot.icon.sprite==null) throw new InvalidOperationException("Unbound catalog card "+slot.itemId);
        VectorShopHierarchyInstaller.Validate(store);
    }
    static RectTransform Rect(Transform parent,string name) {var go=new GameObject(name,typeof(RectTransform)); go.layer=5; go.transform.SetParent(parent,false); return (RectTransform)go.transform;}
    static RectTransform Image(Transform parent,string name,Color color) {var rect=Rect(parent,name); rect.gameObject.AddComponent<Image>().color=color; return rect;}
    static RectTransform Panel(Transform parent,string name,Color color) {var rect=Image(parent,name,color); Stretch(rect,0,0,1,1); return rect;}
    static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color)
    {var rect=Rect(parent,name); Box(rect,x,y,w,h); var label=rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font=font; label.text=text; label.fontSize=size; label.color=color; label.raycastTarget=false; return label;}
    static Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityAction action,Color color)
    {var rect=Image(parent,name,color); Box(rect,x,y,w,h); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>(); var label=Label(rect,"Label",text,4,0,w-8,h,23,Color.white); label.alignment=TextAlignmentOptions.Center; if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action); return button;}
    static ScrollRect Scroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content)
    {
        var viewport=Image(parent,name,Color.clear); Box(viewport,x,y,w,h); viewport.gameObject.AddComponent<RectMask2D>();
        content=Rect(viewport,"Content"); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.sizeDelta=Vector2.zero;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=45; scroll.movementType=ScrollRect.MovementType.Clamped;
        var track=Image(parent,name+"Scrollbar",new Color(.2f,.24f,.3f,.25f)); Box(track,x+w+8,y,6,h);
        var handle=Image(track,"Handle",Color.white); Stretch(handle,0,0,1,1); var bar=track.gameObject.AddComponent<Scrollbar>(); bar.handleRect=handle; bar.targetGraphic=handle.GetComponent<Image>(); bar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=bar; return scroll;
    }
    static void Box(RectTransform rect,float x,float y,float w,float h) {rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h);}
    static void Stretch(RectTransform rect,float x0,float y0,float x1,float y1) {rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
