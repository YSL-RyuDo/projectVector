#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CombatHudAuthoring
{
    public const string Output="C:/Users/GCT/Documents/Vector-CombatHUD-20261028";
    const string ScenePath="Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity";
    static TMP_FontAsset font;static Material fontMaterial,ink;static EquipmentIconCatalog icons;
    static Color Dark=new(.035f,.061f,.09f,.85f),Ice=new(.64f,.88f,1),Muted=new(.77f,.83f,.89f),Red=new(1,.48f,.49f),Gold=new(1,.8f,.4f);
    static T Get<T>(UnityEngine.Object o,string f) where T:UnityEngine.Object=>InGameUiAuthoring.Get<T>(o,f);
    static void Ref(UnityEngine.Object o,string f,UnityEngine.Object v)=>InGameUiAuthoring.Ref(o,f,v);
    static void Arr(UnityEngine.Object o,string f,UnityEngine.Object[] v){var so=new SerializedObject(o);var a=so.FindProperty(f);a.arraySize=v.Length;for(int i=0;i<v.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=v[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static T[] Refs<T>(UnityEngine.Object o,string f) where T:UnityEngine.Object {var a=new SerializedObject(o).FindProperty(f);return Enumerable.Range(0,a.arraySize).Select(i=>(T)a.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static void Fixed(Transform t){foreach(var c in t.GetComponents<LayoutGroup>())c.enabled=false;foreach(var c in t.GetComponents<ContentSizeFitter>())c.enabled=false;}
    static void R(Transform t,float x,float y,float w,float h){Fixed(t);var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition3D=new Vector3(x,y,0);r.sizeDelta=new Vector2(w,h);r.localRotation=Quaternion.identity;r.localScale=Vector3.one;}
    static void Full(Transform t){Fixed(t);var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    static GameObject Node(Transform p,string n){var t=p.Find(n);if(t)return t.gameObject;var g=new GameObject(n,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);return g;}
    static void Move(Transform t,Transform p,float x,float y,float w,float h){t.SetParent(p,false);R(t,x,y,w,h);}
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c){var g=Node(p,n);var v=g.GetComponent<Image>()??g.AddComponent<Image>();R(g.transform,x,y,w,h);v.color=c;v.sprite=null;v.material=null;v.raycastTarget=false;v.enabled=true;v.type=Image.Type.Simple;return v;}
    static HudShapeGraphic Shape(Transform p,string n,float x,float y,float w,float h,Color c,HudShapeGraphic.Shape type=HudShapeGraphic.Shape.CutPanel){var g=Node(p,n);R(g.transform,x,y,w,h);var v=g.GetComponent<HudShapeGraphic>()??g.AddComponent<HudShapeGraphic>();v.shape=type;v.color=c;v.raycastTarget=false;return v;}
    static TMP_Text Text(Transform p,string n,string value,float x,float y,float w,float h,float size,Color c,TextAlignmentOptions a=TextAlignmentOptions.Center){var g=Node(p,n);R(g.transform,x,y,w,h);var v=g.GetComponent<TMP_Text>()??g.AddComponent<TextMeshProUGUI>();Style(v,size,c,a);v.text=value;return v;}
    static void Style(TMP_Text v,float size,Color c,TextAlignmentOptions a=TextAlignmentOptions.Center){v.font=font;v.fontSharedMaterial=fontMaterial;v.fontSize=size;v.enableAutoSizing=false;v.textWrappingMode=TextWrappingModes.NoWrap;v.overflowMode=TextOverflowModes.Ellipsis;v.alignment=a;v.color=c;v.raycastTarget=false;}
    static Image Icon(Transform p,string n,float x,float y,float w,float h,Sprite s,bool equipment=false){var v=Box(p,n,x,y,w,h,Color.white);v.sprite=s;v.preserveAspect=true;v.enabled=s;if(equipment)v.material=ink;return v;}
    static void ClearFace(Transform t){var g=t.GetComponent<Graphic>();if(g){g.color=Color.clear;g.raycastTarget=false;}}
    static void MoveText(UnityEngine.Object owner,string field,Transform p,float x,float y,float w,float h,float size,Color color){var t=Get<TMP_Text>(owner,field);Move(t.transform,p,x,y,w,h);Style(t,size,color);}

    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Batch authoring only");
        Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(ScenePath);var roots=scene.GetRootGameObjects();
        T Find<T>() where T:Component=>roots.SelectMany(r=>r.GetComponentsInChildren<T>(true)).First();
        var source=Find<TMP_Text>();font=source.font;fontMaterial=source.fontSharedMaterial;
        icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);ink=AssetDatabase.LoadAssetAtPath<Material>(EquipmentIconsAuthoring.Folder+"/EquipmentInk.mat");
        var top=Find<TopInfoUI>();var hud=Find<InGameUIManager>();var match=Find<MatchManager>();var kill=Find<KillLogManager>();var feedback=Find<CombatFeedbackUI>();var prompt=Find<InteractionPromptUI>();
        AuthorTop(top,match);AuthorBottom(hud);AuthorKillFeed(kill);AuthorObjective(feedback,prompt);AuthorChat(Find<InGameChatUI>());AuthorMiniMap(Find<MiniMapManager>());AnchorEdges(top,hud,kill);
        foreach(var voice in roots.SelectMany(r=>r.GetComponentsInChildren<VectorVoicePanel>(true)))
        {
            // Hidden in the saved scene as well as at runtime: no login/pick-frame F10 flash.
            voice.openButton.gameObject.SetActive(false);voice.panel.SetActive(false);voice.speakingText.text="";foreach(var row in voice.hudRows)row.SetActive(false);
            R(voice.openButton.transform,-838,-380,168,34);Anchor(voice.openButton.transform,Vector2.zero);var im=voice.openButton.GetComponent<Image>();if(im){im.sprite=null;im.color=Dark;}
            var label=voice.openButton.GetComponentInChildren<TMP_Text>(true);Style(label,16,Muted);label.text="F10   팀 음성";
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Verify(scene.GetRootGameObjects());Preview(top,hud,match,kill,feedback,prompt);Preview(top,hud,match,kill,feedback,prompt,"Desert");
        Debug.Log("COMBAT_HUD_AUTHOR_PASS");
    }
    static void Anchor(Transform t,Vector2 anchor)
    {
        var rt=(RectTransform)t;var offset=rt.anchoredPosition-new Vector2((anchor.x-.5f)*1920,(anchor.y-.5f)*1080);
        rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=offset;
    }
    static void AnchorEdges(TopInfoUI top,InGameUIManager hud,KillLogManager kill)
    {
        foreach(Transform t in top.transform)if(t is RectTransform && ((RectTransform)t).anchorMin==new Vector2(.5f,.5f))Anchor(t,new Vector2(.5f,1));
        Anchor(Get<Image>(hud,"charImage").transform.parent,new Vector2(0,0));
        Anchor(Get<Image>(hud,"cSkillImage").transform.parent,new Vector2(.5f,0));
        foreach(var row in Refs<Image>(hud,"weaponSlotBackgrounds"))Anchor(row.transform,new Vector2(1,0));
        Anchor(hud.GetComponent<CombatHudReadout>().ammoRoot.transform,new Vector2(1,0));
        Anchor(hud.GetComponent<CombatHudReadout>().reloadRoot.transform,new Vector2(.5f,0));
        Anchor(Get<ScrollRect>(kill,"scrollRect").transform,new Vector2(1,1));
    }
    static Sprite CircleMask()
    {
        const string path="Assets/ProjectVector/Modules/Match/UI/Sprites/HUD/MinimapCircle.png";
        if(!File.Exists(path)) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));var t=new Texture2D(512,512,TextureFormat.RGBA32,false);var pixels=new Color[512*512];
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)pixels[y*512+x]=new Color(1,1,1,Mathf.Clamp01(255.5f-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(256,256))));
            t.SetPixels(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new Exception("Minimap circle sprite import failed");return sprite;
    }
    static void AuthorMiniMap(MiniMapManager manager)
    {
        var map=Get<RectTransform>(manager,"_miniMapHUD");R(map,-798,362,220,220);Anchor(map,new Vector2(0,1));ClearFace(map);
        var mask=map.GetComponentInChildren<Mask>(true);Full(mask.transform);mask.showMaskGraphic=false;mask.GetComponent<Image>().sprite=CircleMask();
        foreach(var raw in map.GetComponentsInChildren<RawImage>(true)){Full(raw.transform);raw.uvRect=new Rect(0,0,1,1);raw.color=Color.white;raw.raycastTarget=false;}
        var so=new SerializedObject(manager);so.FindProperty("_hudWidth").floatValue=220;so.FindProperty("_hudHeight").floatValue=220;so.ApplyModifiedPropertiesWithoutUndo();
        var marker=Get<RectTransform>(manager,"_hudPlayerMarker");R(marker,0,0,22,22);
        Shape(marker,"ViewDirection",0,18,12,9,Color.white,HudShapeGraphic.Shape.HeadingArrow);
        var camera=Get<Camera>(manager,"_miniMapCamera");camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.085f,.11f,1);camera.useOcclusionCulling=false;camera.allowHDR=false;camera.transform.localScale=Vector3.one;
        var rt=camera.targetTexture;rt.width=768;rt.height=768;rt.filterMode=FilterMode.Bilinear;EditorUtility.SetDirty(rt);
    }
    static void AuthorTop(TopInfoUI top,MatchManager match)
    {
        var root=top.transform;Full(root);
        foreach(string name in new[]{"OurTeamListRoot/Viewport","EnemyTeamListRoot/Viewport"}){var t=root.Find(name);if(t){ClearFace(t);var mask=t.GetComponent<Mask>();if(mask)mask.showMaskGraphic=false;}}
        foreach(string n in new[]{"Timer","OurTeamScoreImage","EnemyTeamScoreImage","GameDescription","OurTeamList","EnemyTeamList"}){var t=root.Find(n);if(t)ClearFace(t);}
        var clock=Get<TMP_Text>(top,"timerText").transform.parent;Move(clock,root,0,488,144,66);ClearFace(clock);
        Shape(clock,"ClockPlate",0,0,144,66,Dark).transform.SetAsFirstSibling();MoveText(top,"timerText",clock,0,-5,130,46,34,Color.white);
        Ref(top,"roundLabel",Text(clock,"RoundLabel","ROUND  01",0,23,130,19,12,Muted));
        for(int i=0;i<2;i++)
        {
            var field=i==0?"ourTeamScoreText":"enemyTeamScoreText";var score=Get<TMP_Text>(top,field);var p=score.transform.parent;Move(p,root,i==0?-112:112,488,64,66);ClearFace(p);
            Shape(p,"ScorePlate",0,0,64,66,Dark).transform.SetAsFirstSibling();Move(score.transform,p,0,1,64,55);Style(score,38,i==0?Ice:Red);
            Box(p,"TeamStripe",0,-32,48,3,i==0?Ice:Red);
        }
        for(int side=0;side<2;side++)
        {
            var list=Get<GameObject>(match,side==0?"ourTeamListRoot":"enemyTeamListRoot");Move(list.transform,root,side==0?-290:290,488,230,70);
            var portraits=Refs<Image>(match,side==0?"ourTeamImages":"enemyTeamImages");
            for(int i=0;i<portraits.Length;i++)
            {
                var portrait=portraits[i];Move(portrait.transform,list.transform,(i-1)*76,0,60,60);portrait.preserveAspect=true;portrait.raycastTarget=false;
                var badge=portrait.GetComponent<TeamLifeBadge>()??portrait.gameObject.AddComponent<TeamLifeBadge>();badge.teamColor=side==0?Ice:Red;
                badge.line=Box(portrait.transform,"LifeStripe",0,-32,60,3,badge.teamColor);
                var dead=Shape(portrait.transform,"DeathShade",0,0,60,60,new Color(.02f,.04f,.07f,.78f));badge.deathMark=dead.gameObject;
                Text(dead.transform,"Cross","×",0,5,56,46,40,Color.white);badge.state=Text(portrait.transform,"LifeState","",0,-18,60,20,12,Muted);badge.Present(false);
            }
            MoveText(top,side==0?"ourTeamRoleText":"enemyTeamRoleText",root,side==0?-290:290,439,230,24,16,side==0?Ice:Red);
        }
        var phase=Get<TMP_Text>(top,"gameDescriptionText").transform.parent;Move(phase,root,0,409,300,62);ClearFace(phase);
        Shape(phase,"PhasePlate",0,0,300,62,Dark).transform.SetAsFirstSibling();Ref(top,"phaseAccent",Box(phase,"PhaseAccent",0,30,274,2,Ice));
        MoveText(top,"gameDescriptionText",phase,0,8,280,32,23,Color.white);Ref(top,"phaseHint",Text(phase,"Hint","B  장비 구매",0,-17,284,22,14,Muted));
        Ref(top,"announcementText",Text(root,"PhaseAnnouncement","",0,325,620,44,26,Color.white));Get<TMP_Text>(top,"announcementText").gameObject.SetActive(false);
    }
    static void AuthorBottom(InGameUIManager hud)
    {
        var portrait=Get<Image>(hud,"charImage");var player=portrait.transform.parent;var info=player.parent;Full(info);Move(player,info,-690,-447,410,132);ClearFace(player);
        var read=hud.GetComponent<CombatHudReadout>()??hud.gameObject.AddComponent<CombatHudReadout>();Ref(hud,"combatReadout",read);read.icons=icons;
        Shape(player,"VitalsPlate",0,0,374,112,Dark).transform.SetAsFirstSibling();Box(player,"VitalsEdge",-183,0,3,80,Ice);
        Move(portrait.transform,player,-139,0,62,72);portrait.preserveAspect=true;
        MoveText(hud,"hpText",player,-44,0,110,76,46,Color.white);read.health=Get<TMP_Text>(hud,"hpText");read.healthState=Text(player,"HealthState","체력",-45,43,100,22,13,Muted);
        var hp=Get<Image>(hud,"hpBar");var hpBack=hp.transform.parent;Move(hpBack,player,16,-36,260,7);var hb=hpBack.GetComponent<Image>();if(hb){hb.sprite=null;hb.color=new Color(.26f,.32f,.38f,.85f);}Move(hp.transform,hpBack,0,0,260,7);hp.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");hp.type=Image.Type.Filled;hp.fillMethod=Image.FillMethod.Horizontal;hp.fillOrigin=0;hp.color=Ice;read.healthAccent=hp;
        var armor=Get<Image>(hud,"barrierBar");var armorBack=armor.transform.parent;Move(armorBack,player,94,-20,104,3);var ab=armorBack.GetComponent<Image>();if(ab){ab.sprite=null;ab.color=new Color(.26f,.32f,.38f,.85f);}Move(armor.transform,armorBack,0,0,104,3);armor.sprite=hp.sprite;armor.type=Image.Type.Filled;armor.fillMethod=Image.FillMethod.Horizontal;armor.color=new Color(.48f,.73f,1);
        read.armorIcon=Icon(player,"ShieldGlyph",59,8,26,36,icons.Armor(1),true);read.armor=Text(player,"ShieldValue","100",106,7,70,44,30,Ice);Text(player,"ShieldLabel","방어막",101,42,94,22,13,Muted);
        var skills=Get<Image>(hud,"cSkillImage").transform.parent;Move(skills,info,-10,-446,566,148);ClearFace(skills);
        read.skillGroup=skills.GetComponent<CanvasGroup>();if(!read.skillGroup)read.skillGroup=skills.gameObject.AddComponent<CanvasGroup>();read.skillStates=new TMP_Text[4];read.skillStateBackings=new Graphic[4];
        for(int i=0;i<4;i++)
        {
            string key=new[]{"c","q","e","x"}[i];var image=Get<Image>(hud,key+"SkillImage");Move(image.transform,skills,-204+i*136,5,56,56);image.sprite=null;image.preserveAspect=true;image.type=Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;
            // Keep the assigned icon component; only its surrounding treatment changes.
            var plate=Shape(skills,key+"SkillPlate",-204+i*136,4,108,94,Color.clear);plate.transform.SetAsFirstSibling();
            foreach(var legacy in image.GetComponentsInChildren<TMP_Text>(true))if(legacy.name!="UltimatePoints")legacy.gameObject.SetActive(false);
            var gauge=Get<Image>(hud,key+"SkillGauge");Move(gauge.transform,image.transform,0,-35,48,3);gauge.sprite=hp.sprite;gauge.type=Image.Type.Filled;gauge.fillMethod=Image.FillMethod.Horizontal;gauge.color=Ice;
            var keyText=Text(skills,key+"Key",key.ToUpper(),-204+i*136,-52,34,26,17,Color.white);var keyLine=Box(keyText.transform,"Underline",0,-12,22,2,Muted);
            read.skillStateBackings[i]=Shape(skills,key+"StatusBacking",-204+i*136,56,124,23,new Color(.035f,.06f,.085f,.88f));read.skillStateBackings[i].transform.SetAsFirstSibling();
            read.skillStates[i]=Text(skills,key+"State","",-204+i*136,56,124,23,14,Muted);
        }
        var x=Get<Image>(hud,"xSkillImage");R(x.transform,204,-1,50,50);Get<Image>(hud,"xSkillGauge").enabled=false;
        read.ultimateArc=Shape(skills,"UltimateChargeArc",204,-31,92,5,Gold,HudShapeGraphic.Shape.ChargeRail);read.ultimateArc.thickness=5;
        MoveText(hud,"ultimatePointText",skills,204,34,86,18,12,Gold);read.chargeText=Get<TMP_Text>(hud,"ultimatePointText");
        Shape(skills,"SkillReadability",0,1,562,138,new Color(.025f,.04f,.06f,.9f),HudShapeGraphic.Shape.FadePlate).transform.SetAsFirstSibling();
        var weapons=Get<Image>(hud,"mainWeaponImage").transform.parent.parent;Full(weapons);
        var backs=Refs<Image>(hud,"weaponSlotBackgrounds");
        for(int i=0;i<backs.Length;i++)
        {
            var row=backs[i];Move(row.transform,weapons,813,-214+i*58,228,50);row.sprite=null;row.material=null;row.color=new Color(.06f,.10f,.15f,.72f);row.raycastTarget=false;
            var art=Get<Image>(hud,new[]{"mainWeaponImage","subWeaponImage","knifeImage","spikeImage"}[i]);Move(art.transform,row.transform,-14,0,167,42);art.material=ink;art.preserveAspect=true;
            var oldLabel=row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            var badge=Box(row.transform,"SlotKey",92,0,24,24,new Color(.13f,.19f,.25f,.9f));
            foreach(var legacy in row.GetComponentsInChildren<Image>(true))if(legacy!=row && legacy!=art && legacy!=badge && legacy.transform.parent==row.transform)legacy.color=Color.clear;
            if(oldLabel){Move(oldLabel.transform,badge.transform,0,0,24,24);Style(oldLabel,15,Color.white);}
        }
        var ammo=Node(weapons,"AmmoReadout").transform;R(ammo,564,-450,200,114);Shape(ammo,"AmmoPlate",0,0,200,112,Dark).transform.SetAsFirstSibling();
        MoveText(hud,"bulletText",ammo,-27,0,100,66,52,Color.white);read.reserve=Text(ammo,"Reserve","90",63,-8,64,40,24,Muted);Text(ammo,"Divider","/",25,-7,22,38,22,Muted);
        read.ammoRoot=ammo.gameObject;read.ammoState=Text(ammo,"AmmoState","잔탄",0,39,170,22,13,Muted);Text(ammo,"ReserveLabel","예비",63,21,64,20,12,Muted);
        var reload=Node(info,"ReloadNotice").transform;R(reload,0,-218,240,36);Shape(reload,"Plate",0,0,240,36,Dark);read.reloadText=Text(reload,"Text","재장전 중",0,0,224,32,20,Gold);read.reloadRoot=reload.gameObject;read.reloadRoot.SetActive(false);
        var so=new SerializedObject(hud);so.FindProperty("slotColor").colorValue=new Color(.04f,.075f,.12f,.62f);so.FindProperty("selectedSlotColor").colorValue=new Color(.16f,.28f,.37f,.94f);so.ApplyModifiedPropertiesWithoutUndo();
        read.PresentHealth(100,0,100,0);read.PresentAmmo(12,36);read.PresentSkills();ShopReadabilityAuthoring.AuthorVitals(hud);EditorUtility.SetDirty(read);
    }
    static void AuthorKillFeed(KillLogManager manager)
    {
        var content=Get<RectTransform>(manager,"content");var view=Get<ScrollRect>(manager,"scrollRect");view.enabled=false;Move(view.transform,view.transform.parent,716,233,430,290);ClearFace(view.transform);
        var viewport=view.viewport;Full(viewport);ClearFace(viewport);var mask=viewport.GetComponent<Mask>();if(mask)mask.enabled=false;var rectMask=viewport.GetComponent<RectMask2D>();if(rectMask)rectMask.enabled=false;
        Full(content);var entries=new KillLogEntryUI[5];
        for(int i=0;i<5;i++)
        {
            var row=Node(content,"FeedRow"+i);R(row.transform,0,115-i*56,430,52);var e=row.GetComponent<KillLogEntryUI>()??row.AddComponent<KillLogEntryUI>();var group=row.GetComponent<CanvasGroup>();if(!group)group=row.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
            Shape(row.transform,"Plate",0,0,430,52,Dark);Ref(e,"teamAccent",Box(row.transform,"Accent",-213,0,3,36,Ice));
            Ref(e,"killerImage",Icon(row.transform,"Killer",-188,5,32,32,null));Ref(e,"deathImage",Icon(row.transform,"Victim",188,5,32,32,null));
            Ref(e,"killerNicknameText",Text(row.transform,"KillerName","",-116,8,104,27,17,Ice,TextAlignmentOptions.MidlineLeft));
            Ref(e,"deathNicknameText",Text(row.transform,"VictimName","",119,8,100,27,17,Color.white,TextAlignmentOptions.MidlineRight));
            Ref(e,"weaponImage",Icon(row.transform,"Weapon",-8,6,88,30,null,true));
            Ref(e,"sourceText",Text(row.transform,"Source","",-8,6,88,30,16,Muted));Ref(e,"headshotText",Text(row.transform,"Headshot","",51,7,28,24,12,Gold));
            Ref(e,"assistText",Text(row.transform,"Assists","",0,-16,332,20,13,Muted,TextAlignmentOptions.MidlineLeft));Ref(e,"equipmentIcons",icons);entries[i]=e;row.SetActive(false);
        }
        Arr(manager,"authoredEntries",entries);
    }
    static void AuthorObjective(CombatFeedbackUI feedback,InteractionPromptUI prompt)
    {
        var obj=Get<GameObject>(feedback,"objectiveNoticePanel");Move(obj.transform,obj.transform.parent,0,283,430,76);ClearFace(obj.transform);
        var icon=Get<GameObject>(feedback,"objectiveIcon").GetComponent<Image>();Move(icon.transform,obj.transform,-176,0,62,62);icon.sprite=icons.device;icon.material=ink;icon.color=Gold;icon.preserveAspect=true;
        MoveText(feedback,"objectiveNoticeText",obj.transform,30,0,344,68,19,Color.white);Get<TMP_Text>(feedback,"objectiveNoticeText").textWrappingMode=TextWrappingModes.Normal;
        var progress=Get<GameObject>(feedback,"spikeProgressPanel");Move(progress.transform,progress.transform.parent,0,-164,366,8);var slider=Get<Slider>(feedback,"spikeProgressSlider");slider.interactable=false;
        if(slider.fillRect){slider.fillRect.GetComponent<Image>().color=Ice;var fp=slider.fillRect.parent as RectTransform;Full(fp);}
        if(slider.handleRect)slider.handleRect.gameObject.SetActive(false);
        MoveText(feedback,"spikeProgressText",progress.transform,0,30,400,34,22,Color.white);
        var result=Get<GameObject>(feedback,"roundResultPanel");Move(result.transform,result.transform.parent,0,245,520,136);ClearFace(result.transform);
        Shape(result.transform,"ResultPlate",0,0,520,136,Dark).transform.SetAsFirstSibling();Box(result.transform,"ResultRule",0,64,440,2,Muted);
        MoveText(feedback,"resultTitleText",result.transform,-40,25,390,48,34,Ice);MoveText(feedback,"resultReasonText",result.transform,0,-22,460,30,19,Muted);MoveText(feedback,"resultScoreText",result.transform,200,25,90,42,24,Color.white);
        var root=Get<GameObject>(prompt,"root");Move(root.transform,root.transform.parent,0,-282,420,72);ClearFace(root.transform);Shape(root.transform,"PromptPlate",0,0,420,72,Dark).transform.SetAsFirstSibling();
        var key=Get<Image>(prompt,"keyImage");Move(key.transform,root.transform,-170,0,42,42);key.sprite=null;key.color=new Color(.77f,.89f,.96f);MoveText(prompt,"keyText",key.transform,0,0,40,40,23,Dark);
        MoveText(prompt,"promptText",root.transform,28,11,320,30,21,Color.white);MoveText(prompt,"actionHint",root.transform,28,-17,320,24,14,Muted);
    }
    static void AuthorChat(InGameChatUI chat)
    {
        // Reskin existing controls; their references, history and channel callbacks remain intact.
        var input=Get<GameObject>(chat,"inputRoot");input.SetActive(false);
        var messages=Get<GameObject>(chat,"messageRoot");var group=Get<CanvasGroup>(chat,"messageCanvasGroup");if(group)group.alpha=0;
        foreach(var im in messages.GetComponentsInChildren<Image>(true)){im.sprite=null;im.color=im.name=="Handle"?new Color(.5f,.64f,.72f,.4f):new Color(.035f,.06f,.085f,.25f);}
        foreach(var im in input.GetComponentsInChildren<Image>(true)){im.sprite=null;im.color=Dark;}
        var field=Get<TMP_InputField>(chat,"inputField");Style(field.textComponent,17,Color.white,TextAlignmentOptions.MidlineLeft);
        if(field.placeholder is TMP_Text placeholder){Style(placeholder,16,Muted,TextAlignmentOptions.MidlineLeft);placeholder.text="메시지 입력";}
        Style(Get<TMP_Text>(chat,"channelText"),16,Ice);Get<TMP_Text>(chat,"channelText").text="팀";
    }
    static void Verify(GameObject[] roots)
    {
        var top=roots.SelectMany(r=>r.GetComponentsInChildren<TopInfoUI>(true)).First();top.ApplyRoundRules(12,12);
        if(!top.IsATeamAttackRound(12)||top.IsATeamAttackRound(13)||!top.IsATeamAttackRound(25)||top.IsATeamAttackRound(26))throw new Exception("HUD round role mismatch");top.ApplyRoundRules(5,5);
        if(VectorVoicePanel.ShouldShowContext("Game_TestVersion",true,false,false,false)||VectorVoicePanel.ShouldShowContext("Career",true,true,true,true)||!VectorVoicePanel.ShouldShowContext("Lobby",true,false,false,true))throw new Exception("Voice visibility mismatch");
        var read=roots.SelectMany(r=>r.GetComponentsInChildren<CombatHudReadout>(true)).First();read.PresentHealth(28,43,100,100);if(read.health.text!="28"||read.armor.text!="43"||read.healthState.text!="체력 위험")throw new Exception("Vitals mismatch");read.PresentHealth(100,0,100,0);
        File.WriteAllText(Output+"/Authoring-checks.txt","PASS saved hierarchy, separate HP/armor, server round rule roles, voice context gating.\n");
    }
    static void Preview(TopInfoUI top,InGameUIManager hud,MatchManager match,KillLogManager kill,CombatFeedbackUI feedback,InteractionPromptUI prompt,string mapName="TestMap")
    {
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var canvas=top.GetComponentInParent<Canvas>().gameObject;var map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/"+mapName+".prefab"));
        map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var savedCam=UnityEngine.Object.FindFirstObjectByType<MiniMapCamera>(FindObjectsInactive.Include);
        var mapCamGo=UnityEngine.Object.Instantiate(savedCam.gameObject);var mapCam=mapCamGo.GetComponent<Camera>();mapCam.enabled=false;
        var mapTexture=new RenderTexture(768,768,24);mapCam.targetTexture=mapTexture;
        var controller=mapCamGo.GetComponent<MiniMapCamera>();MiniMapCamera.Instance=null;
        typeof(MiniMapCamera).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
        var refs=map.GetComponentInChildren<MiniMapWorldReferences>(true);var sr=new SerializedObject(refs);
        controller.SetMapFraming(sr.FindProperty("hudSize").floatValue,sr.FindProperty("fullMapSize").floatValue,sr.FindProperty("hudHeight").floatValue,sr.FindProperty("fullMapHeight").floatValue);
        controller.SetOverlayMapCenter((Transform)sr.FindProperty("fullMapCenter").objectReferenceValue);
        var spawn=map.GetComponent<MapInfo>().attackSpawnPoints[0];var origin=spawn.position+Vector3.up*1.5f;
        var view=new GameObject("HUD preview view").AddComponent<Camera>();view.enabled=false;view.transform.SetPositionAndRotation(origin,spawn.rotation);
        controller.SetTarget(spawn);controller.SetViewCamera(view);controller.SetOverlayMode(false);
        var lightGO=new GameObject("HUD preview light");var light=lightGO.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;lightGO.transform.rotation=Quaternion.Euler(45,-25,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
        mapCam.Render();
        for(int state=0;state<3;state++)
        {
            map.GetComponent<MapInfo>().SetBarriersActive(state==0);
            top.ApplyGameStartInfo();top.SetScoreByTeam(3,2);top.SetCurrentRoundRole(state==0?(byte)6:(byte)8);top.SetTimer(state==0?(ushort)21:(ushort)48);
            SkillInventoryState.SetCharacter(pick.GetCharacterData(1));SkillInventoryState.ApplyServerState(15,state==0?(byte)0:(byte)2);SkillInventoryState.ApplyUltimateState(state==0?4:7,7,100,false);
            hud.InitializeLocalPlayerUI(1,pick.GetCharacterData(1).characterImage,pick.GetCharacterData(1));hud.SetHpBarrier(state==0?100:28,state==0?100:0,100,100);hud.SetBulletText(state==0?30:0,90);hud.SetSelectedWeaponSlot(0);hud.SetSpikePossession(false);
            Get<Image>(hud,"mainWeaponImage").sprite=icons.Weapon(9);Get<Image>(hud,"mainWeaponImage").enabled=true;
            Get<GameObject>(match,"ourTeamListRoot").SetActive(true);Get<GameObject>(match,"enemyTeamListRoot").SetActive(true);
            foreach(var pair in new[]{("ourTeamImages",0),("enemyTeamImages",1)}){var ims=Refs<Image>(match,pair.Item1);for(int i=0;i<ims.Length;i++){ims[i].sprite=pick.GetCharacterData((i+pair.Item2)%4).characterImage;ims[i].enabled=true;ims[i].color=Color.white;ims[i].GetComponent<TeamLifeBadge>().Present(state>0 && i==2);}}
            var read=hud.GetComponent<CombatHudReadout>();read.PresentSkills();read.PresentReload(state==1);
            kill.ClearAll();kill.ShowDeath(1,2,true,1,3,"LYS","JJY");kill.ApplyDetails(1,2,9,true,"KSW");kill.ShowDeath(4,5,false,0,2,"KDH","CJU");kill.ApplyDetails(4,5,12,false,"");
            if(state>0){top.SetGameDescription("전투 단계");Get<TMP_Text>(top,"announcementText").gameObject.SetActive(false);}
            var objective=Get<GameObject>(feedback,"objectiveNoticePanel");objective.SetActive(state==2);if(state==2){top.SetGameDescription("디바이스 접속 중");top.SetUnknownTimer();Get<GameObject>(feedback,"objectiveIcon").SetActive(true);Get<TMP_Text>(feedback,"objectiveNoticeText").text="디바이스가 설치되었습니다";}
            var progress=Get<GameObject>(feedback,"spikeProgressPanel");progress.SetActive(state==2);if(state==2){Get<Slider>(feedback,"spikeProgressSlider").value=.56f;Get<TMP_Text>(feedback,"spikeProgressText").text="디바이스 접속 차단 중";}
            Get<GameObject>(feedback,"roundResultPanel").SetActive(false);prompt.Hide();if(state==2)prompt.Show("디바이스 접속 차단",true);
            var wrapper=new GameObject("HudCapture",typeof(RectTransform));
            var mini=UnityEngine.Object.FindFirstObjectByType<MiniMapManager>(FindObjectsInactive.Include);
            if(mini){var miniCopy=UnityEngine.Object.Instantiate(Get<RectTransform>(mini,"_miniMapHUD").gameObject,wrapper.transform,false);miniCopy.SetActive(true);foreach(var raw in miniCopy.GetComponentsInChildren<RawImage>(true))raw.texture=mapTexture;}
            var topCopy=UnityEngine.Object.Instantiate(top.gameObject,wrapper.transform,false);topCopy.name=top.name;topCopy.SetActive(true);
            var gameCopy=UnityEngine.Object.Instantiate(canvas.transform.Find("GameUIs").gameObject,wrapper.transform,false);gameCopy.name="GameUIs";gameCopy.SetActive(true);
            EquipmentIconsAuthoring.Render(wrapper,Output+"/"+mapName+new[]{"-HUD-Buy.png","-HUD-Combat.png","-HUD-Device.png"}[state],clone=>{
                foreach(string n in new[]{"PlayerPick","MatchLoadingPanel"}){var t=clone.transform.Find(n);if(t)t.gameObject.SetActive(false);}
                foreach(var chat in clone.GetComponentsInChildren<InGameChatUI>(true)){Get<GameObject>(chat,"inputRoot").SetActive(false);Get<CanvasGroup>(chat,"messageCanvasGroup").alpha=0;}
                var game=clone.transform.Find("GameUIs");game.gameObject.SetActive(true);clone.GetComponentInChildren<TopInfoUI>(true).gameObject.SetActive(true);
                foreach(var board in clone.GetComponentsInChildren<InGameScoreboardUI>(true))Get<GameObject>(board,"rootPanel").SetActive(false);
                foreach(var store in clone.GetComponentsInChildren<StorePanelUI>(true))Get<GameObject>(store,"storePanelRoot").SetActive(false);
            },null,cam=>{cam.cullingMask|=LayerMask.GetMask("Default","MiniMap");cam.transform.position=origin;cam.transform.rotation=view.transform.rotation;cam.fieldOfView=65;cam.nearClipPlane=.03f;cam.farClipPlane=300;cam.backgroundColor=new Color(.45f,.56f,.68f);});
            UnityEngine.Object.DestroyImmediate(wrapper);
        }
        UnityEngine.Object.DestroyImmediate(view.gameObject);UnityEngine.Object.DestroyImmediate(mapTexture);UnityEngine.Object.DestroyImmediate(mapCamGo);UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(lightGO);SkillInventoryState.Reset();
    }
}
#endif
