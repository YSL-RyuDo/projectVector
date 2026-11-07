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

// Explicit editor migration. No runtime UI generation and no per-launch rebuilding.
public static class VectorShopHierarchyInstaller
{
    private static TMP_FontAsset font;
    private static Button buttonTemplate;
    private static readonly Color Dark = new Color32(68,73,83,255), Paper = new Color32(193,203,215,255), Ink = new Color32(32,35,41,255);
    [MenuItem("Project Vector/UI/Install Saved Shop Panels")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before editing scene UI.");
        var scene = SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Lobby has unsaved edits. Save it before installing shop panels.");
        try
        {
            var store = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Store Panel");
            if (store.GetComponent<VectorShopPanel>() != null) { Validate(store); return; }
            var weapon = store.Find("Weapon Skin Gacha Panel");
            buttonTemplate = weapon.Find("Gacha Button").GetComponent<Button>();
            font = weapon.Find("Weapon Name Text").GetComponent<TMP_Text>().font;
            var shop = store.gameObject.AddComponent<VectorShopPanel>();
            shop.skins = AssetDatabase.LoadAssetAtPath<WeaponSkinDatabase>("Assets/ProjectVector/Shared/Weapons/Assets/Weapon Skin Database.asset");
            shop.sequence = store.gameObject.AddComponent<GachaManager>();

            // Keep the original store main/profile/exchange sections and all their serialized references.
            Fixed((RectTransform)buttonTemplate.transform, 670,-330,350,72);
            var oneLabel = buttonTemplate.GetComponentInChildren<TMP_Text>(true); oneLabel.text = "1회 뽑기";
            var ten = Button(weapon,"DrawTenButton","10회 뽑기",670,-425,350,85,shop.DrawTen);
            Button(weapon,"ChangeCurrencyButton","재화 변경",670,-245,350,44,shop.ChangeCurrency);
            Button(weapon,"RewardDetailsButton","보상 상세",550,-500,110,42,shop.ShowDetails);
            Button(weapon,"HistoryButton","뽑기 기록",670,-500,110,42,shop.ShowHistory);
            Button(weapon,"ProbabilityButton","상세 확률",790,-500,110,42,shop.ShowProbabilities);
            var featured = (RectTransform)weapon.Find("Item Group"); featured.localScale = Vector3.one * .75f; featured.anchoredPosition = new Vector2(-325,-288);
            for(int i=0;i<3;++i)
            {
                var grade = Button(weapon,"Grade"+i+"Rewards",new[] { "신화 보상 5%","희귀 보상 24%","고급 보상 71%" }[i],-680+i*230,-420,215,58,null);
                UnityEventTools.AddIntPersistentListener(grade.onClick,shop.ShowGrade,i);
            }
            shop.balance = Text(weapon,"ShopBalance","상점 재화 0: —  /  재화 1: —",.60f,.73f,.96f,.78f,22,Color.white);
            shop.status = Text(weapon,"ShopStatus","상점 전용 보유 · 인게임 보유와 분리",.05f,.025f,.55f,.065f,18,Color.white);
            Button(weapon,"RetryButton","응답 재확인",340,-490,140,36,shop.Retry);
            shop.transactionButtons = new[] { buttonTemplate, ten };

            var detail = Panel(store,"RewardDetailsPanel",Paper); shop.rewardDetails=detail.gameObject;
            Header(detail,"보상 상세",shop.CloseDialogs);
            var content = Scroll(detail,"RewardListScroll",.025f,.12f,.255f,.88f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.childControlHeight=false; layout.childControlWidth=true; layout.childForceExpandHeight=false; layout.spacing=16;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var icons = Resources.LoadAll<Sprite>("Shop/ShopRewardAtlas").ToDictionary(s=>s.name);
            var entries = new List<VectorShopItem>();
            foreach(var skin in shop.skins.skins.Where(s=>s!=null && s.skinId>0))
                entries.Add(new VectorShopItem { id=1000+skin.baseWeapon.weaponId*100+skin.skinId,kind=1,model=skin.baseWeapon.weaponId,skin=skin.skinId,grade=skin.skinId==1?0:skin.skinId<=3?1:2,name=skin.skinName });
            for(int i=0;i<4;++i) entries.Add(new VectorShopItem {id=100+i,kind=0,model=i,grade=0,name=new[]{"Casual1","PicoChan","UnityChan","UnityChanSSU"}[i]});
            var slots = new List<VectorShopSlot>(); shop.rarityGroups=new GameObject[3];
            for(int grade=0;grade<3;++grade)
            {
                var items=entries.Where(i=>i.grade==grade).OrderBy(i=>i.id).ToArray();
                var group=Rect(content,"Grade"+grade+"RewardGroup"); shop.rarityGroups[grade]=group.gameObject;
                var height=40+Mathf.CeilToInt(items.Length/3f)*166;
                group.gameObject.AddComponent<LayoutElement>().preferredHeight=height; group.sizeDelta=new Vector2(440,height);
                var title=Image(group,"GradeHeading",Dark); Fixed(title,0,height/2f-16,440,32);
                Text(title,"Title",new[]{"신화 보상 5%","희귀 보상 24%","고급 보상 71%"}[grade],.02f,0,.98f,1,20,Color.white);
                for(int i=0;i<items.Length;++i)
                {
                    var item=items[i]; var slot=Card(group,"Reward_"+item.id,-148+(i%3)*148,height/2f-117-(i/3)*166,140,154);
                    slot.itemId=item.id; slot.label.text=item.name+"\n미보유"; slot.rarity.color=VectorShopPanel.GradeColors[grade];
                    icons.TryGetValue(item.kind==0?"character_"+item.model:$"weapon_{item.model}_{item.skin}",out var icon); slot.icon.sprite=icon;
                    var select=slot.root.AddComponent<Button>(); select.targetGraphic=slot.root.GetComponent<Image>(); UnityEventTools.AddIntPersistentListener(select.onClick,shop.SelectReward,item.id); slots.Add(slot);
                }
            }
            shop.rewardSlots=slots.ToArray();
            shop.preview=Preview(detail,"SelectedRewardModel",.27f,.15f,.77f,.87f);
            shop.description=Text(detail,"RewardDescription","보상을 선택하세요",.79f,.38f,.975f,.86f,23,Ink);
            Text(detail,"RotateHint","마우스 왼쪽 버튼 드래그 · 회전",.40f,.075f,.75f,.11f,18,Ink);
            Button(detail,"PurchaseSelected","선택한 스킨 구매",720,-280,300,54,shop.ConfirmPurchase);

            var history=Modal(store,"DrawHistoryPanel","기록",shop.CloseDialogs); shop.historyPanel=history.parent.gameObject;
            shop.historyText=Text(history,"HistoryRows","아직 획득 기록이 없습니다.",.05f,.19f,.95f,.84f,22,Ink);
            shop.historyPage=Text(history,"Page","1 / 1",.46f,.07f,.55f,.14f,22,Ink);
            Button(history,"Previous","이전",-180,-270,130,44,shop.PreviousHistory); Button(history,"Next","다음",180,-270,130,44,shop.NextHistory);
            var probability=Modal(store,"DetailedProbabilityPanel","상세 확률",shop.CloseDialogs); shop.probabilityPanel=probability.parent.gameObject;
            var probabilityContent=Scroll(probability,"ProbabilityScroll",.05f,.12f,.95f,.84f); probabilityContent.sizeDelta=new Vector2(0,2600);
            shop.probabilityText=Text(probabilityContent,"ProbabilityText","신화 5% · 희귀 24% · 고급 71%",0,0,1,1,23,Ink);
            var results=Modal(store,"DrawResultPanel","획득한 보상",shop.CloseDialogs); shop.resultPanel=results.parent.gameObject;
            var resultSlots=new List<VectorShopSlot>();
            for(int i=0;i<10;++i) resultSlots.Add(Card(results,"ResultSlot"+(i+1),-500+(i%5)*250,150-(i/5)*230,230,210));
            shop.resultSlots=resultSlots.ToArray();
            var purchase=Modal(store,"PurchaseConfirmPanel","구매 확인",shop.CloseDialogs); shop.purchasePanel=purchase.parent.gameObject;
            shop.purchaseText=Text(purchase,"Description","선택한 스킨을 구매합니다.",.15f,.3f,.85f,.75f,30,Ink);
            Button(purchase,"PurchaseButton","구매",0,-130,250,60,shop.Purchase);
            MakeSequence(store,shop.sequence);
            ConfigureForeground(shop);
            foreach(var panel in new[]{shop.rewardDetails,shop.historyPanel,shop.probabilityPanel,shop.resultPanel,shop.purchasePanel}) panel.SetActive(false);
            EditorUtility.SetDirty(shop); EditorUtility.SetDirty(shop.sequence); EditorSceneManager.MarkSceneDirty(scene);
            Validate(store);
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Lobby scene.");
            Debug.Log("[SHOP HIERARCHY] Saved all panels, 70 reward cards, 10 result slots and sequence UI in Lobby. Original store sections retained.");
        }
        finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
    }
    private static void MakeSequence(Transform store,GachaManager sequence)
    {
        var overlay=Rect(store,"GachaSequencePanel"); Stretch(overlay,0,0,1,1); overlay.gameObject.AddComponent<RawImage>(); sequence.overlay=overlay.gameObject;
        var flash=Image(overlay,"ImpactFlash",Color.clear); Stretch(flash,0,0,1,1); sequence.flash=flash.GetComponent<Image>(); sequence.flash.raycastTarget=false;
        var glitch=Image(flash,"GlitchScan",Color.clear); Stretch(glitch,0,.2f,1,.225f); sequence.glitch=glitch.GetComponent<Image>(); sequence.glitch.raycastTarget=false;
        sequence.revealPreview=Preview(overlay,"HologramReveal",.2f,.15f,.8f,.9f); sequence.revealPreview.gameObject.SetActive(false);
        sequence.label=Text(overlay,"SequenceStatus","전송 준비",.1f,.05f,.9f,.13f,30,Color.white);
        Button(overlay,"SkipButton","건너뛰기  ESC",770,470,280,52,sequence.Skip); overlay.gameObject.SetActive(false);
    }
    public static void Validate(Transform store)
    {
        var shop=store.GetComponent<VectorShopPanel>();
        if(shop==null || shop.rewardSlots?.Length!=70 || shop.resultSlots?.Length!=10 || shop.sequence?.overlay==null) throw new InvalidOperationException("Saved shop references missing.");
        foreach(var name in new[]{"Main Panel","Profile Gacha Panel","Exchange Store Panel","Weapon Skin Gacha Panel","Money Group"}) if(store.Find(name)==null) throw new InvalidOperationException("Original panel missing: "+name);
        foreach(var slot in shop.rewardSlots.Concat(shop.resultSlots)) if(slot.root==null || slot.icon==null || slot.label==null || slot.rarity==null) throw new InvalidOperationException("Unbound reward slot");
    }
    // One-time repair of presentation order on an existing saved hierarchy. No screen replacement.
    public static void RepairSavedPresentation()
    {
        var scene = SceneManager.GetSceneByPath("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save unsaved Lobby edits first.");
        try
        {
            var shop = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<VectorShopPanel>(true)).Single();
            ConfigureForeground(shop); Validate(shop.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Lobby.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
    }
    private static void ConfigureForeground(VectorShopPanel shop)
    {
        int order=1000;
        foreach(var panel in new[]{shop.rewardDetails,shop.historyPanel,shop.probabilityPanel,shop.resultPanel,shop.purchasePanel,shop.sequence.overlay})
        {
            var canvas=panel.GetComponent<Canvas>();
            if(canvas==null) canvas=panel.AddComponent<Canvas>();
            if(panel.GetComponent<GraphicRaycaster>()==null) panel.AddComponent<GraphicRaycaster>();
            // Inactive nested canvases can reset the native setter during editor setup.
            // Serialize the override explicitly so it survives scene reload and activation.
            var serialized = new SerializedObject(canvas);
            serialized.FindProperty("m_OverrideSorting").boolValue=true;
            serialized.FindProperty("m_SortingOrder").intValue=order++;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var slot in shop.rewardSlots)
        {
            var outline=slot.root.GetComponent<UnityEngine.UI.Outline>();
            if(outline==null) outline=slot.root.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor=VectorShopPanel.GradeColors[0]; outline.effectDistance=new Vector2(3,3); outline.enabled=false;
        }
    }
    private static RectTransform Rect(Transform parent,string name)
    { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return (RectTransform)go.transform; }
    private static RectTransform Image(Transform parent,string name,Color color)
    { var rect=Rect(parent,name); rect.gameObject.AddComponent<Image>().color=color; return rect; }
    private static RectTransform Panel(Transform parent,string name,Color color)
    { var rect=Image(parent,name,color); Stretch(rect,0,0,1,1); return rect; }
    private static void Header(RectTransform parent,string title,UnityAction close)
    { var header=Image(parent,"Header",Ink); Stretch(header,0,.94f,1,1); Text(header,"Title",title,.025f,0,.7f,1,26,Color.white); Button(parent,"CloseButton","ESC  뒤로",810,480,200,45,close); }
    private static RectTransform Modal(Transform parent,string name,string title,UnityAction close)
    { var shade=Panel(parent,name,new Color(0,0,0,.6f)); var card=Image(shade,"Window",new Color32(219,223,228,255)); Fixed(card,0,0,1380,720); Text(card,"Title",title,.025f,.87f,.85f,.98f,32,Ink); Button(card,"CloseButton","확인",0,-320,200,44,close); return card; }
    private static VectorShopPreview Preview(Transform parent,string name,float x0,float y0,float x1,float y1)
    { var rect=Rect(parent,name); Stretch(rect,x0,y0,x1,y1); rect.gameObject.AddComponent<RawImage>(); return rect.gameObject.AddComponent<VectorShopPreview>(); }
    private static VectorShopSlot Card(Transform parent,string name,float x,float y,float w,float h)
    {
        var card=Image(parent,name,Dark); Fixed(card,x,y,w,h);
        var icon=Image(card,"Icon",Color.white); Stretch(icon,.035f,.29f,.965f,.965f); var picture=icon.GetComponent<Image>(); picture.preserveAspect=true; picture.raycastTarget=false;
        var bar=Image(card,"RarityLine",VectorShopPanel.GradeColors[0]); Stretch(bar,0,.265f,1,.29f); bar.GetComponent<Image>().raycastTarget=false;
        var text=Text(card,"ItemNameAndCount","보상\n×1",.02f,.015f,.98f,.26f,17,Color.white);
        return new VectorShopSlot {root=card.gameObject,icon=picture,rarity=bar.GetComponent<Image>(),label=text};
    }
    private static RectTransform Scroll(Transform parent,string name,float x0,float y0,float x1,float y1)
    {
        var viewport=Image(parent,name,Color.clear); Stretch(viewport,x0,y0,x1,y1); viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect(viewport,"Content"); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.sizeDelta=Vector2.zero;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.content=content; scroll.viewport=viewport; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=40; return content;
    }
    private static TMP_Text Text(Transform parent,string name,string text,float x0,float y0,float x1,float y1,int size,Color color)
    { var rect=Rect(parent,name); Stretch(rect,x0,y0,x1,y1); var label=rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font=font; label.text=text; label.fontSize=size; label.color=color; label.raycastTarget=false; return label; }
    private static Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityAction action)
    {
        var rect=Image(parent,name,buttonTemplate.GetComponent<Image>().color); Fixed(rect,x,y,w,h);
        var source=buttonTemplate.GetComponent<Image>(); var image=rect.GetComponent<Image>(); image.sprite=source.sprite; image.type=source.type;
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image; button.colors=buttonTemplate.colors;
        var label=Text(rect,"Label",text,.02f,.04f,.98f,.96f,22,Color.white); label.alignment=TextAlignmentOptions.Center;
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action); return button;
    }
    private static void Fixed(RectTransform rect,float x,float y,float w,float h)
    { rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(w,h); }
    private static void Stretch(RectTransform rect,float x0,float y0,float x1,float y1)
    { rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); rect.offsetMin=rect.offsetMax=Vector2.zero; }
}
