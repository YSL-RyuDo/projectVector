#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class InGameUiAuthoring
{
    const string ScenePath="Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity";
    public const string Output="C:/Users/GCT/Documents/Vector-InGameUI-20261028";
    static TMP_FontAsset font; static Material fontMaterial,ink; static EquipmentIconCatalog icons;
    static Color Slate=new Color(.11f,.17f,.24f,.92f), Muted=new Color(.77f,.85f,.9f), Cyan=new Color(.62f,.87f,.95f);
    public static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new Exception(field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    public static T Get<T>(UnityEngine.Object target,string field) where T:UnityEngine.Object => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    static void Rect(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition3D=new Vector3(x,y,0);r.localRotation=Quaternion.identity;r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static GameObject Node(Transform parent,string name){var t=parent.Find(name);if(t)return t.gameObject;var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(parent,false);return g;}
    static Image Box(Transform p,string name,float x,float y,float w,float h,Color color){var g=Node(p,name);var v=g.GetComponent<Image>()??g.AddComponent<Image>();Rect(g.transform,x,y,w,h);v.sprite=null;v.material=null;v.color=color;v.raycastTarget=false;return v;}
    static TMP_Text Text(Transform p,string name,string text,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft){var g=Node(p,name);var t=g.GetComponent<TMP_Text>()??g.AddComponent<TextMeshProUGUI>();Rect(t.transform,x,y,w,h);Style(t,size,color,align);t.text=text;return t;}
    static void Style(TMP_Text t,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft){t.font=font;t.fontSharedMaterial=fontMaterial;t.fontSize=size;t.enableAutoSizing=false;t.color=color;t.alignment=align;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;}
    static Image Icon(Transform p,string name,float x,float y,float w,float h,Sprite sprite){var v=Box(p,name,x,y,w,h,Color.white);v.material=ink;v.sprite=sprite;v.preserveAspect=true;v.enabled=sprite;return v;}
    static void Fixed(Transform t){foreach(var c in t.GetComponents<LayoutGroup>())c.enabled=false;foreach(var c in t.GetComponents<ContentSizeFitter>())c.enabled=false;}
    static void Move(Transform t,Transform parent,float x,float y,float w,float h){t.SetParent(parent,false);Fixed(t);Rect(t,x,y,w,h);}

    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor batch only");
        var scene=EditorSceneManager.OpenScene(ScenePath);var roots=scene.GetRootGameObjects();
        var source=roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).First(t=>t.font);
        font=source.font;fontMaterial=source.fontSharedMaterial;
        icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        ink=AssetDatabase.LoadAssetAtPath<Material>(EquipmentIconsAuthoring.Folder+"/EquipmentInk.mat");
        var board=roots.SelectMany(r=>r.GetComponentsInChildren<InGameScoreboardUI>(true)).First();
        var store=roots.SelectMany(r=>r.GetComponentsInChildren<StorePanelUI>(true)).First();
        var hud=roots.SelectMany(r=>r.GetComponentsInChildren<InGameUIManager>(true)).First();
        var prompt=roots.SelectMany(r=>r.GetComponentsInChildren<InteractionPromptUI>(true)).First();
        var pick=roots.SelectMany(r=>r.GetComponentsInChildren<PickManager>(true)).First();
        foreach(var owner in new UnityEngine.Object[]{board,store}){var obj=new SerializedObject(owner);var sprites=obj.FindProperty("characterSprites");for(int i=0;i<sprites.arraySize;i++){var data=pick.GetCharacterData(i);if(data?.characterImage)sprites.GetArrayElementAtIndex(i).objectReferenceValue=data.characterImage;}obj.ApplyModifiedPropertiesWithoutUndo();}
        AuthorShieldSprites();AuthorBoard(board);AuthorStore(store);AuthorPrompt(prompt);
        var x=Get<Image>(hud,"xSkillImage");
        foreach(string key in new[]{"c","q","e","x"}){var icon=Get<Image>(hud,key+"SkillImage");var gauge=icon.transform.Find(key.ToUpper()+"SkillGauge");if(gauge)Ref(hud,key+"SkillGauge",gauge.GetComponent<Image>());}
        Ref(hud,"ultimatePointText",Text(x.transform,"UltimatePoints","—",0,51,120,26,19,Cyan,TextAlignmentOptions.Center));
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Preview(board,store,hud,prompt);
        Debug.Log("INGAME_UI_AUTHOR_PASS");
    }

    static void AuthorBoard(InGameScoreboardUI board)
    {
        var outer=Get<GameObject>(board,"rootPanel").transform;Fixed(outer);
        var root=Node(outer,"ScoreboardContent").transform;
        foreach(Transform child in outer.Cast<Transform>().ToArray())if(child!=root)child.SetParent(root,false);
        Rect(root,0,-10,1920,1080);root.localScale=Vector3.one*.82f;
        var bg=outer.GetComponent<Image>();if(bg){bg.color=Color.clear;bg.raycastTarget=false;bg.sprite=null;}
        var panel=Box(root,"CombatPanel",0,30,1240,752,new Color(.045f,.07f,.11f,.80f));panel.transform.SetAsFirstSibling();
        Text(root,"Title","경기 현황",-430,356,300,48,30,Color.white);
        var tab=Box(root,"TabBadge",548,356,64,34,Slate);Text(tab.transform,"Label","TAB",0,0,64,34,17,Color.white,TextAlignmentOptions.Center);
        Text(root,"AllyHeading","우리 팀",-480,295,190,30,17,Cyan);
        Text(root,"UltHeading","궁극기",-205,295,120,30,16,Muted,TextAlignmentOptions.Center);
        Text(root,"KdaHeading","K / D / A",-65,295,150,30,16,Muted,TextAlignmentOptions.Center);
        Text(root,"GearHeading","보유 장비",245,295,350,30,16,Muted,TextAlignmentOptions.Center);
        Text(root,"CreditHeading","크레딧",515,295,112,30,16,Muted,TextAlignmentOptions.MidlineRight);
        var so=new SerializedObject(board);
        foreach(string field in new[]{"ourTeamRows","enemyTeamRows"})
        {
            var rows=so.FindProperty(field);bool ally=field=="ourTeamRows";
            for(int i=0;i<rows.arraySize;i++)
            {
                var row=(ScoreboardRowUI)rows.GetArrayElementAtIndex(i).objectReferenceValue;
                Move(row.transform,root,0,(ally?239:-133)-i*72,1160,64);
                var rb=row.GetComponent<Image>()??row.gameObject.AddComponent<Image>();rb.sprite=null;rb.material=null;rb.color=ally?Slate:new Color(.25f,.18f,.22f);Ref(row,"rowBackground",rb);
                Ref(row,"selfAccent",Box(row.transform,"SelfAccent",-578,0,4,64,Cyan));
                var portrait=Get<Image>(row,"charImage");Move(portrait.transform,row.transform,-542,0,54,54);portrait.preserveAspect=true;
                var nick=Get<TMP_Text>(row,"nickNameText");Move(nick.transform,row.transform,-402,10,210,28);Style(nick,22,Color.white);
                Ref(row,"stateText",Text(row.transform,"State","",-402,-17,210,22,13,Muted));
                var kda=Get<TMP_Text>(row,"kdaText");Move(kda.transform,row.transform,-65,0,150,40);Style(kda,23,Color.white,TextAlignmentOptions.Center);
                var ult=Get<GameObject>(row,"ultTextObject").GetComponent<TMP_Text>();Move(ult.transform,row.transform,-205,0,120,40);Style(ult,18,Color.white,TextAlignmentOptions.Center);
                var weapon=Get<GameObject>(row,"weaponImageObject").GetComponent<Image>();Move(weapon.transform,row.transform,113,0,116,44);weapon.material=ink;weapon.preserveAspect=true;
                Ref(row,"pistolImage",Icon(row.transform,"Pistol",211,0,68,42,null));
                Ref(row,"knifeImage",Icon(row.transform,"Knife",286,0,66,40,null));
                Ref(row,"armorImage",Icon(row.transform,"Armor",352,0,34,42,null));
                Ref(row,"armorText",Text(row.transform,"ArmorValue","",395,0,54,32,19,Color.white,TextAlignmentOptions.Center));
                var credit=Get<GameObject>(row,"creditTextObject").GetComponent<TMP_Text>();Move(credit.transform,row.transform,515,0,112,38);Style(credit,23,Color.white,TextAlignmentOptions.MidlineRight);
                Ref(row,"equipmentIcons",icons);row.Clear();
            }
        }
        Text(root,"EnemyHeading","상대 팀",-480,-79,190,28,17,new Color(.91f,.64f,.66f));
        Text(root,"PrivacyNote","",183,-79,785,28,14,Muted,TextAlignmentOptions.MidlineRight);
        var footer=root.Find("Footer");if(footer)footer.gameObject.SetActive(false);
        var history=Get<RoundHistoryUI>(board,"roundHistoryUI");AuthorHistory(history,root);
        foreach(string oldName in new[]{"Header","Table"}) {var old=root.Find(oldName);if(old)foreach(var graphic in old.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;}
        // Superseded legacy containers remain in the authored hierarchy; hide their decorative images/text only.
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t==root || t==panel.transform || t.GetComponentInParent<ScoreboardRowUI>() || t.GetComponentInParent<RoundHistoryUI>())continue;
            if(t.parent==root)continue;
            if(t.name=="Label" && t.parent==tab.transform)continue;
            if(t.GetComponent<TMP_Text>() || t.GetComponent<Image>())
                Debug.Log("BOARD_LEGACY_REVIEW "+t.name);
        }
    }
    static void AuthorHistory(RoundHistoryUI history,Transform root)
    {
        string materialPath=EquipmentIconsAuthoring.Folder+"/RoundResultInk.mat";
        var resultInk=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!resultInk){var shader=Shader.Find("UI/RoundResultInk");if(!shader)throw new Exception("Missing round result icon shader");resultInk=new Material(shader);AssetDatabase.CreateAsset(resultInk,materialPath);}
        Move(history.transform,root,0,0,1160,108);Fixed(history.transform);
        var bg=history.GetComponent<Image>()??history.gameObject.AddComponent<Image>();bg.sprite=null;bg.material=null;bg.color=new Color(.045f,.07f,.10f,.98f);bg.raycastTarget=false;
        var oldScroll=history.transform.Find("HistoryScrollView");if(oldScroll)foreach(var graphic in oldScroll.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
        string[] names={"ourTeamRoleText","enemyTeamRoleText","ourTeamScoreText","enemyTeamScoreText"};
        for(int i=0;i<names.Length;i++) {var t=Get<TMP_Text>(history,names[i]);if(!t)continue;Move(t.transform,root,i<2?-355:(i==2?-55:55),i<2?(i==0?295:-79):349,i<2?80:72,i<2?42:66);Style(t,i<2?16:38,i%2==0?Cyan:new Color(.98f,.71f,.73f),TextAlignmentOptions.Center);}
        Text(root,"ScoreOurLabel","우리 팀",-140,350,80,32,16,Cyan,TextAlignmentOptions.Center);
        Text(root,"ScoreEnemyLabel","상대 팀",140,350,80,32,16,new Color(.98f,.71f,.73f),TextAlignmentOptions.Center);
        Text(root,"ScoreDivider",":",0,349,24,42,30,Muted,TextAlignmentOptions.Center);
        Ref(history,"historyPhaseText",Text(history.transform,"HistoryLabel","정규전",-514,29,100,26,18,Color.white));
        var viewerLabel=history.transform.Find("ViewerTeam");if(viewerLabel)viewerLabel.gameObject.SetActive(false);
        Ref(history,"historyRangeText",Text(history.transform,"HistoryRange","1 - 10R",-514,4,100,24,14,Muted));
        for(int i=0;i<2;i++)
        {
            var face=Box(history.transform,i==0?"PreviousRounds":"NextRounds",-540+i*52,-27,44,26,Slate);face.raycastTarget=true;
            var button=face.GetComponent<Button>()??face.gameObject.AddComponent<Button>();button.targetGraphic=face;
            Text(face.transform,"Key",i==0?"PgUp":"PgDn",0,0,44,26,12,Color.white,TextAlignmentOptions.Center);
            while(button.onClick.GetPersistentEventCount()>0)UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,0);
            if(i==0)UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,history.PreviousPage);
            else UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,history.NextPage);
            Ref(history,i==0?"previousPageButton":"nextPageButton",button);
        }
        var pool=Node(history.transform,"AuthoredRounds").transform;Rect(pool,66,0,994,94);Fixed(pool);
        var prefab=Get<RoundHistoryColumnUI>(history,"roundColumnPrefab");
        var serialized=new SerializedObject(history);var array=serialized.FindProperty("authoredColumns");array.arraySize=24;
        for(int i=0;i<24;i++)
        {
            var name="Round_"+i;var found=pool.Find(name);var col=found?found.GetComponent<RoundHistoryColumnUI>():UnityEngine.Object.Instantiate(prefab,pool);
            col.name=name;Rect(col.transform,-450+(i%10)*100,0,94,88);Fixed(col.transform);
            var cb=col.GetComponent<Image>();if(cb){cb.sprite=null;cb.material=null;cb.color=new Color(.12f,.17f,.22f,1);}
            foreach(string markerName in new[]{"topMarker","bottomMarker"}) {var marker=Get<GameObject>(col,markerName);Move(marker.transform,col.transform,0,markerName=="topMarker"?43:-43,94,2);var im=marker.GetComponent<Image>();if(im){im.sprite=null;im.color=new Color(.85f,.91f,.96f);}}
            foreach(string iconName in new[]{"ourTeamResultIcon","enemyTeamResultIcon"}){var im=Get<Image>(col,iconName);Move(im.transform,col.transform,-12,-7,56,56);im.material=resultInk;im.color=Color.white;im.preserveAspect=true;im.raycastTarget=false;im.enabled=false;}
            Ref(col,"background",cb);Ref(col,"roundNumber",Text(col.transform,"Number",(i+1)+"R",0,31,84,20,15,Muted,TextAlignmentOptions.Center));
            Ref(col,"winnerText",Text(col.transform,"Winner","",32,-7,22,28,19,Color.white,TextAlignmentOptions.Center));
            Ref(col,"currentText",Text(col.transform,"Current","진행 중",0,-7,84,28,17,Color.white,TextAlignmentOptions.Center));
            Ref(col,"resultAccent",Box(col.transform,"ResultAccent",0,-42,78,3,Color.white));
            Ref(col,"reasonText",Text(col.transform,"Reason","",0,-27,84,24,14,Color.white,TextAlignmentOptions.Center));
            array.GetArrayElementAtIndex(i).objectReferenceValue=col;col.SetEmpty();col.gameObject.SetActive(i==0);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();history.Init(0);
        Ref(history,"currentRoundText",Text(root,"RoundNumber","라운드 1",350,356,260,32,19,Cyan,TextAlignmentOptions.MidlineRight));
    }

    static void AuthorShieldSprites()
    {
        // Sprite sub-rects trim only the imported framing; source PNGs remain untouched.
        for(int i=0;i<2;i++)
        {
            string path=EquipmentIconsAuthoring.Folder+(i==0?"/LightShield.png":"/HeavyShield.png");
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            string asset=EquipmentIconsAuthoring.Folder+"/ShieldHud"+i+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
            if(!sprite){sprite=Sprite.Create(tex,new Rect(tex.width*.35f,tex.height*.13f,tex.width*.3f,tex.height*.72f),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name="ShieldHud"+i;AssetDatabase.CreateAsset(sprite,asset);}
            icons.armor[i]=sprite;
        }
        EditorUtility.SetDirty(icons);AssetDatabase.SaveAssets();
    }

    static void AuthorStore(StorePanelUI store)
    {
        var root=Get<GameObject>(store,"storePanelRoot").transform;Fixed(root);
        var bg=root.GetComponent<Image>();if(bg){bg.color=new Color(.035f,.05f,.075f,.18f);bg.sprite=null;}
        Text(root,"Title","전투 준비",-652,440,380,55,34,Color.white);
        Text(root,"PurchaseHelp","장비 선택",-20,440,940,40,18,Muted,TextAlignmentOptions.MidlineRight);
        var close=Get<Button>(store,"closeButton");Move(close.transform,root,822,440,48,42);close.GetComponent<Image>().color=Slate;
        foreach(var t in close.GetComponentsInChildren<TMP_Text>(true)){Style(t,22,Color.white,TextAlignmentOptions.Center);t.text="×";}
        var card=Box(root,"PlayerLoadout",-770,31,224,710,new Color(.075f,.12f,.18f,.92f));card.transform.SetAsFirstSibling();
        var portrait=Get<Image>(store,"playerCharacterImage");var nick=Get<TMP_Text>(store,"playerNicknameText");var money=Get<TMP_Text>(store,"playerCreditText");
        Transform old=portrait.transform.parent;
        Move(portrait.transform,card.transform,0,267,108,108);portrait.preserveAspect=true;
        Move(nick.transform,card.transform,0,182,192,38);Style(nick,26,Color.white,TextAlignmentOptions.Center);nick.text="";
        Move(money.transform,card.transform,0,100,192,50);Style(money,36,Cyan,TextAlignmentOptions.Center);money.text="0";
        Text(card.transform,"CreditsLabel","크레딧",0,140,192,25,15,Muted,TextAlignmentOptions.Center);
        Text(card.transform,"LoadoutLabel","현재 로드아웃",0,31,192,28,17,Color.white);
        var summary=card.GetComponent<LoadoutSummaryUI>()??card.gameObject.AddComponent<LoadoutSummaryUI>();summary.icons=icons;
        summary.primary=Icon(card.transform,"Primary",0,-30,192,56,null);summary.pistol=Icon(card.transform,"Pistol",-51,-96,90,50,null);summary.knife=Icon(card.transform,"Knife",51,-96,90,46,null);
        summary.armorIcon=Icon(card.transform,"Shield",-88,-166,36,42,null);
        summary.armorText=Text(card.transform,"ShieldValue","방어막  —",23,-166,155,34,16,Color.white);
        summary.ultimateText=Text(card.transform,"UltimateValue","궁극기  —",0,-244,192,34,20,Cyan);
        Ref(store,"loadoutSummary",summary);
        // Player info's previous background is no longer needed after moving its same bound children.
        var legacyInfo=root.Find("PlayerStoreInfo");if(legacyInfo)foreach(var graphic in legacyInfo.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
        string[] categories={"보조 무기","기관단총 / 산탄총","소총","기관총 / 저격총","방어막"};
        float[] xs={-535,-321,-107,107,321};
        for(int i=0;i<5;i++){Box(root,"CategoryBackground"+i,xs[i],359,202,34,Slate);Text(root,"Category"+i,categories[i],xs[i],359,178,30,18,Color.white).transform.SetAsLastSibling();}
        var detailsRoot=Box(root,"WeaponDetails",672,31,416,710,new Color(.075f,.12f,.18f,.94f));
        var details=detailsRoot.GetComponent<StoreWeaponDetailsUI>()??detailsRoot.gameObject.AddComponent<StoreWeaponDetailsUI>();details.icons=icons;
        details.title=Text(detailsRoot.transform,"Name","",0,303,368,42,30,Color.white);
        details.category=Text(detailsRoot.transform,"Category","",0,263,368,27,17,Cyan);
        details.artwork=Icon(detailsRoot.transform,"Art",0,190,340,100,null);
        details.handling=Text(detailsRoot.transform,"Handling","",0,64,368,155,20,Color.white);details.handling.lineSpacing=7;
        Box(detailsRoot.transform,"Divider",0,-34,368,1,new Color(.5f,.67f,.76f,.5f));
        details.damageTitle=Text(detailsRoot.transform,"DamageTitle","",0,-65,368,30,20,Cyan);
        details.damageHeader=Text(detailsRoot.transform,"DamageHeader","",0,-105,368,28,17,Muted);
        details.damageRows=Text(detailsRoot.transform,"DamageRows","",0,-164,368,94,19,Color.white);details.damageRows.lineSpacing=9;
        details.falloffNote=Text(detailsRoot.transform,"FalloffNote","",0,-263,368,90,16,Muted);details.falloffNote.lineSpacing=5;
        details.ShowWeapon(9);
        foreach(var item in root.GetComponentsInChildren<StoreItemButton>(true))
        {
            int id=item.ItemId;int col,row;
            if(item.IsArmorButton()){col=4;row=id;}
            else if(id<3){col=0;row=id;}else if(id<7){col=1;row=id-3;}else if(id<10){col=2;row=id-7;}else{col=3;row=id-10;}
            Move(item.transform,root,xs[col],277-row*136,202,124);Ref(item,"details",details);
            item.GetComponent<Image>().color=Slate;
            var icon=Get<Image>(item,"equipmentImage");Rect(icon.transform,0,17,182,72);icon.color=Color.white;
            var name=Get<TMP_Text>(item,"itemNameText");Rect(name.transform,-31,-38,118,30);Style(name,17,Color.white);
            var price=Get<TMP_Text>(item,"priceText");Rect(price.transform,61,-38,64,30);Style(price,16,Cyan,TextAlignmentOptions.MidlineRight);
            item.Refresh(0,false);
        }
        Box(root,"SkillsHeadingBackground",-535,-242,202,34,Slate);
        Text(root,"SkillsHeading","스킬",-535,-242,178,28,18,Color.white).transform.SetAsLastSibling();
        var skills=new StoreSkillButton[4];var storeSO=new SerializedObject(store);var skillRefs=storeSO.FindProperty("skillButtons");skillRefs.arraySize=4;
        for(int i=0;i<4;i++) {var obj=root.Find(new[]{"CSkillButton","QSkillButton","ESkillButton","XSkillButton"}[i]);var b=obj.GetComponent<StoreSkillButton>()??obj.gameObject.AddComponent<StoreSkillButton>();b.Configure((SkillSlot)i);skills[i]=b;skillRefs.GetArrayElementAtIndex(i).objectReferenceValue=b;}
        storeSO.ApplyModifiedPropertiesWithoutUndo();
        for(int i=0;i<skills.Length;i++)
        {
            var b=skills[i];Move(b.transform,root,-507+i*267,-329,258,116);b.GetComponent<Image>().color=Slate;
            var label=Get<TMP_Text>(b,"label");if(!label)label=b.GetComponentInChildren<TMP_Text>(true);
            Move(label.transform,b.transform,33,32,160,28);Style(label,19,Color.white);Ref(b,"label",label);
            var badge=Box(b.transform,"KeyBadge",-98,32,30,30,new Color(.29f,.37f,.44f));Ref(b,"keyLabel",Text(badge.transform,"Key",b.Slot.ToString(),0,0,30,30,18,Color.white,TextAlignmentOptions.Center));
            var image=Icon(b.transform,"SkillIcon",-91,-20,46,46,null);image.material=null;Ref(b,"skillIcon",image);
            Ref(b,"stateLabel",Text(b.transform,"State","",33,0,160,25,14,Muted));
            Ref(b,"priceLabel",Text(b.transform,"Price","",33,-32,160,28,20,Cyan,TextAlignmentOptions.MidlineRight));
            var colors=b.GetComponent<Button>().colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.65f,.87f,1);colors.disabledColor=new Color(.75f,.8f,.85f);b.GetComponent<Button>().colors=colors;
            b.Refresh(0,false);
        }
        Text(root,"Footer","구매 단계에서 장비를 준비하세요",0,-437,1696,30,16,Muted);
    }
    static void AuthorPrompt(InteractionPromptUI prompt)
    {
        var root=Get<GameObject>(prompt,"root").transform;Fixed(root);Rect(root,0,-143,420,80);
        var background=root.GetComponent<Image>()??root.gameObject.AddComponent<Image>();background.sprite=null;background.color=new Color(.1f,.16f,.22f,.94f);background.raycastTarget=false;
        var key=Get<Image>(prompt,"keyImage");Move(key.transform,root,-167,0,44,44);key.sprite=null;key.color=new Color(.32f,.43f,.51f);key.raycastTarget=false;
        Ref(prompt,"keyText",Text(key.transform,"Key","F",0,0,44,44,27,Color.white,TextAlignmentOptions.Center));
        var text=Get<TMP_Text>(prompt,"promptText");Move(text.transform,root,33,13,310,32);Style(text,22,Color.white);
        Ref(prompt,"actionHint",Text(root,"ActionHint","누르기",33,-17,310,24,14,Muted));
        root.gameObject.SetActive(false);
    }

    static void Preview(InGameScoreboardUI board,StorePanelUI store,InGameUIManager hud,InteractionPromptUI prompt)
    {
        var map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/TestMap.prefab"));map.name="UI preview TestMap";
        foreach(var t in map.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
        var spawn=map.GetComponentsInChildren<Transform>(true).First(t=>t.name=="AttackSpawnPoint 2");
        var lightObject=new GameObject("UI preview light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.cullingMask=1<<28;light.transform.rotation=Quaternion.Euler(45,-35,0);
        Action<Camera> world=c=>{c.cullingMask=(1<<30)|(1<<28);c.transform.position=spawn.position+Vector3.up*1.5f;c.transform.rotation=Quaternion.LookRotation(Vector3.right+Vector3.forward*.15f);c.fieldOfView=65;c.farClipPlane=500;c.backgroundColor=new Color(.46f,.59f,.71f);};
        var so=new SerializedObject(board);var portraits=so.FindProperty("characterSprites");
        var me=new ScoreboardMemberData{playerId=1,nickname="LYS",credits=2300,hasLoadout=true,primaryWeaponId=9,pistolWeaponId=0,knifeWeaponId=13,armor=76,maxArmor=100,ultimatePoints=4,ultimateRequired=7,equipmentAlive=true};
        Action<GameObject> configureBoard=clone=>{
            var rows=clone.GetComponentsInChildren<ScoreboardRowUI>(true);for(int i=0;i<rows.Length;i++){
                var m=new ScoreboardMemberData{playerId=(uint)(i+1),nickname=new[]{"LYS","KSW","CJU","JJY","KDH","KSH"}[i],kills=8-i,deaths=2+i,assists=i,credits=2300+i*300,hasLoadout=i<3,hasLifeState=true,primaryWeaponId=new[]{9,12,8}[i%3],pistolWeaponId=i%3,knifeWeaponId=13,armor=i==1||i==2?0:76,maxArmor=100,ultimatePoints=i==1?7:4,ultimateRequired=7,equipmentAlive=i!=2&&i!=5};
                rows[i].SetData(m,(Sprite)portraits.GetArrayElementAtIndex(i%portraits.arraySize).objectReferenceValue,1,i<3);
            }
            var history=clone.GetComponentInChildren<RoundHistoryUI>(true);history.Init(0);history.SetRoundResult(1,0,RoundWinReason.ATTACK_ELIMINATION,1,0);history.SetRoundResult(2,1,RoundWinReason.DEFENSE_SPIKE_DEFUSE,1,1);history.SetRoundResult(3,0,RoundWinReason.ATTACK_SPIKE_EXPLODE,2,1);history.SetRoundResult(4,1,RoundWinReason.DEFENSE_TIME_OUT,2,2);history.SetCurrentRound(5);
        };
        EquipmentIconsAuthoring.Render(Get<GameObject>(board,"rootPanel"),Output+"/Scoreboard.png",configureBoard,null,world);
        EquipmentIconsAuthoring.Render(Get<GameObject>(board,"rootPanel"),Output+"/RoundHistory-Icons.png",configureBoard,new Rect(480,480,960,120),world);
        Action<GameObject> configureLongMatch=clone=>{
            configureBoard(clone);var history=clone.GetComponentInChildren<RoundHistoryUI>(true);history.Init(0);history.ApplyRoundRules(5,5);
            byte a=0,b=0;
            for(byte round=1;round<=18;round++){
                byte winner=(byte)(round%2);if(winner==0)a++;else b++;
                bool aAttack=round<=5 || round>=11 && (round-11)%2==0;bool attack=winner==0?aAttack:!aAttack;
                var reason=attack?(round%3==0?RoundWinReason.ATTACK_SPIKE_EXPLODE:RoundWinReason.ATTACK_ELIMINATION):(round%3==0?RoundWinReason.DEFENSE_TIME_OUT:RoundWinReason.DEFENSE_SPIKE_DEFUSE);
                history.SetRoundResult(round,winner,reason,a,b);
            }
        };
        EquipmentIconsAuthoring.Render(Get<GameObject>(board,"rootPanel"),Output+"/RoundHistory-Regular10.png",clone=>{configureLongMatch(clone);clone.GetComponentInChildren<RoundHistoryUI>(true).PreviousPage();},new Rect(480,480,960,120),world);
        EquipmentIconsAuthoring.Render(Get<GameObject>(board,"rootPanel"),Output+"/RoundHistory-Overtime.png",configureLongMatch,new Rect(480,480,960,120),world);
        // Preview uses sample server state; no account, credits or live match are changed.
        SkillInventoryState.ApplyServerState(1,0);SkillInventoryState.ApplyUltimateState(4,7,100,false);
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var character=pick.GetCharacterData(1);
        if(character==null)throw new Exception("Missing actual character skill data");
        SkillInventoryState.SetCharacter(character);
        EquipmentIconsAuthoring.Render(Get<GameObject>(store,"storePanelRoot"),Output+"/Store.png",clone=>{
            var summary=clone.GetComponentInChildren<LoadoutSummaryUI>(true);summary.SetData(me);
            clone.transform.Find("PlayerLoadout").GetComponentsInChildren<TMP_Text>(true).First(t=>t.name==Get<TMP_Text>(store,"playerNicknameText").name).text="LYS";
            var texts=clone.GetComponentsInChildren<TMP_Text>(true);var nick=Get<TMP_Text>(store,"playerNicknameText");var credit=Get<TMP_Text>(store,"playerCreditText");
            foreach(var t in texts){if(t.name==nick.name)t.text="LYS";if(t.name==credit.name)t.text="2,300";}
            var portrait=clone.GetComponentsInChildren<Image>(true).First(t=>t.name==Get<Image>(store,"playerCharacterImage").name);portrait.sprite=(Sprite)portraits.GetArrayElementAtIndex(1).objectReferenceValue;
            foreach(var item in clone.GetComponentsInChildren<StoreItemButton>(true)){item.Refresh(2300,true);item.SetOwned(item.IsWeaponButton()&&(item.ItemId==9||item.ItemId==0)||item.IsArmorButton()&&item.ItemId==1);}
            foreach(var b in clone.GetComponentsInChildren<StoreSkillButton>(true))b.Refresh(2300,true);
        },null,world);
        var promptRoot=Get<GameObject>(prompt,"root");
        // Wrapper preserves the actual prompt's anchored size in the full-resolution renderer.
        var wrapper=new GameObject("PromptPreview",typeof(RectTransform));
        for(int i=0;i<3;i++){
            var clone=UnityEngine.Object.Instantiate(promptRoot,wrapper.transform,false);clone.SetActive(true);Rect(clone.transform,0,120-i*120,420,80);
            clone.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name==Get<TMP_Text>(prompt,"promptText").name).text=new[]{"AK47 줍기","디바이스 설치","디바이스 해체"}[i];
            clone.transform.Find("ActionHint").GetComponent<TMP_Text>().text=i==0?"누르기":"길게 누르기";
        }
        EquipmentIconsAuthoring.Render(wrapper,Output+"/Interactions.png",null,new Rect(600,300,720,480));UnityEngine.Object.DestroyImmediate(wrapper);
        UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(lightObject);
        SkillInventoryState.Reset();
    }
}
#endif
