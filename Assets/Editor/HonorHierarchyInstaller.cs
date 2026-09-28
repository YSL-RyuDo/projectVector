#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-time scene authoring tool: the shipped Honor UI consists of editable Lobby.unity objects.
public static class HonorHierarchyInstaller
{
    private static readonly Color Ink = new Color32(31, 34, 39, 255);
    private static readonly Color Slate = new Color32(76, 85, 101, 248);
    private static readonly Color Blue = new Color32(68, 99, 158, 255);
    private static readonly Color SoftBlue = new Color32(102, 119, 153, 235);
    private static readonly Color Gold = new Color32(236, 179, 61, 255);
    private static readonly Color Red = new Color32(192, 43, 57, 255);
    private static TMP_FontAsset font;

    [MenuItem("Project Vector/UI/Install Honor Hierarchy")]
    public static void Install()
    {
        const string scenePath = "Assets/Scenes/Lobby.unity";
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        GameObject root = FindInScene(scene, "MyPagePanel");
        if (root == null) throw new InvalidOperationException("MyPagePanel not found in Lobby scene.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        if (font == null || font.atlasTexture == null || font.material == null)
            throw new InvalidOperationException("Pretendard font atlas/material is unavailable.");
        Transform header = root.transform.Find("Header");
        if (header == null) throw new InvalidOperationException("MyPagePanel/Header not found.");

        GameObject panel = root.transform.Find("HonorPanel")?.gameObject;
        if (panel == null)
        {
            panel = BuildPanel(root.transform);
            panel.transform.SetSiblingIndex(header.GetSiblingIndex());
        }
        foreach (Transform item in panel.GetComponentsInChildren<Transform>(true))
        {
            if (item == null) continue;
            if (!System.Text.RegularExpressions.Regex.IsMatch(item.name,
                "^Honor(CurrentBadge|GradeBadge[1-6]|RewardBadge[4-6])$")) continue;
            if (item.GetComponent<HonorEmblemGraphic>() != null) continue;
            int grade = item.name == "HonorCurrentBadge" ? 3 : item.name[item.name.Length - 1] - '0';
            foreach (string suffix in new[] { "Outer", "Mid", "Inner" })
            {
                Transform oldLayer = item.Find(item.name + suffix);
                if (oldLayer != null) UnityEngine.Object.DestroyImmediate(oldLayer.gameObject);
            }
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Image>());
            var emblem = item.gameObject.AddComponent<HonorEmblemGraphic>();
            emblem.Grade = grade;
            emblem.raycastTarget = false;
        }
        Transform right = panel.transform.Find("HonorRight");
        Transform left = panel.transform.Find("HonorLeft");
        if (left.Find("HonorLeftTitleBar") == null)
        {
            Box("HonorLeftTitleBar", left, Ink, 0, 0, 530, 42).transform.SetAsFirstSibling();
            left.Find("HonorLeftTitle").GetComponent<TMP_Text>().color = Color.white;
            RectTransform title = left.Find("HonorLeftTitle") as RectTransform;
            title.anchoredPosition = new Vector2(14, 0);
            title.sizeDelta = new Vector2(510, 42);
            Transform voice = left.Find("HonorVoiceCard");
            Box("HonorVoiceTitleBar", voice, Ink, 0, 0, 530, 42).transform.SetAsFirstSibling();
            (voice.Find("HonorVoiceTitle") as RectTransform).anchoredPosition = new Vector2(14, 0);
        }
        Transform handle = right.Find("HonorSection3/HonorGuideScrollbar/HonorGuideScrollHandle");
        if (handle != null)
        {
            RectTransform handleRect = (RectTransform)handle;
            handleRect.sizeDelta = Vector2.zero;
            handleRect.pivot = new Vector2(.5f, .5f);
        }
        foreach (Transform item in panel.GetComponentsInChildren<Transform>(true))
        {
            int symbol = -1;
            if (item.name == "HonorVoiceSymbol") symbol = 2;
            for (int i = 0; i < 7; ++i) if (item.name == $"HonorPerk{i}Icon") symbol = i;
            if (symbol < 0 || item.GetComponent<HonorSymbolGraphic>() != null) continue;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<TMP_Text>());
            var glyph = item.gameObject.AddComponent<HonorSymbolGraphic>();
            glyph.Kind = (HonorSymbolGraphic.Symbol)symbol;
            glyph.color = symbol == 2 ? new Color32(133, 231, 246, 255) : Color.white;
            glyph.raycastTarget = false;
        }
        RectTransform status = right.Find("HonorStatus") as RectTransform;
        status.anchoredPosition = new Vector2(870, 0);
        status.sizeDelta = new Vector2(340, 46);
        Transform history = right.Find("HonorSection2");
        if (history.Find("HonorHistoryEmpty") == null)
            Text("HonorHistoryEmpty", history, "아직 명예 변동 기록이 없습니다.", 22,
                220, 270, 770, 75, Color.white, true);
        GameObject tab = header.Find("HonorTabButton")?.gameObject;
        if (tab == null)
            tab = Button("HonorTabButton", header, "명예", 1090, 2, 205, 60, Ink);
        GameObject selected = header.Find("HonorTabSelected")?.gameObject;
        if (selected == null)
        {
            selected = Box("HonorTabSelected", header, Red, 1090, 57, 200, 6);
            selected.SetActive(false);
        }
        var tabs = root.GetComponent<MyPageTabController>();
        if (tabs == null) throw new InvalidOperationException("MyPageTabController missing.");
        var serialized = new SerializedObject(tabs);
        BindArray(serialized, "pages", 4, panel);
        BindArray(serialized, "tabButtons", 4, tab.GetComponent<Button>());
        BindArray(serialized, "selectedIndicators", 4, selected);
        serialized.FindProperty("honor").objectReferenceValue =
            panel.GetComponent<MyPageHonorController>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[HONOR] Scene-authored HonorPanel and tab saved in Lobby.unity.");
    }

    private static void BindArray(SerializedObject owner, string name, int index, UnityEngine.Object item)
    {
        SerializedProperty array = owner.FindProperty(name);
        array.arraySize = Math.Max(array.arraySize, index + 1);
        array.GetArrayElementAtIndex(index).objectReferenceValue = item;
    }

    private static GameObject BuildPanel(Transform root)
    {
        GameObject panel = Box("HonorPanel", root, new Color32(204, 215, 229, 250), 0, 0, 0, 0);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(0, 0); rect.offsetMax = new Vector2(0, -64);
        panel.AddComponent<MyPageHonorController>();

        GameObject left = Box("HonorLeft", panel.transform, Color.clear, 65, 34, 530, 880);
        Text("HonorLeftTitle", left.transform, "현재 명예 등급", 25, 0, 0, 530, 42, Ink, false);
        GameObject upper = Box("HonorCurrentCard", left.transform, Blue, 0, 44, 530, 548);
        Badge("HonorCurrentBadge", upper.transform, 152, 36, 225, 3);
        Text("HonorCurrentGradeTitle", upper.transform, "3  보통", 34, 90, 284, 350, 50, Color.white, true);
        Text("HonorCurrentScore", upper.transform, "601 / 1,800", 19, 295, 347, 207, 30, Color.white, false);
        Text("HonorProgressLabel", upper.transform, "명예 등급 평가 진행도", 18, 20, 347, 270, 30, Color.white, false);
        Box("HonorProgressBackground", upper.transform, Slate, 28, 384, 474, 7);
        Image fill = Box("HonorProgressFill", upper.transform, Gold, 28, 384, 474, 7).GetComponent<Image>();
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.fillAmount = 601f / 1800f;
        Text("HonorCurrentRules", upper.transform,
            "• 긍정적인 참여로 명예가 올라갑니다.\n• 위반 시 명예가 크게 하락합니다.\n• 등급 5·6에서 재화 보상을 받습니다.\n• 현재 모습을 유지해 주세요!",
            18, 34, 421, 468, 102, Color.white, false);
        GameObject lower = Box("HonorVoiceCard", left.transform, new Color32(74, 90, 122, 255), 0, 604, 530, 276);
        Text("HonorVoiceTitle", lower.transform, "현재 음성 매너 평가", 24, 0, 0, 530, 42, Color.white, false);
        Text("HonorVoiceSymbol", lower.transform, "VOICE", 27, 30, 86, 165, 132, new Color32(123, 222, 246, 255), true);
        Text("HonorVoiceStatus", lower.transform, "보통", 34, 230, 83, 255, 50,
            new Color32(133, 231, 246, 255), false);
        Text("HonorVoiceDetail", lower.transform, "음성 매너 평가는 준비 중입니다.\n기본 상태로 표시합니다.",
            18, 225, 147, 275, 90, Color.white, false);

        GameObject right = Box("HonorRight", panel.transform, Color.clear, 634, 34, 1215, 880);
        string[] tabs = { "등급 설명", "등급 보상", "변동 상세 조회", "위반 안내" };
        for (int i = 0; i < 4; ++i)
            Button($"HonorSectionButton{i}", right.transform, tabs[i], i * 216, 0, 211, 46,
                i == 0 ? Red : Slate);
        Box("HonorTopRule", right.transform, Ink, 0, 52, 1210, 3);
        Text("HonorStatus", right.transform, "", 18, 0, 56, 1210, 30, Red, false);
        BuildDescription(right.transform);
        BuildRewards(right.transform);
        BuildHistory(right.transform);
        BuildGuide(right.transform);
        return panel;
    }

    private static void BuildDescription(Transform right)
    {
        GameObject section = Box("HonorSection0", right, Color.clear, 0, 65, 1210, 800);
        GameObject row = Box("HonorGradeRow", section.transform,
            new Color32(130, 138, 151, 172), 0, 0, 1210, 420);
        string[] names = { "위험", "주의 필요", "보통", "양호", "우수", "탁월" };
        int[] thresholds = { 0, 0, 301, 601, 901, 1201, 1601 };
        var controller = section.GetComponentInParent<MyPageHonorController>();
        for (int grade = 1; grade <= 6; ++grade)
        {
            int x = (grade - 1) * 202;
            GameObject column = Box($"HonorGrade{grade}", row.transform,
                new Color(0, 0, 0, 0.01f), x, 0, 199, 420);
            column.GetComponent<Image>().raycastTarget = true;
            var hover = column.AddComponent<HonorGradeHover>();
            hover.Configure(grade, controller);
            GameObject highlight = Box($"HonorGradeHighlight{grade}", column.transform,
                SoftBlue, 0, 0, 199, 420);
            highlight.SetActive(false);
            Badge($"HonorGradeBadge{grade}", column.transform, 35, 116, 126, grade);
            Text($"HonorGradeThreshold{grade}", column.transform, thresholds[grade].ToString("N0"),
                17, 40, 310, 120, 29, Color.white, true);
            Text($"HonorGradeName{grade}", column.transform, names[grade - 1],
                24, 12, 363, 176, 43, Color.white, true);
            GameObject marker = Box($"HonorGradeCurrent{grade}", column.transform,
                Gold, 22, 24, 153, 28);
            Text($"HonorGradeCurrentLabel{grade}", marker.transform, "현재 등급", 17,
                0, 0, 153, 28, Ink, true);
            marker.SetActive(false);
        }
        Text("HonorViewedGrade", section.transform, "3  보통", 23, 0, 435, 245, 34, Ink, false);
        Text("HonorViewedThreshold", section.transform, "601점부터", 18, 260, 439, 225, 30, Ink, false);
        string[] perks = { "사용자 지정 방/난투", "챌린지", "채팅/음성", "랭크 폭파",
            "전용 스킨", "전용 훈장", "전용 케이스" };
        for (int i = 0; i < perks.Length; ++i)
        {
            GameObject card = Box($"HonorPerk{i}", section.transform, SoftBlue,
                i * 173, 503, 166, 275);
            Text($"HonorPerk{i}State", card.transform, "해제", 22, 35, 18, 95, 55,
                new Color32(75, 222, 104, 255), true);
            Text($"HonorPerk{i}Icon", card.transform,
                new[] { "ROOM", "VS", "CHAT", "RANK", "SKIN", "MEDAL", "CASE" }[i],
                22, 16, 76, 134, 100, Color.white, true);
            Text($"HonorPerk{i}Name", card.transform, perks[i], 17,
                9, 204, 148, 59, Color.white, true);
        }
    }

    private static void BuildRewards(Transform right)
    {
        GameObject section = Box("HonorSection1", right, Color.clear, 0, 65, 1210, 800);
        Text("HonorRewardIntro", section.transform,
            "'우수' 이상 등급에 도달하면 보상을 수령할 수 있습니다.",
            24, 70, 23, 1070, 45, Ink, true);
        string[] descriptions = { "양호", "우수", "탁월" };
        for (int grade = 4; grade <= 6; ++grade)
        {
            int x = 70 + (grade - 4) * 398;
            Badge($"HonorRewardBadge{grade}", section.transform, x + 95, 154, 180, grade);
            Text($"HonorRewardGrade{grade}", section.transform, descriptions[grade - 4],
                27, x + 38, 390, 290, 46, Ink, true);
            if (grade == 4) continue;
            GameObject reward = Box($"HonorRewardCard{grade}", section.transform,
                new Color32(48, 53, 62, 255), x + 40, 493, 320, 165);
            Image free = Box($"HonorReward{grade}FreeIcon", reward.transform,
                Color.white, 30, 29, 75, 75).GetComponent<Image>();
            free.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/LevelRewards/Currency0Ticket.png");
            free.preserveAspect = true;
            Image victory = Box($"HonorReward{grade}VictoryIcon", reward.transform,
                Color.white, 175, 29, 75, 75).GetComponent<Image>();
            victory.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/LevelRewards/Currency1Crystal.png");
            victory.preserveAspect = true;
            Text($"HonorReward{grade}Free", reward.transform,
                grade == 5 ? "×1,000" : "×1,500", 18, 15, 113, 132, 36, Color.white, true);
            Text($"HonorReward{grade}Victory", reward.transform,
                grade == 5 ? "×2" : "×3", 18, 170, 113, 100, 36, Color.white, true);
            Text($"HonorReward{grade}State", section.transform, "잠김", 19,
                x + 35, 676, 330, 30, Ink, true);
            Button($"HonorClaim{grade}", section.transform, "수령하기",
                x + 92, 714, 215, 51, Red);
        }
        section.SetActive(false);
    }

    private static void BuildHistory(Transform right)
    {
        GameObject section = Box("HonorSection2", right, new Color32(106, 115, 130, 230),
            0, 65, 1210, 800);
        Box("HonorHistoryHeader", section.transform, Slate, 0, 0, 1210, 61);
        Text("HonorHistoryReasonHeader", section.transform, "변동 원인", 23,
            50, 15, 540, 34, Color.white, false);
        Text("HonorHistoryTimeHeader", section.transform, "적용 시간", 23,
            635, 15, 300, 34, Color.white, false);
        Text("HonorHistoryDeltaHeader", section.transform, "변동 결과", 23,
            975, 15, 220, 34, Color.white, false);
        for (int i = 0; i < 10; ++i)
        {
            int y = 66 + i * 61;
            GameObject row = Box($"HonorHistoryRow{i}", section.transform,
                i % 2 == 0 ? new Color32(117, 124, 136, 160)
                    : new Color32(110, 118, 131, 140), 0, y, 1210, 59);
            Text($"HonorHistoryReason{i}", row.transform, "", 19,
                50, 13, 530, 35, Color.white, false);
            Text($"HonorHistoryTime{i}", row.transform, "", 19,
                635, 13, 300, 35, Color.white, false);
            Text($"HonorHistoryDelta{i}", row.transform, "", 19,
                975, 13, 220, 35, Color.white, false);
            row.SetActive(false);
        }
        Button("HonorFirstPage", section.transform, "|◀", 394, 713, 60, 47, Ink);
        Button("HonorPreviousPage", section.transform, "◀", 460, 713, 60, 47, Ink);
        Text("HonorHistoryPage", section.transform, "1 / 1", 22,
            542, 719, 128, 33, Color.white, true);
        Button("HonorNextPage", section.transform, "▶", 689, 713, 60, 47, Ink);
        Button("HonorLastPage", section.transform, "▶|", 755, 713, 60, 47, Ink);
        section.SetActive(false);
    }

    private static void BuildGuide(Transform right)
    {
        GameObject section = Box("HonorSection3", right, Color.clear, 0, 65, 1210, 800);
        GameObject viewport = Box("HonorGuideViewport", section.transform,
            new Color(1, 1, 1, .01f), 0, 0, 1172, 790);
        viewport.GetComponent<Image>().raycastTarget = true;
        viewport.AddComponent<RectMask2D>();
        GameObject content = Box("HonorGuideContent", viewport.transform,
            Color.clear, 0, 0, 1150, 8 * 151);
        string[] titles = { "소극적 플레이", "중도 퇴장", "타인 플레이 방해", "확인 거절",
            "채팅 위반", "매치 조작", "제3자 소프트웨어/악성 프로그램", "대리 플레이" };
        string[] details =
        {
            "고의로 상대를 공격하지 않거나 경기 승리를 목표로 하지 않는 행위입니다.",
            "경기 도중 자발적으로 퇴장하거나 연결을 끊어 팀의 진행을 방해하는 행위입니다.",
            "캐릭터 행동, 아이템 또는 맵 기능으로 다른 플레이어의 정상적인 진행을 방해하는 행위입니다.",
            "매칭 성사 후 입장 확인을 반복적으로 거절하여 경기를 시작하지 못하게 하는 행위입니다.",
            "문자나 음성 채팅으로 모욕, 괴롭힘 또는 불쾌감을 주는 행위입니다.",
            "고의적인 파티·매칭 조작으로 경기 결과를 바꾸는 행위입니다.",
            "게임 클라이언트 동작을 수정하는 외부 프로그램을 사용하는 행위입니다.",
            "타인이 대신 플레이하여 랭크·명예 결과를 왜곡하는 행위입니다."
        };
        for (int i = 0; i < titles.Length; ++i)
        {
            GameObject row = Box($"HonorGuideRow{i}", content.transform,
                new Color32(100, 109, 124, 188), 0, i * 151, 1150, 143);
            Box($"HonorGuideIcon{i}", row.transform, Slate, 0, 0, 122, 143);
            Text($"HonorGuideSymbol{i}", row.transform,
                (i + 1).ToString("00"),
                50, 17, 44, 85, 60, Color.white, true);
            Text($"HonorGuideTitle{i}", row.transform, titles[i], 23,
                155, 18, 950, 38, Color.white, false);
            Text($"HonorGuideDescription{i}", row.transform,
                "위반 안내: " + details[i] + "\n반복되거나 중대한 위반은 명예가 크게 하락할 수 있습니다.",
                16, 155, 63, 960, 72, Color.white, false);
        }
        HonorGuideScrollRect scroll = section.AddComponent<HonorGuideScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content.GetComponent<RectTransform>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 0;
        GameObject track = Box("HonorGuideScrollbar", section.transform,
            Ink, 1183, 0, 17, 790);
        GameObject handle = Box("HonorGuideScrollHandle", track.transform,
            Red, 0, 0, 17, 245);
        handle.GetComponent<Image>().raycastTarget = true;
        HonorRestrictedScrollbar scrollbar = track.AddComponent<HonorRestrictedScrollbar>();
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        section.SetActive(false);
    }

    private static void Badge(string name, Transform parent, int x, int y, int size, int grade)
    {
        GameObject badge = Box(name, parent, Color.clear, x, y, size, size);
        UnityEngine.Object.DestroyImmediate(badge.GetComponent<Image>());
        HonorEmblemGraphic emblem = badge.AddComponent<HonorEmblemGraphic>();
        emblem.Grade = grade;
        emblem.raycastTarget = false;
        Text(name + "Number", badge.transform, grade.ToString(), size * .38f,
            size / 5, size / 3, size * 3 / 5, size / 3, Color.white, true);
    }

    private static GameObject Box(string name, Transform parent, Color color,
        float x, float y, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        Image image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return go;
    }

    private static GameObject Button(string name, Transform parent, string label,
        float x, float y, float width, float height, Color color)
    {
        GameObject go = Box(name, parent, color, x, y, width, height);
        go.GetComponent<Image>().raycastTarget = true;
        go.AddComponent<Button>();
        Text(name + "Label", go.transform, label, 20, 4, 6,
            width - 8, height - 12, Color.white, true);
        return go;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value,
        float size, float x, float y, float width, float height, Color color, bool center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.text = value;
        text.color = color; text.enableWordWrapping = true; text.raycastTarget = false;
        text.alignment = center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        return null;
    }
}
#endif
