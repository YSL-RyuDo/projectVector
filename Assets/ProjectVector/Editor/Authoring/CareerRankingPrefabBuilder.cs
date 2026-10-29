using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerRankingPrefabBuilder
{
    private const string LobbyScenePath = "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity";
    private const string PrefabFolder = "Assets/ProjectVector/Modules/Lobby/Resources/Career";
    private const string RowPrefabPath =
        PrefabFolder + "/CareerRankingRow.prefab";
    private const string SelectorPrefabPath =
        PrefabFolder + "/CareerRankingCharacterSelector.prefab";

    [InitializeOnLoadMethod]
    private static void BuildAutomaticallyWhenMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SelectorPrefabPath) != null)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling &&
                AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath) == null)
            {
                Build();
            }
        };
    }

    [MenuItem("Tools/Career/Build Ranking Prefabs")]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);

        Scene openedScene = default;
        bool closeOpenedScene = false;
        Transform rankingPanel = FindTransformInLoadedScenes("Ranking Panel");
        Transform sourceRow = rankingPanel != null
            ? FindTransform(rankingPanel, "Rank Item")
            : null;
        if (rankingPanel == null || sourceRow == null)
        {
            openedScene = EditorSceneManager.OpenScene(
                LobbyScenePath,
                OpenSceneMode.Additive
            );
            closeOpenedScene = true;
            rankingPanel = FindTransform(openedScene, "Ranking Panel");
            sourceRow = rankingPanel != null
                ? FindTransform(rankingPanel, "Rank Item")
                : null;
        }

        try
        {
            if (rankingPanel == null || sourceRow == null)
            {
                throw new InvalidOperationException(
                    "Lobby scene Ranking Panel or Rank Item was not found."
                );
            }

            BuildRowPrefab(sourceRow);
            TMP_FontAsset font =
                sourceRow.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
            BuildCharacterSelectorPrefab(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[CAREER RANKING] Built {RowPrefabPath} and {SelectorPrefabPath}"
            );
        }
        finally
        {
            if (closeOpenedScene && openedScene.IsValid())
                EditorSceneManager.CloseScene(openedScene, true);
        }
    }

    private static void BuildRowPrefab(Transform sourceRow)
    {
        GameObject clone = UnityEngine.Object.Instantiate(
            sourceRow.gameObject
        );
        clone.name = "CareerRankingRow";
        clone.transform.SetParent(null, false);
        clone.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(clone, RowPrefabPath);
        UnityEngine.Object.DestroyImmediate(clone);
    }

    private static void BuildCharacterSelectorPrefab(TMP_FontAsset font)
    {
        GameObject root = CreateUiObject("CareerRankingCharacterSelector");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetRect(
            rootRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(-170f, 365f),
            new Vector2(240f, 50f)
        );

        GameObject selected = CreateButton(
            "SelectedCharacterButton",
            root.transform,
            new Color32(55, 58, 64, 245)
        );
        Stretch(selected.GetComponent<RectTransform>());
        CreateIcon(
            "Icon",
            selected.transform,
            LoadCharacterSprite(0),
            new Vector2(-92f, 0f),
            new Vector2(48f, 48f)
        );
        CreateText(
            "Label",
            selected.transform,
            "캐릭터 0",
            font,
            25f,
            TextAlignmentOptions.Center,
            new Vector2(20f, 0f),
            new Vector2(150f, 50f)
        );
        CreateText(
            "Arrow",
            selected.transform,
            "▼",
            font,
            20f,
            TextAlignmentOptions.Center,
            new Vector2(102f, 0f),
            new Vector2(30f, 50f)
        );

        GameObject popup = CreateUiObject("CharacterPopup", root.transform);
        Image popupImage = popup.AddComponent<Image>();
        popupImage.color = new Color32(31, 34, 39, 250);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        SetRect(
            popupRect,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            new Vector2(260f, 260f),
            new Vector2(0.5f, 1f)
        );

        GridLayoutGroup grid = popup.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(7, 7, 7, 7);
        grid.cellSize = new Vector2(120f, 120f);
        grid.spacing = new Vector2(6f, 6f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        for (int characterId = 0; characterId < 4; ++characterId)
        {
            GameObject button = CreateButton(
                $"CharacterButton{characterId}",
                popup.transform,
                characterId == 0
                    ? new Color32(48, 174, 205, 255)
                    : new Color32(75, 78, 84, 255)
            );
            CreateStretchIcon(
                "Icon",
                button.transform,
                LoadCharacterSprite(characterId),
                5f
            );
            CreateCornerLabel(
                button.transform,
                characterId.ToString(),
                font
            );
        }

        popup.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, SelectorPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static GameObject CreateUiObject(
        string name,
        Transform parent = null)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = 5;
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        Color background)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = background;
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.75f, 0.9f, 1f);
        colors.pressedColor = new Color(0.16f, 0.55f, 0.72f, 1f);
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.45f);
        button.colors = colors;
        return gameObject;
    }

    private static void CreateIcon(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size)
    {
        GameObject iconObject = CreateUiObject(name, parent);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void CreateStretchIcon(
        string name,
        Transform parent,
        Sprite sprite,
        float padding)
    {
        GameObject iconObject = CreateUiObject(name, parent);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void CreateCornerLabel(
        Transform parent,
        string value,
        TMP_FontAsset font)
    {
        GameObject label = CreateText(
            "CharacterId",
            parent,
            value,
            font,
            20f,
            TextAlignmentOptions.Center,
            new Vector2(47f, -47f),
            new Vector2(24f, 24f)
        );
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return textObject;
    }

    public static Sprite LoadCharacterSprite(int characterId)
    {
        // Same ID order and authored portraits as character selection, never the full-body sprites.
        string[] names = { "Initiator", "Controller", "Sentinel", "Duelist" };
        if (characterId < 0 || characterId >= names.Length) return null;
        var data = AssetDatabase.LoadAssetAtPath<CharacterPickSO>(
            "Assets/ProjectVector/Modules/Match/Data/Characters/" + names[characterId] + ".asset");
        return data != null ? data.characterIcon : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2? pivot = null)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Transform FindTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindTransform(root.transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindTransformInLoadedScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            Transform found = FindTransform(scene, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindTransform(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
