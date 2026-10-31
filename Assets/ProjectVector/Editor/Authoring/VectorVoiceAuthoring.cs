using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VectorVoiceAuthoring
{
    private static TMP_FontAsset font;
    private static readonly Color PanelColor = new(.075f, .085f, .11f, .98f);
    private static readonly Color ButtonColor = new(.19f, .22f, .27f, 1f);
    [MenuItem("Project Vector/UI/Author Voice Settings")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene; refusing overwrite");
        string[] paths = { "Login", "Lobby", "Game_TestVersion", "RankResult", "Result" };
        foreach (string name in paths)
        {
            var scene = EditorSceneManager.OpenScene(ProjectEditorPaths.Scene(name));
            var roots = scene.GetRootGameObjects();
            foreach (var network in roots.SelectMany(r => r.GetComponentsInChildren<NetworkManager>(true)))
                if (network.GetComponent<VectorVoiceChat>() == null) network.gameObject.AddComponent<VectorVoiceChat>();
            if (name == "Login") { EditorSceneManager.SaveScene(scene); continue; }
            var existingVoice = roots.FirstOrDefault(r => r.name == "VoiceUI");
            font = roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).Select(t => t.font).FirstOrDefault(f => f != null) ?? TMP_Settings.defaultFontAsset;
            if (existingVoice != null) UnityEngine.Object.DestroyImmediate(existingVoice);
            roots = scene.GetRootGameObjects();
            var root = new GameObject("VoiceUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var binding = root.AddComponent<VectorVoicePanel>();
            var open = Button(root.transform, "VoiceSettingsButton", "음성  F10", 0, 0, 146, 34);
            PlaceCorner(open.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-26, 118));
            binding.openButton = open;
            binding.speakingText = Label(root.transform, "SpeakingIndicator", "", 0, 0, 550, 30, 18);
            PlaceCorner(binding.speakingText.rectTransform, new Vector2(0, 0), new Vector2(38, 150));
            if (name == "Game_TestVersion")
            {
                binding.hudRows = new GameObject[3]; binding.hudNames = new TMP_Text[3];
                binding.hudIcons = new Image[3]; binding.hudActivity = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    var hud = Box(root.transform, "TeamVoiceHud" + i, new Color(.06f,.075f,.10f,.8f), 0, 0, 270, 40);
                    PlaceCorner(hud, Vector2.zero, new Vector2(38, 350 - i * 46));
                    hud.GetComponent<Image>().raycastTarget = false;
                    binding.hudRows[i] = hud.gameObject;
                    binding.hudActivity[i] = Box(hud, "Activity", new Color(.27f,.33f,.40f), 0, 0, 3, 40).GetComponent<Image>();
                    binding.hudIcons[i] = Box(hud, "ProfileIcon", Color.white, 8, 5, 30, 30).GetComponent<Image>();
                    binding.hudIcons[i].preserveAspect = true;
                    binding.hudNames[i] = Label(hud, "Nickname", "", 50, 0, 212, 40, 17);
                    binding.hudNames[i].richText = false;
                    foreach (var graphic in hud.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
                    hud.gameObject.SetActive(false);
                }
            }
            if (name == "Lobby")
            {
                var game = roots.SelectMany(r => r.GetComponentsInChildren<GamePanelController>(true)).FirstOrDefault();
                if (game != null) binding.partyView = (GameObject)new SerializedObject(game).FindProperty("tempGameRoomPanel").objectReferenceValue;
                binding.partyButton = Button(root.transform, "PartyVoiceShortcut", "팀 음성  ·  0/3", 0, 0, 200, 44);
                PlaceCorner(binding.partyButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-62, -176));
                binding.partyButton.gameObject.SetActive(false);
            }
            var backdrop = Box(root.transform, "VoiceSettingsModal", PanelColor, 0, 0, 1100, 720);
            binding.panel = backdrop.gameObject;
            AddShield(backdrop);
            Box(backdrop, "TopAccent", new Color(.25f,.76f,.87f), 0, 0, 1100, 3);
            Label(backdrop, "Title", "음성 채팅", 30, 35, 900, 48, 30);
            binding.closeButton = Button(backdrop, "Close", "닫기  ESC", 944, 30, 126, 42);
            binding.statusText = Label(backdrop, "Status", "음성 연결 대기", 32, 106, 810, 30, 18);
            Box(backdrop, "Divider", new Color(.24f,.29f,.34f), 30, 150, 1040, 1);
            Label(backdrop, "MySettingsTitle", "내 음성 설정", 30, 177, 330, 34, 23);
            Label(backdrop, "ModeLabel", "송신 방식", 30, 226, 330, 27, 17);
            binding.modeButton = Button(backdrop, "Mode", "눌러서 말하기  [ ~ ]", 30, 261, 330, 44);
            binding.modeText = binding.modeButton.GetComponentInChildren<TMP_Text>();
            binding.microphoneButton = Button(backdrop, "MicrophoneMute", "마이크 켜짐", 30, 317, 330, 42);
            binding.microphoneText = binding.microphoneButton.GetComponentInChildren<TMP_Text>();
            Label(backdrop, "InputLabel", "마이크", 30, 377, 90, 34, 17);
            binding.inputDeviceButton = Button(backdrop, "InputDeviceNext", "연결 대기", 120, 377, 240, 34);
            binding.inputDeviceText = binding.inputDeviceButton.GetComponentInChildren<TMP_Text>();
            Label(backdrop, "OutputLabel", "스피커", 30, 425, 90, 34, 17);
            binding.outputDeviceButton = Button(backdrop, "OutputDeviceNext", "연결 대기", 120, 425, 240, 34);
            binding.outputDeviceText = binding.outputDeviceButton.GetComponentInChildren<TMP_Text>();
            Label(backdrop, "DeviceHint", "장치 버튼으로 변경", 120, 465, 240, 24, 14).color = new Color(.62f,.68f,.74f);
            Label(backdrop, "InputVolumeLabel", "마이크 음량", 30, 510, 330, 28, 17);
            binding.inputVolume = Slider(backdrop, "InputVolume", 30, 548, 330);
            Label(backdrop, "OutputVolumeLabel", "전체 듣는 음량", 30, 582, 330, 28, 17);
            binding.outputVolume = Slider(backdrop, "OutputVolume", 30, 620, 330);
            Box(backdrop, "ColumnDivider", new Color(.24f,.29f,.34f), 394, 177, 1, 467);
            Label(backdrop, "MembersLabel", "파티 / 팀 참가자", 428, 177, 500, 34, 23);
            binding.participantCount = Label(backdrop, "MemberCount", "0 / 3", 980, 177, 90, 34, 21);
            for (int i = 0; i < 3; ++i)
            {
                var row = Box(backdrop, "ParticipantRow" + i, new Color(.115f,.14f,.18f), 428, 230 + i * 136, 642, 122);
                binding.participantActivity[i] = Box(row, "Activity", new Color(.27f,.33f,.40f), 0, 0, 3, 122).GetComponent<Image>();
                Box(row, "AvatarBackground", new Color(.18f,.22f,.28f), 16, 16, 52, 52);
                binding.participantIcons[i] = Box(row, "ProfileIcon", Color.white, 16, 16, 52, 52).GetComponent<Image>();
                binding.participantIcons[i].preserveAspect = true; binding.participantIcons[i].enabled = false;
                binding.participantTexts[i] = Label(row, "Nickname", "참가자 대기", 84, 13, 365, 30, 21);
                binding.participantTexts[i].richText = false;
                binding.participantStates[i] = Label(row, "State", "같은 파티·팀에 연결되면 표시됩니다", 84, 47, 470, 24, 15);
                var controls = Box(row, "ListeningControls", Color.clear, 0, 0, 642, 122);
                controls.GetComponent<Image>().raycastTarget = false;
                binding.participantControls[i] = controls.gameObject;
                binding.participantMuteButtons[i] = Button(controls, "Mute", "음소거", 488, 14, 136, 36);
                binding.participantVolumes[i] = Slider(controls, "Volume", 84, 85, 270);
                binding.participantVolumes[i].minValue = 0; binding.participantVolumes[i].maxValue = 200; binding.participantVolumes[i].value = 100;
                binding.participantVolumeLabels[i] = Label(controls, "VolumeValue", "100%", 372, 76, 86, 32, 18);
                binding.participantResetButtons[i] = Button(controls, "Reset", "초기화", 488, 76, 136, 32);
                controls.gameObject.SetActive(false);
            }
            Label(backdrop, "MemberHint", "나에게 들리는 소리만 조절합니다.  100% = 기본", 428, 646, 642, 26, 15).color = new Color(.62f,.68f,.74f);
            Label(backdrop, "Footer", "F10  음성 설정    /    설정창을 닫고 ~ 키를 눌러 말하세요", 30, 681, 1040, 25, 15).color = new Color(.62f,.68f,.74f);
            // Existing authored modal roots only. No scene-object discovery in runtime.
            binding.blockingPopups = PopupRoots(roots);
            backdrop.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        VoiceRoomUpgrade.Run();
        Debug.Log("[VoiceAuthoring] Scene-authored voice controls saved. Live service testing still required.");
    }
    private static RectTransform Box(Transform parent, string name, Color color, float x, float y, float w, float h)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = parent.GetComponent<Canvas>() != null ? new Vector2(.5f, .5f) : new Vector2(0, 1);
        rect.pivot = parent.GetComponent<Canvas>() != null ? new Vector2(.5f, .5f) : new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        obj.GetComponent<Image>().color = color; return rect;
    }
    private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        var label = obj.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size;
        label.color = new Color(.92f, .94f, .98f); label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false; label.overflowMode = TextOverflowModes.Ellipsis; return label;
    }
    private static Button Button(Transform parent, string name, string text, float x, float y, float w, float h)
    {
        var rect = Box(parent, name, ButtonColor, x, y, w, h);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        var label = Label(rect, "Label", text, 8, 0, w - 16, h, 19); label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static Slider Slider(Transform parent, string name, float x, float y, float width = 350)
    {
        var rect = Box(parent, name, Color.clear, x, y, width, 24);
        Box(rect, "Track", ButtonColor, 0, 9, width, 6).GetComponent<Image>().raycastTarget = false;
        var fill = Box(rect, "Fill", new Color(.25f,.68f,.77f), 0, 9, width, 6);
        fill.GetComponent<Image>().raycastTarget = false;
        var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = -50; slider.maxValue = 50; slider.wholeNumbers = true;
        var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rect, false); fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(0,9); fillArea.offsetMax = new Vector2(0,-9);
        fill.SetParent(fillArea, false); fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
        slider.fillRect = fill;
        var handle = Box(rect, "Handle", new Color(.7f,.89f,.93f), 0, 0, 12, 24);
        slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        handle.pivot = new Vector2(.5f, .5f); handle.sizeDelta = new Vector2(16, 0); handle.anchoredPosition = Vector2.zero;
        return slider;
    }
    private static void PlaceCorner(RectTransform rect, Vector2 anchor, Vector2 position)
    { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; }
    private static GameObject[] PopupRoots(GameObject[] roots) => roots.Where(r => r.name != "VoiceUI")
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
        .Where(t => t.name.EndsWith("Popup", StringComparison.OrdinalIgnoreCase) || t.name.EndsWith("ConfirmPanel", StringComparison.OrdinalIgnoreCase) || string.Equals(t.name, "SettingsPanel", StringComparison.OrdinalIgnoreCase))
        .Select(t => t.gameObject).ToArray();
    private static void AddShield(Transform parent)
    {
        if (parent.Find("InputShield") != null) return;
        var shield = Box(parent, "InputShield", Color.clear, 0, 0, 6000, 4000);
        shield.anchorMin = shield.anchorMax = shield.pivot = new Vector2(.5f, .5f);
        shield.anchoredPosition = Vector2.zero; shield.SetAsFirstSibling();
    }
}
