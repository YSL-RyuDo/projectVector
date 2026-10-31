using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RankResultSceneBuilder
{
    private const string ScenePath = "Assets/ProjectVector/Modules/Results/Scenes/RankResult.unity";
    private const string FontPath =
        "Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset";

    [MenuItem("Tools/Project Vector/Build Rank Result Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite[] rankSprites = LoadRankSprites();

        CreateCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Image background = CreateImage(
            "Background",
            canvasRect,
            new Color32(13, 18, 29, 255));
        Stretch(background.rectTransform);

        Image glow = CreateImage(
            "Center Glow",
            canvasRect,
            new Color32(24, 74, 102, 115));
        SetRect(glow.rectTransform, new Vector2(0f, 10f), new Vector2(1120f, 780f));

        Image topLine = CreateImage(
            "Top Accent",
            canvasRect,
            new Color32(40, 201, 255, 255));
        SetRect(topLine.rectTransform, new Vector2(0f, 430f), new Vector2(1320f, 5f));

        Image bottomLine = CreateImage(
            "Bottom Accent",
            canvasRect,
            new Color32(40, 201, 255, 160));
        SetRect(bottomLine.rectTransform, new Vector2(0f, -430f), new Vector2(1320f, 3f));

        RectTransform root = CreateRect("RankResultRoot", canvasRect);
        SetRect(root, Vector2.zero, new Vector2(1200f, 900f));
        RankResultController controller = root.gameObject.AddComponent<RankResultController>();

        TMP_Text header = CreateText(
            "Header",
            root,
            "랭크",
            font,
            38f,
            new Color32(216, 239, 247, 255));
        SetRect(header.rectTransform, new Vector2(0f, 360f), new Vector2(900f, 64f));

        TMP_Text changeText = CreateText(
            "Rank Change",
            root,
            "랭크 점수 상승",
            font,
            28f,
            new Color32(74, 213, 255, 255));
        SetRect(changeText.rectTransform, new Vector2(0f, 305f), new Vector2(900f, 52f));

        Image rankFrame = CreateImage(
            "Rank Frame",
            root,
            new Color32(4, 10, 18, 220));
        SetRect(rankFrame.rectTransform, new Vector2(0f, 115f), new Vector2(330f, 330f));

        Image rankImage = CreateImage("Rank Image", rankFrame.rectTransform, Color.white);
        SetRect(rankImage.rectTransform, Vector2.zero, new Vector2(270f, 270f));
        rankImage.preserveAspect = true;
        if (rankSprites.Length > 0)
            rankImage.sprite = rankSprites[0];

        TMP_Text rankName = CreateText(
            "Rank Name",
            root,
            "브론즈 5",
            font,
            54f,
            Color.white);
        SetRect(rankName.rectTransform, new Vector2(0f, -95f), new Vector2(900f, 82f));

        Slider slider = CreateSlider(root);
        SetRect(slider.GetComponent<RectTransform>(), new Vector2(0f, -175f), new Vector2(720f, 30f));

        TMP_Text pointText = CreateText(
            "Rank Points",
            root,
            "0 / 100",
            font,
            28f,
            new Color32(213, 232, 239, 255));
        SetRect(pointText.rectTransform, new Vector2(0f, -225f), new Vector2(720f, 52f));

        TMP_Text deltaText = CreateText(
            "Rank Point Delta",
            root,
            "+25",
            font,
            42f,
            new Color32(50, 220, 255, 255));
        SetRect(deltaText.rectTransform, new Vector2(0f, -280f), new Vector2(720f, 62f));

        Button continueButton = CreateButton(
            "Continue Button",
            root,
            "결과 확인",
            font);
        SetRect(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(0f, -365f),
            new Vector2(300f, 68f));
        UnityEventTools.AddPersistentListener(
            continueButton.onClick,
            controller.OnClickContinue);

        TMP_Text hint = CreateText(
            "Continue Hint",
            root,
            "Enter 또는 Space로 계속",
            font,
            18f,
            new Color32(142, 166, 178, 255));
        SetRect(hint.rectTransform, new Vector2(0f, -415f), new Vector2(600f, 38f));

        BindController(
            controller,
            rankImage,
            rankName,
            pointText,
            deltaText,
            changeText,
            slider,
            slider.gameObject,
            continueButton,
            rankSprites);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RANK RESULT SCENE] Saved: " + ScenePath);
    }

    private static void BindController(
        RankResultController controller,
        Image rankImage,
        TMP_Text rankName,
        TMP_Text pointText,
        TMP_Text deltaText,
        TMP_Text changeText,
        Slider slider,
        GameObject progressRoot,
        Button continueButton,
        Sprite[] rankSprites)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("rankImage").objectReferenceValue = rankImage;
        serialized.FindProperty("rankNameText").objectReferenceValue = rankName;
        serialized.FindProperty("rankPointText").objectReferenceValue = pointText;
        serialized.FindProperty("rankDeltaText").objectReferenceValue = deltaText;
        serialized.FindProperty("rankChangeText").objectReferenceValue = changeText;
        serialized.FindProperty("rankProgressSlider").objectReferenceValue = slider;
        serialized.FindProperty("rankProgressRoot").objectReferenceValue = progressRoot;
        serialized.FindProperty("continueButton").objectReferenceValue = continueButton;

        SerializedProperty sprites = serialized.FindProperty("rankSprites");
        sprites.arraySize = rankSprites.Length;
        for (int i = 0; i < rankSprites.Length; i++)
            sprites.GetArrayElementAtIndex(i).objectReferenceValue = rankSprites[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static Sprite[] LoadRankSprites()
    {
        string[] paths =
        {
            "Assets/ProjectVector/Shared/Resources/Rank/Bronze.png",
            "Assets/ProjectVector/Shared/Resources/Rank/Silver.png",
            "Assets/ProjectVector/Shared/Resources/Rank/Gold.png",
            "Assets/ProjectVector/Shared/Resources/Rank/Platinum.png",
            "Assets/ProjectVector/Shared/Resources/Rank/Diamond.png",
            "Assets/ProjectVector/Shared/Resources/Rank/Master.png"
        };

        return paths
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(sprite => sprite != null)
            .ToArray();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(13, 18, 29, 255);
        camera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.position = Vector3.zero;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Slider CreateSlider(RectTransform parent)
    {
        RectTransform root = CreateRect("Rank Progress", parent);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color32(42, 55, 65, 255);

        RectTransform fillArea = CreateRect("Fill Area", root);
        Stretch(fillArea, 5f, 5f, 5f, 5f);

        RectTransform fill = CreateRect("Fill", fillArea);
        Stretch(fill);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color32(33, 194, 237, 255);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.fillRect = fill;
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Button CreateButton(
        string name,
        RectTransform parent,
        string label,
        TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color32(25, 157, 204, 255);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(147, 231, 255, 255);
        colors.pressedColor = new Color32(88, 196, 229, 255);
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            rect,
            label,
            font,
            26f,
            Color.white);
        Stretch(text.rectTransform);
        return button;
    }

    private static Image CreateImage(
        string name,
        RectTransform parent,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        RectTransform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static RectTransform CreateRect(
        string name,
        RectTransform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(
        RectTransform rect,
        float left = 0f,
        float right = 0f,
        float top = 0f,
        float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes.ToList();
        int existingIndex = scenes.FindIndex(scene => scene.path == ScenePath);
        if (existingIndex >= 0)
        {
            scenes[existingIndex].enabled = true;
        }
        else
        {
            int resultIndex = scenes.FindIndex(
                scene => scene.path == "Assets/ProjectVector/Modules/Results/Scenes/Result.unity");
            int insertIndex = resultIndex >= 0 ? resultIndex : scenes.Count;
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(ScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
