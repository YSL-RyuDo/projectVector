#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MyPageHierarchyInstaller
{
    private static readonly Color Background = new Color32(202, 211, 220, 248);
    private static readonly Color Panel = new Color32(111, 121, 134, 235);
    private static readonly Color Dark = new Color32(27, 31, 34, 255);
    private static readonly Color Dark2 = new Color32(48, 54, 63, 255);
    private static readonly Color Red = new Color32(203, 43, 58, 255);
    private static TMP_FontAsset font;

    [MenuItem("Project Vector/UI/Install My Page Panel")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Lobby") return;
        GameObject existing = FindInScene(scene, "MyPagePanel");
        if (existing != null)
        {
            UpgradeExisting(scene, existing);
            return;
        }
        GameObject canvas = FindInScene(scene, "Canvas");
        if (canvas == null) return;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        GameObject root = UI("MyPagePanel", canvas.transform, Background);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup rootGroup = root.AddComponent<CanvasGroup>();
        rootGroup.alpha = 1f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        root.AddComponent<MyPagePanelController>();
        root.transform.SetAsLastSibling();

        GameObject header = UI("Header", root.transform, Dark);
        Anchor(header, 0, 1, 1, 1, 0, -64, 0, 0);
        Text("PageTitle", header.transform, "프로필", 32, TextAlignmentOptions.Left, Color.white, new Vector2(25, 0), new Vector2(180, 64));
        Button("ProfileTabButton", header.transform, "프로필", new Vector2(-560, 0), new Vector2(180, 64), new Color(0, 0, 0, 0));
        GameObject tabMark = UI("ProfileTabSelected", header.transform, Red);
        Fixed(tabMark, new Vector2(-560, -28), new Vector2(180, 6));
        Button("CareerTabButton", header.transform, "커리어 데이터", new Vector2(-350, 0), new Vector2(220, 64), new Color(0, 0, 0, 0));
        GameObject careerTabMark = UI("CareerTabSelected", header.transform, Red);
        Fixed(careerTabMark, new Vector2(-350, -28), new Vector2(220, 6));
        careerTabMark.SetActive(false);
        Button("BackButton", header.transform, "ESC   뒤로", new Vector2(830, 0), new Vector2(150, 42), Dark2);

        GameObject scroll = UI("MainScrollView", root.transform, new Color(0, 0, 0, 0));
        // Reference layout: the scrolling profile information owns the left side,
        // while the player card remains fixed in a dedicated right column.
        Anchor(scroll, 0, 0, 1, 1, 84, 34, -520, -305);
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 42f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;

        GameObject viewport = UI("Viewport", scroll.transform, new Color(0, 0, 0, 0));
        Anchor(viewport, 0, 0, 1, 1, 0, 0, -20, 0);
        viewport.AddComponent<RectMask2D>();
        GameObject content = UI("Content", viewport.transform, new Color(0, 0, 0, 0));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1); contentRect.pivot = new Vector2(.5f, 1);
        contentRect.anchoredPosition = Vector2.zero; contentRect.sizeDelta = new Vector2(0, 1460);
        scrollRect.viewport = viewport.GetComponent<RectTransform>(); scrollRect.content = contentRect;
        GameObject bar = UI("MainScrollbar", scroll.transform, Dark2);
        Anchor(bar, 1, 0, 1, 1, -14, 0, 0, 0);
        Scrollbar scrollbar = bar.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        GameObject handleArea = UI("Sliding Area", bar.transform, new Color(0,0,0,0)); Stretch(handleArea.GetComponent<RectTransform>(), 3);
        GameObject handle = UI("Handle", handleArea.transform, Red); Stretch(handle.GetComponent<RectTransform>());
        scrollbar.handleRect = handle.GetComponent<RectTransform>(); scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollRect.verticalScrollbar = scrollbar; scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Profile summary is intentionally outside the ScrollRect. It remains
        // fixed while rank, character, battle and skin sections are scrolled.
        BuildProfile(root.transform);
        BuildRank(content.transform);
        BuildCharacters(content.transform);
        BuildBattle(content.transform);
        BuildSkinCollection(content.transform);
        BuildPlayerCard(root.transform);
        BuildBattleCustom(root.transform);
        BuildSkinCustom(root.transform);
        BuildVisitors(root.transform);
        BuildCareerData(root.transform);
        root.AddComponent<MyPageCareerDataController>();

        ConfigureEntry(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MY PAGE] Lobby hierarchy installed and saved.");
    }

    [MenuItem("Project Vector/UI/Rebuild My Page Reference Layout")]
    public static void RebuildReferenceLayout()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Single);
        GameObject existing = FindInScene(scene, "MyPagePanel");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        Install();
        ValidateReferenceLayout(FindInScene(scene, "MyPagePanel"));
        Debug.Log("[MY PAGE LAYOUT] Rebuilt Lobby MyPagePanel using the reference two-column layout.");
    }

    private static void ValidateReferenceLayout(GameObject root)
    {
        if (root == null) throw new InvalidOperationException("MyPagePanel was not rebuilt.");
        Transform scroll = FindChild(root.transform, "MainScrollView");
        Transform card = FindChild(root.transform, "PlayerCardPreview");
        Transform profile = FindChild(root.transform, "ProfileSection");
        if (scroll == null || card == null || profile == null)
            throw new InvalidOperationException("MyPage reference layout objects are incomplete.");

        RectTransform scrollRect = scroll.GetComponent<RectTransform>();
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform profileRect = profile.GetComponent<RectTransform>();
        bool scrollMatches = Mathf.Approximately(scrollRect.offsetMin.x, 84f) && Mathf.Approximately(scrollRect.offsetMax.x, -520f) && Mathf.Approximately(scrollRect.offsetMax.y, -305f);
        bool cardMatches = card.parent == root.transform && cardRect.sizeDelta == new Vector2(390f, 920f);
        bool contentMatches = profile.parent == root.transform &&
                              Mathf.Approximately(profileRect.anchorMin.x, 0f) &&
                              Mathf.Approximately(profileRect.anchoredPosition.x, 729f) &&
                              Mathf.Approximately(profileRect.sizeDelta.x, 1290f);
        if (!scrollMatches || !cardMatches || !contentMatches)
            throw new InvalidOperationException($"MyPage reference layout validation failed: scroll=({scrollRect.offsetMin.x},{scrollRect.offsetMax.x}), cardParent={card.parent.name}, cardSize={cardRect.sizeDelta}, contentWidth={profileRect.sizeDelta.x}.");

        int statCanvasGroups = 0;
        for (int i = 0; i < 15; ++i)
        {
            Transform stat = FindChild(root.transform, "StatItem" + i);
            if (stat != null && stat.GetComponent<CanvasGroup>() != null) statCanvasGroups++;
        }
        if (statCanvasGroups != 15)
            throw new InvalidOperationException($"MyPage drag item validation failed: CanvasGroup={statCanvasGroups}/15.");

        Debug.Log($"[MY PAGE LAYOUT VALIDATION] fixedProfile={profile.parent.name}, scrollTop={scrollRect.offsetMax.y}, contentWidth={profileRect.sizeDelta.x}, card={cardRect.sizeDelta.x}x{cardRect.sizeDelta.y}, dragCanvasGroup={statCanvasGroups}/15.");
    }

    public static void ValidateEntryPoint()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Single);
        GameObject icon = GameObject.Find("PlayerIcon");
        GameObject panel = GameObject.Find("MyPagePanel");
        if (icon == null || panel == null) throw new InvalidOperationException("PlayerIcon or MyPagePanel is missing.");
        Button button = icon.GetComponent<Button>();
        MyPageOpenButton opener = icon.GetComponent<MyPageOpenButton>();
        MyPagePanelController controller = panel.GetComponent<MyPagePanelController>();
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (button == null || opener == null || controller == null || group == null)
            throw new InvalidOperationException("My Page entry components are incomplete.");
        SerializedObject serializedOpener = new SerializedObject(opener);
        if (serializedOpener.FindProperty("panel").objectReferenceValue != controller)
            throw new InvalidOperationException("MyPageOpenButton has no serialized panel reference.");
        if (!icon.activeInHierarchy || !panel.activeSelf || group.alpha != 1f || group.blocksRaycasts || group.interactable)
            throw new InvalidOperationException("My Page initial active/CanvasGroup state is invalid.");
        Transform obsoleteCardSync = FindChild(panel.transform, "CardSyncText");
        if (obsoleteCardSync != null && (obsoleteCardSync.gameObject.activeSelf ||
            !string.IsNullOrEmpty(obsoleteCardSync.GetComponent<TMP_Text>()?.text)))
            throw new InvalidOperationException("Internal PlayerCard linkage text is still visible.");
        bool hasPersistentOpen = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); ++i)
        {
            if (button.onClick.GetPersistentTarget(i) == controller &&
                button.onClick.GetPersistentMethodName(i) == nameof(MyPagePanelController.OpenSelf))
            {
                hasPersistentOpen = true;
                break;
            }
        }
        if (!hasPersistentOpen)
            throw new InvalidOperationException("PlayerIcon has no persistent OpenSelf listener in the hierarchy.");

        TMP_Dropdown season = FindChild(panel.transform, "CareerSeasonDropdown")?.GetComponent<TMP_Dropdown>();
        TMP_Dropdown mode = FindChild(panel.transform, "CareerModeDropdown")?.GetComponent<TMP_Dropdown>();
        Image characterImage = FindChild(panel.transform, "CareerCharacterImage")?.GetComponent<Image>();
        if (season == null || season.options.Count != 1 || season.options[0].text != "시즌0")
            throw new InvalidOperationException("Career season dropdown is not serialized as 시즌0.");
        if (mode == null || mode.options.Count != 3 || mode.options[0].text != "랭크" ||
            mode.options[1].text != "일반" || mode.options[2].text != "사설")
            throw new InvalidOperationException("Career mode dropdown options are incomplete.");
        if (characterImage == null || characterImage.sprite == null || !characterImage.enabled)
            throw new InvalidOperationException("Career selected character image is not visible in the hierarchy.");

        MyPageCareerDataController career = panel.GetComponent<MyPageCareerDataController>();
        Image picoImage = FindChild(panel.transform, "CareerCharacterOption1Image")?.GetComponent<Image>();
        if (career == null || picoImage == null || picoImage.sprite == null)
            throw new InvalidOperationException("Career runtime preview dependencies are missing.");
        CareerDetailData preview = new CareerDetailData
        {
            success = true,
            hasData = false,
            seasonId = 0,
            modeType = 1,
            selectedCharacterId = 1
        };
        preview.seasons.Add(new CareerDetailSeasonData
        {
            seasonId = 0,
            seasonName = "\uFFFD\uFFFD\uFFFD0",
            isActive = true
        });
        for (int i = 0; i < 4; ++i)
            preview.characters.Add(new CareerDetailCharacterData { characterId = i });
        career.ApplyData(preview);
        if (season.captionText == null || season.captionText.text != "시즌0")
            throw new InvalidOperationException("Career season caption still accepts a corrupted server label.");
        if (mode.captionText == null || mode.captionText.text != "랭크")
            throw new InvalidOperationException("Career mode caption is blank.");
        RectTransform modeRect = mode.GetComponent<RectTransform>();
        RectTransform modeCaptionRect = mode.captionText.GetComponent<RectTransform>();
        RectTransform modeArrowRect = FindChild(mode.transform, "Arrow")?.GetComponent<RectTransform>();
        RectTransform modeItemRect = mode.itemText != null ? mode.itemText.GetComponent<RectTransform>() : null;
        if (modeRect == null || !FitsFixedWidth(modeRect, modeCaptionRect) ||
            !FitsFixedWidth(modeRect, modeArrowRect) || !FitsFixedWidth(modeRect, modeItemRect))
            throw new InvalidOperationException("Career mode dropdown children overflow the 300px dropdown bounds.");
        if (!characterImage.enabled || characterImage.sprite != picoImage.sprite)
            throw new InvalidOperationException("Selecting PicoChan does not update the main character image.");

        Debug.Log("[MY PAGE VALIDATION] Hierarchy visibility, PlayerIcon OnClick, dropdown labels/layout, and PicoChan selection image are valid.");
    }

    [MenuItem("Project Vector/UI/Repair Career Dropdown Layout")]
    public static void RepairCareerDropdownLayout()
    {
        const string lobbyPath = "Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity";
        Scene scene = SceneManager.GetSceneByPath(lobbyPath);
        bool closeWhenDone = !scene.IsValid() || !scene.isLoaded;
        if (closeWhenDone) scene = EditorSceneManager.OpenScene(lobbyPath, OpenSceneMode.Additive);
        GameObject panel = FindInScene(scene, "MyPagePanel");
        TMP_Dropdown mode = panel != null
            ? FindChild(panel.transform, "CareerModeDropdown")?.GetComponent<TMP_Dropdown>()
            : null;
        if (mode == null) throw new InvalidOperationException("CareerModeDropdown is missing.");

        RectTransform modeRect = mode.GetComponent<RectTransform>();
        float dropdownWidth = modeRect.sizeDelta.x;
        RectTransform captionRect = mode.captionText.GetComponent<RectTransform>();
        captionRect.anchoredPosition = new Vector2(-18f, 0f);
        captionRect.sizeDelta = new Vector2(Mathf.Max(120f, dropdownWidth - 70f), 42f);

        RectTransform arrowRect = FindChild(mode.transform, "Arrow")?.GetComponent<RectTransform>();
        if (arrowRect == null) throw new InvalidOperationException("CareerModeDropdown Arrow is missing.");
        arrowRect.anchoredPosition = new Vector2(dropdownWidth * .5f - 20f, 0f);
        arrowRect.sizeDelta = new Vector2(32f, 42f);

        RectTransform itemRect = mode.itemText != null ? mode.itemText.GetComponent<RectTransform>() : null;
        if (itemRect == null) throw new InvalidOperationException("CareerModeDropdown Item Label is missing.");
        itemRect.anchoredPosition = new Vector2(10f, 0f);
        itemRect.sizeDelta = new Vector2(Mathf.Max(120f, dropdownWidth - 40f), 44f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (closeWhenDone) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[MY PAGE REPAIR] Career mode dropdown caption, arrow, and item label now fit the 300px root.");
    }

    private static bool FitsFixedWidth(RectTransform parent, RectTransform child)
    {
        if (parent == null || child == null) return false;
        float parentHalfWidth = parent.sizeDelta.x * .5f;
        float childHalfWidth = child.sizeDelta.x * .5f;
        return child.anchoredPosition.x - childHalfWidth >= -parentHalfWidth &&
               child.anchoredPosition.x + childHalfWidth <= parentHalfWidth;
    }

    public static void RepairViewportMasks()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Single);
        GameObject root = GameObject.Find("MyPagePanel");
        if (root == null) throw new InvalidOperationException("MyPagePanel is missing.");

        foreach (Mask legacyMask in root.GetComponentsInChildren<Mask>(true))
            UnityEngine.Object.DestroyImmediate(legacyMask);

        int repaired = 0;
        foreach (Transform value in root.GetComponentsInChildren<Transform>(true))
        {
            if (value.name != "Viewport") continue;
            if (value.GetComponent<RectMask2D>() == null)
            {
                value.gameObject.AddComponent<RectMask2D>();
                repaired++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MY PAGE REPAIR] Replaced transparent legacy masks; added RectMask2D to {repaired} viewport(s).");
    }

    private static void BuildProfile(Transform parent)
    {
        GameObject section = Section(parent, "ProfileSection", -84, 215);
        RectTransform sectionRect = section.GetComponent<RectTransform>();
        sectionRect.anchorMin = sectionRect.anchorMax = new Vector2(0, 1);
        sectionRect.pivot = new Vector2(.5f, 1);
        sectionRect.anchoredPosition = new Vector2(729, -84);
        GameObject left = PanelBox(section.transform, "ProfileInfoBox", new Vector2(-400, -5), new Vector2(490, 195));
        Image image = ImageObject("ProfileIconImage", left.transform, new Vector2(-175, 0), new Vector2(155, 155));
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/PlayerIcon/0.jpg"); image.preserveAspect = true;
        Text("ProfileNicknameText", left.transform, "Nickname", 27, TextAlignmentOptions.Left, Color.white, new Vector2(30, 60), new Vector2(250, 40));
        Text("ProfileIdText", left.transform, "ID: ----", 18, TextAlignmentOptions.Left, Color.white, new Vector2(30, 20), new Vector2(250, 30));
        Text("ProfileLevelText", left.transform, "Lv.1", 20, TextAlignmentOptions.Left, Color.white, new Vector2(30, -18), new Vector2(250, 30));
        Text("GuildText", left.transform, "길드: 미가입", 17, TextAlignmentOptions.Left, Color.white, new Vector2(30, -56), new Vector2(250, 30));

        GameObject middle = PanelBox(section.transform, "PersonalInfoBox", new Vector2(100, -5), new Vector2(500, 195));
        Button("NicknameEditButton", middle.transform, "닉네임 수정", new Vector2(-155, 58), new Vector2(145, 38), Dark2);
        Text("RegionText", middle.transform, "지역: 대한민국", 19, TextAlignmentOptions.Left, Color.white, new Vector2(35, 58), new Vector2(290, 34));
        Text("BirthdayText", middle.transform, "생일: ----", 19, TextAlignmentOptions.Left, Color.white, new Vector2(35, 18), new Vector2(290, 34));
        Text("JoinDateText", middle.transform, "가입일: 2026-09-15", 17, TextAlignmentOptions.Left, Color.white, new Vector2(35, -22), new Vector2(290, 34));
        Text("PrivacyLabel", middle.transform, "개인정보 보호", 17, TextAlignmentOptions.Left, Color.white, new Vector2(-120, -68), new Vector2(190, 34));
        GameObject toggle = Button("PrivacyToggleButton", middle.transform, "", new Vector2(80, -68), new Vector2(78, 34), Dark2);
        GameObject knob = UI("PrivacyKnob", toggle.transform, Color.white); Fixed(knob, new Vector2(-18, 0), new Vector2(26, 26));
        Text("PrivacyStateText", middle.transform, "OFF", 16, TextAlignmentOptions.Left, Color.white, new Vector2(150, -68), new Vector2(70, 34));

        GameObject social = PanelBox(section.transform, "SocialBox", new Vector2(495, -5), new Vector2(280, 195));
        Button("LikeButton", social.transform, "따봉", new Vector2(-65, 48), new Vector2(90, 68), Red);
        Text("LikeCountText", social.transform, "0", 27, TextAlignmentOptions.Center, Color.white, new Vector2(65, 48), new Vector2(110, 48));
        Button("RecentVisitorsButton", social.transform, "최근 방문자  ▶", new Vector2(0, -25), new Vector2(240, 44), Dark2);
        Text("RecentVisitorsCountText", social.transform, "0", 17, TextAlignmentOptions.Center, Color.white, new Vector2(0, -70), new Vector2(220, 28));
        Text("StatusMessage", section.transform, "", 15, TextAlignmentOptions.Center, new Color32(255,215,90,255), new Vector2(0, -105), new Vector2(1100, 24));
    }

    private static void BuildPlayerCard(Transform parent)
    {
        GameObject card = PanelBox(parent, "PlayerCardPreview", new Vector2(-220, -84), new Vector2(390, 920), new Vector2(1, 1));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.pivot = new Vector2(.5f, 1f);
        GameObject cardBorder = UI("CardBorderImage", card.transform, new Color32(72, 82, 95, 255));
        Stretch(cardBorder.GetComponent<RectTransform>(), 8);
        cardBorder.transform.SetAsFirstSibling();
        Text("CardTitle", card.transform, "PLAYER CARD", 18, TextAlignmentOptions.Center, new Color32(120,220,255,255), new Vector2(0, 395), new Vector2(310, 32));
        Image cardIcon = ImageObject("CardIconImage", card.transform, new Vector2(0, 80), new Vector2(325, 810));
        cardIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/IconCard/icon0_card.png"); cardIcon.preserveAspect = false;
        Text("CardNicknameText", card.transform, "Nickname", 29, TextAlignmentOptions.Center, new Color32(255,215,90,255), new Vector2(0, 85), new Vector2(330, 48));
        Text("CardLevelText", card.transform, "1", 24, TextAlignmentOptions.Center, Color.white, new Vector2(0, 30), new Vector2(160, 40));
    }

    private static void BuildRank(Transform parent)
    {
        GameObject section = Section(parent, "RankSection", -10, 400);
        SectionTitle(section.transform, "랭크 정보", "SeasonTitleText", "시즌0");
        string[] imageNames = { "SeasonHighRankImage", "CurrentRankImage", "AllTimeRankImage" };
        string[] textNames = { "SeasonHighRankText", "CurrentRankText", "AllTimeRankText" };
        string[] labels = { "이번 시즌 최고 랭크\n브론즈", "현재 랭크\n브론즈", "전체 시즌 최고 랭크\n시즌0 · 브론즈" };
        float[] xs = { -410, 0, 410 };
        for (int i=0;i<3;i++)
        {
            GameObject box = PanelBox(section.transform, "RankBox"+i, new Vector2(xs[i], -15), new Vector2(385, 300));
            Image rank = ImageObject(imageNames[i], box.transform, new Vector2(0, 38), new Vector2(i==1?165:125, i==1?165:125));
            rank.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Resources/Rank/Bronze.png"); rank.preserveAspect = true;
            Text(textNames[i], box.transform, labels[i], 18, TextAlignmentOptions.Center, Color.white, new Vector2(0,-87), new Vector2(310,70));
        }
    }

    private static void BuildCharacters(Transform parent)
    {
        GameObject section = Section(parent, "FrequentCharactersSection", -430, 300);
        SectionTitle(section.transform, "자주 사용하는 캐릭터", null, null);
        string[] characters={"Casual1","PicoChan","UnityChan","UnityChanSSU"};
        string[] roles={"이니시에이터","컨트롤러","센티널","엔트리"};
        for (int i=0;i<4;i++)
        {
            float x=-465+i*310;
            GameObject card=PanelBox(section.transform,"CharacterCard"+i,new Vector2(x,-20),new Vector2(290,205));
            Image image=ImageObject("Character"+i+"Image",card.transform,new Vector2(-92,2),new Vector2(105,145));
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectVector/Shared/Characters/Portraits/Image/Char{i}.png"); image.preserveAspect=true; image.color=new Color(.35f,.38f,.43f,.45f);
            Text("Character"+i+"NameText",card.transform,characters[i],18,TextAlignmentOptions.Left,Color.white,new Vector2(50,50),new Vector2(160,34));
            Text("Character"+i+"RoleText",card.transform,roles[i],13,TextAlignmentOptions.Left,new Color32(105,215,240,255),new Vector2(50,15),new Vector2(160,26));
            Text("Character"+i+"StatsText",card.transform,"플레이 기록 없음",14,TextAlignmentOptions.Left,new Color32(210,218,225,255),new Vector2(50,-42),new Vector2(160,72));
        }
    }

    private static void BuildBattle(Transform parent)
    {
        GameObject section = Section(parent, "BattleDataSection", -750, 390);
        SectionTitle(section.transform, "전투 데이터", null, null);
        Button("BattleCustomButton", section.transform, "교체", new Vector2(550, 160), new Vector2(135, 40), Red);
        Vector2[] positions = { new(-420,75), new(-420,-30), new(-420,-135), new(0,-35), new(420,75), new(420,-30), new(420,-135) };
        for(int i=0;i<7;i++)
        {
            Vector2 size=i==3?new Vector2(310,260):new Vector2(330,88);
            GameObject slot=PanelBox(section.transform,"BattleSlot"+i,positions[i],size);
            MyPageStatDropSlot drop=slot.AddComponent<MyPageStatDropSlot>(); drop.Configure(i);
            GameObject icon=UI("BattleSlot"+i+"Icon",slot.transform,Dark); Fixed(icon,Vector2.zero,i==3?new Vector2(105,105):new Vector2(55,55));
            Text("BattleSlot"+i+"ValueText",slot.transform,"0",i==3?44:24,TextAlignmentOptions.Center,i==3?new Color32(255,210,55,255):Color.white,new Vector2(0,i==3?-55:15),new Vector2(210,48));
            Text("BattleSlot"+i+"LabelText",slot.transform,"통계",i==3?21:15,TextAlignmentOptions.Center,Color.white,new Vector2(0,i==3?-100:-20),new Vector2(250,35));
        }
    }

    private static void BuildSkinCollection(Transform parent)
    {
        GameObject section=Section(parent,"SkinCollectionSection",-1160,245);
        SectionTitle(section.transform,"스킨 수집",null,null);
        GameObject weapon=PanelBox(section.transform,"WeaponSkinSummary",new Vector2(-390,-20),new Vector2(480,155));
        Text("WeaponSkinCountText",weapon.transform,"0 / 0\n무기 스킨 수",24,TextAlignmentOptions.Center,Color.white,Vector2.zero,new Vector2(430,105));
        GameObject character=PanelBox(section.transform,"CharacterSkinSummary",new Vector2(120,-20),new Vector2(480,155));
        Text("CharacterSkinCountText",character.transform,"0 / 12\n캐릭터 스킨 수",24,TextAlignmentOptions.Center,Color.white,Vector2.zero,new Vector2(430,105));
        Button("SkinCustomButton",section.transform,"스킨 선택",new Vector2(510,-20),new Vector2(190,76),Red);
    }

    private static void BuildBattleCustom(Transform parent)
    {
        GameObject panel=UI("BattleCustomPanel",parent,Background); Anchor(panel,1,0,1,1,-470,70,-20,-90); panel.AddComponent<CanvasGroup>();
        Text("BattleCustomTitle",panel.transform,"전투 데이터 커스텀",25,TextAlignmentOptions.Left,Color.white,new Vector2(0,0),new Vector2(390,55),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-30));
        Button("BattleCustomCloseButton",panel.transform,"X",new Vector2(190,-30),new Vector2(46,42),Red,new Vector2(.5f,1));
        Text("BattleCustomHelp",panel.transform,"항목을 왼쪽 슬롯으로 드래그하여 교체하세요.\n이미 표시 중인 항목은 선택할 수 없습니다.",14,TextAlignmentOptions.Left,Dark,new Vector2(0,0),new Vector2(390,60),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-90));
        GameObject scroll=UI("BattleCustomScroll",panel.transform,new Color(0,0,0,0)); Anchor(scroll,0,0,1,1,15,20,-20,-165); ScrollRect sr=scroll.AddComponent<ScrollRect>();sr.horizontal=false;sr.vertical=true;sr.scrollSensitivity=35;sr.movementType=ScrollRect.MovementType.Clamped;
        GameObject vp=UI("Viewport",scroll.transform,new Color(0,0,0,0));Anchor(vp,0,0,1,1,0,0,-16,0);vp.AddComponent<RectMask2D>();
        GameObject content=UI("Content",vp.transform,new Color(0,0,0,0));RectTransform cr=content.GetComponent<RectTransform>();cr.anchorMin=new Vector2(0,1);cr.anchorMax=new Vector2(1,1);cr.pivot=new Vector2(.5f,1);cr.sizeDelta=new Vector2(0,1050);cr.anchoredPosition=Vector2.zero;sr.viewport=vp.GetComponent<RectTransform>();sr.content=cr;
        string[] keys={"totalKills","playCount","avgDamage","winRate","mvpRate","headshotRate","avgKills","totalAssists","totalDamage","headshotKills","skinCount","weaponSkinCount","spikePlants","spikeDefuses","mvpCount"};
        string[] labels={"총 처치 수","플레이 횟수","라운드 평균 피해량","승률","MVP 확률","헤드샷 처치율","라운드 평균 처치 수","총 도움","총 피해량","헤드샷 처치 횟수","스킨 수","무기 스킨 수","폭탄 설치 횟수","폭탄 해체 횟수","MVP 횟수"};
        for(int i=0;i<keys.Length;i++)
        {
            GameObject item=Button("StatItem"+i,content.transform,labels[i],new Vector2(0,-35-i*68),new Vector2(380,58),Dark2,new Vector2(.5f,1));
            item.AddComponent<CanvasGroup>();
            MyPageStatDragItem drag=item.AddComponent<MyPageStatDragItem>();drag.Configure(keys[i]);
        }
        AddVerticalScrollbar(scroll,sr);
    }

    private static void BuildSkinCustom(Transform parent)
    {
        GameObject panel=UI("SkinCustomPanel",parent,Background);Anchor(panel,1,0,1,1,-520,70,-20,-90);panel.AddComponent<CanvasGroup>();
        Text("SkinCustomTitle",panel.transform,"스킨 선택",25,TextAlignmentOptions.Left,Color.white,new Vector2(0,0),new Vector2(440,55),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-30));
        Button("SkinCustomCloseButton",panel.transform,"X",new Vector2(215,-30),new Vector2(46,42),Red,new Vector2(.5f,1));
        GameObject dropdown=UI("SkinCharacterDropdown",panel.transform,Dark2);RectTransform dr=dropdown.GetComponent<RectTransform>();dr.anchorMin=dr.anchorMax=new Vector2(.5f,1);dr.sizeDelta=new Vector2(430,48);dr.anchoredPosition=new Vector2(0,-90);BuildDropdown(dropdown);
        GameObject scroll=UI("SkinCustomScroll",panel.transform,new Color(0,0,0,0));Anchor(scroll,0,0,1,1,15,20,-20,-135);ScrollRect sr=scroll.AddComponent<ScrollRect>();sr.horizontal=false;sr.vertical=true;sr.scrollSensitivity=35;sr.movementType=ScrollRect.MovementType.Clamped;
        GameObject vp=UI("Viewport",scroll.transform,new Color(0,0,0,0));Anchor(vp,0,0,1,1,0,0,-16,0);vp.AddComponent<RectMask2D>();
        GameObject content=UI("Content",vp.transform,new Color(0,0,0,0));RectTransform cr=content.GetComponent<RectTransform>();cr.anchorMin=new Vector2(0,1);cr.anchorMax=new Vector2(1,1);cr.pivot=new Vector2(.5f,1);cr.sizeDelta=new Vector2(0,920);cr.anchoredPosition=Vector2.zero;sr.viewport=vp.GetComponent<RectTransform>();sr.content=cr;
        string[] characters={"Casual1","PicoChan","UnityChan","UnityChanSSU"};
        for(int i=0;i<12;i++)
        {
            int c=i/3,skin=i%3;
            GameObject card=PanelBox(content.transform,"SkinCard"+i,new Vector2(0,-45-i*72),new Vector2(420,62),new Vector2(.5f,1));
            Text("SkinCard"+i+"Text",card.transform,$"{characters[c]}  ·  {(skin==0?"기본 스킨":"스킨 "+skin)}",17,TextAlignmentOptions.Left,Color.white,new Vector2(0,0),new Vector2(380,50));
        }
        AddVerticalScrollbar(scroll,sr);
    }

    private static void BuildVisitors(Transform parent)
    {
        GameObject panel=UI("RecentVisitorsPopup",parent,Background);Fixed(panel,Vector2.zero,new Vector2(760,620));panel.AddComponent<CanvasGroup>();
        Text("VisitorsTitle",panel.transform,"최근 방문자",28,TextAlignmentOptions.Left,Color.white,new Vector2(-230,265),new Vector2(260,48));
        Button("VisitorsCloseButton",panel.transform,"X",new Vector2(330,265),new Vector2(48,42),Red);
        for(int i=0;i<8;i++)
        {
            GameObject row=PanelBox(panel.transform,"VisitorRow"+i,new Vector2(0,205-i*61),new Vector2(690,54));
            Image icon=ImageObject("VisitorRow"+i+"Icon",row.transform,new Vector2(-310,0),new Vector2(42,42));icon.preserveAspect=true;
            Text("VisitorRow"+i+"Text",row.transform,"방문자",16,TextAlignmentOptions.Left,Color.white,new Vector2(25,0),new Vector2(590,45));
        }
    }

    private static void UpgradeExisting(Scene scene, GameObject root)
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        root.SetActive(true);
        CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
        Transform likeLabel = FindChild(root.transform, "LikeButtonLabel");
        if (likeLabel != null) likeLabel.GetComponent<TMP_Text>().text = "따봉";
        Transform battleLabel = FindChild(root.transform, "BattleCustomButtonLabel");
        if (battleLabel != null) battleLabel.GetComponent<TMP_Text>().text = "교체";

        ConfigureEntry(root);

        Transform header = FindChild(root.transform, "Header");
        if (header != null && FindChild(header, "ProfileTabButton") == null)
        {
            Transform oldTab = FindChild(header, "ProfileTabText");
            if (oldTab != null) oldTab.gameObject.SetActive(false);
            Button("ProfileTabButton", header, "프로필", new Vector2(-560, 0), new Vector2(180, 64), new Color(0, 0, 0, 0));
            Button("CareerTabButton", header, "커리어 데이터", new Vector2(-350, 0), new Vector2(220, 64), new Color(0, 0, 0, 0));
            if (FindChild(header, "CareerTabSelected") == null)
            {
                GameObject mark = UI("CareerTabSelected", header, Red);
                Fixed(mark, new Vector2(-350, -28), new Vector2(220, 6));
                mark.SetActive(false);
            }
        }
        if (FindChild(root.transform, "CareerDataPanel") == null) BuildCareerData(root.transform);
        if (root.GetComponent<MyPageCareerDataController>() == null) root.AddComponent<MyPageCareerDataController>();

        Transform card = FindChild(root.transform, "PlayerCardPreview");
        Transform obsoleteCardSync = card != null ? FindChild(card, "CardSyncText") : null;
        if (obsoleteCardSync != null) UnityEngine.Object.DestroyImmediate(obsoleteCardSync.gameObject);
        if (card != null && FindChild(card, "CardBorderImage") == null)
        {
            GameObject border = UI("CardBorderImage", card, new Color32(72, 82, 95, 255));
            Stretch(border.GetComponent<RectTransform>(), 8);
            border.transform.SetAsFirstSibling();
        }

        Transform dropdownTransform = FindChild(root.transform, "SkinCharacterDropdown");
        TMP_Dropdown dropdown = dropdownTransform != null ? dropdownTransform.GetComponent<TMP_Dropdown>() : null;
        if (dropdown != null && (dropdown.template == null || dropdown.captionText == null || dropdown.itemText == null))
            BuildDropdown(dropdown.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MY PAGE] Existing Lobby hierarchy upgraded and saved (v3 opener).");
    }

    [MenuItem("Project Vector/UI/Install My Page Career Data")]
    public static void InstallCareerData()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity", OpenSceneMode.Single);
        GameObject root = FindInScene(scene, "MyPagePanel");
        if (root == null)
        {
            Install();
            root = FindInScene(scene, "MyPagePanel");
        }
        if (root == null) throw new InvalidOperationException("MyPagePanel is missing.");
        UpgradeExisting(scene, root);
        if (FindChild(root.transform, "CareerDataPanel") == null)
            throw new InvalidOperationException("CareerDataPanel installation failed.");
        Debug.Log("[MY PAGE CAREER] Career tab hierarchy installed and saved.");
    }

    private static void BuildCareerData(Transform parent)
    {
        GameObject panel = UI("CareerDataPanel", parent, new Color(0, 0, 0, 0));
        Anchor(panel, 0, 0, 1, 1, 70, 34, -70, -82);

        GameObject season = UI("CareerSeasonDropdown", panel.transform, Dark2);
        Fixed(season, new Vector2(-620, 422), new Vector2(420, 48));
        BuildDropdown(season);
        SetDropdownOptions(season, "시즌0");
        GameObject mode = UI("CareerModeDropdown", panel.transform, Dark2);
        Fixed(mode, new Vector2(-185, 422), new Vector2(300, 48));
        BuildDropdown(mode);
        SetDropdownOptions(mode, "랭크", "일반", "사설");
        Text("CareerStatusText", panel.transform, "", 15, TextAlignmentOptions.Right,
            new Color32(255, 215, 90, 255), new Vector2(540, 422), new Vector2(620, 36));

        GameObject left = PanelBox(panel.transform, "CareerCharacterDataBox", new Vector2(-605, -10), new Vector2(570, 790));
        GameObject leftHeader = UI("CareerCharacterHeader", left.transform, Dark);
        Fixed(leftHeader, new Vector2(0, 368), new Vector2(550, 48));
        Text("CareerCharacterHeaderText", leftHeader.transform, "캐릭터 데이터", 23,
            TextAlignmentOptions.Left, Color.white, new Vector2(-105, 0), new Vector2(310, 42));
        Button("CareerCharacterSelectButton", leftHeader.transform, "캐릭터 선택  ▶",
            new Vector2(185, 0), new Vector2(170, 38), Red);

        Text("CareerSelectedCharacterNameText", left.transform, "Casual1", 20,
            TextAlignmentOptions.Left, Color.white, new Vector2(-175, 320), new Vector2(190, 34));
        Image portrait = ImageObject("CareerCharacterImage", left.transform, new Vector2(0, 126), new Vector2(500, 350));
        portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/Characters/Portraits/Image/Char0.png");
        portrait.preserveAspect = true;
        GameObject scoreBox = UI("CareerPerformanceScoreBox", left.transform, new Color32(73, 82, 94, 235));
        Fixed(scoreBox, new Vector2(0, -67), new Vector2(530, 92));
        Text("CareerPerformanceScoreText", scoreBox.transform, "-", 36, TextAlignmentOptions.Center,
            new Color32(255, 208, 50, 255), new Vector2(0, 14), new Vector2(300, 42));
        Text("CareerPerformanceScoreLabel", scoreBox.transform, "활약 점수", 16,
            TextAlignmentOptions.Center, new Color32(255, 208, 50, 255), new Vector2(0, -25), new Vector2(250, 28));

        string[] leftNames =
        {
            "CareerCharacterWinRate", "CareerCharacterMatches", "CareerCharacterKills",
            "CareerCharacterKd", "CareerCharacterMaxKills", "CareerCharacterMaxDamage"
        };
        string[] leftLabels = { "승률", "출전 횟수", "누적 처치 수", "K/D", "단일 최대 처치 수", "단일 최대 피해량" };
        for (int i = 0; i < leftNames.Length; ++i)
        {
            int row = i / 2;
            int col = i % 2;
            Vector2 pos = new Vector2(col == 0 ? -135 : 135, -160 - row * 98);
            GameObject stat = UI(leftNames[i] + "Box", left.transform, new Color32(86, 96, 108, 235));
            Fixed(stat, pos, new Vector2(255, 82));
            Text(leftNames[i] + "Text", stat.transform, "-", 24, TextAlignmentOptions.Center,
                Color.white, new Vector2(0, 13), new Vector2(225, 35));
            Text(leftNames[i] + "Label", stat.transform, leftLabels[i], 15, TextAlignmentOptions.Center,
                new Color32(210, 218, 225, 255), new Vector2(0, -21), new Vector2(225, 27));
        }

        GameObject right = PanelBox(panel.transform, "CareerGameDataBox", new Vector2(285, -10), new Vector2(1170, 790));
        GameObject rightHeader = UI("CareerGameHeader", right.transform, Dark);
        Fixed(rightHeader, new Vector2(0, 368), new Vector2(1150, 48));
        Text("CareerGameHeaderText", rightHeader.transform, "게임 데이터", 23,
            TextAlignmentOptions.Left, Color.white, new Vector2(-415, 0), new Vector2(310, 42));

        string[] gameNames =
        {
            "CareerGamePlayCount", "CareerGameAvgDamage", "CareerGameWins", "CareerGameWinRate",
            "CareerGameAvgKills", "CareerGameMaxAvgDamage", "CareerGameTotalKills",
            "CareerGameMaxAvgKills", "CareerGameMaxAssists", "CareerGameHeadshotRate",
            "CareerGameKd", "CareerGameMeleeKills", "CareerGamePlants", "CareerGameDefuses"
        };
        string[] gameLabels =
        {
            "플레이 횟수", "라운드 평균 피해량", "총 승리 횟수", "승률",
            "라운드 평균 처치 수", "최고 라운드 평균 피해량", "총 처치 수",
            "최고 라운드 평균 처치 수", "단일 최대 도움 수", "헤드샷률",
            "K/D", "총 근접 처치 수", "폭탄 설치 횟수", "폭탄 해체 횟수"
        };
        Vector2[] gamePositions =
        {
            new Vector2(-290, 245), new Vector2(290, 245),
            new Vector2(-430, 112), new Vector2(-145, 112), new Vector2(140, 112), new Vector2(425, 112),
            new Vector2(-430, -55), new Vector2(-145, -55), new Vector2(140, -55), new Vector2(425, -55),
            new Vector2(-430, -222), new Vector2(-145, -222), new Vector2(140, -222), new Vector2(425, -222)
        };
        for (int i = 0; i < gameNames.Length; ++i)
        {
            bool featured = i < 2;
            Vector2 size = featured ? new Vector2(545, 115) : new Vector2(265, 120);
            GameObject stat = UI(gameNames[i] + "Box", right.transform,
                featured ? new Color32(80, 90, 102, 235) : new Color32(92, 102, 114, 225));
            Fixed(stat, gamePositions[i], size);
            Text(gameNames[i] + "Text", stat.transform, "-", featured ? 38 : 24,
                TextAlignmentOptions.Center, featured ? new Color32(255, 208, 50, 255) : Color.white,
                new Vector2(0, featured ? 17 : 13), new Vector2(size.x - 20, featured ? 48 : 36));
            Text(gameNames[i] + "Label", stat.transform, gameLabels[i], featured ? 18 : 14,
                TextAlignmentOptions.Center, featured ? new Color32(255, 208, 50, 255) : new Color32(210, 218, 225, 255),
                new Vector2(0, featured ? -30 : -27), new Vector2(size.x - 18, featured ? 30 : 44));
        }

        GameObject popup = UI("CareerCharacterPopup", panel.transform, new Color32(55, 62, 72, 252));
        Fixed(popup, new Vector2(-290, -10), new Vector2(560, 650));
        popup.transform.SetAsLastSibling();
        Text("CareerCharacterPopupTitle", popup.transform, "캐릭터 선택", 25,
            TextAlignmentOptions.Left, Color.white, new Vector2(-110, 290), new Vector2(280, 44));
        Button("CareerCharacterPopupCloseButton", popup.transform, "X", new Vector2(245, 292), new Vector2(44, 40), Red);
        for (int i = 0; i < 4; ++i)
        {
            GameObject row = Button("CareerCharacterOption" + i, popup.transform, "",
                new Vector2(0, 205 - i * 135), new Vector2(510, 118), new Color32(112, 122, 134, 255));
            Image image = ImageObject("CareerCharacterOption" + i + "Image", row.transform,
                new Vector2(-195, 0), new Vector2(92, 100));
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectVector/Shared/Characters/Portraits/Image/Char{i}.png");
            image.preserveAspect = true;
            Text("CareerCharacterOption" + i + "NameText", row.transform,
                new[] { "Casual1", "PicoChan", "UnityChan", "UnityChanSSU" }[i], 20,
                TextAlignmentOptions.Left, Color.white, new Vector2(45, 18), new Vector2(350, 34));
            Text("CareerCharacterOption" + i + "StatsText", row.transform, "플레이 기록 없음", 15,
                TextAlignmentOptions.Left, new Color32(215, 222, 228, 255), new Vector2(45, -24), new Vector2(350, 30));
        }
        popup.SetActive(false);
        panel.SetActive(false);
    }
    private static void BuildDropdown(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; --i)
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>() ?? root.AddComponent<TMP_Dropdown>();
        float dropdownWidth = root.GetComponent<RectTransform>().sizeDelta.x;
        float captionWidth = Mathf.Max(120f, dropdownWidth - 70f);
        float itemLabelWidth = Mathf.Max(120f, dropdownWidth - 40f);
        TMP_Text caption = Text("Caption Text", root.transform, "전체 캐릭터", 18, TextAlignmentOptions.Left,
            Color.white, new Vector2(-18, 0), new Vector2(captionWidth, 42));
        Text("Arrow", root.transform, "V", 16, TextAlignmentOptions.Center, Color.white,
            new Vector2(dropdownWidth * .5f - 20f, 0), new Vector2(32, 42));

        GameObject template = UI("Template", root.transform, Dark2);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, -2);
        templateRect.sizeDelta = new Vector2(0, 250);
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = UI("Viewport", template.transform, new Color(0, 0, 0, 0));
        Stretch(viewport.GetComponent<RectTransform>(), 4);
        viewport.AddComponent<RectMask2D>();

        GameObject content = UI("Content", viewport.transform, new Color(0, 0, 0, 0));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 48);
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = UI("Item", content.transform, new Color32(60, 67, 78, 255));
        LayoutElement element = item.AddComponent<LayoutElement>();
        element.preferredHeight = 48;
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = item.GetComponent<Image>();
        GameObject checkmark = UI("Item Checkmark", item.transform, Red);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, .5f);
        checkRect.anchorMax = new Vector2(0, .5f);
        checkRect.sizeDelta = new Vector2(7, 40);
        checkRect.anchoredPosition = new Vector2(6, 0);
        toggle.graphic = checkmark.GetComponent<Image>();
        TMP_Text itemText = Text("Item Label", item.transform, "전체 캐릭터", 17,
            TextAlignmentOptions.Left, Color.white, new Vector2(10, 0), new Vector2(itemLabelWidth, 44));

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        dropdown.targetGraphic = root.GetComponent<Image>();
        dropdown.captionText = caption;
        dropdown.template = templateRect;
        dropdown.itemText = itemText;
        template.SetActive(false);
    }

    private static void SetDropdownOptions(GameObject root, params string[] labels)
    {
        TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(labels));
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
        if (dropdown.captionText != null && labels.Length > 0)
            dropdown.captionText.text = labels[0];
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform value in root.GetComponentsInChildren<Transform>(true))
            if (value.name == name) return value;
        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (Transform value in sceneRoot.GetComponentsInChildren<Transform>(true))
                if (value.name == name) return value.gameObject;
        }
        return null;
    }

    private static void ConfigureEntry(GameObject root)
    {
        GameObject playerIcon = FindInScene(root.scene, "PlayerIcon");
        if (playerIcon == null) return;
        Button button = playerIcon.GetComponent<Button>() ?? playerIcon.AddComponent<Button>();
        MyPagePanelController controller = root.GetComponent<MyPagePanelController>();
        MyPageOpenButton opener = playerIcon.GetComponent<MyPageOpenButton>() ?? playerIcon.AddComponent<MyPageOpenButton>();
        opener.Configure(controller);
        bool hasPersistentOpen = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); ++i)
        {
            if (button.onClick.GetPersistentTarget(i) == controller &&
                button.onClick.GetPersistentMethodName(i) == nameof(MyPagePanelController.OpenSelf))
            {
                hasPersistentOpen = true;
                break;
            }
        }
        if (!hasPersistentOpen)
            UnityEventTools.AddPersistentListener(button.onClick, controller.OpenSelf);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(opener);
    }

    private static GameObject Section(Transform parent,string name,float y,float height)
    {
        GameObject go=UI(name,parent,new Color(0,0,0,0));RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(1290,height);r.anchoredPosition=new Vector2(0,y);return go;
    }
    private static void SectionTitle(Transform parent,string title,string rightName,string right)
    {
        GameObject bar=UI(title+"Header",parent,Dark);RectTransform r=bar.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.sizeDelta=new Vector2(1270,48);r.anchoredPosition=new Vector2(0,-5);
        Text(title+"Text",bar.transform,title,23,TextAlignmentOptions.Left,Color.white,new Vector2(-500,0),new Vector2(280,42));
        if(!string.IsNullOrEmpty(rightName))Text(rightName,bar.transform,right,19,TextAlignmentOptions.Right,Color.white,new Vector2(500,0),new Vector2(250,42));
    }
    private static GameObject PanelBox(Transform parent,string name,Vector2 pos,Vector2 size,Vector2? anchor=null)
    { GameObject go=UI(name,parent,Panel);Fixed(go,pos,size,anchor??new Vector2(.5f,.5f));return go; }
    private static GameObject UI(string name,Transform parent,Color color)
    { GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go; }
    private static TMP_Text Text(string name,Transform parent,string value,float size,TextAlignmentOptions align,Color color,Vector2 pos,Vector2 dimensions,Vector2? anchor=null,Vector2? anchorMax=null,Vector2? anchoredOverride=null)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.layer=5;go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();Vector2 a=anchor??new Vector2(.5f,.5f);r.anchorMin=a;r.anchorMax=anchorMax??a;r.sizeDelta=dimensions;r.anchoredPosition=anchoredOverride??pos;
        TextMeshProUGUI text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.font=font;text.fontSize=size;text.alignment=align;text.color=color;text.enableWordWrapping=true;text.raycastTarget=false;return text;
    }
    private static GameObject Button(string name,Transform parent,string label,Vector2 pos,Vector2 size,Color color,Vector2? anchor=null)
    { GameObject go=UI(name,parent,color);go.AddComponent<Button>();Fixed(go,pos,size,anchor??new Vector2(.5f,.5f));if(!string.IsNullOrEmpty(label))Text(name+"Label",go.transform,label,17,TextAlignmentOptions.Center,Color.white,Vector2.zero,size-new Vector2(8,8));return go; }
    private static Image ImageObject(string name,Transform parent,Vector2 pos,Vector2 size)
    { GameObject go=UI(name,parent,Color.white);Fixed(go,pos,size);return go.GetComponent<Image>(); }
    private static void Fixed(GameObject go,Vector2 pos,Vector2 size,Vector2? anchor=null)
    { RectTransform r=go.GetComponent<RectTransform>();Vector2 a=anchor??new Vector2(.5f,.5f);r.anchorMin=r.anchorMax=a;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size; }
    private static void Stretch(RectTransform r,float inset=0)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(inset,inset);r.offsetMax=new Vector2(-inset,-inset);}
    private static void Anchor(GameObject go,float minX,float minY,float maxX,float maxY,float left,float bottom,float right,float top)
    {RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(minX,minY);r.anchorMax=new Vector2(maxX,maxY);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(right,top);}
    private static void AddVerticalScrollbar(GameObject scroll,ScrollRect sr)
    {
        GameObject bar=UI("Scrollbar Vertical",scroll.transform,Dark2);Anchor(bar,1,0,1,1,-12,0,0,0);Scrollbar sb=bar.AddComponent<Scrollbar>();sb.direction=Scrollbar.Direction.BottomToTop;
        GameObject area=UI("Sliding Area",bar.transform,new Color(0,0,0,0));Stretch(area.GetComponent<RectTransform>(),2);GameObject handle=UI("Handle",area.transform,Red);Stretch(handle.GetComponent<RectTransform>());sb.handleRect=handle.GetComponent<RectTransform>();sb.targetGraphic=handle.GetComponent<Image>();sr.verticalScrollbar=sb;sr.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }
}
#endif
