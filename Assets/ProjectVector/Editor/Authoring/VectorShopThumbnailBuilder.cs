using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VectorShopThumbnailBuilder
{
    private const string AtlasPath = "Assets/ProjectVector/Modules/Lobby/Resources/Shop/ShopRewardAtlas.png";
    // Update only the replaced model's tile; preserve all sprite IDs, rewards and other artwork.
    public static void UpdateControllerThumbnail()
    {
        var sprite = System.Linq.Enumerable.Single(Resources.LoadAll<Sprite>("Shop/ShopRewardAtlas"), s=>s.name=="character_3");
        var rect = sprite.rect;
        var atlas = new Texture2D(2,2,TextureFormat.RGBA32,false);
        atlas.LoadImage(File.ReadAllBytes(AtlasPath));
        var utility = new PreviewRenderUtility();
        try
        {
            utility.BeginStaticPreview(new Rect(0,0,rect.width,rect.height));
            var root=new GameObject("ControllerThumbnail");root.hideFlags=HideFlags.HideAndDontSave;root.SetActive(false);utility.AddSingleGO(root);
            var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("LobbyCharacter/Controller"),root.transform,false);
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            model.transform.localRotation=Quaternion.Euler(0,180,0);root.SetActive(true);
            var animator=model.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
            var points=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(model.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.bones));
            var bounds=new Bounds(points[0].position,Vector3.zero);foreach(var bone in points)if(bone!=null)bounds.Encapsulate(bone.position);
            utility.camera.fieldOfView=35;
            float distance=bounds.extents.magnitude/Mathf.Sin(17.5f*Mathf.Deg2Rad)*1.25f;
            utility.camera.transform.position=bounds.center+Vector3.back*distance;utility.camera.transform.rotation=Quaternion.identity;
            utility.camera.nearClipPlane=distance*.01f;utility.camera.farClipPlane=distance*4;
            utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.10f,.13f,.18f,1);
            utility.lights[0].intensity=1.5f;utility.lights[0].transform.rotation=Quaternion.Euler(40,30,0);
            utility.lights[1].intensity=1;utility.lights[1].transform.rotation=Quaternion.Euler(340,218,177);utility.ambientColor=new Color(.55f,.55f,.55f);
            utility.Render(true);var thumb=utility.EndStaticPreview();
            atlas.SetPixels((int)rect.x,(int)rect.y,(int)rect.width,(int)rect.height,thumb.GetPixels());atlas.Apply();
            File.WriteAllBytes(AtlasPath,atlas.EncodeToPNG());UnityEngine.Object.DestroyImmediate(thumb);
        }
        finally {utility.Cleanup();UnityEngine.Object.DestroyImmediate(atlas);}
        AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("CONTROLLER_SHOP_THUMBNAIL_UPDATED: character_3 only; sprite IDs retained");
    }
    [MenuItem("Tools/Project Vector/Rebuild Shop Thumbnails")]
    public static void Build()
    {
        var database = AssetDatabase.LoadAssetAtPath<WeaponSkinDatabase>("Assets/ProjectVector/Shared/Weapons/Assets/Weapon Skin Database.asset");
        var entries = new List<(string name, GameObject prefab)>();
        foreach (var skin in database.skins)
            if (skin != null && skin.skinId > 0 && skin.baseWeapon != null && skin.visualModelPrefab != null)
                entries.Add(($"weapon_{skin.baseWeapon.weaponId}_{skin.skinId}", skin.visualModelPrefab));
        string[] characters = { "Casual1", "PicoChan", "unitychan", "Controller" };
        for (int i = 0; i < characters.Length; ++i) entries.Add(("character_" + i, Resources.Load<GameObject>("LobbyCharacter/" + characters[i])));
        const int size = 256, columns = 8;
        var atlas = new Texture2D(columns * size, Mathf.CeilToInt(entries.Count / (float)columns) * size, TextureFormat.RGBA32, false);
        var metadata = new SpriteMetaData[entries.Count];
        try
        {
            for (int i = 0; i < entries.Count; ++i)
            {
                var utility = new PreviewRenderUtility();
                try
                {
                    utility.BeginStaticPreview(new Rect(0, 0, size, size));
                    var root = new GameObject("ShopThumbnail");
                    root.hideFlags = HideFlags.HideAndDontSave; root.SetActive(false);
                    utility.AddSingleGO(root);
                    var clone = UnityEngine.Object.Instantiate(entries[i].prefab, root.transform, false);
                    foreach (var script in clone.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                    foreach (var camera in clone.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                    foreach (var light in clone.GetComponentsInChildren<Light>(true)) light.enabled = false;
                    foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    clone.SetActive(true); root.SetActive(true);
                    clone.transform.rotation = Quaternion.Euler(0, entries[i].name.StartsWith("weapon") ? 65 : 160, 0);
                    var renderers = clone.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) throw new InvalidOperationException("No renderers: " + entries[i].name);
                    var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    utility.camera.fieldOfView = 35;
                    float distance = Mathf.Max(.1f, bounds.extents.magnitude) / Mathf.Sin(17.5f * Mathf.Deg2Rad) * 1.1f;
                    utility.camera.transform.position = bounds.center + Vector3.back * distance;
                    utility.camera.transform.rotation = Quaternion.identity;
                    utility.camera.nearClipPlane = distance * .01f; utility.camera.farClipPlane = distance * 4;
                    utility.camera.clearFlags = CameraClearFlags.SolidColor; utility.camera.backgroundColor = new Color(.10f, .13f, .18f, 1);
                    utility.lights[0].intensity = 1.5f; utility.lights[0].transform.rotation = Quaternion.Euler(40, 30, 0);
                    utility.lights[1].intensity = 1f; utility.lights[1].transform.rotation = Quaternion.Euler(340, 218, 177);
                    utility.ambientColor = new Color(.55f, .55f, .55f);
                    utility.Render(true);
                    var thumb = utility.EndStaticPreview();
                    int x = i % columns * size, y = i / columns * size;
                    atlas.SetPixels(x, y, size, size, thumb.GetPixels());
                    UnityEngine.Object.DestroyImmediate(thumb);
                    metadata[i] = new SpriteMetaData { name = entries[i].name, rect = new Rect(x, y, size, size), alignment = 0, pivot = new Vector2(.5f, .5f) };
                }
                finally { utility.Cleanup(); }
            }
            atlas.Apply(); File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(atlas); }
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritesheet = metadata; importer.SaveAndReimport();
        Debug.Log("[SHOP] Generated " + entries.Count + " real-model thumbnails in a single atlas.");
    }
    public static void BuildAndVerifyBatch()
    {
        try { Build(); VectorShopVerification.Run(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
