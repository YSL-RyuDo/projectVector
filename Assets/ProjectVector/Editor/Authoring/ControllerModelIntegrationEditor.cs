#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ControllerModelIntegrationEditor
{
    const string GamePrefab = "Assets/ProjectVector/Shared/Characters/Prefabs/Controller/Controller.prefab";
    const string LobbyPrefab = "Assets/ProjectVector/Modules/Lobby/Resources/LobbyCharacter/Controller.prefab";
    const string NewModel = "Assets/ProjectVector/Shared/Characters/Models/Controller/Controller Model_Riging (1).fbx";
    static readonly string[] Scenes = { "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", "Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity", "Assets/ProjectVector/Modules/Results/Scenes/Result.unity", "Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity" };
    static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;
    static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    public static void Audit()
    {
        foreach (var path in new[] { GamePrefab, LobbyPrefab })
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log("MODEL_AUDIT_PREFAB " + path);
            foreach (var a in p.GetComponentsInChildren<Animator>(true))
                Debug.Log($"MODEL_ANIMATOR {Hierarchy(a.transform)} scale={a.transform.lossyScale} controller={AssetDatabase.GetAssetPath(a.runtimeAnimatorController)} avatar={AssetDatabase.GetAssetPath(a.avatar)} clips={string.Join(",",a.runtimeAnimatorController.animationClips.Select(c=>c.name))}");
        }
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            Debug.Log("MODEL_AUDIT_SCENE " + path);
            foreach (var t in All<Transform>(scene).Where(t => t.name.Contains("Controller") || t.name.Contains("UnityChanSSU")))
                if (t.name.Length < 80) Debug.Log($"MODEL_OBJECT {Hierarchy(t)} active={t.gameObject.activeSelf} pos={t.localPosition} scale={t.localScale} source={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}");
            foreach (var p in All<PracticeTargetProfile>(scene)) Debug.Log($"MODEL_TARGET {p.name} model={p.model?.name} animator={p.animator?.name}");
            foreach (var stage in All<ResultCharacterStage>(scene)) Debug.Log($"MODEL_RESULT animator1={(stage.animators[1] == null ? "MISSING" : stage.animators[1].name)} model1={stage.models[1]?.name}");
            Debug.Log("MODEL_MISSING_SCRIPTS " + All<Transform>(scene).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        }
        Debug.Log("MODEL_AUDIT_DONE");
    }
    static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    static Bounds Skeleton(GameObject model)
    {
        var bones = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.bones).Where(b=>b!=null).Distinct().ToArray();
        Require(bones.Length > 0, "No skeleton: " + model.name);
        var bounds = new Bounds(bones[0].position, Vector3.zero);
        foreach (var b in bones) bounds.Encapsulate(b.position);
        return bounds;
    }
    static void VerifyMesh(GameObject model)
    {
        var meshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Require(meshes.Length > 0 && meshes.All(r=>r.sharedMesh != null && AssetDatabase.GetAssetPath(r.sharedMesh) == NewModel), "Old/missing controller mesh: " + model.name);
    }
    // Replace only the explicitly designated 3D model, retaining the authored anchor and all UI.
    static Animator ReplacePresentation(Transform anchor, Vector3 feet, float height, bool result)
    {
        foreach (Transform child in anchor.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        bool active = anchor.gameObject.activeSelf;
        anchor.gameObject.SetActive(true); anchor.localScale = Vector3.one;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefab), anchor);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = result ? Quaternion.Euler(0,180,0) : Quaternion.identity;
        model.transform.localScale = Vector3.one;
        VectorVictorySequenceEditor.Isolate(model);
        var animator = model.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ProjectVector/Modules/Results/UI/ResultSequence/Result_Controller.controller");
        animator.applyRootMotion = false; animator.updateMode = AnimatorUpdateMode.UnscaledTime; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
        anchor.localScale = Vector3.one * (height / Skeleton(model).size.y);
        var bounds = Skeleton(model); anchor.position += feet - new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        animator.Update(0); VerifyMesh(model);
        Require(Mathf.Abs(Skeleton(model).size.y-height)<.025f,"Presentation scale mismatch");
        foreach(var component in model.GetComponentsInChildren<Component>(true))
            if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        anchor.gameObject.SetActive(active);
        return animator;
    }
    static void ReplaceTarget(PracticeTargetProfile target)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefab);
        var sourceModel = source.transform.Find("Model");
        Require(sourceModel != null, "Gameplay model root missing");
        UnityEngine.Object.DestroyImmediate(target.model.gameObject);
        var model = UnityEngine.Object.Instantiate(sourceModel.gameObject, target.transform, false); model.name = "Model";
        foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(b);
        foreach(var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true)) { r.enabled=true; r.gameObject.layer=0; }
        target.model=model.transform; target.animator=model.GetComponent<Animator>();
        target.animator.enabled=true; target.animator.applyRootMotion=false; target.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        target.transform.localScale=source.transform.localScale;
        VerifyMesh(model);
    }
    public static void Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play first");
        for(int i=0;i<SceneManager.sceneCount;i++) Require(!SceneManager.GetSceneAt(i).isDirty,"Unsaved scene: refused");
        foreach(var path in Scenes.Skip(1))
        {
            var scene=EditorSceneManager.OpenScene(path);
            File.Copy(path,Path.Combine(Path.GetTempPath(),"Controller-before-"+scene.name+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".unity"));
            var ui=All<RectTransform>(scene).SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null).Distinct().ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
            if(scene.name=="Game_TestVersion")
            {
                var pick=All<PickManager>(scene).Single(); var so=new SerializedObject(pick);
                var anchor=(GameObject)so.FindProperty("authoredPreviewModels").GetArrayElementAtIndex(1).objectReferenceValue;
                var spawn=(Transform)so.FindProperty("previewSpawnPoint").objectReferenceValue;
                ReplacePresentation(anchor.transform,spawn.position,1.4f,false);
            }
            else if(scene.name=="Result")
            {
                var stage=All<ResultCharacterStage>(scene).Single();
                stage.animators[1]=ReplacePresentation(stage.models[1].transform,stage.transform.position,1.65f,true);
                EditorUtility.SetDirty(stage);
            }
            else
            {
                var target=All<PracticeTargetProfile>(scene).Single(p=>AssetDatabase.GetAssetPath(p.gameplayPrefab)==GamePrefab);
                ReplaceTarget(target);
            }
            foreach(var entry in ui) Require(entry.Key!=null && EditorJsonUtility.ToJson(entry.Key)==entry.Value,"Unrelated UI changed: "+entry.Key);
            EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene),"Scene save failed");
            Debug.Log($"CONTROLLER_MODEL_SAVED {path}; preserved UI components={ui.Count}");
        }
        Verify();
    }
    public static void Verify()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefab);
        VerifyMesh(source.transform.Find("Model").gameObject);
        VerifyMesh(Resources.Load<GameObject>("LobbyCharacter/Controller"));
        foreach(var path in Scenes)
        {
            var scene=EditorSceneManager.OpenScene(path);
            Require(All<Transform>(scene).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"Missing scene scripts: "+path);
            foreach(var stage in All<ResultCharacterStage>(scene))
            {
                Require(stage.animators.All(a=>a!=null),"Missing result animator");
                stage.SelectMap("TestMap"); stage.Select(1,false);
                VerifyMesh(stage.models[1]); Capture(stage.stageCamera,"controller-result-idle");
                stage.Select(1,true); stage.animators[1].Update(.5f); Capture(stage.stageCamera,"controller-result-mvp");
            }
            foreach(var pick in All<PickManager>(scene))
            {
                var so=new SerializedObject(pick); var models=so.FindProperty("authoredPreviewModels");
                if(models==null || models.arraySize<2) continue;
                var anchor=(GameObject)models.GetArrayElementAtIndex(1).objectReferenceValue;
                VerifyMesh(anchor); anchor.SetActive(true);
                var animator=anchor.GetComponentInChildren<Animator>(true); animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
                var camera=scene.GetRootGameObjects().Single(r=>r.name=="Main Camera").GetComponent<Camera>();
                Capture(camera,"controller-pick-model");
            }
            foreach(var target in All<PracticeTargetProfile>(scene).Where(t=>t.gameplayPrefab==source))
            {
                VerifyMesh(target.model.gameObject);
                Require(target.model.localScale==source.transform.Find("Model").localScale && target.transform.localScale==source.transform.localScale,"Target scale differs from gameplay");
                Require(target.GetComponentsInChildren<PracticeTargetZone>(true).Length==3,"Target hit zones changed");
            }
            Debug.Log("CONTROLLER_MODEL_VERIFIED "+path);
        }
        Debug.Log("CONTROLLER_MODEL_INTEGRATION_PASS");
    }
    public static void VerifyAndBuild()
    {
        PracticeUpgradeEditor.Verify();
        PracticeRangeEditor.VerifyCharacterSizes();
        VectorShopThumbnailBuilder.UpdateControllerThumbnail();
        VectorShopVerification.Run();
        PracticeCampusEditor.BuildPlayer();
    }
    public static void AuditADS()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");
        var range=All<PracticeRangeController>(scene).Single();
        foreach(var actor in range.loadout.players)
        {
            var asset=PrefabUtility.GetCorrespondingObjectFromSource(actor.gameObject);
            foreach(var root in new[]{asset,actor.gameObject})
            foreach(var arm in root.GetComponentsInChildren<ArmADS>(true))
            {
                var referenced=(Animator)new SerializedObject(arm).FindProperty("armAnimator").objectReferenceValue;
                Debug.Log($"ADS_ARM_COMPONENT {root.name} enabled={arm.enabled} path={Hierarchy(arm.transform)} animator={AssetDatabase.GetAssetPath(arm.GetComponent<Animator>().runtimeAnimatorController)} referenced={(referenced==null?"MISSING":Hierarchy(referenced.transform))} same={referenced==arm.GetComponent<Animator>()}");
            }
        }
        foreach(var w in range.loadout.weapons.Where(w=>w.weaponType!=WeaponData.WeaponType.Knife && w.weaponType!=WeaponData.WeaponType.Pistol && w.weaponType!=WeaponData.WeaponType.Shotgun))
        {
            Debug.Log("ADS_WEAPON "+w.name);
            foreach(var r in w.weaponPrefab.GetComponentsInChildren<MeshRenderer>(true))
                Debug.Log($"ADS_MESH {Hierarchy(r.transform)} mesh={AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)} pos={r.transform.position} mats={string.Join(",",r.sharedMaterials.Select(m=>m.name))}");
        }
    }
    public static void Capture(Camera camera,string name)
    {
        var rt=new RenderTexture(1280,720,24); var old=RenderTexture.active; var previous=camera.targetTexture;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            var dir=Path.Combine(Path.GetTempPath(),"VectorControllerIntegration"); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir,name+".png"),texture.EncodeToPNG());
        }
        finally { camera.targetTexture=previous; RenderTexture.active=old; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); }
    }
}
#endif
