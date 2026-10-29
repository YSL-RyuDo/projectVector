using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VectorSocialPresentationEditor
{
    public static void InstallAndVerify() { Install(); VerifySaved(); }
    static TMP_FontAsset font;
    static readonly Color dark = new Color32(34, 35, 39, 255);
    static readonly Color red = new Color32(203, 43, 58, 255);
    static readonly Color cyan = new Color32(228, 230, 233, 255);
    static T Ref<T>(UnityEngine.Object obj, string field) where T : UnityEngine.Object => (T)new SerializedObject(obj).FindProperty(field).objectReferenceValue;
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var old = parent.Find(name);
        var rect = old != null ? (RectTransform)old : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
    static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    static TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size, Color color)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false; label.enableWordWrapping = true; return label;
    }
    static Button Button(Transform parent, string name, string text, float x, float y, float w, float h, Color color)
    {
        var image = Box(parent, name, x, y, w, h, color); image.raycastTarget = true;
        var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label(image.transform, "Label", text, 0, 0, w - 20, h, 24, Color.white);
        var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(.7f, .7f, .7f); colors.disabledColor = new Color(.55f, .55f, .55f, .65f); button.colors = colors;
        Box(image.transform, "Accent", -w / 2 + 2, 0, 3, h, cyan);
        return button;
    }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static AuthoredPanelMotion Motion(GameObject go, Vector2 offset, RectTransform content = null)
    {
        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
        var motion = go.GetComponent<AuthoredPanelMotion>() ?? go.AddComponent<AuthoredPanelMotion>();
        motion.entryOffset = offset; motion.content = content != null ? content : go.transform as RectTransform;
        return motion;
    }
    static GameObject Modal(Transform parent, string name, string title, out Transform card)
    {
        var shade = Box(parent, name, 0, 0, 1920, 1080, new Color32(12, 12, 14, 205)); Stretch(shade.rectTransform); shade.raycastTarget = true;
        card = Box(shade.transform, "Card", 0, 0, 820, 370, new Color32(201, 205, 210, 255)).transform;
        Box(card, "Header", 0, 140, 820, 90, dark);
        Box(card, "HeaderAccent", -407, 140, 6, 90, red);
        Label(card, "Title", title, 0, 140, 760, 80, 32, Color.white);
        Motion(shade.gameObject, new Vector2(0, -22), (RectTransform)card);
        return shade.gameObject;
    }
    static Sprite Sprite(string name)
    {
        string path = "Assets/UI/VideoReference/" + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Project Vector/UI/Install Video Social Presentation")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before installing");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene; installation refused");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var originalObjects = all.Select(t => t.gameObject).ToArray();
        var originalRefs = new List<(UnityEngine.Object owner, string field, UnityEngine.Object target)>();
        foreach (var behaviour in all.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(b => b != null))
        {
            var property = new SerializedObject(behaviour).GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                    originalRefs.Add((behaviour, property.propertyPath, property.objectReferenceValue));
        }
        File.Copy("Assets/Scenes/Lobby.unity", Path.Combine(Path.GetTempPath(), "Vector-Lobby-before-social-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".unity"));
        var game = all.Select(t => t.GetComponent<GamePanelController>()).First(c => c != null);
        var friends = all.Select(t => t.GetComponent<FriendPanelController>()).First(c => c != null);
        var ready = Ref<GameObject>(game, "tempGameRoomPanel");
        var canvas = ready.GetComponentInParent<Canvas>().rootCanvas.transform;
        font = Ref<Button>(game, "normalGameButton").GetComponentInChildren<TMP_Text>(true).font;
        var host = Rect(canvas, "SocialPresentation", 0, 0, 1920, 1080); Stretch(host);
        var ui = host.GetComponent<LobbySocialController>() ?? host.gameObject.AddComponent<LobbySocialController>(); ui.friends = friends; ui.game = game;
        ui.invitePanel = Modal(host, "PartyInvitePopup", "파티 초대", out var invite);
        ui.inviteText = Label(invite, "Message", "친구가 파티에 초대했습니다.\n함께 작전에 참여하세요.", 0, 25, 740, 90, 28, dark);
        ui.inviteTimer = Label(invite, "Timer", "30초 후 초대 만료", 0, -44, 700, 36, 21, dark);
        ui.acceptInvite = Button(invite, "Accept", "초대 수락", 170, -122, 280, 58, red);
        ui.declineInvite = Button(invite, "Decline", "거절", -170, -122, 280, 58, dark);
        ui.confirmationPanel = Modal(host, "SocialConfirmationPopup", "확인", out var confirm);
        ui.confirmationText = Label(confirm, "Message", "계속 진행하시겠습니까?", 0, 12, 730, 130, 28, dark);
        ui.confirmButton = Button(confirm, "Confirm", "확인", 170, -122, 280, 58, red);
        ui.cancelButton = Button(confirm, "Cancel", "취소", -170, -122, 280, 58, dark);
        var toast = Box(host, "SocialToast", 0, 420, 980, 84, dark); toast.rectTransform.anchorMin = toast.rectTransform.anchorMax = new Vector2(.5f, .5f);
        ui.toastPanel = toast.gameObject; ui.toastText = Label(toast.transform, "Message", "초대를 보냈습니다.", 0, 0, 930, 76, 25, Color.white);
        Box(toast.transform, "Accent", -488, 0, 4, 84, cyan); Motion(toast.gameObject, new Vector2(0, 12));

        var match = Box(host, "MatchReadyPopup", 0, 0, 1920, 1080, new Color32(25, 25, 27, 255)); Stretch(match.rectTransform); match.raycastTarget = true; ui.readyPanel = match.gameObject;
        var glow = Box(match.transform, "Horizon", 0, 400, 1920, 250, new Color32(86, 88, 91, 255));
        var fade = glow.GetComponent<VectorUIFade>() ?? glow.gameObject.AddComponent<VectorUIFade>(); fade.top = Color.white; fade.bottom = new Color(1, 1, 1, 0);
        Label(match.transform, "Title", "매칭 성공", 0, 362, 950, 75, 48, Color.white);
        Label(match.transform, "Subtitle", "작전에 참여할 준비가 되셨나요?", 0, 284, 950, 50, 24, new Color32(166, 202, 214, 255));
        var texture = Sprite("ReadyCard"); var foot = Sprite("CardFoot");
        ui.readyCards = new Image[6]; ui.readyCardLabels = new TMP_Text[6];
        for (int i = 0; i < 6; ++i)
        {
            float x = -550 + i * 220;
            var frame = Box(match.transform, "Player" + i, x, 25, 180, 370, new Color32(107, 128, 141, 255)); frame.sprite = texture; ui.readyCards[i] = frame;
            Box(frame.transform, "TopLine", 0, 183, 176, 3, cyan);
            Label(frame.transform, "Index", (i + 1).ToString("00"), 0, 104, 150, 60, 37, Color.white);
            ui.readyCardLabels[i] = Label(frame.transform, "State", "대기 중", 0, -116, 160, 48, 25, Color.white);
            Motion(frame.gameObject, new Vector2(0, -18)).duration = .2f + i * .035f;
        }
        ui.readyProgress = Label(match.transform, "Progress", "0 / 6 준비 완료", 0, -232, 700, 48, 28, cyan);
        ui.readyTimer = Label(match.transform, "Timer", "30초 내에 확인해 주세요", 0, -301, 1000, 60, 32, Color.white);
        ui.acceptReady = Button(match.transform, "Accept", "Space   준비 확인", 0, -405, 440, 76, red); ui.readyButtonText = ui.acceptReady.GetComponentInChildren<TMP_Text>();
        ui.declineReady = Button(match.transform, "Decline", "참여 취소", 0, -488, 230, 42, dark);
        Motion(match.gameObject, Vector2.zero).initialScale = 1;
        ui.inviteSlots = new Button[2];
        for (int i = 0; i < 2; ++i)
        {
            var dock = ready.transform.Find("TeamPosition" + (i + 1));
            var waiting = dock.Find("Waiting");
            if (waiting != null) waiting.gameObject.SetActive(false);
            ui.inviteSlots[i] = Button(dock, "InviteFriend", "+  친구 초대", 0, 0, 298, 56, dark);
        }
        ui.partyText = Label(ready.transform, "PartyStatus", "친구와 함께 작전을 시작하세요", -625, 240, 590, 60, 24, dark);
        ui.partyText.alignment = TextAlignmentOptions.Left;
        ui.leaveParty = Button(ready.transform, "LeaveParty", "파티 나가기", -420, -468, 220, 48, dark); ui.leaveParty.gameObject.SetActive(false);
        foreach (var slot in ready.GetComponentsInChildren<TempGamePlayerSlotUI>(true))
        {
            Motion(slot.gameObject, new Vector2(0, -22));
            var edge = Box(slot.transform, "VideoCardFoot", 0, -289, 290, 22, Color.white); edge.sprite = foot;
        }
        var slots = new SerializedObject(game).FindProperty("tempGamePlayerSlots");
        for (int i = 0; i < slots.arraySize; ++i)
        {
            var slot = (TempGamePlayerSlotUI)slots.GetArrayElementAtIndex(i).objectReferenceValue;
            var leader = Find(slot.transform, "LeaderImage");
            if (leader != null) leader.gameObject.SetActive(i == 0);
        }
        var profile = all.Select(t => t.GetComponent<MyPagePanelController>()).First(c => c != null);
        Motion(profile.gameObject, new Vector2(0, -12)).initialScale = 1;
        foreach (string name in new[] {"BattleCustomPanel", "SkinCustomPanel", "RecentVisitorsPopup"})
        {
            var panel = Find(profile.transform, name);
            if (panel != null) Motion(panel.gameObject, new Vector2(34, 0)).initialScale = 1;
        }
        EditFriendPrefab();
        ui.invitePanel.SetActive(false); ui.readyPanel.SetActive(false); ui.confirmationPanel.SetActive(false); ui.toastPanel.SetActive(false);
        foreach (var obj in originalObjects) if (obj == null) throw new Exception("Existing scene object was removed");
        foreach (var reference in originalRefs)
            if (new SerializedObject(reference.owner).FindProperty(reference.field)?.objectReferenceValue != reference.target)
                throw new Exception("Existing serialized reference changed: " + reference.owner.name + "." + reference.field);
        foreach (var component in host.GetComponentsInChildren<Component>(true)) EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
        Debug.Log($"SOCIAL_SAVED: preserved {originalObjects.Length} objects and {originalRefs.Count} serialized references; 6 ready cards, 2 invite slots.");
        Capture();
    }

    public static void VerifySaved()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var ui = all.Select(t => t.GetComponent<LobbySocialController>()).Single(c => c != null);
        if (ui.readyCards.Length != 6 || ui.inviteSlots.Length != 2 || ui.friends == null) throw new Exception("Saved social references missing");
        int before = all.Length;
        typeof(LobbySocialController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(ui, null);
        ui.ReceiveInvite(1, "테스트 <친구>", 30);
        if (!ui.invitePanel.activeSelf || ui.inviteText.richText) throw new Exception("Invite rendering/escaping failed");
        ui.declineInvite.onClick.Invoke();
        if (ui.invitePanel.activeSelf) throw new Exception("Invite decline failed");
        ui.ReceiveReady(10, 0, 30, 6, 2, false);
        if (!ui.readyPanel.activeSelf || ui.readyProgress.text != "2 / 6 준비 완료") throw new Exception("Ready view failed");
        ui.acceptReady.onClick.Invoke();
        if (ui.acceptReady.interactable) throw new Exception("Duplicate ready click not guarded");
        ui.ReceiveReady(10, 0, 29, 6, 3, true);
        if (ui.readyButtonText.text != "확인 완료 · 팀원 대기") throw new Exception("Ready acknowledgement failed");
        ui.ReceiveReady(10, 2, 0, 6, 3, true);
        if (ui.readyPanel.activeSelf || !ui.toastPanel.activeSelf) throw new Exception("Ready cancellation failed");
        ui.ReceiveReady(11, 0, 30, 6, 0, false);
        if (!ui.declineReady.interactable) throw new Exception("New ready check did not reset controls");
        ui.ReceiveReady(11, 1, 29, 6, 6, true);
        int confirmations = 0;
        ui.Confirm("친구 삭제 확인", () => confirmations++); ui.cancelButton.onClick.Invoke();
        if (confirmations != 0) throw new Exception("Cancel executed destructive callback");
        ui.Confirm("친구 삭제 확인", () => confirmations++); ui.confirmButton.onClick.Invoke(); ui.confirmButton.onClick.Invoke();
        if (confirmations != 1) throw new Exception("Confirmation must be single-use");
        int after = scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Transform>(true).Length);
        if (before != after) throw new Exception("Social interaction created runtime UI objects");
        Debug.Log($"SOCIAL_INTERACTIONS_OK: {before} saved objects unchanged; invite, ready acknowledgement, retry, cancel and single-use confirmation.");
        // Existing match/browser validation, including search, pagination, disabled
        // join/start conditions and three customized card reflections.
        VectorMatchLayoutEditor.VerifyAndCapture();
        Capture();
    }

    static void EditFriendPrefab()
    {
        const string path = "Assets/Resources/Friend/FriendListItem.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var item = root.GetComponent<FriendListItemUI>();
            var menu = Ref<GameObject>(item, "contextMenu");
            var rect = menu.GetComponent<RectTransform>();
            var existing = menu.transform.Find("InviteFriend");
            if (existing == null) rect.sizeDelta += new Vector2(0, 52);
            var button = Button(menu.transform, "InviteFriend", "파티 초대", 0, -rect.rect.height / 2 + 28, rect.rect.width - 16, 46, red);
            var serialized = new SerializedObject(item); serialized.FindProperty("inviteButton").objectReferenceValue = button; serialized.ApplyModifiedPropertiesWithoutUndo();
            VectorLobbyRegressionFixEditor.LayoutFriendMenu(item);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void Capture()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LobbySocialController>(true)).Single();
        var canvas = new GameObject("PreviewCanvas", typeof(Canvas)).GetComponent<Canvas>(); canvas.gameObject.layer = 5;
        canvas.renderMode = RenderMode.WorldSpace; ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
        var camera = new GameObject("PreviewCamera", typeof(Camera)).GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540; camera.aspect = 1920f / 1080; camera.transform.position = new Vector3(0, 0, -10); camera.cullingMask = 1 << 5; camera.farClipPlane = 100; canvas.worldCamera = camera;
        foreach (var root in scene.GetRootGameObjects()) if (root != canvas.gameObject && root != camera.gameObject) root.SetActive(false);
        var ready = UnityEngine.Object.Instantiate(ui.readyPanel, canvas.transform); ready.SetActive(true);
        Render(camera, ready, "VectorSocialReady.png"); ready.SetActive(false);
        var invite = UnityEngine.Object.Instantiate(ui.invitePanel, canvas.transform); invite.SetActive(true); Render(camera, invite, "VectorSocialInvite.png");
        Debug.Log("SOCIAL_PREVIEWS: " + Path.GetTempPath());
        // Preview changes are intentionally not saved.
    }
    static void Render(Camera camera, GameObject root, string filename)
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline; var quality = QualitySettings.renderPipeline; var active = RenderTexture.active;
        var target = new RenderTexture(1920, 1080, 24); var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
            camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) { graphic.canvasRenderer.SetColor(Color.white); graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target; texture.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), filename), texture.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = active; UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = quality; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target); }
    }
}
