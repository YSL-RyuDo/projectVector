using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit offline authoring. Card collection, rank, global header and other feature panels are excluded.
public static class VectorCareerTabsEditor
{
    static readonly Color Ink = new Color32(39, 41, 45, 255), Muted = new Color32(91, 94, 98, 255);
    static readonly Color Red = new Color32(177, 28, 61, 255), Paper = new Color32(225, 227, 225, 210);
    static TMP_FontAsset font;
    static T Ref<T>(Object o, string n) where T : Object => new SerializedObject(o).FindProperty(n).objectReferenceValue as T;
    static T Field<T>(object o, string n) => (T)o.GetType().GetField(n, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    static void Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, args);
    static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
    static Transform Find(Transform p, string n) => p.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
    static Sprite Art(string n) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/" + n + ".png");
    static void Pose(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
    }
    static void Fixed(Transform t)
    {
        foreach (var l in t.GetComponents<LayoutGroup>()) l.enabled = false;
        foreach (var f in t.GetComponents<ContentSizeFitter>()) f.enabled = false;
    }
    static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
    {
        var r = p.Find(n) as RectTransform;
        if (r == null) { r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p, false); }
        r.gameObject.layer = 5; Pose(r, x, y, w, h); return r;
    }
    static Image Box(Transform p, string n, float x, float y, float w, float h, Color color)
    {
        var i = Ensure<Image>(Rect(p, n, x, y, w, h).gameObject); i.color = color; i.raycastTarget = false; return i;
    }
    static void Style(TMP_Text t, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        if (t.font == null) t.font = font;
        t.fontSize = size; t.enableAutoSizing = false; t.color = color; t.alignment = align;
        t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
    }
    static TMP_Text Label(Transform p, string n, string value, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var t = Ensure<TextMeshProUGUI>(Rect(p, n, x, y, w, h).gameObject); t.font = font; Style(t, size, color, align); t.text = value; return t;
    }
    static TMP_Text MoveText(Transform p, string n, float x, float y, float w, float h, float size, Color color, string text = null)
    {
        var t = p.Find(n).GetComponent<TMP_Text>(); Pose(t.transform, x, y, w, h); Style(t, size, color);
        if (text != null) t.text = text; return t;
    }
    static Button MakeButton(Transform p, string n, string title, float x, float y, float w, float h)
    {
        var b = Ensure<Button>(Box(p, n, x, y, w, h, Ink).gameObject); b.targetGraphic = b.GetComponent<Image>();
        Label(b.transform, "Label", title, 0, 0, w - 16, h, 21, Color.white, TextAlignmentOptions.Center);
        StyleButton(b); return b;
    }
    static void StyleButton(Button b)
    {
        b.image.raycastTarget = true;
        var c = b.colors; c.normalColor = c.selectedColor = Color.white; c.highlightedColor = new Color(.88f, .88f, .88f);
        c.pressedColor = new Color(.7f, .7f, .7f); b.colors = c;
        var navigation = b.navigation; navigation.mode = Navigation.Mode.None; b.navigation = navigation;
    }
    static Image Icon(Transform p, string n, Sprite sprite, float x, float y, float size)
    {
        var i = Box(p, n, x, y, size, size, Color.white); i.sprite = sprite; i.type = Image.Type.Simple; i.preserveAspect = true; return i;
    }
    static Image Bar(Transform p, string n, float x, float y, float w)
    {
        Box(p, n + "Track", x, y, w, 4, new Color32(124, 128, 128, 100));
        var fill = Box(p, n, x, y, w, 4, Red);
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0; return fill;
    }
    static void Backdrop(Transform root, string title, string subtitle)
    {
        var bg = Box(root, "CareerNeutralBackdrop", 0, 0, 1920, 1080, Color.white); bg.sprite = Art("NeutralBackground"); bg.transform.SetAsFirstSibling();
        Label(root, "SectionTitle", title, -640, 334, 360, 46, 30, Ink);
        Label(root, "SectionSubtitle", subtitle, 420, 334, 800, 42, 20, Muted, TextAlignmentOptions.MidlineRight);
        Box(root, "SectionRule", 0, 302, 1640, 2, new Color(1, 1, 1, .65f));
    }
    static CareerPanelController Controller(UnityEngine.SceneManagement.Scene scene) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CareerPanelController>(true)).Single();
    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused.");
    }
    static Dictionary<Object, string> Snapshot(Transform root) => root.GetComponentsInChildren<Component>(true).Where(c => c != null).Cast<Object>()
        .Concat(root.GetComponentsInChildren<Transform>(true).Select(t => (Object)t.gameObject)).ToDictionary(o => o, EditorJsonUtility.ToJson);
    static void CheckSnapshot(Dictionary<Object, string> snapshot)
    {
        foreach (var p in snapshot) if (p.Key == null || EditorJsonUtility.ToJson(p.Key) != p.Value) throw new Exception("Excluded UI changed: " + p.Key?.name);
    }

    [MenuItem("Project Vector/UI/Install Remaining Career Tabs (Except Collection)")]
    public static void InstallAndVerify()
    {
        Guard(); font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        foreach (var path in new[] { "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", "Assets/ProjectVector/Development/Legacy/Scenes/Lobby2.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path); var controller = Controller(scene);
            var preservation = new VectorExperiencePolishEditor.Preservation(scene);
            var excluded = Snapshot(Ref<GameObject>(controller, "_cardCollectionPanel").transform);
            var rankExcluded = Snapshot(Ref<GameObject>(controller, "_rankPanel").transform);
            var view = Ensure<CareerTabsPresentation>(controller.gameObject);
            var so = new SerializedObject(controller); so.FindProperty("_tabsPresentation").objectReferenceValue = view; so.ApplyModifiedPropertiesWithoutUndo();
            view.portraits = Enumerable.Range(0, 4).Select(CareerRankingPrefabBuilder.LoadCharacterSprite).ToArray();
            view.tiers = new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" }.Select(n => Resources.Load<Sprite>("Rank/" + n)).ToArray();
            view.starterBadge = Art("StatMatches");
            BuildRanking(Ref<GameObject>(controller, "_rankingPanel").transform, view);
            BuildHistory(Ref<GameObject>(controller, "_careerRecordPanel").transform, view);
            BuildInventory(Ref<GameObject>(controller, "_inventoryPanel").transform, view);
            BuildAchievements(Ref<GameObject>(controller, "_achievementsPanel").transform, view);
            preservation.Check(); CheckSnapshot(excluded); CheckSnapshot(rankExcluded);
            foreach (var component in controller.GetComponentsInChildren<Component>(true))
                if (component != null && (component == view || IsEditedPanel(component.transform, controller)))
                { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAREER_EXCLUSIONS_VERIFIED: card collection and rank unchanged; " + path);
        }
        AssetDatabase.SaveAssets(); Verify();
    }
    static bool IsEditedPanel(Transform t, CareerPanelController c) => new[] { "_rankingPanel", "_careerRecordPanel", "_inventoryPanel", "_achievementsPanel" }.Any(n => t.IsChildOf(Ref<GameObject>(c, n).transform));

    static void BuildRanking(Transform root, CareerTabsPresentation view)
    {
        Backdrop(root, "시즌 랭킹", "종합 랭크와 캐릭터별 활약을 확인하세요");
        var list = root.Find("Ranking List"); Pose(list, 0, -25, 1640, 710);
        var header = list.Find("Up"); Pose(header, 0, 226, 1640, 48); header.GetComponent<Image>().color = Ink;
        string[] names = { "Text (TMP)", "Text (TMP) (1)", "Text (TMP) (2)", "Text (TMP) (3)", "Text (TMP) (4)" };
        float[] x = { -775, -510, -145, 365, 650 }; float[] widths = { 70, 300, 320, 160, 130 };
        string[] labels = { "순위", "플레이어", "랭크", "승리 횟수", "승률" };
        for (int i = 0; i < names.Length; ++i) MoveText(header, names[i], x[i], 0, widths[i], 42, 21, Color.white, labels[i]);
        var rows = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Rank Item" || t.name.StartsWith("Rank Item (")).ToArray();
        var content = rows[0].parent; Fixed(content); Pose(content, 0, -56, 1640, 520);
        view.rankingRows = new CareerTabsPresentation.RankingRow[rows.Length];
        for (int i = 0; i < rows.Length; ++i)
        {
            var r = rows[i]; Pose(r, 0, 232 - i * 51, 1640, 49);
            for (int j = 0; j < 4; ++j) MoveText(r, names[j], x[j], 0, widths[j], 45, j == 0 ? 25 : 22, Ink, "—");
            var profile = r.Find("Image").GetComponent<Image>(); Pose(profile.transform, -705, 0, 44, 44); profile.preserveAspect = true;
            var extra = r.Cast<Transform>().First(t => t.childCount >= 3 && t.GetComponentsInChildren<Image>(true).Length >= 3);
            Fixed(extra); Pose(extra, 650, 0, 140, 48);
            // Old static portraits did not represent the ranked player's character usage.
            // Preserve the original objects and sprite references; replace only that misleading presentation.
            foreach (var image in extra.GetComponentsInChildren<Image>(true)) image.enabled = false;
            var rate = Label(extra, "WinRate", "—", 0, 0, 140, 46, 22, Ink);
            view.rankingRows[i] = new CareerTabsPresentation.RankingRow { surface = r.GetComponent<Image>(), rate = rate, tier = Icon(r, "TierEmblem", view.tiers[0], -335, 0, 44) };
            Box(r, "RankStripe", -818, 0, 4, 49, i < 3 ? new Color32(183, 147, 67, 255) : new Color(1, 1, 1, .5f));
        }
        var footer = list.Find("Player Item"); Pose(footer, 0, -340, 1640, 48); footer.GetComponent<Image>().color = Ink;
        view.ownRank = Label(footer, "OwnRank", "내 순위는 현재 목록에 표시되면 강조됩니다.", 0, 0, 1570, 44, 21, Color.white);
        var allButtons = root.GetComponentsInChildren<Button>(true);
        var modes = allButtons.Where(b => b.GetComponentInChildren<TMP_Text>(true)?.text is "종합 랭킹" or "캐릭터 랭킹").ToArray();
        var group = modes[0].transform.parent; Fixed(group); Pose(group, -580, 285, 480, 48);
        for (int i = 0; i < modes.Length; ++i)
        {
            Pose(modes[i].transform, -120 + i * 240, 0, 232, 48); StyleButton(modes[i]);
            var t = modes[i].GetComponentInChildren<TMP_Text>(true); Pose(t.transform, 0, 0, 222, 42); Style(t, 23, Color.white, TextAlignmentOptions.Center);
        }
        var input = root.GetComponentInChildren<TMP_InputField>(true); Pose(input.transform, 590, 285, 460, 48); input.image.color = new Color32(235, 237, 234, 230);
        Style(input.textComponent, 20, Ink); Style((TMP_Text)input.placeholder, 20, Muted);
        var pageButtons = allButtons.Where(b => new[] { "|<", "<", ">", ">|" }.Contains(b.GetComponentInChildren<TMP_Text>(true)?.text)).ToArray();
        var pages = pageButtons[0].transform.parent; Fixed(pages); Pose(pages, 560, -412, 520, 44);
        for (int i = 0; i < pageButtons.Length; ++i)
        {
            Pose(pageButtons[i].transform, new float[] { -225, -160, 160, 225 }[i], 0, 58, 42); pageButtons[i].image.color = Ink; StyleButton(pageButtons[i]);
            var t = pageButtons[i].GetComponentInChildren<TMP_Text>(true); Pose(t.transform, 0, 0, 50, 38); Style(t, 22, Color.white, TextAlignmentOptions.Center);
        }
        var pageLabel = pages.Cast<Transform>().Select(t => t.GetComponent<TMP_Text>()).First(t => t != null); Pose(pageLabel.transform, 0, 0, 180, 42); Style(pageLabel, 23, Ink, TextAlignmentOptions.Center);
        view.rankingCount = Label(list, "RankingCount", "랭킹 불러오는 중", -330, -412, 500, 44, 21, Muted);
        view.rankingEmpty = Label(list, "RankingEmpty", "랭킹을 불러오는 중입니다.", 0, -20, 900, 70, 28, Muted, TextAlignmentOptions.Center);
        var selector = Find(root, "Career Ranking Character Selector"); Pose(selector, -30, 285, 360, 48); selector.SetAsLastSibling();
        var selected = Find(selector, "SelectedCharacterButton");
        Pose(selected.Find("Icon"), -145, 0, 44, 44);
        MoveText(selected, "Label", 0, 0, 230, 44, 23, Color.white);
        Pose(selected.Find("Arrow"), 145, 0, 28, 44);
        Pose(Find(selector, "CharacterPopup"), 0, -172, 260, 260);
    }

    static void BuildHistory(Transform root, CareerTabsPresentation view)
    {
        Backdrop(root, "경기 전적", "경기를 선택해 팀별 기록과 MVP를 확인하세요");
        var dropdown = root.Find("Mode Dropdown"); Pose(dropdown, -630, 258, 380, 48); dropdown.GetComponent<Image>().color = Ink;
        var scroll = Find(root, "Map Group"); Pose(scroll, -630, -94, 380, 628);
        var viewport = Find(scroll, "Viewport"); viewport.GetComponent<Image>().color = Color.white;
        var mask = viewport.GetComponent<Mask>(); if (mask != null) mask.showMaskGraphic = false;
        var content = Find(viewport, "Content") as RectTransform; Fixed(content);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(374, 720);
        var cards = content.Cast<Transform>().Where(t => t.name.StartsWith("Map Item")).ToArray();
        view.matchCards = new CareerTabsPresentation.MatchCard[cards.Length];
        for (int i = 0; i < cards.Length; ++i)
        {
            var r = cards[i]; Pose(r, 0, -58 - i * 120, 368, 110);
            var rr = (RectTransform)r; rr.anchorMin = rr.anchorMax = new Vector2(.5f, 1);
            var picture = r.Find("Map Image").GetComponent<Image>(); Pose(picture.transform, 119, 0, 128, 108); picture.preserveAspect = true; picture.color = new Color(1, 1, 1, .33f);
            if (view.desertArtwork == null) view.desertArtwork = picture.sprite;
            var strip = r.Find("Image"); Pose(strip, 0, 31, 354, 36); strip.GetComponent<Image>().color = Color.clear;
            var title = strip.GetComponentInChildren<TMP_Text>(true); Pose(title.transform, 0, 0, 330, 36); Style(title, 22, Ink); title.text = "경기 기록";
            view.matchCards[i] = new CareerTabsPresentation.MatchCard { surface = r.GetComponent<Image>(), artwork = picture,
                accent = Box(r, "ResultAccent", -182, 0, 4, 110, Red), summary = Label(r, "MatchSummary", "—", 0, -21, 330, 55, 21, Ink) };
            StyleButton(r.GetComponent<Button>());
        }
        var header = root.Cast<Transform>().First(t => t.Find("Text (TMP) (5)") != null && t.name == "Image");
        Pose(header, 240, 243, 1160, 108); header.GetComponent<Image>().color = Paper;
        view.matchArtwork = header.Find("Image").GetComponent<Image>(); Pose(view.matchArtwork.transform, -474, 0, 206, 106); view.matchArtwork.preserveAspect = true;
        MoveText(header, "Text (TMP) (1)", -216, 24, 270, 43, 27, Ink, "—");
        MoveText(header, "Text (TMP) (2)", 370, 24, 80, 35, 18, Muted, "시간");
        MoveText(header, "Text (TMP) (3)", 470, 24, 110, 35, 24, Ink, "00:00");
        MoveText(header, "Text (TMP) (4)", 540, 24, 20, 35, 18, Muted, "");
        MoveText(header, "Text (TMP) (5)", 375, -26, 350, 35, 17, Muted, "매치 번호: —");
        view.matchResult = Label(header, "MatchResult", "경기 기록 없음", 103, 24, 320, 44, 31, Red);
        view.matchDate = Label(header, "MatchDate", "", -95, -26, 510, 35, 18, Muted);
        var columns = root.Find("Image (6)"); Pose(columns, 240, 150, 1160, 42); columns.GetComponent<Image>().color = Ink;
        string[] names = { "Text (TMP)", "Text (TMP) (1)", "Text (TMP) (2)", "Text (TMP) (5)", "Text (TMP) (6)", "Text (TMP) (3)", "Text (TMP) (4)" };
        float[] x = { -363, -172, -58, 56, 170, 308, 470 }; float[] width = { 275, 108, 108, 108, 108, 145, 150 };
        string[] captions = { "플레이어", "활약", "공격", "스킬", "협력", "K / D / A", "피해량" };
        for (int i = 0; i < names.Length; ++i) MoveText(columns, names[i], x[i], 0, width[i], 40, 19, Color.white, captions[i]);
        int side = 0;
        foreach (string groupName in new[] { "Friendly forces", "Opponent" })
        {
            var group = root.Find(groupName); Fixed(group); Pose(group, 240, -9 - side * 277, 1160, 230);
            Label(group, "TeamCaption", side == 0 ? "우리 팀" : "상대 팀", 0, 133, 1158, 25, 18, side == 0 ? new Color32(55, 101, 88, 255) : Red);
            var rows = group.Cast<Transform>().Where(t => t.name.StartsWith("Player Item")).ToArray();
            for (int r = 0; r < rows.Length; ++r)
            {
                var row = rows[r]; Pose(row, 0, 97 - r * 46, 1160, 44); row.GetComponent<Image>().color = new Color32(241, 241, 237, 180);
                for (int i = 0; i < names.Length; ++i) MoveText(row, names[i], x[i], 0, width[i], 41, i == 0 ? 20 : 19, Ink, "—");
                var portrait = row.Find("Image").GetComponent<Image>(); Pose(portrait.transform, -550, 0, 42, 42); portrait.preserveAspect = true;
                MoveText(row, "Text (TMP) (7)", -266, 0, 58, 36, 16, Red, "");
                var nickname = row.Find("Text (TMP)"); Pose(nickname, -405, 0, 220, 41);
                Box(row, "TeamStripe", -578, 0, 3, 44, side == 0 ? new Color32(83, 127, 111, 255) : Red);
            }
            side++;
        }
        view.historyEmpty = Label(root, "HistoryEmpty", "이 조건의 경기 기록이 없습니다.", 230, -36, 1050, 65, 27, Muted, TextAlignmentOptions.Center);
    }

    static void BuildInventory(Transform root, CareerTabsPresentation view)
    {
        var originalImages = root.Cast<Transform>().ToArray();
        var grid = originalImages.OrderByDescending(t => t.childCount).First();
        var heading = originalImages.First(t => t.childCount == 2);
        var rail = originalImages.First(t => t.childCount == 4);
        var preview = originalImages.First(t => t.GetComponentInChildren<TMP_Text>(true)?.text == "준비 중" || t.GetComponentInChildren<TMP_Text>(true)?.name == "InventoryPreviewCaption");
        var divider = originalImages.First(t => ((RectTransform)t).sizeDelta.y < 10);
        Backdrop(root, "인벤토리", "보유 아이템의 수량과 획득 정보를 확인하세요");
        Fixed(grid); Pose(grid, -570, -83, 500, 600); view.inventoryGrid = grid;
        Pose(heading, -570, 251, 500, 42); heading.GetComponent<Image>().color = Ink;
        MoveText(heading, "Text (TMP)", -110, 0, 230, 38, 21, Color.white, "보유 아이템");
        view.inventoryCount = MoveText(heading, "Text (TMP) (1)", 160, 0, 160, 38, 21, Color.white, "보유 0종");
        var slots = grid.Cast<Transform>().Where(t => t.GetComponent<Image>() != null).ToList();
        // The wire contract can deliver 20 items. Author all 20 now, not only the legacy 13 slots.
        while (slots.Count < 20) slots.Add(Box(grid, "Inventory Slot " + slots.Count, 0, 0, 110, 110, Ink).transform);
        view.inventorySlots = new CareerTabsPresentation.InventorySlot[slots.Count];
        for (int i = 0; i < slots.Count; ++i)
        {
            var slot = slots[i]; Pose(slot, -186 + (i % 4) * 124, 242 - (i / 4) * 120, 112, 110);
            var surface = slot.GetComponent<Image>(); surface.color = Ink; var button = Ensure<Button>(slot.gameObject); button.targetGraphic = surface; StyleButton(button);
            foreach (var effect in slot.GetComponents<Shadow>()) effect.enabled = false;
            view.inventorySlots[i] = new CareerTabsPresentation.InventorySlot { root = slot, surface = surface,
                icon = Icon(slot, "ItemIcon", view.starterBadge, 0, 6, 76), quantity = Label(slot, "Quantity", "", 0, -40, 94, 25, 18, Color.white, TextAlignmentOptions.MidlineRight),
                fallback = Label(slot, "ItemFallback", "", 0, 8, 100, 75, 17, Color.white, TextAlignmentOptions.Center) };
            Box(slot, "ItemUnderline", 0, -53, 112, 3, new Color32(162, 170, 159, 255));
        }
        Fixed(rail); Pose(rail, -570, -403, 500, 43);
        view.inventorySortButtons = new Button[4]; string[] sorts = { "기본순", "최근순", "수량순", "이름순" };
        var railSlots = rail.Cast<Transform>().ToArray();
        for (int i = 0; i < 4; ++i)
        {
            Pose(railSlots[i], -186 + i * 124, 0, 116, 42); var b = Ensure<Button>(railSlots[i].gameObject); b.targetGraphic = railSlots[i].GetComponent<Image>(); StyleButton(b);
            foreach (var effect in railSlots[i].GetComponents<Shadow>()) effect.enabled = false;
            Label(b.transform, "SortLabel", sorts[i], 0, 0, 112, 40, 19, Color.white, TextAlignmentOptions.Center); view.inventorySortButtons[i] = b;
        }
        Pose(preview, 13, -2, 540, 590); var oldHint = preview.GetComponentInChildren<TMP_Text>(true); oldHint.name = "InventoryPreviewCaption";
        Pose(oldHint.transform, 0, -225, 480, 70); Style(oldHint, 25, Muted, TextAlignmentOptions.Center); oldHint.text = "보유 아이템 없음"; view.inventoryHint = oldHint;
        var light = Rect(preview, "PreviewLight", 0, 0, 540, 590); Ensure<CanvasRenderer>(light.gameObject);
        var graphic = Ensure<CareerStageGraphic>(light.gameObject); graphic.shape = CareerStageGraphic.Shape.LightColumn; graphic.color = new Color(1, 1, 1, .62f); graphic.raycastTarget = false; light.SetAsFirstSibling();
        view.inventoryPreview = Icon(preview, "ItemPreview", view.starterBadge, 0, 16, 290); view.inventoryPreview.enabled = false;
        view.inventoryFallback = Icon(preview, "EmptyEmblem", view.starterBadge, 0, 16, 138); view.inventoryFallback.color = new Color(1, 1, 1, .3f);
        MoveText(root, "Text (TMP)", 585, 211, 470, 100, 34, Ink, "보유 아이템 없음");
        Pose(divider, 585, 145, 470, 2);
        MoveText(root, "Text (TMP) (1)", 585, 12, 470, 220, 24, Muted, "보유한 아이템을 선택하면 상세 정보를 볼 수 있습니다.");
        view.inventoryMeta = Label(root, "InventoryMeta", "", 585, -242, 470, 145, 21, Ink);
        Label(root, "InventoryScope", "커리어 보관함", 585, -395, 470, 40, 19, Muted);
        view.inventoryEmpty = Label(root, "InventoryEmpty", "아직 보유한 아이템이 없습니다.", -570, -25, 470, 95, 23, Muted, TextAlignmentOptions.Center);
    }

    static void BuildAchievements(Transform root, CareerTabsPresentation view)
    {
        var oldNotice = root.GetComponentInChildren<TMP_Text>(true);
        Backdrop(root, "시즌 업적", "현재 시즌의 랭크 기록을 모아보세요");
        Pose(oldNotice.transform, -350, -401, 960, 45); Style(oldNotice, 18, Muted);
        oldNotice.text = "시즌 기록 기준 · 시즌이 변경되면 진행도도 새 시즌 기준으로 표시됩니다.";
        view.milestoneSeason = Label(root, "MilestoneSeason", "시즌 기록 불러오는 중", -340, 253, 950, 46, 28, Ink);
        view.milestoneSummary = Label(root, "MilestoneSummary", "—", 574, 253, 450, 46, 23, Red, TextAlignmentOptions.MidlineRight);
        view.milestoneFilters = new Button[3]; string[] filters = { "전체", "진행 중", "달성" };
        for (int i = 0; i < 3; ++i) view.milestoneFilters[i] = MakeButton(root, "MilestoneFilter" + i, filters[i], -723 + i * 158, 193, 150, 40);
        view.milestones = new CareerTabsPresentation.Milestone[8];
        string[] titles = { "첫 발걸음", "꾸준한 출전", "첫 승리", "승리의 기록", "정밀한 타격", "전장의 해결사", "누적 화력", "최고의 선수" };
        string[] descriptions = { "랭크 경기 5회 참여", "랭크 경기 30회 참여", "랭크 경기 첫 승리", "랭크 경기 20승 달성", "랭크 경기에서 100회 처치", "랭크 경기에서 500회 처치", "랭크 경기 누적 피해 100,000 달성", "랭크 경기 MVP 10회 달성" };
        int[] metrics = { 0, 0, 1, 1, 2, 2, 3, 4 }, targets = { 5, 30, 1, 20, 100, 500, 100000, 10 };
        string[] arts = { "StatMatches", "StatMatches", "StatWin", "StatWin", "StatKills", "StatKills", "StatDamage", "StatMvp" };
        for (int i = 0; i < 8; ++i)
        {
            var card = Box(root, "Milestone" + i, -712 + (i % 4) * 232, 26 - (i / 4) * 255, 214, 230, Paper);
            var b = Ensure<Button>(card.gameObject); b.targetGraphic = card; StyleButton(b);
            var icon = Icon(card.transform, "Medal", Art(arts[i]), 0, 48, 78);
            Label(card.transform, "Title", titles[i], 0, -21, 192, 48, 23, Ink, TextAlignmentOptions.Center);
            view.milestones[i] = new CareerTabsPresentation.Milestone { title = titles[i], description = descriptions[i], metric = metrics[i], target = targets[i], button = b, icon = icon,
                progress = Bar(card.transform, "Progress", 0, -98, 176), status = Label(card.transform, "Status", "—", 0, -65, 190, 35, 20, Muted, TextAlignmentOptions.Center) };
            Box(card.transform, "MedalRule", 0, 113, 214, 2, new Color(1, 1, 1, .9f));
        }
        var detail = Box(root, "MilestoneDetail", 568, -92, 504, 560, new Color32(211, 214, 213, 195)).transform;
        Box(detail, "TopAccent", -248, 260, 4, 40, Red);
        view.milestoneTitle = Label(detail, "Title", "업적을 선택하세요", 0, 234, 448, 58, 32, Ink);
        view.milestoneIcon = Icon(detail, "Medal", Art("StatMatches"), 0, 114, 128);
        view.milestoneDescription = Label(detail, "Description", "현재 시즌의 랭크 경기 기록으로 계산됩니다.", 0, -32, 448, 135, 23, Muted);
        view.milestoneValue = Label(detail, "Value", "—", 0, -153, 448, 57, 37, Ink);
        view.milestoneProgress = Bar(detail, "Progress", 0, -199, 448);
        view.milestoneState = Label(detail, "State", "기록 불러오는 중", 0, -238, 448, 45, 23, Red);
    }

    public static void Verify()
    {
        VectorCareerPresentationEditor.Verify();
        var scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity"); var c = Controller(scene);
        var v = Ref<CareerTabsPresentation>(c, "_tabsPresentation"); Call(c, "Awake");
        var career = Ref<GameObject>(c, "_rankingPanel").transform.parent;
        int objects = career.GetComponentsInChildren<Transform>(true).Length, components = career.GetComponentsInChildren<Component>(true).Length;
        var fixture = new CareerOverviewData { seasonId = 1, seasonName = "시즌 01", remainingSeasonDays = 42, tierName = "Gold 3", rankImageId = 2,
            rankPoints = 1264, tierMinPoints = 1200, tierMaxPoints = 1299, rankedMatches = 38, wins = 23, roundsPlayed = 456, kills = 541, totalDamage = 153804, mvpCount = 8 };
        c.ApplyOverview(fixture); c.OnAchievementsButton();
        v.milestoneFilters[1].onClick.Invoke();
        Require(v.milestones.Count(m => m.button.gameObject.activeSelf) == 1, "In-progress achievement filter");
        v.milestones[7].button.onClick.Invoke(); Require(v.milestoneValue.text.Contains("8"), "MVP milestone selection");
        v.milestoneFilters[2].onClick.Invoke(); Require(v.milestones.Count(m => m.button.gameObject.activeSelf) == 7, "Completed achievement filter");
        v.milestoneFilters[0].onClick.Invoke(); v.milestones[7].button.onClick.Invoke();
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-Achievements.png");
        fixture.rankedMatches = fixture.wins = fixture.roundsPlayed = fixture.kills = fixture.totalDamage = fixture.mvpCount = 0;
        c.ApplyOverview(fixture); v.milestoneFilters[2].onClick.Invoke(); Require(v.milestones.All(m => !m.button.gameObject.activeSelf), "Empty completion filter");
        v.milestoneFilters[0].onClick.Invoke();
        c.OnRankingButton();
        var page = new CareerRankingPageData { page = 1, totalPages = 3, totalEntries = 24, rankingType = 0, entries = Enumerable.Range(1, 10).Select(i =>
            new CareerRankingEntryData { position = i, nickname = i == 4 ? "VECTOR" : $"플레이어 {i:00}", userId = i == 4 ? "fixture-owner" : "fixture-" + i,
                playerIconId = (i - 1) % 3, rankImageId = i <= 3 ? 2 : 1, tierName = i <= 3 ? "Gold 3" : "Silver 1", rankedMatches = 50, wins = 42 - i }).ToList() };
        c.ApplyRanking(page); v.ApplyRanking(page, "fixture-owner");
        Require(v.rankingRows[0].rate.text == "82%" && v.ownRank.text.Contains("4위"), "Win rate / own row");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-Ranking.png");
        Field<Button>(c, "_characterRankingButton").onClick.Invoke();
        page.rankingType = 1; page.characterId = 0;
        c.ApplyRanking(page); Require(v.rankingRows.All(r => !r.tier.enabled), "Character score must not display rank badges");
        Field<Button>(c, "_selectedCharacterButton").onClick.Invoke();
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-CharacterRanking.png");
        Field<Button[]>(c, "_characterButtons")[2].onClick.Invoke();
        c.ApplyRanking(new CareerRankingPageData { rankingType = 1, characterId = 2, page = 1, totalPages = 1, totalEntries = 0 });
        Require(v.rankingEmpty.gameObject.activeSelf, "Ranking empty state");
        c.OnCareerRecordButton();
        var history = Enumerable.Range(0, 6).Select(i => new CareerMatchData { matchId = (ulong)(2103 + i), mapName = i % 2 == 0 ? "Desert" : "TestMap",
            startedAtUnix = 1790983200 - i * 3600, durationSeconds = 812 - i * 30, scoreA = 7, scoreB = (byte)(4 + i % 3), myTeam = (byte)(i % 2), winnerTeam = 0,
            players = Enumerable.Range(0, 6).Select(p => new CareerMatchPlayerData { userId = p == 0 ? "fixture-owner" : "fixture-player" + p,
                nickname = p == 0 ? "VECTOR" : $"플레이어 {p:00}", characterId = p % 4, team = (byte)(p / 3), kills = 15 - p, deaths = 5 + p,
                assists = 4 + p, damage = 3410 - p * 170, combatScore = 301 - p * 19, attackScore = 170 - p * 11, skillScore = 80 - p * 3, teamworkScore = 51 - p * 5, isMvp = p == 0 }).ToList() }).ToList();
        c.ApplyMatchHistory(0, history);
        Require(v.matchCards[0].artwork.enabled && !v.matchCards[1].artwork.enabled, "Map art must not mislabel TestMap as Desert");
        var friendly = Field<List<Transform>>(c, "_friendlyPlayerRows");
        Require(friendly[0].Find("Image").GetComponent<Image>().sprite == v.portraits[0], "Character portrait binding");
        Require(friendly[0].Find("Text (TMP) (3)").GetComponent<TMP_Text>().text == "15/5/4", "KDA binding");
        Require(friendly[0].Find("Text (TMP) (7)").GetComponent<TMP_Text>().text == "MVP", "MVP binding");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-History.png");
        Field<List<Transform>>(c, "_matchMapRows")[1].GetComponent<Button>().onClick.Invoke(); Require(v.matchResult.text.StartsWith("패배"), "History card selection");
        var mode = Field<TMP_Dropdown>(c, "_matchModeDropdown"); mode.value = 2;
        c.ApplyMatchHistory(0, history); Require(v.historyEmpty.gameObject.activeSelf, "Stale history response rejected");
        c.ApplyMatchHistory(2, history); Require(!v.historyEmpty.gameObject.activeSelf, "Ranked history binding");
        c.ApplyMatchHistory(2, new List<CareerMatchData>()); Require(!v.matchArtwork.enabled && v.historyEmpty.gameObject.activeSelf, "Empty history clears previous match");
        c.OnInventoryButton();
        var items = Enumerable.Range(1, 20).Select(i => new CareerInventoryEntryData { itemId = i, itemName = i == 1 ? "커리어 시작 배지" : $"보관 아이템 {i:00}",
            description = i == 1 ? "커리어의 시작을 기념하는 배지입니다." : "보관함에서 아이템의 보유 수량과 획득일을 확인할 수 있습니다.", iconId = i == 1 ? 0 : 99, quantity = i, acquiredAtUnix = 1790983200 + i * 1000 }).ToList();
        c.ApplyInventory(items); Require(Field<List<Transform>>(c, "_inventorySlots").Count == 20, "Full 20-item wire capacity");
        v.inventorySortButtons[2].onClick.Invoke(); Require(Field<List<CareerInventoryEntryData>>(c, "_inventory")[0].quantity == 20, "Quantity sort");
        v.inventorySortButtons[1].onClick.Invoke(); Require(Field<List<CareerInventoryEntryData>>(c, "_inventory")[0].itemId == 20, "Newest sort");
        v.inventorySortButtons[3].onClick.Invoke(); Require(Field<List<CareerInventoryEntryData>>(c, "_inventory")[0].itemName == "보관 아이템 02", "Name sort");
        v.inventorySortButtons[0].onClick.Invoke();
        Field<List<Transform>>(c, "_inventorySlots")[19].GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(c, "_inventoryNameText").text.Contains("20") && !v.inventoryPreview.enabled, "Last inventory slot / unknown icon fallback");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-InventoryCapacity.png");
        // Presentation capture uses a single known catalog item, not invented live ownership.
        c.ApplyInventory(items.Take(1).ToList());
        Require(v.inventoryPreview.sprite == v.starterBadge && v.inventoryPreview.enabled, "Inventory preview binding");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-Inventory.png");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-Inventory720.png", width: 1280, height: 720);
        c.ApplyInventory(new List<CareerInventoryEntryData>());
        Require(v.inventoryEmpty.gameObject.activeSelf && !v.inventoryPreview.enabled && v.inventoryMeta.text == "", "Inventory empty clears selection");
        VectorCareerPresentationEditor.CaptureFullLobby(c, "VectorCareerNew-InventoryEmpty.png");
        Call(c, "CacheDynamicViews");
        Require(objects == career.GetComponentsInChildren<Transform>(true).Length && components == career.GetComponentsInChildren<Component>(true).Length, "No runtime UI object/component creation");
        Debug.Log("CAREER_TABS_VERIFIED: authored layout; milestone selection/3 filters/season reset; ranking badges/own row/empty state; 6-match selection/map/portraits/KDA/MVP/stale response; 20 inventory slots/4 sorts/fallback/empty state; full header and pointer checks. Fixtures NOT saved.");
    }
    static void Require(bool value, string message)
    {
        if (!value) throw new Exception("Career tab regression: " + message);
    }
}
