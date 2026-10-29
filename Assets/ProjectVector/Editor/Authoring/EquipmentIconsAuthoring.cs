#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class EquipmentIconsAuthoring
{
    public const string Folder = "Assets/ProjectVector/Shared/UI/EquipmentIcons";
    public const string CatalogPath = Folder + "/EquipmentIcons.asset";
    const string ScenePath = "Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity";
    static readonly string[] Names = { "P92", "P1911", "R1895", "UZI", "UMP", "S1897", "AA-12", "Dragunov", "M416", "AK47", "M249", "Kar98", "AWM" };
    static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static T Get<T>(UnityEngine.Object target, string field) where T:UnityEngine.Object => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    static void Layout(RectTransform rt, Vector2 position, Vector2 size)
    { rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * .5f; rt.anchoredPosition = position; rt.sizeDelta = size; }

    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Isolated editor batch only");
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Folder, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        var catalog = AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(CatalogPath);
        if (!catalog) { catalog = ScriptableObject.CreateInstance<EquipmentIconCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        catalog.weapons = new Sprite[14]; catalog.armor = new Sprite[2];
        for (int i = 0; i < 14; i++) catalog.weapons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Weapon_" + i + ".png");
        catalog.armor[0] = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/LightShield.png");
        catalog.armor[1] = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/HeavyShield.png");
        catalog.device = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Device.png");
        if (catalog.weapons.Any(s => !s) || catalog.armor.Any(s => !s)) throw new Exception("Missing equipment icon");
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        var ink=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EquipmentInk.mat");
        if (!ink) {ink=new Material(Shader.Find("UI/EquipmentInk"));AssetDatabase.CreateAsset(ink,Folder+"/EquipmentInk.mat");}
        // Keep a byte-for-byte snapshot for a bounded YAML merge after Unity saves.
        string output = Environment.GetEnvironmentVariable("VECTOR_EQUIPMENT_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "VectorEquipment");
        Directory.CreateDirectory(output);
        if (!File.Exists(Path.Combine(output,"Scene-before.unity"))) File.Copy(ScenePath, Path.Combine(output,"Scene-before.unity"));
        var scene = EditorSceneManager.OpenScene(ScenePath);
        // Opening another scene can unload unreferenced ScriptableObjects/materials.
        catalog=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(CatalogPath);
        ink=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EquipmentInk.mat");
        if (!catalog || !ink) throw new Exception("Persistent equipment assets unavailable after opening scene");
        var roots = scene.GetRootGameObjects();
        var items = roots.SelectMany(r => r.GetComponentsInChildren<StoreItemButton>(true)).ToArray();
        foreach (var item in items)
        {
            var root = (RectTransform)item.transform;
            // Only these two legacy illustrations overlap the new authored icon.
            var legacy = root.Find("AKMImage");
            if (legacy && item.IsWeaponButton() && (item.ItemId == 9 || item.ItemId == 12)) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            var iconTransform = root.Find("EquipmentIcon");
            var icon = iconTransform ? iconTransform.GetComponent<Image>() : new GameObject("EquipmentIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            icon.gameObject.layer = 5; icon.transform.SetParent(root,false); icon.transform.SetAsFirstSibling();
            float h = root.rect.height;
            Layout(icon.rectTransform, new Vector2(0,24), new Vector2(184,Mathf.Max(42,h-66)));
            icon.sprite = item.IsArmorButton() ? catalog.Armor(item.ItemId) : catalog.Weapon(item.ItemId);
            icon.preserveAspect = true; icon.raycastTarget = false; icon.color = Color.white;
            icon.material=ink;
            Ref(item,"equipmentImage",icon); Ref(item,"equipmentIcons",catalog);
            var name = Get<TMP_Text>(item,"itemNameText"); var price = Get<TMP_Text>(item,"priceText");
            var so = new SerializedObject(item);
            so.FindProperty("itemName").stringValue = item.IsArmorButton() ? (item.ItemId==0?"경량 방어막":"중량 방어막") : Names[item.ItemId];
            so.ApplyModifiedPropertiesWithoutUndo();
            Layout(name.rectTransform,new Vector2(-20,-h*.5f+36),new Vector2(144,30));
            name.alignment = TextAlignmentOptions.MidlineLeft; name.fontSize=19; name.color=Color.white; name.raycastTarget=false;
            Layout(price.rectTransform,new Vector2(51,-h*.5f+14),new Vector2(82,22));
            price.alignment=TextAlignmentOptions.MidlineRight; price.fontSize=16; price.color=new Color(.64f,.85f,.93f); price.raycastTarget=false;
            var background=item.GetComponent<Image>(); background.color=new Color(.16f,.21f,.28f,1);
            var button=item.GetComponent<Button>(); var colors=button.colors;
            colors.normalColor=Color.white; colors.highlightedColor=new Color(.6f,.88f,1); colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.3f,.65f,.8f); colors.disabledColor=new Color(.55f,.6f,.65f,1); button.colors=colors;
            item.Refresh(9000,true); item.SetOwned(item.IsWeaponButton() && item.ItemId==0);
        }
        foreach (var hud in roots.SelectMany(r=>r.GetComponentsInChildren<InGameUIManager>(true)))
        {
            Ref(hud,"equipmentIcons",catalog);
            Ref(hud,"defaultPistolSprite",catalog.Weapon(0)); Ref(hud,"defaultKnifeSprite",catalog.Weapon(13));
            Ref(hud,"defaultSpikeSprite",catalog.device);
            var unused=Get<Image>(hud,"throwWeaponImage");
            if (unused) UnityEngine.Object.DestroyImmediate(unused.gameObject);
            Ref(hud,"throwWeaponImage",null);Ref(hud,"defaultThrowWeaponSprite",null);
            var backgrounds=new Image[4];GameObject badgeTemplate=null;Transform rowsParent=null;
            var fields=new[]{"mainWeaponImage","subWeaponImage","knifeImage","spikeImage"};
            for (int i=0;i<fields.Length;i++)
            {
                string field=fields[i];var existing=Get<Image>(hud,field);
                var row=existing.name=="EquipmentArtwork"?existing.transform.parent.GetComponent<Image>():existing;
                if(i==0)rowsParent=row.transform.parent;else row.transform.SetParent(rowsParent,false);
                row.transform.localScale=Vector3.one;
                var rect=row.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,.5f);
                rect.anchoredPosition=new Vector2(-22,54+i*78);rect.sizeDelta=new Vector2(256,66);
                row.sprite=null;row.material=null;row.enabled=true;row.type=Image.Type.Simple;row.raycastTarget=false;
                backgrounds[i]=row;
                var art=row.transform.Find("EquipmentArtwork");
                var icon=art?art.GetComponent<Image>():new GameObject("EquipmentArtwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
                icon.gameObject.layer=5;icon.transform.SetParent(row.transform,false);
                Layout(icon.rectTransform,new Vector2(-18,0),new Vector2(192,54));
                icon.preserveAspect=true;icon.raycastTarget=false;icon.material=ink;icon.color=Color.white;
                icon.sprite=i==0?null:i==3?catalog.device:catalog.Weapon(i==2?13:0);
                icon.enabled=icon.sprite!=null;
                Ref(hud,field,icon);
                var label=row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
                if (!label && badgeTemplate) label=UnityEngine.Object.Instantiate(badgeTemplate,row.transform).GetComponentInChildren<TMP_Text>(true);
                if (!label) throw new Exception("Missing authored slot badge "+field);
                badgeTemplate=label.transform.parent.gameObject;
                Layout((RectTransform)label.transform.parent,new Vector2(108,0),new Vector2(24,24));
                label.text=(i+1).ToString();label.color=Color.white;label.fontSize=18;
                label.alignment=TextAlignmentOptions.Center;
                if(i==3) {Ref(hud,"spikeSlotRoot",row.gameObject);row.gameObject.SetActive(false);}
            }
            var hudSO=new SerializedObject(hud);var bg=hudSO.FindProperty("weaponSlotBackgrounds");bg.arraySize=4;
            for(int i=0;i<4;i++)bg.GetArrayElementAtIndex(i).objectReferenceValue=backgrounds[i];hudSO.ApplyModifiedPropertiesWithoutUndo();
            hud.SetSelectedWeaponSlot(-1);
        }
        foreach (var row in roots.SelectMany(r=>r.GetComponentsInChildren<ScoreboardRowUI>(true)))
        {
            Ref(row,"equipmentIcons",catalog);
            var icon = Get<GameObject>(row,"weaponImageObject").GetComponent<Image>();
            icon.preserveAspect=true; icon.raycastTarget=false;icon.material=ink;
            Layout(icon.rectTransform,new Vector2(205.8f,0),new Vector2(125,48));
            icon.gameObject.SetActive(false);
        }
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.Copy(ScenePath,Path.Combine(output,"Scene-authored.unity"),true);
        // Render the actual authored panels, without starting networking or spawning players.
        var store=roots.SelectMany(r=>r.GetComponentsInChildren<StorePanelUI>(true)).First();
        Render(Get<GameObject>(store,"storePanelRoot"),Path.Combine(output,"Store.png"));
        var board=roots.SelectMany(r=>r.GetComponentsInChildren<InGameScoreboardUI>(true)).First();
        var boardRoot=Get<GameObject>(board,"rootPanel");
        var boardSO=new SerializedObject(board);
        foreach(string field in new[]{"ourTeamRows","enemyTeamRows"})
        {
            var rows=boardSO.FindProperty(field);
            for(int i=0;i<rows.arraySize;i++)
            {
                var row=(ScoreboardRowUI)rows.GetArrayElementAtIndex(i).objectReferenceValue;
                row.SetData(new ScoreboardMemberData {playerId=(uint)(i+1),nickname=new[]{"LYS","KSW","CJU"}[i%3],kills=4+i,deaths=2,assists=1,credits=2300,
                    hasEquipment=true,ownedWeaponId=new[]{9,12,0}[i%3],equipmentAlive=true},
                    (Sprite)boardSO.FindProperty("characterSprites").GetArrayElementAtIndex(i%4).objectReferenceValue,1,field=="ourTeamRows");
            }
        }
        Render(boardRoot,Path.Combine(output,"Scoreboard.png"));
        Debug.Log("EQUIPMENT_AUTHOR_PASS: 13 weapon icons, 2 armor icons, 15 authored shop cards, 6 scoreboard row bindings");
    }

    public static void Render(GameObject source,string path,Action<GameObject> configure=null,Rect? crop=null,Action<Camera> cameraSetup=null)
    {
        var canvasGO=new GameObject("EquipmentRender",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var canvas=canvasGO.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera;
        var cameraGO=new GameObject("EquipmentRenderCamera");var camera=cameraGO.AddComponent<Camera>();
        camera.enabled=false; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.07f,.09f,.12f);
        camera.cullingMask=1<<30;canvasGO.layer=30;canvas.worldCamera=camera;canvas.planeDistance=1;
        var scale=canvasGO.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);
        var clone=UnityEngine.Object.Instantiate(source,canvasGO.transform,false);clone.SetActive(true);
        foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var rect=(RectTransform)clone.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;
        // The capture has a fixed reference-size target; the batch editor's
        // desktop/GameView dimensions must not scale its Canvas down.
        scale.enabled=false;canvas.scaleFactor=1;
        cameraSetup?.Invoke(camera);
        canvas.planeDistance=camera.nearClipPlane+.02f; // World geometry must not occlude the HUD capture.
        // Geometry assertions must see the final target size, not the temporary 100x100 clone.
        Canvas.ForceUpdateCanvases();
        camera.Render(); // Resolve camera-space Canvas geometry against the RenderTexture.
        Canvas.ForceUpdateCanvases();
        configure?.Invoke(clone);
        Canvas.ForceUpdateCanvases();foreach(var t in clone.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate(true);Canvas.ForceUpdateCanvases();camera.Render();
        var area=crop??new Rect(0,0,1920,1080);
        var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D((int)area.width,(int)area.height,TextureFormat.RGB24,false);
        tex.ReadPixels(area,0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(canvasGO);UnityEngine.Object.DestroyImmediate(cameraGO);UnityEngine.Object.DestroyImmediate(rt);
    }
}
#endif
