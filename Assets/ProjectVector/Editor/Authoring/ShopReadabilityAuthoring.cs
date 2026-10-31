#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class ShopReadabilityAuthoring
{
    public const string Output="C:/Users/GCT/Documents/Vector-Shop-20261029";
    static TMP_FontAsset font;static Material fontMaterial,ink;static EquipmentIconCatalog icons;
    public static Sprite SolidFill()
    {
        const string path="Assets/ProjectVector/Modules/Match/UI/Sprites/HUD/SolidFill.png";
        if(!File.Exists(path)){
            var texture=new Texture2D(4,4,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Color Slate=new(.115f,.155f,.20f,1),Muted=new(.64f,.72f,.78f),Cyan=new(.66f,.88f,.94f);
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
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Batch only");
        Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var source=UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.font);
        font=source.font;fontMaterial=source.fontSharedMaterial;icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);ink=AssetDatabase.LoadAssetAtPath<Material>(EquipmentIconsAuthoring.Folder+"/EquipmentInk.mat");
        var store=UnityEngine.Object.FindFirstObjectByType<StorePanelUI>(FindObjectsInactive.Include);var hud=UnityEngine.Object.FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Include);
        AuthorStore(store);AuthorVitals(hud);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Verify(store,hud);Preview(store,hud);Debug.Log("SHOP_READABILITY_PASS");
    }
    static Button Button(Transform p,string name,float x,float y,float w,float h,out TMP_Text label)
    {
        var image=Box(p,name,x,y,w,h,new Color(.22f,.34f,.42f));image.raycastTarget=true;
        var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.76f,.93f,1);colors.pressedColor=new Color(.45f,.68f,.8f);colors.disabledColor=new Color(.52f,.57f,.62f);button.colors=colors;
        label=Text(image.transform,"Label","",0,0,w-16,h,16,Color.white,TextAlignmentOptions.Center);return button;
    }
    static void AuthorStore(StorePanelUI store)
    {
        var root=Get<GameObject>(store,"storePanelRoot").transform;root.GetComponent<Image>().color=new Color(.025f,.04f,.055f,.18f);
        var frame=Box(root,"ShopBackdrop",0,0,1776,878,new Color(.04f,.065f,.09f,1));frame.transform.SetAsFirstSibling();
        Box(root,"HeaderRule",0,373,1712,1,new Color(.3f,.4f,.46f,.5f));
        Text(root,"Title","전투 준비",-680,405,354,44,28,Color.white);
        Text(root,"PurchaseHelp","좌클릭 구매    /    우클릭 구매 요청",-112,405,916,30,15,Muted,TextAlignmentOptions.MidlineRight);Text(root,"Footer","",0,-425,1600,24,14,Muted);
        var close=Get<Button>(store,"closeButton");Move(close.transform,root,831,406,40,36);
        string[] names={"보조 무기","기관단총 / 산탄총","소총","기관총 / 저격총"};float[] xs={-460,-228,4,236};
        for(int i=0;i<4;i++){Box(root,"CategoryBackground"+i,xs[i],343,220,30,new Color(.15f,.21f,.27f));Text(root,"Category"+i,names[i],xs[i],343,200,26,17,Color.white);}
        // Reuse the fifth category for the horizontal shield group.
        Box(root,"CategoryBackground4",-112,-184,916,23,Color.clear);Text(root,"Category4","방어막",-112,-184,916,22,14,Muted);
        foreach(var item in root.GetComponentsInChildren<StoreItemButton>(true)) {
            int id=item.ItemId;var icon=Get<Image>(item,"equipmentImage");var name=Get<TMP_Text>(item,"itemNameText");var price=Get<TMP_Text>(item,"priceText");
            if(item.IsArmorButton()){
                Move(item.transform,root,-344+id*464,-237,452,72);Rect(icon.transform,-174,0,70,54);Rect(name.transform,-28,3,200,30);Rect(price.transform,165,0,100,32);
            }else{
                int col=id<3?0:id<7?1:id<10?2:3,row=id<3?id:id<7?id-3:id<10?id-7:id-10;
                Move(item.transform,root,xs[col],266-row*126,220,114);Rect(icon.transform,0,15,198,66);Rect(name.transform,-36,-34,126,28);Rect(price.transform,70,-34,60,28);
            }
            if(item.IsWeaponButton()){
                var badge=Box(item.transform,"RequestBadge",61,36,76,20,new Color(.19f,.34f,.42f));Text(badge.transform,"Text","요청 중",0,0,76,20,12,Cyan,TextAlignmentOptions.Center);Ref(item,"requestBadge",badge.gameObject);badge.gameObject.SetActive(false);
            }
            Style(name,17,Color.white);Style(price,16,Cyan,TextAlignmentOptions.MidlineRight);item.GetComponent<Image>().color=Slate;item.Refresh(0,false);
        }
        var detail=root.Find("WeaponDetails");Rect(detail,613,-21,486,742);detail.GetComponent<Image>().color=new Color(.075f,.11f,.15f);
        var d=detail.GetComponent<StoreWeaponDetailsUI>();
        Rect(d.title.transform,-110,325,218,38);Style(d.title,29,Color.white);Rect(d.category.transform,125,325,188,28);Style(d.category,16,Cyan,TextAlignmentOptions.MidlineRight);
        Rect(d.artwork.transform,0,228,438,138);d.handling.enabled=false;
        d.stats=new StoreStatBarUI[3];var solid=SolidFill();
        for(int i=0;i<3;i++){
            var obj=Node(detail,"Stat"+i);Rect(obj.transform,0,124-i*56,438,44);var bar=obj.GetComponent<StoreStatBarUI>()??obj.AddComponent<StoreStatBarUI>();d.stats[i]=bar;
            bar.label=Text(obj.transform,"Name","",-140,8,158,24,16,Muted);bar.value=Text(obj.transform,"Value","",95,8,248,24,18,Color.white,TextAlignmentOptions.MidlineRight);
            var back=Box(obj.transform,"Track",0,-14,438,4,new Color(.18f,.24f,.30f));bar.fill=Box(back.transform,"Fill",0,0,438,4,Cyan);
            bar.fill.sprite=solid;bar.fill.preserveAspect=false;bar.fill.type=Image.Type.Filled;bar.fill.fillMethod=Image.FillMethod.Horizontal;bar.fill.fillOrigin=0;
        }
        d.rangeLabel=Text(detail,"Range","",0,-37,438,24,14,Muted,TextAlignmentOptions.MidlineRight);
        Rect(detail.Find("Divider"),0,-60,438,1);Rect(d.damageTitle.transform,0,-87,438,28);Style(d.damageTitle,19,Cyan);
        Rect(d.damageHeader.transform,0,-128,438,26);Style(d.damageHeader,16,Muted);Rect(d.damageRows.transform,0,-181,438,86);Style(d.damageRows,19,Color.white);d.damageRows.lineSpacing=8;
        Rect(d.falloffNote.transform,0,-254,438,60);Style(d.falloffNote,14,Muted);
        d.requestButton=Button(detail,"RequestWeapon",0,-323,438,42,out var requestLabel);d.requestLabel=requestLabel;d.ShowWeapon(9);
        Box(root,"SkillsHeadingBackground",-112,-292,916,24,Color.clear);Text(root,"SkillsHeading","스킬",-112,-292,916,24,14,Muted);
        var skills=root.GetComponentsInChildren<StoreSkillButton>(true).OrderBy(b=>(int)b.Slot).ToArray();
        for(int i=0;i<skills.Length;i++){
            var s=skills[i];Move(s.transform,root,xs[i],-357,220,104);s.GetComponent<Image>().color=Slate;
            var logo=Get<Image>(s,"skillIcon");Rect(logo.transform,-66,2,60,60);
            var label=Get<TMP_Text>(s,"label");Rect(label.transform,39,27,120,26);Style(label,17,Color.white);
            var state=Get<TMP_Text>(s,"stateLabel");Rect(state.transform,39,1,120,24);Style(state,13,Muted);
            var badge=Get<TMP_Text>(s,"keyLabel").transform.parent;Rect(badge,-8,-30,24,24);Rect(Get<TMP_Text>(s,"keyLabel").transform,0,0,24,24);Style(Get<TMP_Text>(s,"keyLabel"),14,Color.white,TextAlignmentOptions.Center);
            var price=Get<TMP_Text>(s,"priceLabel");Rect(price.transform,58,-30,82,24);Style(price,18,Cyan,TextAlignmentOptions.MidlineRight);s.Refresh(0,false);
        }
        AuthorTeam(root,store);
    }
    static void AuthorTeam(Transform root,StorePanelUI store)
    {
        var panel=Node(root,"TeamPurchases");Rect(panel.transform,-724,-21,266,742);
        var team=panel.GetComponent<StoreTeamPanelUI>()??panel.AddComponent<StoreTeamPanelUI>();team.rows=new StoreTeamRowUI[3];
        var portraits=new SerializedObject(store).FindProperty("characterSprites");team.portraits=new Sprite[portraits.arraySize];for(int i=0;i<team.portraits.Length;i++)team.portraits[i]=(Sprite)portraits.GetArrayElementAtIndex(i).objectReferenceValue;
        Text(panel.transform,"Heading","우리 팀",0,364,266,26,17,Color.white);team.notice=Text(panel.transform,"Notice","",0,-367,266,44,13,Cyan);team.notice.textWrappingMode=TextWrappingModes.Normal;
        for(int i=0;i<3;i++) {
            var card=i==0?Get<LoadoutSummaryUI>(store,"loadoutSummary").gameObject:Node(panel.transform,"Teammate"+i);Move(card.transform,panel.transform,0,242-i*220,266,204);
            var bg=card.GetComponent<Image>()??card.AddComponent<Image>();bg.color=new Color(.085f,.125f,.165f);
            var row=card.GetComponent<StoreTeamRowUI>()??card.AddComponent<StoreTeamRowUI>();team.rows[i]=row;
            row.accent=Box(card.transform,"TeamEdge",-132,0,2,204,Cyan);
            row.portrait=i==0?Get<Image>(store,"playerCharacterImage"):Icon(card.transform,"Portrait",-97,62,48,48,null);row.portrait.material=null;Move(row.portrait.transform,card.transform,-97,62,48,48);
            row.nickname=i==0?Get<TMP_Text>(store,"playerNicknameText"):Text(card.transform,"Nickname","",31,72,174,28,20,Color.white);Move(row.nickname.transform,card.transform,31,72,174,28);Style(row.nickname,20,Color.white);
            row.credits=i==0?Get<TMP_Text>(store,"playerCreditText"):Text(card.transform,"Credits","",31,44,174,26,18,Cyan);Move(row.credits.transform,card.transform,31,44,174,26);Style(row.credits,18,Cyan);
            foreach(string n in new[]{"CreditsLabel","LoadoutLabel"}){var legacy=card.transform.Find(n);if(legacy){legacy.GetComponent<TMP_Text>().text="";legacy.GetComponent<TMP_Text>().enabled=false;}}
            var l=card.GetComponent<LoadoutSummaryUI>()??card.AddComponent<LoadoutSummaryUI>();row.loadout=l;l.icons=icons;
            l.primary=Icon(card.transform,"Primary",-62,8,112,38,null);l.pistol=Icon(card.transform,"Pistol",29,8,46,34,null);l.knife=Icon(card.transform,"Knife",88,8,46,30,null);
            l.armorIcon=Icon(card.transform,"Shield",-112,-26,18,22,null);l.armorText=Text(card.transform,"ShieldValue","",-45,-26,110,22,12,Muted);
            l.ultimateText=Text(card.transform,"UltimateValue","",71,-26,98,22,12,Muted,TextAlignmentOptions.MidlineRight);
            row.requestText=Text(card.transform,"Request","",0,-51,242,24,14,Cyan);row.action=Button(card.transform,"PurchaseAction",0,-79,242,30,out var actionText);row.actionText=actionText;
            if(i==0)Ref(store,"loadoutSummary",l);row.action.gameObject.SetActive(false);
        }
    }
    public static void AuthorVitals(InGameUIManager hud)
    {
        var portrait=Get<Image>(hud,"charImage");var player=portrait.transform.parent;var read=hud.GetComponent<CombatHudReadout>();
        var plate=player.Find("VitalsPlate").GetComponent<HudShapeGraphic>();plate.shape=HudShapeGraphic.Shape.CutPanel;plate.corner=0;Rect(plate.transform,-42,0,286,78);plate.color=new Color(.025f,.04f,.06f,1);plate.SetVerticesDirty();
        player.Find("VitalsEdge").GetComponent<Image>().color=Color.clear;
        Rect(portrait.transform,-151,0,44,54);Rect(read.health.transform,-78,5,84,60);read.health.fontSize=42;read.health.alignment=TextAlignmentOptions.Center;
        read.healthState.enabled=false;player.Find("ShieldLabel").GetComponent<TMP_Text>().enabled=false;
        Rect(read.armorIcon.transform,-6,5,22,28);Rect(read.armor.transform,42,4,54,40);read.armor.fontSize=27;
        var solid=SolidFill();
        var hp=Get<Image>(hud,"hpBar");Rect(hp.transform.parent,-78,-24,84,5);Rect(hp.transform,0,0,84,5);
        var armor=Get<Image>(hud,"barrierBar");Rect(armor.transform.parent,26,-24,84,5);Rect(armor.transform,0,0,84,5);
        foreach(var bar in new[]{hp,armor}){bar.sprite=solid;bar.material=null;bar.preserveAspect=false;bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;bar.fillOrigin=0;var back=bar.transform.parent.GetComponent<Image>();if(back){back.sprite=null;back.material=null;back.color=new Color(.25f,.31f,.37f,1);}}
        read.PresentHealth(100,100,100,100);
    }
    static void Verify(StorePanelUI store,InGameUIManager hud)
    {
        var root=Get<GameObject>(store,"storePanelRoot");if(root.GetComponentsInChildren<StoreItemButton>(true).Length!=15)throw new Exception("Store item lost");
        foreach(var card in root.GetComponentsInChildren<StoreSkillButton>(true)){
            var r=(RectTransform)card.transform;
            foreach(var g in card.GetComponentsInChildren<Graphic>(true)){
                if(g.transform==card.transform)continue;var corners=new Vector3[4];((RectTransform)g.transform).GetWorldCorners(corners);
                foreach(var c in corners){var p=r.InverseTransformPoint(c);if(!new Rect(r.rect.xMin+9.5f,r.rect.yMin+9.5f,r.rect.width-19,r.rect.height-19).Contains(p))throw new Exception("Skill content outside padding: "+g.name);}
            }
        }
        var details=root.GetComponentInChildren<StoreWeaponDetailsUI>(true);for(int id=0;id<13;id++){details.ShowWeapon(id);if(!details.handling.text.Contains(WeaponStoreStats.All[id].magazine+"발"))throw new Exception("Stats mismatch");}
        details.ShowWeapon(9);var hp=hud.GetComponent<CombatHudReadout>();hp.PresentHealth(28,43,100,100);if(hp.health.text!="28" || hp.armor.text!="43" || hp.healthState.enabled)throw new Exception("Vitals mismatch");
        File.WriteAllText(Output+"/UI-checks.txt","PASS 15 existing store items, all four skills inside 10px padding, 13 weapon stats, independent health/shield, authored team/request controls.\n");
    }
    static void Preview(StorePanelUI store,InGameUIManager hud)
    {
        var map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/TestMap.prefab"));map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        foreach(var t in map.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
        var spawn=map.GetComponent<MapInfo>().attackSpawnPoints[0];
        var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
        Action<Camera> world=c=>{c.cullingMask|=1<<28;c.transform.SetPositionAndRotation(spawn.position+Vector3.up*1.5f,spawn.rotation);c.fieldOfView=65;c.backgroundColor=new Color(.45f,.56f,.68f);};
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);SkillInventoryState.SetCharacter(pick.GetCharacterData(1));SkillInventoryState.ApplyServerState(1,0);SkillInventoryState.ApplyUltimateState(4,7,100,false);
        var board=new ScoreboardData{roomId=1,myTeam=0};for(int i=0;i<3;i++)board.members.Add(new ScoreboardMemberData{playerId=(uint)(i+1),nickname=new[]{"LYS","KSW","CJU"}[i],characterId=i==0?1:i==1?0:2,slotIndex=(byte)i,credits=new[]{4300,1800,900}[i],hasLoadout=true,hasLifeState=true,equipmentAlive=true,primaryWeaponId=i==0?9:-1,pistolWeaponId=0,knifeWeaponId=13,armor=i==0?76:0,maxArmor=i==0?100:0,ultimatePoints=4-i,ultimateRequired=7});
        var requests=new Dictionary<uint,TeamPurchaseRequest>{{1,new TeamPurchaseRequest{playerId=1,weaponId=8,price=2500,requestId=2,status=1}},{2,new TeamPurchaseRequest{playerId=2,weaponId=12,price=4000,requestId=1,status=1}}};
        EquipmentIconsAuthoring.Render(Get<GameObject>(store,"storePanelRoot"),Output+"/Shop.png",clone=>{
            clone.GetComponentInChildren<StoreTeamPanelUI>(true).Present(board,1,4300,requests,true);
            foreach(var item in clone.GetComponentsInChildren<StoreItemButton>(true)){item.Refresh(4300,true);item.SetOwned(item.IsWeaponButton() && (item.ItemId==0 || item.ItemId==9));item.SetRequestState(item.IsWeaponButton() && item.ItemId==8);}
            foreach(var b in clone.GetComponentsInChildren<StoreSkillButton>(true))b.Refresh(4300,true);
            var d=clone.GetComponentInChildren<StoreWeaponDetailsUI>(true);d.ShowWeapon(8);
        },null,world);
        var portrait=Get<Image>(hud,"charImage");portrait.sprite=pick.GetCharacterData(1).characterImage;var vitals=portrait.transform.parent;
        for(int state=0;state<2;state++){
            hud.SetHpBarrier(state==0?100:28,state==0?76:0,100,100);
            var wrapper=new GameObject("Vitals capture",typeof(RectTransform));var copy=UnityEngine.Object.Instantiate(vitals.gameObject,wrapper.transform,false);Rect(copy.transform,0,0,410,132);copy.SetActive(true);
            EquipmentIconsAuthoring.Render(wrapper,Output+(state==0?"/Health.png":"/Health-Low.png"),null,new Rect(730,455,460,170),world);UnityEngine.Object.DestroyImmediate(wrapper);
        }
        UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(light.gameObject);SkillInventoryState.Reset();
    }
}
#endif

