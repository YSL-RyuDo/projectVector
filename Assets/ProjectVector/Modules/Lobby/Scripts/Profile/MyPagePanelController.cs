using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MyPagePanelController : MonoBehaviour
{
    [SerializeField] private Sprite[] statIcons;
    public static MyPagePanelController Instance { get; private set; }

    private static readonly string[] StatKeys =
    {
        "winRate", "totalKills", "avgDamage", "playCount", "mvpRate",
        "headshotRate", "avgKills", "totalAssists", "totalDamage",
        "headshotKills", "skinCount", "weaponSkinCount", "spikePlants",
        "spikeDefuses", "mvpCount"
    };

    private readonly string[] displayedStats =
    {
        "winRate", "totalKills", "avgDamage", "playCount",
        "mvpRate", "headshotRate", "avgKills"
    };
    private MyPageProfileData current;
    private CanvasGroup panelCanvasGroup;
    private CanvasGroup battleCustomCanvasGroup;
    private CanvasGroup skinCustomCanvasGroup;
    private CanvasGroup visitorCanvasGroup;
    private readonly List<int> skinDropdownCharacterIds = new();
    private bool isOpen;
    private MyPageChipCraftController chipCraft;
    private MyPageLevelRewardController levelRewards;
    private MyPageHonorController honor;
    private MyPageTabController tabs;

    private static readonly Dictionary<string, string> StatLabels = new()
    {
        ["totalKills"] = "총 처치 수", ["playCount"] = "플레이 횟수",
        ["avgDamage"] = "라운드 평균 피해량", ["winRate"] = "승률",
        ["mvpRate"] = "MVP 확률", ["headshotRate"] = "헤드샷 처치율",
        ["avgKills"] = "라운드 평균 처치 수", ["totalAssists"] = "총 도움",
        ["totalDamage"] = "총 피해량", ["headshotKills"] = "헤드샷 처치 횟수",
        ["skinCount"] = "스킨 수", ["weaponSkinCount"] = "무기 스킨 수",
        ["spikePlants"] = "폭탄 설치 횟수", ["spikeDefuses"] = "폭탄 해체 횟수",
        ["mvpCount"] = "MVP 횟수"
    };

    private void Awake()
    {
        Instance = this;
        chipCraft = GetComponentInChildren<MyPageChipCraftController>(true);
        levelRewards = GetComponentInChildren<MyPageLevelRewardController>(true);
        honor = GetComponentInChildren<MyPageHonorController>(true);
        tabs = GetComponent<MyPageTabController>();
        EnsureViewportMasks();
        BindStatInteractions();
        RefreshCharacterRoles();
        panelCanvasGroup = GetComponent<CanvasGroup>();
        battleCustomCanvasGroup = Find("BattleCustomPanel")?.GetComponent<CanvasGroup>();
        skinCustomCanvasGroup = Find("SkinCustomPanel")?.GetComponent<CanvasGroup>();
        visitorCanvasGroup = Find("RecentVisitorsPopup")?.GetComponent<CanvasGroup>();
        BindButton("BackButton", Back);
        BindButton("PrivacyToggleButton", TogglePrivacy);
        BindButton("LikeButton", Like);
        BindButton("RecentVisitorsButton", () => ShowLayer(visitorCanvasGroup, true));
        BindButton("VisitorsCloseButton", () => ShowLayer(visitorCanvasGroup, false));
        BindButton("BattleCustomButton", () => ShowLayer(battleCustomCanvasGroup, true));
        BindButton("BattleCustomCloseButton", () => ShowLayer(battleCustomCanvasGroup, false));
        BindButton("SkinCustomButton", () => ShowLayer(skinCustomCanvasGroup, true));
        BindButton("SkinCustomCloseButton", () => ShowLayer(skinCustomCanvasGroup, false));
        BindButton("NicknameEditButton", () => SetText("StatusMessage", "닉네임 수정 기능은 준비 중입니다."));

        TMP_Dropdown dropdown = Find("SkinCharacterDropdown")?.GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "전체 캐릭터" });
            dropdown.onValueChanged.AddListener(FilterSkinCards);
        }

        SetVisible(false);
        ShowLayer(battleCustomCanvasGroup, false);
        ShowLayer(skinCustomCanvasGroup, false);
        ShowLayer(visitorCanvasGroup, false);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape) && !(LobbySocialController.Instance != null && LobbySocialController.Instance.BlocksProfileBack)) Back();
    }

    private void Back()
    {
        if (GetComponent<MyPageBirthdayController>()?.HandleBack() == true) return;
        foreach (var layer in new[] { visitorCanvasGroup, skinCustomCanvasGroup, battleCustomCanvasGroup })
            if (layer != null && layer.gameObject.activeSelf && layer.blocksRaycasts)
            { ShowLayer(layer, false); return; }
        if (tabs == null || !tabs.HandleBack()) Close();
    }

    public void OpenSelf() => OpenForUser(string.Empty);

    public void OpenForUser(string userId)
    {
        GetComponent<MyPageBirthdayController>()?.ResetContext();
        gameObject.SetActive(true);
        EnsureViewportMasks();
        RefreshCharacterRoles();
        SetVisible(true);
        chipCraft?.SetTarget(userId);
        levelRewards?.Close();
        honor?.Close();
        MyPageCareerDataController career = GetComponentInChildren<MyPageCareerDataController>(true);
        if (career != null)
        {
            career.SetTargetUser(userId ?? string.Empty);
            career.ShowProfilePage();
        }
        SetText("StatusMessage", "프로필 정보를 불러오는 중입니다.");
        ScrollRect scroll = Find("MainScrollView")?.GetComponent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendProfileRequest(userId ?? string.Empty);
            NetworkManager.Instance.SendChipCraftRequest(userId ?? string.Empty);
        }
        else
            SetText("StatusMessage", "서버 연결이 없어 프로필 데이터를 불러올 수 없습니다.");
    }

    public void Close()
    {
        GetComponent<MyPageBirthdayController>()?.ResetContext();
        ShowLayer(battleCustomCanvasGroup, false);
        ShowLayer(skinCustomCanvasGroup, false);
        ShowLayer(visitorCanvasGroup, false);
        chipCraft?.SetTarget(string.Empty);
        levelRewards?.Close();
        honor?.Close();
        SetVisible(false);
    }

    public void ShowLoadFailure() => SetText("StatusMessage", "프로필을 찾을 수 없습니다.");

    public void ApplyProfile(MyPageProfileData data)
    {
        if (data == null) return;
        current = data;
        SetText("StatusMessage", string.Empty);
        bool masked = data.privacyEnabled;
        SetText("ProfileNicknameText", data.nickname);
        SetText("ProfileIdText", masked ? "ID: 비공개" : $"ID: {data.userId}");
        SetText("ProfileLevelText", $"Lv.{data.level}");
        SetText("GuildText", "길드: 미가입");
        SetText("RegionText", masked ? "지역: 비공개" : $"지역: {data.region}");
        SetText("BirthdayText", masked ? "생일: 비공개" : $"생일: {data.birthday}");
        GetComponent<MyPageBirthdayController>()?.ApplyProfile(data);
        SetText("JoinDateText", $"가입일: {data.joinDate}");
        SetText("LikeCountText", data.likeCount.ToString("N0"));
        SetText("PrivacyStateText", data.privacyEnabled ? "ON" : "OFF");
        SetPrivacyVisual(data.privacyEnabled);

        SetSelfOnly("PrivacyLabel", data.isSelf);
        SetSelfOnly("PrivacyToggleButton", data.isSelf);
        SetSelfOnly("PrivacyStateText", data.isSelf);
        SetSelfOnly("BattleCustomButton", data.isSelf);
        SetSelfOnly("SkinCustomButton", data.isSelf);
        SetSelfOnly("ChipCraftOpenButton", data.isSelf);
        SetSelfOnly("LevelRewardOpenButton", data.isSelf);
        SetSelfOnly("HonorTabButton", data.isSelf);
        SetSelfOnly("ChipCraftCardOpenButton", data.isSelf);
        if (!data.isSelf)
        {
            ShowLayer(battleCustomCanvasGroup, false);
            ShowLayer(skinCustomCanvasGroup, false);
        }
        Button likeButton = Find("LikeButton")?.GetComponent<Button>();
        if (likeButton != null) likeButton.interactable = !data.isSelf;
        GameObject nicknameEdit = Find("NicknameEditButton")?.gameObject;
        if (nicknameEdit != null) nicknameEdit.SetActive(data.isSelf);

        ApplySprite("ProfileIconImage", Resources.Load<Sprite>($"PlayerIcon/{Mathf.Max(0, data.playerIconId)}"));
        ApplySprite("CardIconImage", Resources.Load<Sprite>($"IconCard/icon{Mathf.Clamp(data.playerIconId, 0, 2)}_card"));
        ApplyCardBorder(data.logoBorderId);
        SetText("CardNicknameText", data.nickname);
        SetText("CardLevelText", data.level.ToString());

        if (data.isSelf)
        {
            ProfilePanelController profile = FindObjectOfType<ProfilePanelController>();
            if (profile != null) profile.currentLogoBorderId = data.logoBorderId;
        }

        Sprite[] ranks = LoadRankSprites();
        ApplySprite("SeasonHighRankImage", SafeSprite(ranks, data.seasonHighestRankImageId));
        ApplySprite("CurrentRankImage", SafeSprite(ranks, data.currentRankImageId));
        ApplySprite("AllTimeRankImage", SafeSprite(ranks, data.allTimeHighestRankImageId));
        SetText("SeasonTitleText", $"시즌{data.seasonId}");
        SetText("SeasonHighRankText", $"이번 시즌 최고 랭크\n{RankName(data.seasonHighestRankImageId)}");
        SetText("CurrentRankText", $"현재 랭크\n{data.currentTierName}");
        SetText("AllTimeRankText", $"전체 시즌 최고 랭크\n시즌{data.allTimeHighestSeasonId} · {RankName(data.allTimeHighestRankImageId)}");

        ApplyBattleStatIds(data.battleStatIds);

        for (int i = 0; i < 4; ++i)
        {
            MyPageCharacterData character = data.characters.FirstOrDefault(value => value.characterId == i);
            bool played = character != null && character.matches > 0;
            SetText($"Character{i}NameText", CharacterName(i));
            SetText($"Character{i}RoleText", CharacterRole(i));
            SetText($"Character{i}StatsText", played
                ? $"승률 {(character.wins * 100f / character.matches):0.0}%   출전 {character.matches}   최고 처치 {character.maxKills}"
                : "플레이 기록 없음");
            Image image = Find($"Character{i}Image")?.GetComponent<Image>();
            if (image != null) image.color = played ? Color.white : new Color(0.35f, 0.38f, 0.43f, 0.45f);
        }

        RefreshBattleSlots();
        SetText("WeaponSkinCountText", $"{data.weaponSkinOwnedCount} / {data.weaponSkinTotalCount}\n무기 스킨 수");
        SetText("CharacterSkinCountText", $"{data.characterSkinOwnedCount} / {data.characterSkinTotalCount}\n캐릭터 스킨 수");
        RefreshSkinDropdown();
        RefreshSkinCardLabels();
        RefreshVisitors();
        RefreshStatCustomItems();
    }

    public void ApplyPrivacyResult(bool success, bool enabled)
    {
        if (!success || current == null) return;
        current.privacyEnabled = enabled;
        ApplyProfile(current);
    }

    public void ApplyLikeResult(bool success, string targetUserId, int count)
    {
        if (current == null || current.userId != targetUserId) return;
        current.likeCount = count;
        SetText("LikeCountText", count.ToString("N0"));
        SetText("StatusMessage", success ? "따봉을 보냈습니다." : "이미 따봉을 보냈습니다.");
    }

    public void ApplyBattleStatsResult(bool success, int[] battleStatIds)
    {
        if (current == null || !current.isSelf) return;
        if (!success || !ApplyBattleStatIds(battleStatIds))
        {
            SetText("BattleCustomHelp", "전투 데이터 구성을 저장하지 못했습니다. 다시 시도해 주세요.");
            NetworkManager.Instance?.SendProfileRequest(string.Empty);
            return;
        }

        current.battleStatIds = battleStatIds.ToArray();
        RefreshBattleSlots();
        RefreshStatCustomItems();
        SetText("BattleCustomHelp", "전투 데이터 구성을 서버에 저장했습니다.");
    }

    public void ApplyChipCraft(ChipCraftData value) => chipCraft?.ApplyData(value);
    public void ApplyChipCraftSaveResult(bool success, ChipCraftPresetData preset)
    {
        chipCraft?.ApplySaveResult(success, preset);
        if (success && preset != null)
        {
            if (current != null && current.isSelf)
            {
                current.playerIconId = preset.cardId;
                ApplySprite("ProfileIconImage", Resources.Load<Sprite>($"PlayerIcon/{preset.cardId}"));
            }
        }
    }

    public bool TryReplaceStat(int slotIndex, string statKey)
    {
        if (slotIndex < 0 || slotIndex >= displayedStats.Length || current == null || !current.isSelf) return false;
        if (!StatLabels.ContainsKey(statKey) || displayedStats.Contains(statKey)) return false;
        displayedStats[slotIndex] = statKey;
        RefreshBattleSlots();
        RefreshStatCustomItems();
        int[] statIds = displayedStats.Select(StatKeyToId).ToArray();
        current.battleStatIds = statIds;
        NetworkManager.Instance?.SendProfileBattleStatsSet(statIds);
        SetText("BattleCustomHelp", $"{StatLabels[statKey]} 항목으로 교체했습니다.\n서버에 저장하는 중입니다.");
        return true;
    }

    private bool ApplyBattleStatIds(IReadOnlyList<int> battleStatIds)
    {
        if (battleStatIds == null || battleStatIds.Count != displayedStats.Length) return false;
        HashSet<int> used = new();
        for (int i = 0; i < displayedStats.Length; ++i)
        {
            int statId = battleStatIds[i];
            if (statId < 0 || statId >= StatKeys.Length || !used.Add(statId)) return false;
        }
        for (int i = 0; i < displayedStats.Length; ++i) displayedStats[i] = StatKeys[battleStatIds[i]];
        return true;
    }

    public bool TryDropStatAtScreenPosition(Vector2 screenPosition, string statKey, Camera eventCamera)
    {
        for (int i = 0; i < displayedStats.Length; ++i)
        {
            RectTransform slot = Find($"BattleSlot{i}") as RectTransform;
            if (slot == null) continue;

            // Use the slot's real screen bounds instead of relying only on EventSystem's
            // drop event. This also works when an icon/text child receives the raycast.
            Vector3[] corners = new Vector3[4];
            slot.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
            Rect hitArea = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x) - 12f,
                Mathf.Min(min.y, max.y) - 12f,
                Mathf.Max(min.x, max.x) + 12f,
                Mathf.Max(min.y, max.y) + 12f);
            if (hitArea.Contains(screenPosition))
                return TryReplaceStat(i, statKey);
        }
        return false;
    }

    private void RefreshBattleSlots()
    {
        if (current == null) return;
        for (int i = 0; i < displayedStats.Length; ++i)
        {
            SetText($"BattleSlot{i}ValueText", GetStatValue(displayedStats[i]));
            SetText($"BattleSlot{i}LabelText", StatLabels[displayedStats[i]]);
            var icon = Find($"BattleSlot{i}Icon")?.GetComponent<Image>();
            int iconIndex = Array.IndexOf(StatKeys, displayedStats[i]);
            if (icon != null && statIcons != null)
            {
                icon.sprite = iconIndex >= 0 && iconIndex < statIcons.Length ? statIcons[iconIndex] : null;
                icon.color = icon.sprite != null ? Color.white : new Color32(29, 36, 46, 255);
            }
        }
    }

    private string GetStatValue(string key)
    {
        float winRate = current.playCount > 0 ? current.wins * 100f / current.playCount : 0f;
        float mvpRate = current.playCount > 0 ? current.mvpCount * 100f / current.playCount : 0f;
        float headshotRate = current.totalKills > 0 ? current.headshotKills * 100f / current.totalKills : 0f;
        float avgKills = current.roundsPlayed > 0 ? (float)current.totalKills / current.roundsPlayed : 0f;
        float avgDamage = current.roundsPlayed > 0 ? (float)current.totalDamage / current.roundsPlayed : 0f;
        return key switch
        {
            "totalKills" => current.totalKills.ToString("N0"), "playCount" => current.playCount.ToString("N0"),
            "avgDamage" => avgDamage.ToString("0.00"), "winRate" => winRate.ToString("0.00") + "%",
            "mvpRate" => mvpRate.ToString("0.00") + "%", "headshotRate" => headshotRate.ToString("0.00") + "%",
            "avgKills" => avgKills.ToString("0.00"), "totalAssists" => current.totalAssists.ToString("N0"),
            "totalDamage" => current.totalDamage.ToString("N0"), "headshotKills" => current.headshotKills.ToString("N0"),
            "skinCount" => (current.characterSkinOwnedCount + current.weaponSkinOwnedCount).ToString("N0"),
            "weaponSkinCount" => current.weaponSkinOwnedCount.ToString("N0"), "spikePlants" => current.spikePlants.ToString("N0"),
            "spikeDefuses" => current.spikeDefuses.ToString("N0"), "mvpCount" => current.mvpCount.ToString("N0"), _ => "0"
        };
    }

    private void RefreshStatCustomItems()
    {
        foreach (MyPageStatDragItem item in GetComponentsInChildren<MyPageStatDragItem>(true))
            item.SetAvailable(!displayedStats.Contains(item.StatKey));
    }

    private void RefreshVisitors()
    {
        for (int i = 0; i < 8; ++i)
        {
            Transform row = Find($"VisitorRow{i}");
            if (row == null) continue;
            bool visible = current != null && i < current.visitors.Count;
            row.gameObject.SetActive(visible);
            if (!visible) continue;
            MyPageVisitorData visitor = current.visitors[i];
            SetText($"VisitorRow{i}Text", $"{visitor.nickname}   ID {visitor.userId}");
            ApplySprite($"VisitorRow{i}Icon", Resources.Load<Sprite>($"PlayerIcon/{Mathf.Max(0, visitor.playerIconId)}"));
        }
        SetText("RecentVisitorsCountText", current == null ? "0" : current.visitors.Count.ToString());
    }

    private void FilterSkinCards(int option)
    {
        for (int i = 0; i < 12; ++i)
        {
            Transform card = Find($"SkinCard{i}");
            if (card == null) continue;
            int characterId = i / 3;
            bool owned = current != null && current.characters.Any(value => value.characterId == characterId && value.owned);
            int selectedCharacterId = option > 0 && option - 1 < skinDropdownCharacterIds.Count
                ? skinDropdownCharacterIds[option - 1]
                : -1;
            card.gameObject.SetActive(owned && (option == 0 || characterId == selectedCharacterId));
        }
    }

    private void RefreshSkinDropdown()
    {
        TMP_Dropdown dropdown = Find("SkinCharacterDropdown")?.GetComponent<TMP_Dropdown>();
        if (dropdown == null || current == null) return;
        skinDropdownCharacterIds.Clear();
        List<string> options = new() { "전체 캐릭터" };
        foreach (MyPageCharacterData character in current.characters.Where(value => value.owned).OrderBy(value => value.characterId))
        {
            skinDropdownCharacterIds.Add(character.characterId);
            options.Add(CharacterName(character.characterId));
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
        FilterSkinCards(0);
    }

    private void RefreshSkinCardLabels()
    {
        for (int i = 0; i < 12; ++i)
        {
            int characterId = i / 3;
            int skinIndex = i % 3;
            SetText($"SkinCard{i}Text",
                $"{CharacterName(characterId)}  ·  {(skinIndex == 0 ? "기본 스킨" : $"스킨 {skinIndex}")}");
        }
    }

    private void TogglePrivacy()
    {
        if (current != null && current.isSelf) NetworkManager.Instance?.SendProfilePrivacySet(!current.privacyEnabled);
    }

    private void Like()
    {
        if (current != null && !current.isSelf) NetworkManager.Instance?.SendProfileLike(current.userId);
    }

    private void SetPrivacyVisual(bool enabled)
    {
        RectTransform knob = Find("PrivacyKnob") as RectTransform;
        Image track = Find("PrivacyToggleButton")?.GetComponent<Image>();
        if (knob != null) knob.anchoredPosition = new Vector2(enabled ? 18f : -18f, 0f);
        if (track != null) track.color = enabled ? new Color32(210, 52, 67, 255) : new Color32(70, 76, 88, 255);
    }

    private void EnsureViewportMasks()
    {
        int disabled = 0;
        int added = 0;
        foreach (Mask legacyMask in GetComponentsInChildren<Mask>(true))
        {
            legacyMask.enabled = false;
            disabled++;
        }

        foreach (Transform value in GetComponentsInChildren<Transform>(true))
        {
            if (value.name == "Viewport" && value.GetComponent<RectMask2D>() == null)
            {
                value.gameObject.AddComponent<RectMask2D>();
                added++;
            }
        }
        if (disabled > 0 || added > 0)
            Debug.Log($"[MY PAGE] Runtime viewport repair: disabled Mask={disabled}, added RectMask2D={added}.");
    }

    private void BindStatInteractions()
    {
        for (int i = 0; i < 15; ++i)
        {
            Transform item = Find("StatItem" + i);
            if (item != null && item.GetComponent<CanvasGroup>() == null)
                item.gameObject.AddComponent<CanvasGroup>();
        }

        for (int i = 0; i < displayedStats.Length; ++i)
        {
            Transform slot = Find("BattleSlot" + i);
            if (slot == null) continue;
            MyPageStatDropSlot drop = slot.GetComponent<MyPageStatDropSlot>();
            if (drop == null) drop = slot.gameObject.AddComponent<MyPageStatDropSlot>();
            drop.Configure(i);
        }
    }


    private void RefreshCharacterRoles()
    {
        for (int i = 0; i < 4; ++i)
            SetText($"Character{i}RoleText", CharacterRole(i));
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;
        if (panelCanvasGroup == null) return;
        panelCanvasGroup.alpha = visible ? 1f : 0f;
        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;
        var motion = GetComponent<AuthoredPanelMotion>();
        if (visible) motion?.Play(); else motion?.ResetMotion();
        GetComponent<ProfileRevealSequence>()?.Show(visible);
    }

    private static void ShowLayer(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        if (visible) group.transform.SetAsLastSibling();
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        group.gameObject.SetActive(visible);
    }

    private void BindButton(string objectName, Action action)
    {
        Button button = Find(objectName)?.GetComponent<Button>();
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action());
    }

    private Transform Find(string objectName)
    {
        return GetComponentsInChildren<Transform>(true).FirstOrDefault(value => value.name == objectName);
    }

    private void SetText(string objectName, string value)
    {
        TMP_Text label = Find(objectName)?.GetComponent<TMP_Text>();
        if (label != null) label.text = value ?? string.Empty;
    }

    private void SetSelfOnly(string objectName, bool isSelf)
    {
        GameObject target = Find(objectName)?.gameObject;
        if (target != null) target.SetActive(isSelf);
    }

    private void ApplySprite(string objectName, Sprite sprite)
    {
        Image image = Find(objectName)?.GetComponent<Image>();
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ApplyCardBorder(int borderId)
    {
        Image preview = Find("CardBorderImage")?.GetComponent<Image>();
        if (preview == null) return;
        preview.sprite = Resources.Load<Sprite>("IconBorder/border");
        preview.enabled = preview.sprite != null;
        preview.color = Color.white;
    }

    private static Sprite[] LoadRankSprites() => new[]
    {
        Resources.Load<Sprite>("Rank/Bronze"), Resources.Load<Sprite>("Rank/Silver"),
        Resources.Load<Sprite>("Rank/Gold"), Resources.Load<Sprite>("Rank/Platinum"),
        Resources.Load<Sprite>("Rank/Diamond"), Resources.Load<Sprite>("Rank/Master")
    };
    private static Sprite SafeSprite(Sprite[] sprites, int index) => sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];
    private static string RankName(int index) => new[] { "브론즈", "실버", "골드", "플래티넘", "다이아몬드", "마스터" }[Mathf.Clamp(index, 0, 5)];
    private static int StatKeyToId(string key) => Array.IndexOf(StatKeys, key);
    // Temporary model names stay in the white title until formal character names are decided.
    private static string CharacterName(int index) => new[] { "Casual1", "PicoChan", "UnityChan", "UnityChanSSU" }[Mathf.Clamp(index, 0, 3)];
    private static string CharacterRole(int index) => new[] { "이니시에이터", "컨트롤러", "센티널", "엔트리" }[Mathf.Clamp(index, 0, 3)];
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
