using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyPageCareerDataController : MonoBehaviour
{
    private static readonly string[] CharacterNames =
    {
        "Casual1", "PicoChan", "UnityChan", "UnityChanSSU"
    };

    private string targetUserId = string.Empty;
    private int selectedSeasonId;
    private int selectedModeType = 1; // 1: ranked, 0: normal, 2: private
    private int selectedCharacterId;
    private bool suppressDropdownEvents;
    private bool initialized;
    private readonly List<int> seasonIds = new();

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        BindButton("CareerCharacterSelectButton", ToggleCharacterPopup);
        BindButton("CareerCharacterPopupCloseButton", () => SetCharacterPopup(false));
        for (int i = 0; i < 4; ++i)
        {
            int characterId = i;
            BindButton($"CareerCharacterOption{i}", () => SelectCharacter(characterId));
        }

        TMP_Dropdown season = Find("CareerSeasonDropdown")?.GetComponent<TMP_Dropdown>();
        if (season != null) season.onValueChanged.AddListener(OnSeasonChanged);

        TMP_Dropdown mode = Find("CareerModeDropdown")?.GetComponent<TMP_Dropdown>();
        if (mode != null)
        {
            if (mode.options.Count < 3)
            {
                mode.ClearOptions();
                mode.AddOptions(new List<string> { "랭크", "일반", "사설" });
            }
            mode.SetValueWithoutNotify(0);
            mode.RefreshShownValue();
            if (mode.captionText != null) mode.captionText.text = "랭크";
            mode.onValueChanged.AddListener(OnModeChanged);
        }

        SetCharacterPopup(false);
    }

    public void SetTargetUser(string userId)
    {
        Initialize();
        targetUserId = userId ?? string.Empty;
        selectedSeasonId = 0;
        selectedModeType = 1;
        selectedCharacterId = 0;
        TMP_Dropdown mode = Find("CareerModeDropdown")?.GetComponent<TMP_Dropdown>();
        if (mode != null) mode.SetValueWithoutNotify(0);
        SetCharacterPopup(false);
    }

    public void ShowProfilePage()
    {
        GetComponentInParent<MyPageTabController>(true)?.Select(MyPageTabController.Page.Profile);
        SetCharacterPopup(false);
    }

    public void ShowCareerPage()
    {
        Initialize();
        GetComponentInParent<MyPageTabController>(true)?.Select(MyPageTabController.Page.Career);
        RequestData();
    }

    public void ApplyData(CareerDetailData data)
    {
        Initialize();
        if (data == null || !data.success)
        {
            SetText("CareerStatusText", "커리어 데이터를 불러오지 못했습니다.");
            ApplyCharacterEmpty();
            ApplyGameEmpty();
            return;
        }

        SetText("CareerStatusText", string.Empty);
        selectedSeasonId = data.seasonId;
        selectedModeType = data.modeType;
        selectedCharacterId = Mathf.Clamp(data.selectedCharacterId, 0, 3);
        RefreshSeasonDropdown(data.seasons, data.seasonId);
        RefreshModeDropdown(data.modeType);
        RefreshCharacterOptions(data.characters);
        ApplySelectedCharacter(data.characters.FirstOrDefault(value => value.characterId == selectedCharacterId));
        if (data.hasData) ApplyGameData(data); else ApplyGameEmpty();
    }

    private void RequestData()
    {
        SetText("CareerStatusText", "커리어 데이터를 불러오는 중입니다.");
        NetworkManager.Instance?.SendCareerDetailRequest(
            targetUserId, selectedSeasonId, selectedModeType, selectedCharacterId);
        if (NetworkManager.Instance == null)
            SetText("CareerStatusText", "서버 연결이 없어 커리어 데이터를 불러올 수 없습니다.");
    }

    private void RefreshSeasonDropdown(IReadOnlyList<CareerDetailSeasonData> seasons, int selectedId)
    {
        TMP_Dropdown dropdown = Find("CareerSeasonDropdown")?.GetComponent<TMP_Dropdown>();
        if (dropdown == null) return;
        suppressDropdownEvents = true;
        seasonIds.Clear();
        dropdown.ClearOptions();
        List<string> labels = new();
        if (seasons != null)
        {
            foreach (CareerDetailSeasonData season in seasons)
            {
                seasonIds.Add(season.seasonId);
                labels.Add($"시즌{season.seasonId}");
            }
        }
        if (labels.Count == 0)
        {
            seasonIds.Add(selectedId);
            labels.Add($"시즌{selectedId}");
        }
        dropdown.AddOptions(labels);
        int index = Mathf.Max(0, seasonIds.IndexOf(selectedId));
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
        if (dropdown.captionText != null) dropdown.captionText.text = labels[index];
        suppressDropdownEvents = false;
    }

    private void RefreshModeDropdown(int modeType)
    {
        TMP_Dropdown dropdown = Find("CareerModeDropdown")?.GetComponent<TMP_Dropdown>();
        if (dropdown == null) return;
        int index = modeType == 1 ? 0 : modeType == 0 ? 1 : 2;
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
        if (dropdown.captionText != null)
            dropdown.captionText.text = index == 0 ? "랭크" : index == 1 ? "일반" : "사설";
    }

    private void RefreshCharacterOptions(IReadOnlyList<CareerDetailCharacterData> characters)
    {
        for (int i = 0; i < 4; ++i)
        {
            CareerDetailCharacterData value = characters?.FirstOrDefault(entry => entry.characterId == i);
            SetText($"CareerCharacterOption{i}NameText", CharacterNames[i]);
            SetText($"CareerCharacterOption{i}StatsText", value != null && value.matches > 0
                ? $"승률 {Percent(value.wins, value.matches)}  ·  {value.matches}회"
                : "플레이 기록 없음");
        }
    }

    private void ApplySelectedCharacter(CareerDetailCharacterData value)
    {
        SetText("CareerSelectedCharacterNameText", CharacterNames[selectedCharacterId]);
        Image mainImage = Find("CareerCharacterImage")?.GetComponent<Image>();
        Image optionImage = Find($"CareerCharacterOption{selectedCharacterId}Image")?.GetComponent<Image>();
        if (mainImage != null)
        {
            mainImage.sprite = optionImage != null ? optionImage.sprite : null;
            mainImage.preserveAspect = true;
        }
        if (value == null || value.matches <= 0)
        {
            ApplyCharacterEmpty();
            return;
        }
        if (mainImage != null) mainImage.enabled = mainImage.sprite != null;
        SetText("CareerPerformanceScoreText", value.performanceScore.ToString("N0"));
        SetText("CareerCharacterWinRateText", Percent(value.wins, value.matches));
        SetText("CareerCharacterMatchesText", value.matches.ToString("N0"));
        SetText("CareerCharacterKillsText", value.kills.ToString("N0"));
        SetText("CareerCharacterKdText", Ratio(value.kills, value.deaths));
        SetText("CareerCharacterMaxKillsText", value.maxKills.ToString("N0"));
        SetText("CareerCharacterMaxDamageText", value.maxDamage.ToString("N0"));
    }

    private void ApplyCharacterEmpty()
    {
        Image image = Find("CareerCharacterImage")?.GetComponent<Image>();
        if (image != null) image.enabled = image.sprite != null;
        string[] names =
        {
            "CareerPerformanceScoreText", "CareerCharacterWinRateText", "CareerCharacterMatchesText",
            "CareerCharacterKillsText", "CareerCharacterKdText", "CareerCharacterMaxKillsText",
            "CareerCharacterMaxDamageText"
        };
        foreach (string name in names) SetText(name, "-");
    }

    private void ApplyGameData(CareerDetailData data)
    {
        SetText("CareerGamePlayCountText", data.playCount.ToString("N0"));
        SetText("CareerGameWinsText", data.wins.ToString("N0"));
        SetText("CareerGameWinRateText", Percent(data.wins, data.playCount));
        SetText("CareerGameAvgDamageText", Average(data.totalDamage, data.roundsPlayed));
        SetText("CareerGameAvgKillsText", Average(data.kills, data.roundsPlayed));
        SetText("CareerGameMaxAvgDamageText", Hundredths(data.maxRoundDamageHundredths));
        SetText("CareerGameTotalKillsText", data.kills.ToString("N0"));
        SetText("CareerGameMaxAvgKillsText", Hundredths(data.maxRoundKillsHundredths));
        SetText("CareerGameMaxAssistsText", data.maxAssists.ToString("N0"));
        SetText("CareerGameHeadshotRateText", Percent(data.headshotKills, data.kills));
        SetText("CareerGameKdText", Ratio(data.kills, data.deaths));
        SetText("CareerGameMeleeKillsText", data.meleeKills.ToString("N0"));
        SetText("CareerGamePlantsText", data.spikePlants.ToString("N0"));
        SetText("CareerGameDefusesText", data.spikeDefuses.ToString("N0"));
    }

    private void ApplyGameEmpty()
    {
        string[] names =
        {
            "CareerGamePlayCountText", "CareerGameWinsText", "CareerGameWinRateText",
            "CareerGameAvgDamageText", "CareerGameAvgKillsText", "CareerGameMaxAvgDamageText",
            "CareerGameTotalKillsText", "CareerGameMaxAvgKillsText", "CareerGameMaxAssistsText",
            "CareerGameHeadshotRateText", "CareerGameKdText", "CareerGameMeleeKillsText",
            "CareerGamePlantsText", "CareerGameDefusesText"
        };
        foreach (string name in names) SetText(name, "-");
    }

    private void OnSeasonChanged(int index)
    {
        if (suppressDropdownEvents || index < 0 || index >= seasonIds.Count) return;
        selectedSeasonId = seasonIds[index];
        RequestData();
    }

    private void OnModeChanged(int index)
    {
        if (suppressDropdownEvents) return;
        selectedModeType = index == 0 ? 1 : index == 1 ? 0 : 2;
        RequestData();
    }

    private void SelectCharacter(int characterId)
    {
        selectedCharacterId = Mathf.Clamp(characterId, 0, 3);
        SetCharacterPopup(false);
        RequestData();
    }

    private void ToggleCharacterPopup()
    {
        Transform popup = Find("CareerCharacterPopup");
        if (popup != null) popup.gameObject.SetActive(!popup.gameObject.activeSelf);
    }

    private void SetCharacterPopup(bool visible) => SetActive("CareerCharacterPopup", visible);

    private static string Percent(int numerator, int denominator) =>
        denominator <= 0 ? "0.00%" : $"{numerator * 100f / denominator:0.00}%";

    private static string Ratio(int numerator, int denominator) =>
        denominator <= 0 ? numerator.ToString("0.00") : (numerator / (float)denominator).ToString("0.00");

    private static string Average(int total, int count) =>
        count <= 0 ? "0.00" : (total / (float)count).ToString("0.00");

    private static string Hundredths(int value) => (value / 100f).ToString("0.00");

    private void BindButton(string name, Action callback)
    {
        Button button = Find(name)?.GetComponent<Button>();
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback());
    }

    private void SetText(string name, string value)
    {
        TMP_Text text = Find(name)?.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private void SetActive(string name, bool active)
    {
        Transform value = Find(name);
        if (value != null) value.gameObject.SetActive(active);
    }

    private Transform Find(string name)
    {
        foreach (Transform value in GetComponentsInChildren<Transform>(true))
            if (value.name == name) return value;
        return null;
    }
}
