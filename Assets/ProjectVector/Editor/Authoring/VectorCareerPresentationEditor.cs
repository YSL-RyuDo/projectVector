using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class VectorCareerPresentationEditor
{
    const string Lobby = "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity";
    static T Ref<T>(Object owner, string field) where T : Object =>
        new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static CareerPanelController Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new Exception("Unsaved scene: refused.");
        var scene = EditorSceneManager.OpenScene(Lobby);
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CareerPanelController>(true)).Single();
    }
    public static void Inspect()
    {
        var controller = Open();
        var rank = Ref<GameObject>(controller, "_rankPanel");
        var root = rank.transform.parent;
        var lines = root.GetComponentsInChildren<Transform>(true).Select(t =>
        {
            var rect = t as RectTransform;
            var text = t.GetComponent<TMP_Text>();
            var img = t.GetComponent<Image>();
            var button = t.GetComponent<Button>();
            string path = t.name; for (var p = t.parent; p != null && p != root.parent; p = p.parent) path = p.name + "/" + path;
            return path + " | active=" + t.gameObject.activeSelf +
                (rect == null ? "" : $" | pos={rect.anchoredPosition} size={rect.sizeDelta} anchors={rect.anchorMin}-{rect.anchorMax} scale={rect.localScale}") +
                (text == null ? "" : $" | text={text.text.Replace('\n', '|')} font={AssetDatabase.GetAssetPath(text.font)} size={text.fontSize}") +
                (img == null ? "" : $" | sprite={AssetDatabase.GetAssetPath(img.sprite)} color={img.color}") +
                (button == null ? "" : " | click=" + string.Join(",", Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(i => button.onClick.GetPersistentMethodName(i))));
        });
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "VectorCareerHierarchy.txt"), lines);
        Capture(root.gameObject, "VectorCareerBefore.png");
        Debug.Log("CAREER_INSPECTED " + root.name);
    }

    static readonly Color Ink = new Color32(39, 41, 45, 255), Muted = new Color32(86, 89, 94, 255);
    static readonly Color Red = new Color32(177, 28, 61, 255), Paper = new Color32(219, 220, 217, 255);
    static TMP_FontAsset font;
    static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static void Pose(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
    }
    static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
    {
        var t = p.Find(n) as RectTransform;
        if (t == null) { t = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); t.SetParent(p, false); }
        t.gameObject.layer = 5; Pose(t, x, y, w, h); return t;
    }
    static Image Box(Transform p, string n, float x, float y, float w, float h, Color color)
    {
        var image = Ensure<Image>(Rect(p, n, x, y, w, h).gameObject); image.color = color; image.raycastTarget = false; return image;
    }
    static TMP_Text Label(Transform p, string n, string value, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var text = Ensure<TextMeshProUGUI>(Rect(p, n, x, y, w, h).gameObject);
        Style(text, size, color, align); text.text = value; return text;
    }
    static void Style(TMP_Text text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        text.font = font; text.fontSize = size; text.color = color; text.alignment = align;
        text.richText = true; text.raycastTarget = false; text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
    }
    static void Decoration(Transform p, string n, float x, float y, float w, float h, CareerStageGraphic.Shape shape, Color color)
    {
        var rect = Rect(p, n, x, y, w, h); Ensure<CanvasRenderer>(rect.gameObject);
        var g = Ensure<CareerStageGraphic>(rect.gameObject); g.shape = shape; g.color = color; g.raycastTarget = false;
    }
    static void ButtonStyle(Button button, float x, float y, float w, float h, string title)
    {
        Pose(button.transform, x, y, w, h); button.image.color = Ink; button.image.type = Image.Type.Simple;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
        colors.pressedColor = new Color(.75f, .75f, .75f); colors.selectedColor = Color.white; button.colors = colors;
        var text = button.GetComponentInChildren<TMP_Text>(true); text.text = title;
        Pose(text.transform, 0, 0, w - 24, h - 8); Style(text, 22, Color.white);
        Box(button.transform, "CareerAccent", -w / 2 + 2, 0, 4, h, Red);
    }
    static Button Button(Transform p, string name, string label, float x, float y, float w, float h)
    {
        var image = Box(p, name, x, y, w, h, Ink); image.raycastTarget = true;
        var button = Ensure<Button>(image.gameObject); button.targetGraphic = image;
        Label(button.transform, "Label", label, 0, 0, w, h, 22, Color.white);
        ButtonStyle(button, x, y, w, h, label); return button;
    }
    static void Wire(Object owner, string field, Object value)
    {
        var so = new SerializedObject(owner); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    [MenuItem("Project Vector/UI/Install Authored Career Presentation")]
    public static void InstallAndVerify()
    {
        var controller = Open(); var scene = controller.gameObject.scene;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var preserved = new VectorExperiencePolishEditor.Preservation(scene);
        var rank = Ref<GameObject>(controller, "_rankPanel"); var root = rank.transform.parent;
        var view = Ensure<CareerRankPresentation>(rank);
        Wire(controller, "_rankPresentation", view);
        view.rankSprites = new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" }
            .Select(n => Sprite("Assets/ProjectVector/Shared/Resources/Rank/" + n + ".png")).ToArray();
        view.characterSprites = Enumerable.Range(0, 4).Select(CareerRankingPrefabBuilder.LoadCharacterSprite).ToArray();
        var backdrop = Box(rank.transform, "CareerBackdrop", 0, 0, 1920, 1080, Color.white);
        backdrop.sprite = Sprite("Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/NeutralBackground.png"); backdrop.transform.SetAsFirstSibling();
        var scenery = Rect(rank.transform, "CareerStage", 0, 0, 1920, 1080); scenery.SetSiblingIndex(1);
        Decoration(scenery, "Light", 0, 40, 520, 950, CareerStageGraphic.Shape.LightColumn, new Color(1, 1, 1, .56f));
        Decoration(scenery, "Podium", 0, -490, 1740, 240, CareerStageGraphic.Shape.Podium, new Color32(227, 228, 225, 255));
        Box(scenery, "UpperRule", 0, 295, 1640, 1, new Color(1, 1, 1, .5f));
        view.seasonName = Label(rank.transform, "CurrentSeason", "시즌 정보 불러오는 중", -655, 330, 330, 45, 25, Ink);
        var seasonNotice = Ref<TMP_Text>(controller, "_seasonNoticeText"); Pose(seasonNotice.transform, 590, 330, 460, 45); Style(seasonNotice, 21, Muted, TextAlignmentOptions.MidlineRight);
        view.seasonNotice = seasonNotice;
        seasonNotice.text = "시즌 정보를 불러오는 중입니다";
        view.emblem = Ref<Image>(controller, "_rankImage"); Pose(view.emblem.transform, 0, 65, 450, 450); view.emblem.preserveAspect = true;
        var info = Find(rank.transform, "Rank Info"); Pose(info, 0, -245, 520, 135); info.GetComponent<Image>().color = Color.clear;
        var rankName = Ref<TMP_Text>(controller, "_rankNameText"); Pose(rankName.transform, 0, 35, 520, 65); Style(rankName, 48, Ink); rankName.text = "—";
        var pointText = Ref<TMP_Text>(controller, "_rankPointText"); Pose(pointText.transform, 148, -44, 224, 34); Style(pointText, 23, Ink, TextAlignmentOptions.MidlineRight);
        var caption = info.Find("Text (TMP) (1)").GetComponent<TMP_Text>(); Pose(caption.transform, -140, -44, 240, 34); Style(caption, 20, Muted, TextAlignmentOptions.MidlineLeft); caption.text = "랭크 포인트";
        var slider = Ref<Slider>(controller, "_rankPointSlider"); Pose(slider.transform, 0, -10, 520, 18); slider.interactable = false;
        slider.fillRect.GetComponent<Image>().color = Red;
        view.nextRank = Label(rank.transform, "NextRank", "랭크 정보를 불러오는 중입니다", 0, -340, 560, 38, 23, Ink, TextAlignmentOptions.Center);
        view.totalPoints = Label(rank.transform, "TotalPoints", "누적 포인트 —", 0, -378, 560, 32, 18, Muted, TextAlignmentOptions.Center);

        string[] stats = { "_rankCountText", "_roundKillCountText", "_winRateText", "_avgDamageText", "_mvpCountText" };
        string[] art = { "StatMatches", "StatKills", "StatWin", "StatDamage", "StatMvp" };
        view.statisticTexts = stats.Select(n => Ref<TMP_Text>(controller, n)).ToArray();
        var left = Find(rank.transform, "Rank Statistics Group(Left)"); var right = Find(rank.transform, "Rank Statistics Group(Right)");
        Pose(left, -625, -120, 490, 700); Pose(right, 625, -120, 490, 700);
        for (int i = 0; i < stats.Length; ++i)
        {
            var text = view.statisticTexts[i]; var pillar = text.transform.parent; int col = i < 3 ? i : i - 3;
            Pose(pillar, -164 + col * 164, -195, 110, 400);
            var oldImage = pillar.GetComponent<Image>(); oldImage.color = Color.clear;
            Decoration(pillar, "PillarSurface", 0, 0, 110, 400, CareerStageGraphic.Shape.Pillar, new Color32(63, 65, 69, 150));
            pillar.Find("PillarSurface").SetAsFirstSibling();
            Box(pillar, "PillarCap", 0, 200, 110, 3, new Color(1, 1, 1, .7f));
            Pose(text.transform, 0, 235, 156, 96); Style(text, 22, Ink);
            var icon = pillar.GetComponentsInChildren<Image>(true).First(img => img != oldImage && img.name != "PillarCap");
            preserved.AllowSpriteChange(icon); icon.sprite = Sprite("Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/" + art[i] + ".png");
            icon.preserveAspect = true; Pose(icon.transform, 0, 330, 78, 78);
            foreach (var effect in icon.GetComponents<Shadow>()) effect.enabled = false;
        }
        var recentPillar = right.Find("Image (1)"); Pose(recentPillar, 164, -195, 110, 400); recentPillar.GetComponent<Image>().color = Color.clear;
        Decoration(recentPillar, "PillarSurface", 0, 0, 110, 400, CareerStageGraphic.Shape.Pillar, new Color32(63, 65, 69, 150)); recentPillar.Find("PillarSurface").SetAsFirstSibling();
        var recent = recentPillar.Find("Image"); Pose(recent, 0, 280, 126, 270);
        view.recentCharacters = recent.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < view.recentCharacters.Length; ++i) { Pose(view.recentCharacters[i].transform, 0, 90 - i * 90, 76, 76); view.recentCharacters[i].preserveAspect = true; }
        view.recentCaption = recent.GetComponentInChildren<TMP_Text>(true); Pose(view.recentCaption.transform, 0, -182, 158, 70); Style(view.recentCaption, 21, Ink);
        AlignRecentCharacters(view);
        view.rankButton = Ref<Button>(controller, "_rankIconInfoButton"); view.seasonButton = Ref<Button>(controller, "_seasonRewardButton");
        ButtonStyle(view.rankButton, -134, -458, 248, 60, "랭크 안내"); ButtonStyle(view.seasonButton, 134, -458, 248, 60, "시즌 보상");
        view.revealGroups = new[] { Ensure<CanvasGroup>(view.emblem.gameObject), Ensure<CanvasGroup>(info.gameObject), Ensure<CanvasGroup>(left.gameObject), Ensure<CanvasGroup>(right.gameObject) };
        foreach (var g in view.revealGroups) g.alpha = 1;
        BuildGuides(view, rank.transform);
        var settings = new SerializedObject(controller); settings.FindProperty("_buttonPressColor").colorValue = Red; settings.ApplyModifiedPropertiesWithoutUndo();
        var tabs = Find(root, "Tab Button Group");
        foreach (var b in tabs.GetComponentsInChildren<Button>(true))
        {
            var cb = b.colors; cb.normalColor = cb.selectedColor = Color.white; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); b.colors = cb;
            b.image.color = b == Ref<Button>(controller, "_rankButton") ? Red : Ink;
        }
        MigrateRanking(controller);
        view.Clear();
        // Keep representative artwork visible in the editor; runtime Start clears pending server data.
        view.emblem.enabled = true; view.seasonEmblem.sprite = view.rankSprites[0]; view.seasonEmblem.enabled = true;
        foreach (var component in root.GetComponentsInChildren<Component>(true)) if (component != null) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
        EditorUtility.SetDirty(controller); preserved.Check();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        // The other authored lobby uses the same controller. Preserve its existing layout while migrating the selector.
        var other = EditorSceneManager.OpenScene("Assets/ProjectVector/Development/Legacy/Scenes/Lobby2.unity");
        var otherController = other.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CareerPanelController>(true)).Single();
        var otherPreserved = new VectorExperiencePolishEditor.Preservation(other); MigrateRanking(otherController); otherPreserved.Check();
        EditorSceneManager.MarkSceneDirty(other); EditorSceneManager.SaveScene(other);
        AssetDatabase.SaveAssets();
        Verify();
    }

    static GameObject Overlay(Transform root, string name, string title, out Button close)
    {
        var image = Box(root, name, 0, 0, 1920, 1080, Color.white);
        image.sprite = Sprite("Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/NeutralBackground.png"); image.raycastTarget = true;
        Label(image.transform, "Title", title, -450, 310, 720, 70, 44, Ink);
        Box(image.transform, "Rule", 0, 257, 1640, 2, new Color(1, 1, 1, .8f));
        close = Button(image.transform, "Close", "닫기  ×", 715, 312, 210, 54);
        return image.gameObject;
    }
    static void BuildGuides(CareerRankPresentation view, Transform rank)
    {
        view.rankGuide = Overlay(rank, "RankGuide", "랭크 안내", out view.rankClose);
        view.guideCurrentRank = Label(view.rankGuide.transform, "CurrentRank", "현재 랭크 —", 0, 185, 1600, 45, 24, Muted);
        string[] names = { "브론즈", "실버", "골드", "플래티넘", "다이아몬드", "마스터" };
        view.rankGuideHighlights = new Image[6];
        for (int i = 0; i < 6; ++i)
        {
            float x = -675 + 270 * i;
            var card = Rect(view.rankGuide.transform, "RankGroup" + i, x, -20, 244, 360);
            Decoration(card, "Pedestal", 0, -110, 200, 130 + i * 10, CareerStageGraphic.Shape.Pillar, new Color32(83, 85, 89, 150));
            var icon = Box(card, "Icon", 0, 80, 190, 190, Color.white); icon.sprite = view.rankSprites[i]; icon.preserveAspect = true;
            Label(card, "Name", names[i], 0, -46, 240, 45, 27, Ink, TextAlignmentOptions.Center);
            Label(card, "Divisions", i == 5 ? "순위 경쟁" : "V · IV · III · II · I", 0, -93, 240, 38, 19, Muted, TextAlignmentOptions.Center);
            view.rankGuideHighlights[i] = Box(card, "CurrentAccent", 0, -155, 188, 4, Red);
        }
        Label(view.rankGuide.transform, "RankRules", "포인트와 티어\n<size=21>경기 결과가 반영되면 현재 티어와 포인트가 갱신됩니다.</size>", -390, -340, 760, 100, 27, Ink);
        Label(view.rankGuide.transform, "SeasonRules", "나의 시즌 기록\n<size=21>랭크 경기 · 승률 · 라운드당 처치와 피해 · MVP를 확인하세요.</size>", 420, -340, 760, 100, 27, Ink);
        view.seasonGuide = Overlay(rank, "SeasonGuide", "시즌 보상", out view.seasonClose);
        view.seasonTitle = Find(view.seasonGuide.transform, "Title").GetComponent<TMP_Text>();
        var left = Rect(view.seasonGuide.transform, "SeasonRank", -500, -70, 600, 610);
        left.SetAsFirstSibling();
        Decoration(left, "Light", 0, 40, 580, 680, CareerStageGraphic.Shape.LightColumn, new Color(1, 1, 1, .75f));
        view.seasonEmblem = Box(left, "Emblem", 0, 100, 330, 330, Color.white); view.seasonEmblem.preserveAspect = true;
        view.seasonSummary = Label(left, "Summary", "시즌 정보를 불러오는 중입니다.", 0, -155, 570, 130, 38, Ink, TextAlignmentOptions.Center);
        var panel = Box(view.seasonGuide.transform, "SeasonDetails", 345, -30, 840, 525, new Color(1, 1, 1, .35f));
        Label(panel.transform, "PeriodLabel", "시즌 일정", 0, 185, 730, 44, 25, Muted);
        view.seasonPeriod = Label(panel.transform, "Period", "종료 일정 —", 0, 105, 730, 100, 32, Ink);
        Box(panel.transform, "Divider", 0, 15, 730, 2, new Color(1, 1, 1, .85f));
        Label(panel.transform, "RewardTitle", "시즌 보상 안내", 0, -40, 730, 45, 28, Ink);
        Label(panel.transform, "RewardAvailability", "현재 시즌에는 등록된 보상이 없습니다.", 0, -115, 730, 75, 24, Muted);
        // No claim button or invented rewards: the existing protocol has no seasonal reward configuration/claim contract.
        view.CloseOverlays();
    }

    static void MigrateRanking(CareerPanelController controller)
    {
        var history = Ref<GameObject>(controller, "_careerRecordPanel");
        foreach (var row in history.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Map Item" || t.name.StartsWith("Map Item (", StringComparison.Ordinal)))
            Ensure<Button>(row.gameObject).targetGraphic = row.GetComponent<Image>();
        var inventory = Ref<GameObject>(controller, "_inventoryPanel");
        var grid = inventory.GetComponentsInChildren<Transform>(true).OrderByDescending(t => t.childCount).First();
        foreach (Transform child in grid) if (child.GetComponent<Image>() != null) Ensure<Button>(child.gameObject).targetGraphic = child.GetComponent<Image>();
        var panel = Ref<GameObject>(controller, "_rankingPanel");
        if (panel.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Career Ranking Character Selector")) return;
        var parent = Find(panel.transform, "Ranking List");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Modules/Lobby/Resources/Career/CareerRankingCharacterSelector.prefab");
        var selector = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        selector.name = "Career Ranking Character Selector"; selector.SetActive(false);
        foreach (var img in panel.GetComponentsInChildren<Image>(true))
            if (img.sprite != null && img.sprite.texture != null && img.sprite.texture.name != "UISprite") img.preserveAspect = true;
        foreach (var layout in parent.GetComponentsInChildren<VerticalLayoutGroup>(true))
        { layout.childAlignment = TextAnchor.UpperCenter; layout.childForceExpandHeight = false; }
        foreach (var c in selector.GetComponentsInChildren<Component>(true)) if (c != null) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    public static void Verify()
    {
        var controller = Open(); var rank = Ref<GameObject>(controller, "_rankPanel"); var view = rank.GetComponent<CareerRankPresentation>();
        if (view == null || Ref<CareerRankPresentation>(controller, "_rankPresentation") != view) throw new Exception("Missing authored rank presentation");
        int count = rank.transform.parent.GetComponentsInChildren<Transform>(true).Length;
        int components = rank.transform.parent.GetComponentsInChildren<Component>(true).Length;
        var awake = typeof(CareerPanelController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        awake.Invoke(controller, null); view.Bind();
        var fixture = new CareerOverviewData { seasonId = 1, seasonName = "시즌 01", seasonEndUnix = DateTimeOffset.UtcNow.AddDays(42).ToUnixTimeSeconds(), remainingSeasonDays = 42, tierId = 12, tierName = "Gold 3", rankImageId = 2, tierMinPoints = 1200, tierMaxPoints = 1299, rankPoints = 1264, rankedMatches = 38, wins = 23, roundsPlayed = 456, kills = 541, totalDamage = 153804, mvpCount = 8 };
        controller.ApplyOverview(fixture);
        if (view.emblem.sprite != view.rankSprites[2] || !view.nextRank.text.Contains("36")) throw new Exception("Overview mapping/progress failed");
        var slider = Ref<Slider>(controller, "_rankPointSlider"); if (slider.value != 64) throw new Exception("Rank progress regression");
        view.rankButton.onClick.Invoke(); if (!view.rankGuide.activeSelf || view.seasonGuide.activeSelf) throw new Exception("Rank guide open failed");
        Capture(rank.transform.parent.gameObject, "VectorCareerRankGuide.png");
        CaptureFullLobby(controller, "VectorCareerRankGuideFull.png");
        view.rankClose.onClick.Invoke(); if (view.rankGuide.activeSelf) throw new Exception("Rank guide close failed");
        view.seasonButton.onClick.Invoke(); if (!view.seasonGuide.activeSelf) throw new Exception("Season guide open failed");
        Capture(rank.transform.parent.gameObject, "VectorCareerSeason.png");
        CaptureFullLobby(controller, "VectorCareerSeasonFull.png");
        controller.OnRankingButton(); if (view.seasonGuide.activeSelf) throw new Exception("Tab switch left overlay open");
        controller.OnRankButton();
        for (int imageId = 0; imageId < 6; ++imageId) { fixture.rankImageId = imageId; controller.ApplyOverview(fixture); if (view.emblem.sprite != view.rankSprites[imageId]) throw new Exception("Tier emblem mapping failed"); }
        fixture.rankImageId = 2; controller.ApplyOverview(fixture);
        var history = new System.Collections.Generic.List<CareerMatchData>();
        foreach (int id in new[] { 0, 0, 0, 1, 1, 2 }) history.Add(new CareerMatchData { matchId = (ulong)(history.Count + 1), mapName = "테스트맵", durationSeconds = 480, myTeam = 0, winnerTeam = 0, players = new System.Collections.Generic.List<CareerMatchPlayerData> { new CareerMatchPlayerData { userId = "career-fixture", nickname = "VECTOR", characterId = id, team = 0, kills = 8, deaths = 3, assists = 4 } } });
        controller.ApplyMatchHistory(0, history); view.ApplyHistory(history, "career-fixture", 0);
        if (view.recentCharacters[0].sprite != view.characterSprites[0] || view.recentCharacters[2].sprite != view.characterSprites[2]) throw new Exception("Recent character usage failed");
        Capture(rank.transform.parent.gameObject, "VectorCareerMain.png");
        Capture(rank.transform.parent.gameObject, "VectorCareerMain720.png", 1280, 720);
        CaptureFullLobby(controller, "VectorCareerFullLobby.png");
        VerifyRanking(controller);
        controller.OnRankButton();
        fixture.tierMaxPoints = -1; controller.ApplyOverview(fixture); if (view.nextRank.text != "순위 경쟁 구간") throw new Exception("Uncapped tier failed");
        fixture.rankedMatches = fixture.roundsPlayed = fixture.wins = fixture.kills = fixture.totalDamage = 0; controller.ApplyOverview(fixture);
        if (view.statisticTexts.Any(t => t.text.Contains("NaN") || t.text.Contains("Infinity"))) throw new Exception("Empty season divide by zero");
        var cache = typeof(CareerPanelController).GetMethod("CacheDynamicViews", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); cache.Invoke(controller, null);
        if (count != rank.transform.parent.GetComponentsInChildren<Transform>(true).Length) throw new Exception("Runtime UI object count changed");
        if (components != rank.transform.parent.GetComponentsInChildren<Component>(true).Length) throw new Exception("Runtime UI components were created");
        view.Clear(); if (view.emblem.enabled || view.recentCharacters.Any(i => i.enabled)) throw new Exception("Loading state retained previous account data");
        Debug.Log($"CAREER_VERIFIED: {count} authored objects, 6 tier images, progress, empty/unbounded season, popup lifecycle, no runtime construction.");
        // Fixtures are deliberately not saved.
    }

    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(owner);
    [MenuItem("Project Vector/UI/Fix Career Portrait References")]
    public static void FixPortraitsAndVerify()
    {
        var controller = Open();
        var scene = controller.gameObject.scene;
        var view = Ref<CareerRankPresentation>(controller, "_rankPresentation");
        var buttonsBefore = controller.GetComponentsInChildren<Button>(true).Select(b => b.GetInstanceID()).ToArray();
        File.Copy(scene.path, Path.Combine(Path.GetTempPath(), "Vector-before-career-portraits-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".unity"), true);
        view.characterSprites = Enumerable.Range(0, 4).Select(CareerRankingPrefabBuilder.LoadCharacterSprite).ToArray();
        if (view.characterSprites.Any(s => s == null)) throw new Exception("Character selection portrait missing");
        for (int i = 0; i < view.recentCharacters.Length; ++i)
        {
            var image = view.recentCharacters[i]; image.sprite = view.characterSprites[i]; image.type = Image.Type.Simple; image.preserveAspect = true;
            Pose(image.transform, 0, 90 - i * 90, 82, 82); EditorUtility.SetDirty(image); PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
        AlignRecentCharacters(view);
        UpdateSelectorPortraits(Ref<GameObject>(controller, "_rankingPanel").transform, view.characterSprites);
        if (!buttonsBefore.SequenceEqual(controller.GetComponentsInChildren<Button>(true).Select(b => b.GetInstanceID()))) throw new Exception("Portrait fix changed buttons");
        EditorUtility.SetDirty(view); PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        // The authored selector in the alternate lobby shares the same character IDs.
        var other = EditorSceneManager.OpenScene("Assets/ProjectVector/Development/Legacy/Scenes/Lobby2.unity");
        var otherController = other.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CareerPanelController>(true)).Single();
        UpdateSelectorPortraits(Ref<GameObject>(otherController, "_rankingPanel").transform, Enumerable.Range(0, 4).Select(CareerRankingPrefabBuilder.LoadCharacterSprite).ToArray());
        EditorSceneManager.MarkSceneDirty(other); EditorSceneManager.SaveScene(other);
        AssetDatabase.SaveAssets(); Verify();
    }
    static void UpdateSelectorPortraits(Transform root, Sprite[] portraits)
    {
        var selector = Find(root, "Career Ranking Character Selector");
        for (int i = 0; i < 4; ++i)
        {
            var icon = Find(selector, "CharacterButton" + i).Find("Icon").GetComponent<Image>();
            icon.sprite = portraits[i]; icon.type = Image.Type.Simple; icon.preserveAspect = true;
            EditorUtility.SetDirty(icon); PrefabUtility.RecordPrefabInstancePropertyModifications(icon);
        }
        var selected = Find(selector, "SelectedCharacterButton").Find("Icon").GetComponent<Image>();
        selected.sprite = portraits[0]; selected.preserveAspect = true;
        EditorUtility.SetDirty(selected); PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
    }

    // Explicit editor authoring: the old VerticalLayoutGroup must not override these saved positions.
    static void AlignRecentCharacters(CareerRankPresentation view)
    {
        var root = view.recentCaption.transform.parent;
        foreach (var layout in root.GetComponents<LayoutGroup>()) layout.enabled = false;
        foreach (var fitter in root.GetComponents<ContentSizeFitter>()) fitter.enabled = false;
        Pose(root, 0, 365, 126, 350);
        for (int i = 0; i < view.recentCharacters.Length; ++i)
        {
            Pose(view.recentCharacters[i].transform, 0, 150 - i * 90, 82, 82);
            view.recentCharacters[i].preserveAspect = true;
        }
        Pose(view.recentCaption.transform, 0, -125, 158, 70);
        foreach (var component in root.GetComponentsInChildren<Component>(true))
            if (component != null) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
    }

    [MenuItem("Project Vector/UI/Align Career Portraits and Capture All Tabs")]
    public static void AlignRecentAndCaptureAll()
    {
        var controller = Open();
        var scene = controller.gameObject.scene;
        var preserved = new VectorExperiencePolishEditor.Preservation(scene);
        var view = Ref<CareerRankPresentation>(controller, "_rankPresentation");
        AlignRecentCharacters(view);
        preserved.Check();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Verify();
    }
    public static void CaptureFullLobby(CareerPanelController controller, string file, bool verifyPanelPointers = true, int width = 1920, int height = 1080)
    {
        var nav = controller.GetComponent<NavigationPanelController>();
        nav.InitializeLobbyMainNavigation(); nav.SelectLobbyMainMenu(LobbyMainMenu.Career);
        foreach (string field in new[] { "lobbyMainButton", "characterMainButton", "gameMainButton", "careerMainButton", "storeMainButton" })
        {
            var button = Ref<Button>(nav, field);
            if (button == null || !button.gameObject.activeInHierarchy) throw new Exception("Existing top navigation hidden: " + field);
        }
        Capture(controller.GetComponentInParent<Canvas>().gameObject, file, width, height, verifyPanelPointers);
        Debug.Log("CAREER_HEADER_VERIFIED: original 5 main navigation buttons present and active; full lobby canvas rendered.");
    }
    static void VerifyRanking(CareerPanelController controller)
    {
        controller.OnRankingButton();
        var rows = Field<System.Collections.Generic.List<Transform>>(controller, "_rankingRows");
        if (rows.Count != 10) throw new Exception("Ranking row count changed");
        Field<Button>(controller, "_characterRankingButton").onClick.Invoke();
        var popup = Field<GameObject>(controller, "_characterPopup");
        for (int id = 0; id < 4; ++id)
        {
            Field<Button>(controller, "_selectedCharacterButton").onClick.Invoke();
            if (!popup.activeSelf) throw new Exception("Character popup did not open");
            Field<Button[]>(controller, "_characterButtons")[id].onClick.Invoke();
            if (popup.activeSelf || Field<int>(controller, "_selectedRankingCharacterId") != id) throw new Exception("Character selection failed");
            controller.ApplyRanking(new CareerRankingPageData { rankingType = 1, characterId = id, page = 2, totalPages = 3, totalEntries = 25, entries = new System.Collections.Generic.List<CareerRankingEntryData> { new CareerRankingEntryData { position = 11, nickname = "검증 플레이어", userId = "career-fixture", playerIconId = 0, rankPoints = 450, wins = 8 } } });
            if (!rows[0].gameObject.activeSelf || rows[1].gameObject.activeSelf || !rows[0].Find("Text (TMP) (2)").GetComponent<TMP_Text>().text.Contains("450")) throw new Exception("Character ranking data binding failed");
        }
        Field<Button>(controller, "_overallRankingButton").onClick.Invoke();
        controller.ApplyRanking(new CareerRankingPageData { rankingType = 0, page = 1, totalPages = 1, entries = new System.Collections.Generic.List<CareerRankingEntryData> { new CareerRankingEntryData { position = 1, nickname = "검증 플레이어", playerIconId = 0, tierName = "Gold 3", wins = 23 } } });
        if (rows[0].Find("Text (TMP) (2)").GetComponent<TMP_Text>().text != "Gold 3") throw new Exception("Overall ranking regression");
        controller.ApplyRanking(new CareerRankingPageData { rankingType = 1, characterId = 0 });
        if (!rows[0].gameObject.activeSelf) throw new Exception("Stale character ranking overwrote overall ranking");
        string[] buttonFields = { "_rankButton", "_rankingButton", "_achievementsButton", "_careerRecordButton", "_inventoryButton", "_cardCollectionButton" };
        string[] panelFields = { "_rankPanel", "_rankingPanel", "_achievementsPanel", "_careerRecordPanel", "_inventoryPanel", "_cardCollectionPanel" };
        string[] captures = { "Rank", "Ranking", "Achievements", "History", "Inventory", "Collection" };
        // Existing tabs are documented as-is. Diagnostic entries are never saved to the scene or account.
        controller.ApplyRanking(new CareerRankingPageData { rankingType = 0, page = 1, totalPages = 1,
            entries = Enumerable.Range(1, 10).Select(i => new CareerRankingEntryData { position = i, nickname = $"플레이어 {i:00}", userId = "career-fixture-" + i,
                playerIconId = (i - 1) % 3, tierName = i <= 3 ? "Gold 3" : "Silver 1", wins = 42 - i }).ToList() });
        controller.ApplyInventory(new System.Collections.Generic.List<CareerInventoryEntryData>());
        for (int i = 0; i < 6; ++i)
        {
            Ref<Button>(controller, buttonFields[i]).onClick.Invoke();
            for (int j = 0; j < 6; ++j) if (Ref<GameObject>(controller, panelFields[j]).activeSelf != (i == j)) throw new Exception("Career tab regression");
            CaptureFullLobby(controller, "VectorCareerTab-" + captures[i] + ".png", i == 0);
        }
        controller.ApplyInventory(new System.Collections.Generic.List<CareerInventoryEntryData> { new CareerInventoryEntryData { itemId = 1, itemName = "검증 아이템", quantity = 2, description = "기존 인벤토리 연결 확인" } });
        if (!Field<TMP_Text>(controller, "_inventoryNameText").text.Contains("검증 아이템")) throw new Exception("Inventory data binding regression");
        var slots = Field<System.Collections.Generic.List<Transform>>(controller, "_inventorySlots");
        if (slots.Any(t => t.GetComponent<Button>() == null)) throw new Exception("Inventory buttons not authored");
        Debug.Log("CAREER_REGRESSIONS_VERIFIED: 6 tabs, 4 ranking characters, overall/character packets, stale response, match history and inventory binding.");
    }

    static void Capture(GameObject source, string file, int width = 1920, int height = 1080, bool verifyPanelPointers = true)
    {
        var canvas = new GameObject("CareerDiagnosticCanvas", typeof(Canvas)).GetComponent<Canvas>(); canvas.gameObject.layer = 5;
        canvas.renderMode = RenderMode.WorldSpace; ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
        var clone = Object.Instantiate(source, canvas.transform, false); clone.SetActive(true); Pose(clone.transform, 0, 0, 1920, 1080);
        var camera = new GameObject("CareerDiagnosticCamera", typeof(Camera)).GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540; camera.aspect = 1920f / 1080;
        camera.transform.position = new Vector3(0, 0, -10); camera.farClipPlane = 100; camera.cullingMask = 1 << 5; canvas.worldCamera = camera;
        var cloneCanvas = clone.GetComponent<Canvas>();
        if (cloneCanvas != null) { cloneCanvas.renderMode = RenderMode.WorldSpace; cloneCanvas.worldCamera = camera; }
        var scaler = clone.GetComponent<CanvasScaler>(); if (scaler != null) scaler.enabled = false;
        var originals = source.scene.GetRootGameObjects().Where(r => r != canvas.gameObject && r != camera.gameObject).Select(r => (r, r.activeSelf)).ToArray();
        var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline; var quality = QualitySettings.renderPipeline; var active = RenderTexture.active;
        var target = new RenderTexture(width, height, 24); var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            foreach (var entry in originals) entry.r.SetActive(false);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null; camera.targetTexture = target;
            foreach (var group in clone.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>()) { graphic.canvasRenderer.SetColor(Color.white); graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            foreach (var t in clone.GetComponentsInChildren<TMP_Text>()) t.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active = target; texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), file), texture.EncodeToPNG());
            var raycaster = Ensure<GraphicRaycaster>(cloneCanvas != null ? cloneCanvas.gameObject : canvas.gameObject);
            var eventSystem = new GameObject("CareerDiagnosticEvents", typeof(EventSystem)).GetComponent<EventSystem>();
            try
            {
                var view = clone.GetComponentInChildren<CareerRankPresentation>(true);
                var overlay = view != null ? (view.rankGuide.activeSelf ? view.rankGuide : view.seasonGuide.activeSelf ? view.seasonGuide : null) : null;
                var nav = clone.GetComponentInChildren<NavigationPanelController>(true);
                var mainButtons = nav == null ? Array.Empty<Button>() : new[] { "lobbyMainButton", "characterMainButton", "gameMainButton", "careerMainButton", "storeMainButton" }.Select(n => Ref<Button>(nav, n)).ToArray();
                foreach (var button in clone.GetComponentsInChildren<Button>().Where(b => b.IsInteractable()))
                {
                    if (!verifyPanelPointers && !mainButtons.Contains(button)) continue;
                    if (cloneCanvas != null && !mainButtons.Contains(button) && (view == null || !button.transform.IsChildOf(view.transform.parent))) continue;
                    if (overlay != null && button.transform.IsChildOf(view.transform) && !button.transform.IsChildOf(overlay.transform)) continue;
                    var rect = (RectTransform)button.transform;
                    var scroll = button.GetComponentInParent<ScrollRect>();
                    if (scroll != null && scroll.viewport != null)
                    {
                        var center = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                        if (!scroll.viewport.rect.Contains(center)) continue; // Authored offscreen rows must remain clipped.
                    }
                    var pointer = new PointerEventData(eventSystem) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
                    var hits = new System.Collections.Generic.List<RaycastResult>(); raycaster.Raycast(pointer, hits);
                    if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                        throw new Exception("Career pointer blocked: " + button.name + " by " + (hits.Count > 0 ? hits[0].gameObject.name : "nothing"));
                }
            }
            finally { Object.DestroyImmediate(eventSystem.gameObject); }
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = active; UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = quality;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(target); Object.DestroyImmediate(canvas.gameObject); Object.DestroyImmediate(camera.gameObject);
            foreach (var entry in originals) entry.r.SetActive(entry.activeSelf);
        }
    }
}
