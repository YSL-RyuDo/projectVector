#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Saves real, editable ChipCraft UI objects in Lobby.unity. No play-mode UI construction.
public static class ChipCraftHierarchyInstaller
{
    private static readonly Color Dark = new Color32(31, 34, 38, 255);
    private static readonly Color Option = new Color32(54, 59, 65, 255);
    private static readonly Color Red = new Color32(193, 37, 53, 255);
    private static readonly Color Paper = new Color32(199, 209, 219, 250);
    private static TMP_FontAsset font;

    [MenuItem("Project Vector/UI/Install Chip Craft Hierarchy")]
    public static void Install()
    {
        const string path = "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        GameObject root = FindInScene(scene, "MyPagePanel");
        if (root == null) throw new InvalidOperationException("Lobby/MyPagePanel is missing.");
        font = GetKoreanFont();
        bool changed = false;
        if (root.GetComponentInChildren<MyPageChipCraftController>(true) == null)
        {
            root.AddComponent<MyPageChipCraftController>();
            changed = true;
        }
        if (Find(root.transform, "ChipCraftPanel") == null)
        {
            BuildPanel(root.transform);
            changed = true;
        }
        if (EnsureProfileCard(root.transform)) changed = true;
        if (EnsureTempRoomMedals(scene)) changed = true;
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CHIP CRAFT] Saved ChipCraftPanel, preview layers, controls and Korean font in Lobby hierarchy.");
        }
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    private static TMP_FontAsset GetKoreanFont()
    {
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        if (asset == null || asset.atlasTexture == null || asset.material == null)
            throw new InvalidOperationException("Pretendard-Black SDF atlas/material is missing.");
        return asset;
    }

    private static bool EnsureTempRoomMedals(Scene scene)
    {
        bool changed = false;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (TempGamePlayerSlotUI slot in sceneRoot.GetComponentsInChildren<TempGamePlayerSlotUI>(true))
        {
            SerializedObject serialized = new(slot);
            Image card = serialized.FindProperty("iconImage").objectReferenceValue as Image;
            if (card == null) continue;
            SerializedProperty medals = serialized.FindProperty("medalImages");
            if (medals.arraySize != 3) { medals.arraySize = 3; changed = true; }
            for (int i = 0; i < 3; ++i)
            {
                string name = "TempGameMedal" + i;
                Transform existing = Find(card.transform, name);
                Image image = existing != null ? existing.GetComponent<Image>() : null;
                if (image == null)
                {
                    image = UI(name, card.transform, Color.white).GetComponent<Image>();
                    Fix(image.gameObject, Vector2.zero, new Vector2(92, 92));
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.gameObject.SetActive(false);
                    changed = true;
                }
                SerializedProperty entry = medals.GetArrayElementAtIndex(i);
                if (entry.objectReferenceValue != image)
                {
                    entry.objectReferenceValue = image;
                    changed = true;
                }
            }
            if (serialized.hasModifiedProperties) serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return changed;
    }

    private static bool EnsureProfileCard(Transform root)
    {
        bool changed = false;
        Transform card = Find(root, "PlayerCardPreview");
        if (card == null) throw new InvalidOperationException("PlayerCardPreview is missing.");
        Transform art = Find(card, "CardIconImage");
        if (art == null) throw new InvalidOperationException("CardIconImage is missing.");
        RectTransform rect = art as RectTransform;
        if (rect.sizeDelta != new Vector2(325, 810) || rect.anchoredPosition != new Vector2(0, -35))
        {
            Fix(art.gameObject, new Vector2(0, -35), new Vector2(325, 810));
            changed = true;
        }
        Image image = art.GetComponent<Image>();
        Sprite artSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconCard/icon0_card.png");
        if (image.sprite != artSprite || image.preserveAspect || image.raycastTarget)
        {
            image.sprite = artSprite; image.preserveAspect = false; image.raycastTarget = false; changed = true;
        }
        for (int i = 0; i < 3; ++i)
        {
            if (Find(art, "CardAppliedMedal" + i) != null) continue;
            Image medal = UI("CardAppliedMedal" + i, art, Color.white).GetComponent<Image>();
            Fix(medal.gameObject, Vector2.zero, new Vector2(120, 120));
            medal.raycastTarget = false;
            medal.gameObject.SetActive(false);
            changed = true;
        }
        Transform border = Find(card, "CardBorderImage");
        if (border != null)
        {
            Image frame = border.GetComponent<Image>();
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconBorder/border.png");
            if (frame.sprite != frameSprite || frame.color != Color.white || frame.raycastTarget)
            {
                frame.sprite = frameSprite; frame.color = Color.white; frame.raycastTarget = false; changed = true;
            }
            if (border.GetSiblingIndex() != card.childCount - 1) { border.SetAsLastSibling(); changed = true; }
        }
        Transform nickname = Find(card, "CardNicknameText");
        if (nickname != null)
        {
            RectTransform nameRect = nickname as RectTransform;
            if (nameRect.anchoredPosition.y != 388)
            { nameRect.anchoredPosition = new Vector2(0, 388); changed = true; }
        }
        Transform level = Find(card, "CardLevelText");
        if (level != null)
        {
            RectTransform levelRect = level as RectTransform;
            if (levelRect.anchoredPosition.y != 348)
            { levelRect.anchoredPosition = new Vector2(0, 348); changed = true; }
        }
        Transform title = Find(card, "CardTitle");
        if (title != null && title.gameObject.activeSelf) { title.gameObject.SetActive(false); changed = true; }
        Transform header = Find(root, "Header");
        if (header != null && Find(header, "ChipCraftOpenButton") == null)
        {
            Button("ChipCraftOpenButton", header, "칩 크래프트", new Vector2(-110, 0), new Vector2(210, 60), new Color(0, 0, 0, 0));
            changed = true;
        }
        if (Find(card, "ChipCraftCardOpenButton") == null)
        {
            GameObject open = UI("ChipCraftCardOpenButton", card, new Color(0, 0, 0, 0));
            Stretch(open.GetComponent<RectTransform>());
            open.AddComponent<Button>();
            open.transform.SetAsLastSibling();
            changed = true;
        }
        // Use the existing serialized Pretendard atlas. A runtime-created font without
        // atlas/material sub-assets produces TMP UnassignedReferenceException.
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font != font) { text.font = font; changed = true; }
            if (text.fontSharedMaterial != font.material)
            { text.fontSharedMaterial = font.material; changed = true; }
            if (!text.enabled) { text.enabled = true; changed = true; }
        }
        return changed;
    }

    private static void BuildPanel(Transform root)
    {
        GameObject panel = UI("ChipCraftPanel", root, Paper);
        Stretch(panel.GetComponent<RectTransform>());
        panel.transform.SetAsLastSibling();
        GameObject header = UI("ChipCraftHeader", panel.transform, Dark);
        Anchor(header.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -54, 0, 0);
        Text("ChipCraftPageTitle", header.transform, "칩 크래프트", 30, new Vector2(0, 0), new Vector2(500, 45), new Vector2(.5f, .5f));
        Button("ChipCraftBackButton", header.transform, "ESC   뒤로", new Vector2(-92, 0), new Vector2(175, 42), Option, new Vector2(1, .5f));

        string[] tabs = { "플레이어 카드", "케이스", "훈장" };
        for (int i = 0; i < tabs.Length; ++i)
            Button("ChipCraftTab" + i, panel.transform, tabs[i], new Vector2(135 + 150 * i, -128),
                new Vector2(142, 46), i == 0 ? Red : Option, new Vector2(0, 1));

        GameObject inventory = UI("ChipCraftInventory", panel.transform, new Color(0, 0, 0, .1f));
        Anchor(inventory.GetComponent<RectTransform>(), 0, 0, 0, 1, 34, 110, 485, -175);
        BuildCardList(inventory.transform);
        BuildCaseList(inventory.transform);
        BuildMedalList(inventory.transform);

        GameObject preview = UI("ChipCraftPreview", panel.transform, Dark);
        Fix(preview, new Vector2(0, -12), new Vector2(360, 820));
        Image art = UI("ChipCraftPreviewArt", preview.transform, Color.white).GetComponent<Image>();
        Fix(art.gameObject, Vector2.zero, new Vector2(342, 800));
        art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconCard/icon0_card.png");
        art.raycastTarget = false;
        for (int i = 0; i < 3; ++i)
        {
            GameObject placed = UI("ChipCraftPlacedMedal" + i, art.transform, Color.white);
            Fix(placed, Vector2.zero, new Vector2(128, 128));
            placed.AddComponent<Button>();
            ChipCraftMedalDragHandle drag = placed.AddComponent<ChipCraftMedalDragHandle>();
            drag.SetSlot(i);
            placed.SetActive(false);
        }
        Image border = UI("ChipCraftPreviewCase", preview.transform, Color.white).GetComponent<Image>();
        Stretch(border.GetComponent<RectTransform>());
        border.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconBorder/border.png");
        border.raycastTarget = false;
        Text("ChipCraftMedalCount", panel.transform, "훈장 수  0 / 1", 18, new Vector2(0, -450), new Vector2(280, 35));

        Text("ChipCraftItemTitle", panel.transform, "플레이어 카드", 29, new Vector2(-305, -130),
            new Vector2(440, 52), new Vector2(1, 1));
        Text("ChipCraftItemDescription", panel.transform, "", 21, new Vector2(-305, -225),
            new Vector2(440, 130), new Vector2(1, 1));
        Button("ChipCraftEditButton", panel.transform, "편집 모드 진입", new Vector2(-290, 105),
            new Vector2(400, 62), Option, new Vector2(1, 0));
        Button("ChipCraftResetButton", panel.transform, "초기화", new Vector2(-290, 168),
            new Vector2(400, 54), Option, new Vector2(1, 0));
        Button("ChipCraftSaveButton", panel.transform, "저장하기", new Vector2(-290, 105),
            new Vector2(400, 62), Red, new Vector2(1, 0));
        Text("ChipCraftStatus", panel.transform, "", 17, new Vector2(-290, 232),
            new Vector2(400, 50), new Vector2(1, 0));

        GameObject controls = UI("ChipCraftEditControls", panel.transform, new Color(0, 0, 0, 0));
        Fix(controls, new Vector2(0, -475), new Vector2(760, 58));
        string[] buttonLabels = { "훈장 적용", "삭제", "45° 회전", "좌우 반전", "확대 +", "축소 -" };
        string[] names = { "ChipCraftMedalApplyButton", "ChipCraftMedalDeleteButton", "ChipCraftMedalRotateButton",
            "ChipCraftMedalFlipButton", "ChipCraftMedalLargerButton", "ChipCraftMedalSmallerButton" };
        for (int i = 0; i < names.Length; ++i)
            Button(names[i], controls.transform, buttonLabels[i], new Vector2(-315 + 126 * i, 0),
                new Vector2(120, 45), i == 0 ? Red : Option);

        GameObject shade = UI("ChipCraftSaveDialog", panel.transform, new Color(0, 0, 0, .7f));
        Stretch(shade.GetComponent<RectTransform>());
        GameObject box = UI("ChipCraftConfirmBox", shade.transform, Paper);
        Fix(box, Vector2.zero, new Vector2(620, 285));
        Text("ChipCraftConfirmTitle", box.transform, "안내", 27, new Vector2(0, 95), new Vector2(510, 40));
        Text("ChipCraftConfirmMessage", box.transform, "프리셋을 저장하시겠습니까?", 25,
            new Vector2(0, 18), new Vector2(510, 60));
        Button("ChipCraftCancelButton", box.transform, "뒤로", new Vector2(-155, -92), new Vector2(220, 50), Option);
        Button("ChipCraftConfirmButton", box.transform, "확인", new Vector2(155, -92), new Vector2(220, 50), Red);
        shade.SetActive(false);
        panel.SetActive(false);
    }

    private static void BuildCardList(Transform parent)
    {
        GameObject list = UI("ChipCraftCardList", parent, new Color(0, 0, 0, 0));
        Stretch(list.GetComponent<RectTransform>());
        for (int i = 0; i < 3; ++i)
        {
            GameObject option = Button("ChipCraftCardOption" + i, list.transform, "", new Vector2(82 + i * 145, -115),
                new Vector2(130, 195), Option, new Vector2(0, 1));
            Image art = UI("ChipCraftCardOption" + i + "Art", option.transform, Color.white).GetComponent<Image>();
            Fix(art.gameObject, new Vector2(0, 13), new Vector2(115, 160));
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectVector/Shared/Resources/IconCard/icon{i}_card.png");
            art.raycastTarget = false;
            Text("ChipCraftCardOption" + i + "Label", option.transform, $"카드 {i + 1}", 16,
                new Vector2(0, -83), new Vector2(110, 27));
        }
    }

    private static void BuildCaseList(Transform parent)
    {
        GameObject list = UI("ChipCraftCaseList", parent, new Color(0, 0, 0, 0));
        Stretch(list.GetComponent<RectTransform>());
        GameObject option = Button("ChipCraftCaseOption0", list.transform, "", new Vector2(85, -115),
            new Vector2(135, 200), Option, new Vector2(0, 1));
        Image art = UI("ChipCraftCaseOption0Art", option.transform, Color.white).GetComponent<Image>();
        Fix(art.gameObject, new Vector2(0, 11), new Vector2(115, 160));
        art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconBorder/border.png");
        art.raycastTarget = false;
        Text("ChipCraftCaseOption0Label", option.transform, "기본 케이스", 16,
            new Vector2(0, -85), new Vector2(120, 28));
        list.SetActive(false);
    }

    private static void BuildMedalList(Transform parent)
    {
        GameObject list = UI("ChipCraftMedalList", parent, new Color(0, 0, 0, 0));
        Stretch(list.GetComponent<RectTransform>());
        string[] ranks = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" };
        for (int i = 0; i < ranks.Length; ++i)
        {
            GameObject option = Button("ChipCraftMedalOption" + i, list.transform, "",
                new Vector2(82 + i % 3 * 145, -82 - i / 3 * 165), new Vector2(130, 150),
                Option, new Vector2(0, 1));
            Image art = UI("ChipCraftMedalOption" + i + "Art", option.transform, Color.white).GetComponent<Image>();
            Fix(art.gameObject, new Vector2(0, 10), new Vector2(105, 105));
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectVector/Shared/Resources/Rank/{ranks[i]}.png");
            art.preserveAspect = true; art.raycastTarget = false;
            Text("ChipCraftMedalOption" + i + "Label", option.transform, ranks[i], 16,
                new Vector2(0, -62), new Vector2(110, 24));
        }
        list.SetActive(false);
    }

    private static GameObject UI(string name, Transform parent, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5; go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }
    private static GameObject Button(string name, Transform parent, string label, Vector2 position,
        Vector2 size, Color color, Vector2? anchor = null)
    {
        GameObject go = UI(name, parent, color);
        Fix(go, position, size, anchor);
        go.AddComponent<Button>();
        if (!string.IsNullOrEmpty(label)) Text(name + "Label", go.transform, label, 18, Vector2.zero, size - new Vector2(8, 8));
        return go;
    }
    private static void Text(string name, Transform parent, string value, float size,
        Vector2 position, Vector2 bounds, Vector2? anchor = null)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = 5; go.transform.SetParent(parent, false);
        Fix(go, position, bounds, anchor);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value; text.font = font; text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        bool darkText = name == "ChipCraftItemTitle" || name == "ChipCraftItemDescription" ||
            name == "ChipCraftMedalCount" || name == "ChipCraftStatus" ||
            name == "ChipCraftConfirmTitle" || name == "ChipCraftConfirmMessage";
        text.color = darkText ? Dark : Color.white;
        text.enableWordWrapping = true; text.raycastTarget = false;
    }
    private static void Fix(GameObject go, Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 value = anchor ?? new Vector2(.5f, .5f);
        rect.anchorMin = value; rect.anchorMax = value; rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
    private static void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY,
        float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(minX, minY); rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(right, top);
    }
    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = Find(root.transform, name);
            if (result != null) return result.gameObject;
        }
        return null;
    }
}
#endif
