using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class MyPageBirthdayEditor
{
    static TMP_FontAsset font;
    static readonly Color Ink = new Color32(39, 41, 45, 255), Red = new Color32(177, 28, 61, 255);
    static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
    static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
    {
        var r = p.Find(n) as RectTransform;
        if (r == null) { r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p, false); }
        r.gameObject.layer = 5; r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one; return r;
    }
    static Image Box(Transform p, string n, float x, float y, float w, float h, Color color)
    {
        var i = Ensure<Image>(Rect(p, n, x, y, w, h).gameObject); i.color = color; i.raycastTarget = false; return i;
    }
    static TMP_Text Label(Transform p, string n, string value, float x, float y, float w, float h, float size, Color color)
    {
        var t = Ensure<TextMeshProUGUI>(Rect(p, n, x, y, w, h).gameObject);
        t.font = font; t.fontSize = size; t.color = color; t.text = value; t.raycastTarget = false;
        t.alignment = TextAlignmentOptions.MidlineLeft; t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
    }
    static Button Button(Transform p, string n, string label, float x, float y, float w, float h, Color color)
    {
        var image = Box(p, n, x, y, w, h, color); image.raycastTarget = true;
        var button = Ensure<Button>(image.gameObject); button.targetGraphic = image;
        Label(button.transform, "Label", label, 0, 0, w - 12, h, 24, Color.white).alignment = TextAlignmentOptions.Center;
        return button;
    }
    static TMP_InputField Input(Transform p, string n, string suffix, float x)
    {
        var image = Box(p, n, x, 30, 200, 70, Color.white); image.raycastTarget = true;
        var input = Ensure<TMP_InputField>(image.gameObject); input.targetGraphic = image;
        var area = Rect(input.transform, "Text Area", -20, 0, 128, 64); Ensure<RectMask2D>(area.gameObject);
        var text = Label(area, "Text", "", 0, 0, 120, 60, 34, Ink);
        var placeholder = Label(area, "Placeholder", n == "MonthInput" ? "월" : "일", 0, 0, 120, 60, 30, Color.gray);
        text.alignment = placeholder.alignment = TextAlignmentOptions.Center;
        input.textViewport = area; input.textComponent = (TextMeshProUGUI)text; input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterValidation = TMP_InputField.CharacterValidation.Digit;
        input.characterLimit = 2; input.lineType = TMP_InputField.LineType.SingleLine;
        input.customCaretColor = true; input.caretColor = Ink; input.selectionColor = new Color(0.69f, .11f, .24f, .25f);
        Label(input.transform, "Unit", suffix, 66, 0, 40, 64, 24, Ink);
        Box(input.transform, "Underline", 0, -34, 200, 2, Red);
        return input;
    }
    static Transform Find(Transform p, string n) => p.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
    static MyPagePanelController Profile(UnityEngine.SceneManagement.Scene scene) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MyPagePanelController>(true)).Single();
    static void Require(bool condition, string message) { if (!condition) throw new Exception("Birthday verification: " + message); }
    static void Set(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);

    [MenuItem("Project Vector/UI/Install Authored Birthday Settings")]
    public static void InstallAndVerify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var preserved = new VectorExperiencePolishEditor.Preservation(scene);
        var profile = Profile(scene);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var view = Ensure<MyPageBirthdayController>(profile.gameObject);
        view.birthdayText = Find(profile.transform, "BirthdayText").GetComponent<TMP_Text>();
        view.entryButton = Ensure<Button>(view.birthdayText.gameObject); view.entryButton.targetGraphic = view.birthdayText;
        view.entryButton.transition = Selectable.Transition.None;
        var overlay = Box(profile.transform, "BirthdayPopup", 0, 0, 1920, 1080, new Color(0, 0, 0, .64f));
        overlay.raycastTarget = true; overlay.transform.SetAsLastSibling(); view.popup = overlay.gameObject;
        var panel = Box(overlay.transform, "BirthdayDialog", 0, 0, 720, 440, new Color32(220, 223, 221, 255)).transform;
        Box(panel, "Accent", -357, 165, 6, 76, Red);
        Label(panel, "Title", "생일 설정", -22, 165, 590, 62, 36, Ink);
        Label(panel, "Description", "생일의 월과 일을 입력해 주세요. 연도는 저장하지 않습니다.", 0, 102, 630, 52, 21, Ink);
        view.monthInput = Input(panel, "MonthInput", "월", -116);
        view.dayInput = Input(panel, "DayInput", "일", 116);
        view.message = Label(panel, "Status", "올바른 월과 일을 입력해 주세요. (2월 29일 가능)", 0, -53, 630, 70, 20, Ink);
        view.cancelButton = Button(panel, "Cancel", "취소", -163, -153, 300, 58, Ink);
        view.saveButton = Button(panel, "Save", "저장", 163, -153, 300, 58, Red);
        view.saveLabel = view.saveButton.GetComponentInChildren<TMP_Text>(true);
        view.closeButton = Button(panel, "Close", "×", 312, 171, 44, 44, Ink);
        view.popup.SetActive(false); view.birthdayText.raycastTarget = true;
        view.birthdayText.text = "생일: 미설정  [설정]";
        preserved.Check();
        EditorUtility.SetDirty(view); EditorUtility.SetDirty(view.birthdayText); EditorUtility.SetDirty(view.entryButton);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Verify();
    }
    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity"); var profile = Profile(scene);
        var view = profile.GetComponent<MyPageBirthdayController>(); Require(view != null, "authored component missing");
        var group = profile.GetComponent<CanvasGroup>(); group.alpha = 1; group.interactable = group.blocksRaycasts = true; profile.gameObject.SetActive(true);
        foreach (string n in new[] { "BattleCustomPanel", "SkinCustomPanel", "RecentVisitorsPopup" }) Find(profile.transform, n).gameObject.SetActive(false);
        profile.GetComponent<MyPageTabController>().Select(MyPageTabController.Page.Profile);
        int count = profile.GetComponentsInChildren<Transform>(true).Length;
        int components = profile.GetComponentsInChildren<Component>(true).Length;
        var data = new MyPageProfileData { isSelf = true, userId = "birthday-fixture", nickname = "VECTOR", level = 23, birthday = "----", region = "대한민국", joinDate = "2026-09-15", currentTierName = "Gold 3", currentRankImageId = 2, seasonHighestRankImageId = 2, allTimeHighestRankImageId = 3 };
        profile.ApplyProfile(data); view.entryButton.onClick.Invoke();
        Require(view.IsOpen && !view.saveButton.interactable, "unset birthday entry");
        foreach (var date in new[] { new[] {0,1}, new[] {13,1}, new[] {1,0}, new[] {2,30}, new[] {4,31} })
        {
            view.monthInput.text = date[0].ToString(); view.dayInput.text = date[1].ToString();
            Require(!view.saveButton.interactable, "invalid date accepted");
        }
        for (int month = 1; month <= 12; ++month)
        {
            Require(MyPageBirthdayController.IsValidDate(month, DateTime.DaysInMonth(2000, month)), "month end rejected");
            Require(!MyPageBirthdayController.IsValidDate(month, DateTime.DaysInMonth(2000, month) + 1), "month overflow accepted");
        }
        view.monthInput.text = "02"; view.dayInput.text = "29";
        Require(view.saveButton.interactable, "leap birthday");
        var packet = NetworkManager.BirthdayPacket(77, 2, 29);
        Require(packet.Length == 10 && BitConverter.ToUInt16(packet, 2) == 363 && BitConverter.ToUInt32(packet, 4) == 77 && packet[8] == 2 && packet[9] == 29, "wire contract");
        view.Save(); Require(!view.IsSaving && data.birthday == "----", "offline save must not pretend success");
        view.monthInput.text = "10"; view.dayInput.text = "04";
        VectorExperiencePolishEditor.CaptureCanvas(profile.GetComponentInParent<Canvas>(), "VectorBirthdayPopup.png", profile.gameObject, verify: VerifyPointers);
        // Capture temporarily disables the source. Rebind before testing simulated acknowledgements.
        view.ApplyProfile(data); view.Open();
        Set(view, "pending", (uint)77); Set(view, "pendingUser", data.userId);
        view.ApplyResult(76, 0, 10, 4); Require(data.birthday == "----" && view.IsSaving, "stale response");
        view.ApplyResult(77, 3, 0, 0); Require(data.birthday == "----" && !view.IsSaving && view.IsOpen, "disk failure must not pretend success");
        Set(view, "pending", (uint)78); Set(view, "pendingUser", data.userId);
        view.ApplyResult(78, 0, 2, 29); Require(data.birthday == "02.29" && !view.IsOpen && view.birthdayText.text.Contains("02.29"), "confirmed birthday binding");
        view.Open(); Require(view.monthInput.text == "02" && view.dayInput.text == "29", "edit prefill");
        Require(view.HandleBack() && !view.IsOpen, "escape close");
        data.privacyEnabled = true; profile.ApplyProfile(data); Require(view.birthdayText.text.Contains("비공개") && !view.birthdayText.text.Contains("02.29"), "privacy caption");
        data.isSelf = false; view.ApplyProfile(data); view.Open(); Require(!view.IsOpen && !view.entryButton.interactable, "other profile read-only");
        data.isSelf = true; data.privacyEnabled = false; profile.ApplyProfile(data); view.Open();
        Set(view, "pending", (uint)79); Set(view, "pendingUser", data.userId);
        view.ResetContext(); view.ApplyResult(79, 0, 12, 31); Require(!view.IsOpen && data.birthday == "02.29", "late ack after context switch");
        profile.ApplyProfile(data);
        view.Open(); Set(view, "pending", (uint)80); Set(view, "pendingUser", data.userId); Set(view, "sentAt", -100f);
        typeof(MyPageBirthdayController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
        Require(!view.IsSaving && data.birthday == "02.29", "timeout must not pretend success");
        view.Close();
        VectorExperiencePolishEditor.CaptureCanvas(profile.GetComponentInParent<Canvas>(), "VectorBirthdaySaved.png", profile.gameObject, verify: VerifyPointers);
        Require(count == profile.GetComponentsInChildren<Transform>(true).Length && components == profile.GetComponentsInChildren<Component>(true).Length, "runtime UI creation");
        Debug.Log("BIRTHDAY_UI_VERIFIED: scene-authored popup, monthly date limits/leap day, packet layout, offline/error/stale/confirmed response, edit prefill, privacy/self-only, escape/context reset, no runtime UI construction. Fixture acknowledgements only; not live server test.");
    }
    static void VerifyPointers(Transform clone, Camera camera)
    {
        var view = clone.GetComponent<MyPageBirthdayController>();
        var canvas = view.GetComponentInParent<Canvas>();
        var raycaster = Ensure<GraphicRaycaster>(canvas.gameObject);
        var events = new GameObject("BirthdayDiagnosticEvents", typeof(EventSystem)).GetComponent<EventSystem>();
        try
        {
            var targets = view.IsOpen ? new Selectable[] { view.monthInput, view.dayInput, view.saveButton, view.cancelButton, view.closeButton } : new Selectable[] { view.entryButton };
            foreach (var target in targets)
            {
                var rect = (RectTransform)target.transform;
                var pointer = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
                var hits = new System.Collections.Generic.List<RaycastResult>(); raycaster.Raycast(pointer, hits);
                Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == target.gameObject, "blocked click: " + target.name);
            }
            if (view.IsOpen)
            {
                var rect = (RectTransform)view.entryButton.transform;
                var pointer = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
                var hits = new System.Collections.Generic.List<RaycastResult>(); raycaster.Raycast(pointer, hits);
                Require(hits.Count > 0 && hits[0].gameObject == view.popup, "popup must block underlying profile controls");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(events.gameObject); }
        Debug.Log("BIRTHDAY_POINTERS_VERIFIED: " + (view.IsOpen ? "inputs, save/cancel/close, background click blocking" : "profile birthday entry"));
    }
}
