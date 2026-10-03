using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VectorStageReadabilityEditor
{
    const string Folder = "Assets/UI/ResultSequence";
    static T Ref<T>(UnityEngine.Object owner, string field) where T : UnityEngine.Object =>
        new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static GameObject Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }
    static void Save(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var obj in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            foreach (var component in obj.GetComponents<Component>())
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Stage save failed");
        AssetDatabase.SaveAssets();
    }
    static void RestoreMapPresentation(GameObject map)
    {
        // Restore source visibility, including intentionally disabled source renderers.
        // Preparation barriers are the only map geometry excluded from presentation.
        int restored = 0;
        foreach (var renderer in map.GetComponentsInChildren<Renderer>(true))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            if (source == null) continue;
            renderer.enabled = source.enabled;
            if (renderer.enabled) restored++;
        }
        var floor = map.transform.Find("PresentationFloor")?.gameObject;
        if (floor != null) floor.SetActive(false);
        var info = map.GetComponent<MapInfo>();
        if (info == null || restored == 0) throw new Exception("Missing original map geometry: " + map.name);
        if (info.barriers != null) info.barriers.SetActive(false);
        Debug.Log($"PRESENTATION_MAP_RESTORED: {map.name}, {restored} source renderers, preparation barriers hidden");
    }
    static void Background(Camera camera)
    {
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(.61f,.54f,.48f);
        camera.allowHDR = false;
    }
    static Bounds PosedBounds(GameObject model)
    {
        // Imported generic rigs use different mesh units. Skeleton world positions
        // avoid applying the FBX renderer scale a second time after BakeMesh.
        var vertices = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .SelectMany(r=>r.bones).Where(t=>t!=null).Distinct().Select(t=>t.position).ToArray();
        if(vertices.Length == 0) throw new Exception("Missing posed mesh");
        var b=new Bounds(vertices[0],Vector3.zero); foreach(var p in vertices) b.Encapsulate(p); return b;
    }
    [MenuItem("Project Vector/UI/Fix Pick and Result Readability")]
    public static void InstallAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var backdrop=Ref<PickMapBackdrop>(pick,"mapBackdrop");
        var spawn=Ref<Transform>(pick,"previewSpawnPoint");
        foreach (var map in backdrop.maps) RestoreMapPresentation(map);
        var camera=scene.GetRootGameObjects().Single(r=>r.name=="Main Camera").GetComponent<Camera>(); Background(camera);
        var properties=new SerializedObject(pick);
        var models=properties.FindProperty("authoredPreviewModels"); models.arraySize=4;
        var root=Child(backdrop.stageRoot.transform,"AuthoredPickCharacters").transform;
        string[] names={"unitychan","UnityChanSSU","Casual1","PicoChan"};
        string[] roles={"Initiator","Controller","Sentinel","Entry"};
        for(int i=0;i<4;i++)
        {
            var anchor=Child(root,"Character_"+i+"_"+roles[i]); anchor.SetActive(true);
            anchor.transform.position=spawn.position; anchor.transform.rotation=spawn.rotation; anchor.transform.localScale=Vector3.one;
            bool existing=anchor.transform.childCount>0;
            var model=existing?anchor.transform.GetChild(0).gameObject:
                (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("LobbyCharacter/"+names[i]),anchor.transform);
            model.transform.localPosition=Vector3.zero; model.transform.localRotation=Quaternion.identity; model.transform.localScale=Vector3.one;
            if(!existing) VectorVictorySequenceEditor.Isolate(model);
            var animator=model.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder+"/Result_"+roles[i]+".controller");
            animator.applyRootMotion=false; animator.updateMode=AnimatorUpdateMode.UnscaledTime; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
            // Explicit per-model feet/centre alignment saved in Hierarchy; no shared runtime transform override.
            var b=PosedBounds(model); anchor.transform.localScale=Vector3.one*(1.4f/b.size.y);
            b=PosedBounds(model); anchor.transform.position+=spawn.position-new Vector3(b.center.x,b.min.y,b.center.z);
            animator.Update(0); b=PosedBounds(model);
            if(Mathf.Abs(b.min.y-spawn.position.y)>.02f || Mathf.Abs(b.center.x-spawn.position.x)>.02f || Mathf.Abs(b.size.y-1.4f)>.03f)
                throw new Exception("Pick model anchoring failed: "+roles[i]);
            Debug.Log($"PICK_ANCHOR_VERIFIED {roles[i]} position={anchor.transform.localPosition} scale={anchor.transform.localScale} bounds={b}");
            models.GetArrayElementAtIndex(i).objectReferenceValue=anchor; anchor.SetActive(false);
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
        var panel=Ref<GameObject>(pick,"characterInfoPanel").transform.parent;
        foreach(var image in panel.GetComponentsInChildren<Image>(true))
            if(image.name=="CharacterInfo" || image.name=="CharacterScroll" || image.name=="MapReadability" || image.name.StartsWith("TeamRow"))
                image.color=new Color32(29,34,43,255);
        preserved.Check(); Save(scene);
        CapturePicks(pick,backdrop,camera);

        scene=EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
        preserved=new VectorExperiencePolishEditor.Preservation(scene);
        var stage=UnityEngine.Object.FindFirstObjectByType<ResultCharacterStage>(FindObjectsInactive.Include);
        foreach (var map in stage.maps) RestoreMapPresentation(map);
        Background(stage.stageCamera);
        // Keep every existing result button and page, strengthen only panel readability.
        var sequence=UnityEngine.Object.FindFirstObjectByType<ResultVictorySequence>(FindObjectsInactive.Include);
        foreach(var page in new[]{sequence.rankPage,sequence.rewardPage})
            foreach(var image in page.GetComponentsInChildren<Image>(true))
                if(image.rectTransform.rect.width>400 && image.rectTransform.rect.height>100 && image.sprite==null)
                    image.color=new Color32(29,34,43,245);
        preserved.Check(); Save(scene);
        VectorVictorySequenceEditor.Verify();
        Debug.Log("PRESENTATION_READABILITY_SAVED: both original map backgrounds restored; only preparation barriers hidden; 4 saved anchored pick models; gameplay prefabs and score table untouched.");
    }
    static void CapturePicks(PickManager pick,PickMapBackdrop backdrop,Camera camera)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var panel=Ref<GameObject>(pick,"characterInfoPanel").transform.parent;
        foreach(var slot in panel.GetComponentsInChildren<TeamPickSlotUI>(true))
            typeof(TeamPickSlotUI).GetMethod("Awake",flags).Invoke(slot,null);
        foreach(var method in new[]{"InitTeamPickSlotsAsWaiting","InitCharacterButtons","InitSkillButtons"})
            typeof(PickManager).GetMethod(method,flags).Invoke(pick,null);
        var canvas=panel.GetComponentInParent<Canvas>();
        foreach(Transform child in canvas.transform) if(child.name!="PlayerPick" && child.name!="InGameUi") child.gameObject.SetActive(false);
        foreach (string mapName in new[]{"TestMap", "Desert"})
        for(int i=0;i<4;i++)
        {
            var state=new PickStateData{roomId=1,mapName=mapName,myTeam=RoomSlotTeam.A};
            state.members.Add(new PickTeamMemberData{playerId=1,isMe=true,nickname="LYS",slotIndex=0,hoverCharacterId=i,lockedCharacterId=-1});
            pick.OnRecvPickTimer(PickPhase.PICKING,20);pick.OnRecvPickState(state);
            var selected = backdrop.maps.Single(m=>m.name==mapName);
            if (!selected.activeInHierarchy || !selected.GetComponentsInChildren<Renderer>().Any(r=>r.enabled))
                throw new Exception("Pick background not visible: " + mapName);
            VectorPickMapEditor.Capture(canvas,camera,"VectorPick-Restored-"+mapName+"-"+i+".png");
        }
    }
    public static void Inspect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var pick = UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var panel = Ref<GameObject>(pick, "characterInfoPanel");
        foreach(var g in panel.GetComponentsInParent<CanvasGroup>(true)) Debug.Log($"STAGE_GROUP {Path(g.transform)} alpha={g.alpha}");
        foreach(var image in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
            if(image.gameObject.activeInHierarchy && image.rectTransform.rect.width > 400)
                Debug.Log($"STAGE_IMAGE {Path(image.transform)} color={image.color} size={image.rectTransform.rect.size}");
        var spawn = Ref<Transform>(pick,"previewSpawnPoint");
        Debug.Log($"STAGE_SPAWN {spawn.position} {spawn.rotation.eulerAngles}");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("LobbyCharacter/Casual1"),spawn);
        var animator=model.GetComponentInChildren<Animator>(true); animator.Rebind();animator.Update(0);
        foreach(var t in model.GetComponentsInChildren<Transform>(true))
            if(t.name.ToLowerInvariant().Contains("head") || t.name.ToLowerInvariant().Contains("foot") || t==model.transform || t==animator.transform)
                Debug.Log($"STAGE_BONE {Path(t)} pos={t.position} rot={t.localEulerAngles} scale={t.localScale}");
        UnityEngine.Object.DestroyImmediate(model);
    }
}
