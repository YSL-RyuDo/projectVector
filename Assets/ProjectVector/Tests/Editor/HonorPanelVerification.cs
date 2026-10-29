#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class HonorPanelVerification
{
    public static void Run()
    {
        Verify(true);
    }

    // Does not run the installer or save the user's Lobby scene.
    public static void RunHover() => Verify(false);

    private static void Verify(bool install)
    {
        if (install) HonorHierarchyInstaller.Install();
        var scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Single);
        var source = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MyPagePanelController>(true)).First();
        var canvasObject = new GameObject("HonorVerificationCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = 5;
        var canvas = canvasObject.GetComponent<Canvas>();
        var camera = new GameObject("HonorVerificationCamera", typeof(Camera)).GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 540;
        camera.aspect = 1920f / 1080f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 5;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
        camera.transform.position = new Vector3(0, 0, -10);
        camera.farClipPlane = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var clone = UnityEngine.Object.Instantiate(source.gameObject, canvas.transform, false);
        foreach (var original in scene.GetRootGameObjects())
            if (original != canvasObject && original != camera.gameObject) original.SetActive(false);
        clone.SetActive(true);
        var visibility = clone.GetComponent<CanvasGroup>();
        if (visibility != null) { visibility.alpha = 1; visibility.interactable = true; visibility.blocksRaycasts = true; }
        foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != null && !(behaviour is Graphic) && !(behaviour is LayoutGroup) &&
                !(behaviour is ContentSizeFitter) && !(behaviour is RectMask2D) && !(behaviour is Mask) &&
                !(behaviour is HonorGradeHover)) behaviour.enabled = false;
        var rootRect = clone.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        var honor = clone.GetComponentInChildren<MyPageHonorController>(true);
        var tabs = clone.GetComponent<MyPageTabController>();
        tabs.Select(MyPageTabController.Page.Honor);
        var eventSystem = new GameObject("HonorVerificationEvents", typeof(EventSystem)).GetComponent<EventSystem>();
        var raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        var input = eventSystem.gameObject.AddComponent<HonorVerificationPointerModule>();
        var hoverPointer = new PointerEventData(eventSystem);
        // Exercise existing null scene references through the actual raycast/event path.
        foreach (var hover in honor.GetComponentsInChildren<HonorGradeHover>(true))
            hover.Configure(int.Parse(hover.name.Substring("HonorGrade".Length)), null);
        Capture(camera, clone, "honor-hover-pending.png");
        MoveToGrade(clone, camera, raycaster, input, hoverPointer, 1);
        Require(Text(clone, "HonorPerk3State").text == "잠김", "Hover while server response is pending");
        input.Move(hoverPointer, null);
        honor.ApplyState(true, 601, 0, 1, 1, Array.Empty<HonorChangeData>());
        Require(Text(clone, "HonorCurrentGradeTitle").text == "3  보통", "Initial grade");
        Require(Text(clone, "HonorVoiceStatus").text == "보통", "Initial voice status");
        Capture(camera, clone, "honor-description.png");
        int[] requiredGrades = { 1, 2, 3, 3, 5, 6, 6 };
        for (int grade = 1; grade <= 6; ++grade)
        {
            MoveToGrade(clone, camera, raycaster, input, hoverPointer, grade);
            for (int perk = 0; perk < requiredGrades.Length; ++perk)
                Require(Text(clone, $"HonorPerk{perk}State").text ==
                    (grade >= requiredGrades[perk] ? "해제" : "잠김"), $"Grade {grade} perk {perk}");
            for (int column = 1; column <= 6; ++column)
            {
                Require(Find<Transform>(clone, $"HonorGradeHighlight{column}").gameObject.activeSelf ==
                    (column == grade), $"Grade {grade} highlight {column}");
                Require(Find<Transform>(clone, $"HonorGradeCurrent{column}").gameObject.activeSelf ==
                    (column == 3), "Actual grade marker must not follow preview");
            }
            Require(Text(clone, "HonorCurrentGradeTitle").text == "3  보통", "Hover must not change own grade");
            Capture(camera, clone, $"honor-hover-{grade}.png");
        }
        Capture(camera, clone, "honor-hover.png");
        input.Move(hoverPointer, null);
        Require(Text(clone, "HonorViewedGrade").text == "3  보통", "Hover leave restores own grade");
        MoveToGrade(clone, camera, raycaster, input, hoverPointer, 2);
        honor.ApplyState(true, 1601, 0, 1, 1, Array.Empty<HonorChangeData>());
        Require(Text(clone, "HonorViewedGrade").text == "2  주의 필요", "Response preserves active hover");
        input.Move(hoverPointer, null);
        Require(Text(clone, "HonorViewedGrade").text == "6  탁월", "Leave restores updated own grade");
        if (!install)
        {
            honor.ApplyState(true, 551, 0, 1, 1, new[]
            {
                new HonorChangeData(1791590400L, -50, 8),
                new HonorChangeData(1791589800L, 0, 1),
                new HonorChangeData(1791589200L, -50, 7)
            });
            Button(clone, "HonorSectionButton2").onClick.Invoke();
            Require(Text(clone, "HonorHistoryReason0").text == "장시간 미참여", "Inactivity reason");
            Require(Text(clone, "HonorHistoryDelta0").text == "-50", "Inactivity penalty");
            Require(Text(clone, "HonorHistoryDelta1").text == "0", "Capped completion history");
            Require(Text(clone, "HonorHistoryReason2").text == "입장 확인 시간 초과", "Confirmation timeout reason");
            Capture(camera, clone, "honor-activity-history.png");
            Debug.Log("[HONOR ACTIVITY UI VERIFY] PASS: reasons 1/7/8 and actual deltas in existing saved history rows. Scene not saved.");
            Debug.Log("[HONOR HOVER VERIFY] PASS: six grades via GraphicRaycaster and pointer enter/exit, null binding recovery, all perk states, own-grade marker, pending response and hover restore. Lobby scene not saved.");
            return;
        }
        Button(clone, "HonorSectionButton1").onClick.Invoke();
        Require(!Button(clone, "HonorClaim5").interactable, "Locked claim");
        honor.ApplyState(true, 1601, 0, 1, 5, Enumerable.Range(0, 10)
            .Select(i => new HonorChangeData(1774915200L - i * 3600, i == 4 ? -100 : 20, (byte)(i == 4 ? 2 : 1))).ToArray());
        Require(Button(clone, "HonorClaim6").interactable, "Unlocked claim");
        Capture(camera, clone, "honor-rewards.png");
        honor.ApplyClaim(true, 6, 2500, 5);
        var popup = clone.GetComponentInChildren<RewardReceiptPopup>(true);
        Require(popup != null && popup.IsOpen, "Receipt popup");
        Capture(camera, clone, "honor-receipt.png");
        popup.Close();
        Require(!popup.IsOpen && honor.gameObject.activeSelf, "Receipt return");
        Button(clone, "HonorSectionButton2").onClick.Invoke();
        Require(Text(clone, "HonorHistoryPage").text == "1 / 5", "Five-page history");
        Require(Text(clone, "HonorHistoryDelta4").text == "-100", "Negative history");
        Capture(camera, clone, "honor-history.png");
        Button(clone, "HonorSectionButton3").onClick.Invoke();
        Capture(camera, clone, "honor-guide.png");
        var scroll = honor.GetComponentInChildren<HonorGuideScrollRect>(true);
        scroll.enabled = true;
        Canvas.ForceUpdateCanvases();
        Vector2 start = RectTransformUtility.WorldToScreenPoint(camera, scroll.viewport.position);
        var pointer = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left, position = start,
            pointerPressRaycast = new RaycastResult { module = raycaster }
        };
        Vector2 origin = scroll.content.anchoredPosition;
        scroll.OnBeginDrag(pointer);
        pointer.position = start + Vector2.up * 150;
        scroll.OnDrag(pointer); scroll.OnEndDrag(pointer);
        Require(scroll.content.anchoredPosition == origin, "Left content drag blocked");
        pointer.button = PointerEventData.InputButton.Right; pointer.position = start;
        scroll.OnBeginDrag(pointer);
        pointer.position = start + Vector2.up * 150;
        scroll.OnDrag(pointer); scroll.OnEndDrag(pointer);
        Require(scroll.content.anchoredPosition.y > origin.y + 10, "Right content drag");
        Capture(camera, clone, "honor-guide-scrolled.png");
        foreach (Transform item in honor.GetComponentsInChildren<Transform>(true))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) == 0, "Missing script: " + item.name);
        Debug.Log("[HONOR VERIFY] PASS: serialized hierarchy, default Normal, grade hover/restore, locked/unlocked rewards, receipt/return, history and font meshes.");
    }

    private static T Find<T>(GameObject root, string name) where T : Component =>
        root.GetComponentsInChildren<T>(true).First(c => c.name == name);
    private static TMP_Text Text(GameObject root, string name) => Find<TMP_Text>(root, name);
    private static Button Button(GameObject root, string name) => Find<Button>(root, name);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Honor verification failed: " + message); }

    private static void MoveToGrade(GameObject root, Camera camera, GraphicRaycaster raycaster,
        HonorVerificationPointerModule input, PointerEventData pointer, int grade)
    {
        Canvas.ForceUpdateCanvases();
        var rect = Find<RectTransform>(root, $"HonorGradeBadge{grade}");
        pointer.position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var hits = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(pointer, hits);
        if (hits.Count == 0)
        {
            var column = Find<Image>(root, $"HonorGrade{grade}");
            Debug.Log($"[HONOR RAYCAST] screen={Screen.width}x{Screen.height} camera={camera.pixelRect} pointer={pointer.position} active={column.gameObject.activeInHierarchy} target={column.raycastTarget} cull={column.canvasRenderer.cull} depth={column.depth} canvas={column.canvas} valid={column.Raycast(pointer.position, camera)}");
        }
        Require(hits.Count > 0, $"Grade {grade} is reachable by GraphicRaycaster");
        GameObject hit = hits[0].gameObject;
        Require(hit.GetComponentInParent<HonorGradeHover>() != null &&
            hit.GetComponentInParent<HonorGradeHover>().name == $"HonorGrade{grade}",
            $"Grade {grade} raycast is not blocked by another graphic: {hit.name}");
        input.Move(pointer, hit);
    }

    private static void Capture(Camera camera, GameObject root, string file)
    {
        var previousPipeline = GraphicsSettings.defaultRenderPipeline;
        var previousQuality = QualitySettings.renderPipeline;
        var texture = new RenderTexture(1920, 1080, 24);
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        try
        {
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            camera.targetTexture = texture;
            Canvas.ForceUpdateCanvases();
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>())
            {
                graphic.canvasRenderer.SetColor(Color.white);
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>())
            {
                Require(label.font != null && label.font.atlasTexture != null, "Font atlas: " + label.name);
                label.ForceMeshUpdate();
            }
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[HONOR CAPTURE] {file} root={root.activeInHierarchy} rect={root.GetComponent<RectTransform>().rect} graphics={root.GetComponentsInChildren<Graphic>().Length} alpha={root.GetComponent<CanvasGroup>()?.alpha}");
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            string directory = Path.Combine(Path.GetTempPath(), "VectorHonorActivityVerification");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, file), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousTarget;
            GraphicsSettings.defaultRenderPipeline = previousPipeline;
            QualitySettings.renderPipeline = previousQuality;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}

public sealed class HonorVerificationPointerModule : BaseInputModule
{
    public override void Process() { }
    public void Move(PointerEventData pointer, GameObject target) => HandlePointerExitAndEnter(pointer, target);
}
#endif
