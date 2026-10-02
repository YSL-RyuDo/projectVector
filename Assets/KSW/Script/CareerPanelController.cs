using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CareerPanelController : MonoBehaviour
{
    private const int OverallRankingMode = 0;
    private const int CharacterRankingMode = 1;
    private const int CharacterCount = 4;
    [SerializeField] private CareerRankPresentation _rankPresentation;
    [SerializeField] private CareerTabsPresentation _tabsPresentation;
    private int _inventorySort;

    private enum TabState
    {
        Rank,
        Ranking,
        Achievements,
        CareerRecord,
        Inventory,
        CardCollection
    }

    private TabState _currentTabState = TabState.Rank;

    [Header("Buttons")]
    [SerializeField] private Button _rankButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private Button _achievementsButton;
    [SerializeField] private Button _careerRecordButton;
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private Button _cardCollectionButton;

    [Header("Panels")]
    [SerializeField] private GameObject _rankPanel;
    [SerializeField] private GameObject _rankingPanel;
    [SerializeField] private GameObject _achievementsPanel;
    [SerializeField] private GameObject _careerRecordPanel;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _cardCollectionPanel;

    [Header("Colors")]
    [SerializeField] private Color _buttonPressColor = Color.yellow;
    [SerializeField] private Color _defaultColor = Color.white;

    private Button[] _allButtons;
    private GameObject[] _allPanels;

    [Header("Rank Panel - Main UI")]
    [SerializeField] private Image _rankImage;
    [SerializeField] private TextMeshProUGUI _rankNameText;
    [SerializeField] private Slider _rankPointSlider;
    [SerializeField] private TextMeshProUGUI _rankPointText;

    [Header("Rank Panel - Stats UI")]
    [SerializeField] private TextMeshProUGUI _rankCountText;
    [SerializeField] private TextMeshProUGUI _roundKillCountText;
    [SerializeField] private TextMeshProUGUI _winRateText;
    [SerializeField] private TextMeshProUGUI _avgDamageText;
    [SerializeField] private TextMeshProUGUI _mvpCountText;

    [Header("Rank Panel - Sub Info UI")]
    [SerializeField] private Sprite[] _favoriteCharacterIcons;
    [SerializeField] private TextMeshProUGUI _seasonNoticeText;

    [Header("Rank Panel - Bottom Buttons")]
    [SerializeField] private Button _rankIconInfoButton;
    [SerializeField] private Button _seasonRewardButton;

    [SerializeField] private string _rankName = "-";
    [SerializeField] private int _currentPoint;
    [SerializeField] private int _minPoint;
    [SerializeField] private int _maxPoint = 100;
    [SerializeField] private int _totalRankPoints;
    [SerializeField] private bool _isUncappedRank;
    [SerializeField] private int _rankCount;
    [SerializeField] private float _roundKillCount;
    [SerializeField] private float _winRate;
    [SerializeField] private float _avgDamage;
    [SerializeField] private string _mvp = "MVP\n0";
    [SerializeField] private int _remainingSeasonDays;
    [SerializeField] private GameObject[] _statisticsGroup;

    [Header("Card Collection Tabs")]
    [SerializeField] private Button _achievementsTabButton;
    [SerializeField] private Button _cardCollectionTabButton;

    [Header("Card Collection Screens")]
    [SerializeField] private GameObject _achievementsScreen;
    [SerializeField] private GameObject _cardCollectionScreen;

    [Header("Card Collection Colors")]
    [SerializeField] private Color _selectButtonColor = Color.white;
    [SerializeField] private Color _defaultButtonColor = Color.gray;

    private readonly List<Transform> _rankingRows = new();
    private readonly List<Transform> _matchMapRows = new();
    private readonly List<Transform> _friendlyPlayerRows = new();
    private readonly List<Transform> _opponentPlayerRows = new();
    private readonly List<Transform> _inventorySlots = new();

    private readonly List<CareerMatchData> _matchHistory = new();
    private readonly List<CareerInventoryEntryData> _inventory = new();

    private Button _overallRankingButton;
    private Button _characterRankingButton;
    private Button _firstRankingPageButton;
    private Button _previousRankingPageButton;
    private Button _nextRankingPageButton;
    private Button _lastRankingPageButton;
    private TMP_InputField _rankingSearchInput;
    private TextMeshProUGUI _rankingPageText;
    private Transform _rankingHeader;

    private GameObject _rankingCharacterSelector;
    private Button _selectedCharacterButton;
    private Image _selectedCharacterIcon;
    private TextMeshProUGUI _selectedCharacterLabel;
    private GameObject _characterPopup;
    private readonly Button[] _characterButtons = new Button[CharacterCount];
    private readonly Sprite[] _characterSprites = new Sprite[CharacterCount];

    private int _rankingMode = OverallRankingMode;
    private int _selectedRankingCharacterId;
    private int _rankingPage = 1;
    private int _rankingTotalPages = 1;
    private string _rankingSearchQuery = string.Empty;
    private string _pendingRankingSearch = string.Empty;

    private Transform _matchHeader;
    private TMP_Dropdown _matchModeDropdown;
    private int _matchFilterType;
    private TextMeshProUGUI _inventoryNameText;
    private TextMeshProUGUI _inventoryDescriptionText;

    private void Awake()
    {
        _allButtons = new[]
        {
            _rankButton,
            _rankingButton,
            _achievementsButton,
            _careerRecordButton,
            _inventoryButton,
            _cardCollectionButton
        };

        _allPanels = new[]
        {
            _rankPanel,
            _rankingPanel,
            _achievementsPanel,
            _careerRecordPanel,
            _inventoryPanel,
            _cardCollectionPanel
        };

        for (int i = 0; i < _allButtons.Length; i++)
        {
            if (_allButtons[i] == null)
                continue;

            TabState targetState = (TabState)i;
            _allButtons[i].onClick.AddListener(() => ChangeTab(targetState));
        }

        CacheDynamicViews();
        _tabsPresentation?.Bind(this);
    }

    private void Start()
    {
        ChangeTab(TabState.Rank);
        OnAchievementsTabButton();
        ClearServerDrivenViews();
        RefreshFromServer();
    }

    public void RefreshFromServer()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[CAREER] NetworkManager is null");
            return;
        }

        NetworkManager.Instance.SendCareerDataRequests(_matchFilterType);
    }

    public void OnRankButton() => ChangeTab(TabState.Rank);
    public void OnRankingButton() => ChangeTab(TabState.Ranking);
    public void OnAchievementsButton() => ChangeTab(TabState.Achievements);
    public void OnCareerRecordButton() => ChangeTab(TabState.CareerRecord);
    public void OnInventoryButton() => ChangeTab(TabState.Inventory);
    public void OnCardCollectionButton() => ChangeTab(TabState.CardCollection);

    private void ChangeTab(TabState newState)
    {
        _rankPresentation?.CloseOverlays();
        _currentTabState = newState;
        int targetIndex = (int)newState;

        for (int i = 0; i < _allButtons.Length; i++)
        {
            if (_allButtons[i] != null && _allButtons[i].image != null)
            {
                _allButtons[i].image.color =
                    i == targetIndex ? _buttonPressColor : _defaultColor;
            }
        }

        for (int i = 0; i < _allPanels.Length; i++)
        {
            if (_allPanels[i] != null)
                _allPanels[i].SetActive(i == targetIndex);
        }
    }

    public void StatisticsGroupAnimation()
    {
        if (_rankPresentation != null) { _rankPresentation.PlayIntro(); return; }
        if (_statisticsGroup == null)
            return;

        foreach (GameObject group in _statisticsGroup)
        {
            if (group != null && group.TryGetComponent(out UIAnimator animator))
                animator.Show();
        }
    }

    public void ApplyOverview(CareerOverviewData data)
    {
        if (data == null)
            return;

        _rankName = string.IsNullOrWhiteSpace(data.tierName) ? "-" : data.tierName;
        _totalRankPoints = Math.Max(0, data.rankPoints);
        _isUncappedRank = data.tierMaxPoints < 0;
        _minPoint = 0;
        _maxPoint = _isUncappedRank ? 1 : 100;
        _currentPoint = _isUncappedRank
            ? 0
            : Math.Max(0, Math.Min(100, data.rankPoints - data.tierMinPoints));
        _rankCount = data.rankedMatches;
        _roundKillCount = data.roundsPlayed > 0
            ? (float)data.kills / data.roundsPlayed
            : 0f;
        _winRate = data.rankedMatches > 0
            ? (float)data.wins / data.rankedMatches
            : 0f;
        _avgDamage = data.roundsPlayed > 0
            ? (float)data.totalDamage / data.roundsPlayed
            : 0f;
        _mvp = $"MVP\n{data.mvpCount}";
        _remainingSeasonDays = Math.Max(0, data.remainingSeasonDays);

        UpdateRankPanelData();
        _rankPresentation?.Apply(data);
        _tabsPresentation?.ApplyOverview(data);
    }

    public void UpdateRankPanelData()
    {
        if (_rankNameText != null)
            _rankNameText.text = _rankName;

        if (_rankPointSlider != null)
        {
            _rankPointSlider.minValue = _minPoint;
            _rankPointSlider.maxValue = _maxPoint;
            _rankPointSlider.value = Mathf.Clamp(
                _currentPoint,
                _minPoint,
                _maxPoint
            );
        }

        if (_rankPointText != null)
        {
            _rankPointText.text = _isUncappedRank
                ? $"{_totalRankPoints}점 (상한 없음)"
                : $"{_currentPoint} / 100";
        }
        if (_rankCountText != null)
            _rankCountText.text = $"랭크 횟수\n{_rankCount}";
        if (_roundKillCountText != null)
            _roundKillCountText.text = $"라운드 처치 수\n{_roundKillCount:F2}";
        if (_winRateText != null)
            _winRateText.text = $"랭크 승률\n{_winRate * 100f:F0}%";
        if (_avgDamageText != null)
            _avgDamageText.text = $"라운드 평균 피해량\n{_avgDamage:F2}";
        if (_mvpCountText != null)
            _mvpCountText.text = _mvp;
        if (_seasonNoticeText != null)
        {
            _seasonNoticeText.text = _remainingSeasonDays > 0
                ? $"현재 시즌은 {_remainingSeasonDays}일 후 종료됩니다"
                : "현재 시즌이 종료되었습니다";
        }
    }

    public void ApplyRanking(CareerRankingPageData pageData)
    {
        if (pageData == null || pageData.rankingType != _rankingMode)
            return;

        if (_rankingMode == CharacterRankingMode &&
            pageData.characterId != _selectedRankingCharacterId)
        {
            return;
        }

        _rankingPage = Mathf.Max(1, pageData.page);
        _rankingTotalPages = Mathf.Max(1, pageData.totalPages);
        List<CareerRankingEntryData> entries =
            pageData.entries ?? new List<CareerRankingEntryData>();

        for (int i = 0; i < _rankingRows.Count; ++i)
        {
            Transform row = _rankingRows[i];
            bool hasData = i < entries.Count;
            row.gameObject.SetActive(hasData);

            if (!hasData)
                continue;

            CareerRankingEntryData entry = entries[i];
            string navigatorName = string.IsNullOrWhiteSpace(entry.nickname)
                ? entry.userId
                : entry.nickname;
            SetDirectChildText(row, "Text (TMP)", entry.position.ToString());
            SetDirectChildText(row, "Text (TMP) (1)", navigatorName);
            SetRankingProfileIcon(row, entry.playerIconId);
            SetDirectChildText(
                row,
                "Text (TMP) (2)",
                _rankingMode == CharacterRankingMode
                    ? entry.rankPoints.ToString("N0")
                    : entry.tierName
            );
            SetDirectChildText(row, "Text (TMP) (3)", entry.wins.ToString());
            SetSuperstringVisible(row, _tabsPresentation != null || _rankingMode == OverallRankingMode);
        }

        if (_rankingRows.Count > 0 &&
            _rankingRows[0].parent is RectTransform rankingContent)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rankingContent);
        }

        UpdateRankingPageControls();
        _tabsPresentation?.ApplyRanking(pageData, NetworkManager.Instance?.MyUserId);
    }

    public void ApplyMatchHistory(int filterType, List<CareerMatchData> matches)
    {
        if (filterType != _matchFilterType)
            return;

        SetMatchHistory(matches);
        _rankPresentation?.ApplyHistory(matches, NetworkManager.Instance?.MyUserId, filterType);
    }

    private void SetMatchHistory(List<CareerMatchData> matches)
    {
        _tabsPresentation?.ApplyHistory(matches);
        _matchHistory.Clear();
        if (matches != null)
            _matchHistory.AddRange(matches);

        for (int i = 0; i < _matchMapRows.Count; ++i)
        {
            bool hasData = i < _matchHistory.Count;
            _matchMapRows[i].gameObject.SetActive(hasData);

            if (!hasData)
                continue;

            CareerMatchData match = _matchHistory[i];
            TextMeshProUGUI label =
                _matchMapRows[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                string result = CareerTabsPresentation.Outcome(match);
                label.text = $"{match.mapName} ({result})";
            }
        }

        if (_matchHistory.Count > 0)
            ShowMatch(0);
        else
            ClearMatchDetails();
    }

    public void ApplyInventory(List<CareerInventoryEntryData> entries)
    {
        _inventory.Clear();
        if (entries != null)
            _inventory.AddRange(entries);

        SortCareerInventory(_inventorySort);
    }

    public void SortCareerInventory(int sort)
    {
        _inventorySort = Mathf.Clamp(sort, 0, 3);
        _inventory.Sort((a, b) =>
        {
            int comparison = _inventorySort switch
            {
                1 => b.acquiredAtUnix.CompareTo(a.acquiredAtUnix),
                2 => b.quantity.CompareTo(a.quantity),
                3 => string.Compare(a.itemName, b.itemName, StringComparison.Ordinal),
                _ => a.itemId.CompareTo(b.itemId)
            };
            return comparison != 0 ? comparison : a.itemId.CompareTo(b.itemId);
        });
        _tabsPresentation?.ApplyInventory(_inventory, _inventorySort);

        for (int i = 0; i < _inventorySlots.Count; ++i)
            _inventorySlots[i].gameObject.SetActive(i < _inventory.Count);

        if (_inventory.Count > 0)
            ShowInventoryItem(0);
        else
        {
            if (_inventoryNameText != null)
                _inventoryNameText.text = "보유 아이템 없음";
            if (_inventoryDescriptionText != null)
                _inventoryDescriptionText.text = "현재 보유한 커리어 아이템이 없습니다.";
        }
    }

    public void OnAchievementsTabButton()
    {
        if (_achievementsScreen != null)
            _achievementsScreen.SetActive(true);
        if (_cardCollectionScreen != null)
            _cardCollectionScreen.SetActive(false);

        SetButtonColor(_achievementsTabButton, _selectButtonColor);
        SetButtonColor(_cardCollectionTabButton, _defaultButtonColor);
    }

    public void OnCardCollectionTabButton()
    {
        if (_achievementsScreen != null)
            _achievementsScreen.SetActive(false);
        if (_cardCollectionScreen != null)
            _cardCollectionScreen.SetActive(true);

        SetButtonColor(_achievementsTabButton, _defaultButtonColor);
        SetButtonColor(_cardCollectionTabButton, _selectButtonColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.image != null)
            button.image.color = color;
    }

    private void CacheDynamicViews()
    {
        CacheRankingRows();
        CacheRankingControls();
        CacheCharacterSelector();
        UpdateRankingModeVisuals();
        CacheMatchViews();
        CacheInventoryViews();
    }

    private void CacheRankingRows()
    {
        _rankingRows.Clear();
        if (_rankingPanel == null)
            return;

        foreach (Transform transform in
                 _rankingPanel.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == "Rank Item" ||
                transform.name.StartsWith("Rank Item (", StringComparison.Ordinal))
            {
                _rankingRows.Add(transform);
            }
        }

        _rankingRows.Sort((lhs, rhs) =>
        {
            RectTransform lhsRect = lhs as RectTransform;
            RectTransform rhsRect = rhs as RectTransform;
            if (lhsRect != null && rhsRect != null &&
                !Mathf.Approximately(
                    lhsRect.anchoredPosition.y,
                    rhsRect.anchoredPosition.y))
            {
                return rhsRect.anchoredPosition.y.CompareTo(
                    lhsRect.anchoredPosition.y);
            }

            return lhs.GetSiblingIndex().CompareTo(rhs.GetSiblingIndex());
        });
    }

    private void CacheRankingControls()
    {
        if (_rankingPanel == null)
            return;

        Transform root = _rankingPanel.transform;
        _overallRankingButton = FindButtonByLabel(root, "종합 랭킹");
        _characterRankingButton = FindButtonByLabel(root, "캐릭터 랭킹");
        _firstRankingPageButton = FindButtonByLabel(root, "|<");
        _previousRankingPageButton = FindButtonByLabel(root, "<");
        _nextRankingPageButton = FindButtonByLabel(root, ">");
        _lastRankingPageButton = FindButtonByLabel(root, ">|");
        _rankingSearchInput = root.GetComponentInChildren<TMP_InputField>(true);
        _rankingHeader = FindDescendant(root, "Up");

        Transform pageControls = _firstRankingPageButton != null
            ? _firstRankingPageButton.transform.parent
            : null;
        if (pageControls != null)
        {
            foreach (Transform child in pageControls)
            {
                if (child.TryGetComponent(out TextMeshProUGUI text))
                {
                    _rankingPageText = text;
                    break;
                }
            }
        }

        ReplaceButtonListener(
            _overallRankingButton,
            () => SelectRankingMode(OverallRankingMode)
        );
        ReplaceButtonListener(
            _characterRankingButton,
            () => SelectRankingMode(CharacterRankingMode)
        );
        ReplaceButtonListener(_firstRankingPageButton, () => MoveRankingPage(1));
        ReplaceButtonListener(
            _previousRankingPageButton,
            () => MoveRankingPage(_rankingPage - 1)
        );
        ReplaceButtonListener(
            _nextRankingPageButton,
            () => MoveRankingPage(_rankingPage + 1)
        );
        ReplaceButtonListener(
            _lastRankingPageButton,
            () => MoveRankingPage(_rankingTotalPages)
        );

        if (_rankingSearchInput != null)
        {
            _rankingSearchInput.onValueChanged =
                new TMP_InputField.OnChangeEvent();
            _rankingSearchInput.onValueChanged.AddListener(
                OnRankingSearchChanged
            );
        }
    }

    private void CacheCharacterSelector()
    {
        if (_rankingPanel == null)
            return;

        _rankingCharacterSelector = FindDescendant(
            _rankingPanel.transform, "Career Ranking Character Selector")?.gameObject;
        if (_rankingCharacterSelector == null) return;

        Transform selected = FindDescendant(
            _rankingCharacterSelector.transform,
            "SelectedCharacterButton"
        );
        if (selected != null)
        {
            _selectedCharacterButton = selected.GetComponent<Button>();
            Transform icon = selected.Find("Icon");
            Transform label = selected.Find("Label");
            _selectedCharacterIcon = icon != null
                ? icon.GetComponent<Image>()
                : null;
            _selectedCharacterLabel = label != null
                ? label.GetComponent<TextMeshProUGUI>()
                : null;
            ReplaceButtonListener(
                _selectedCharacterButton,
                ToggleCharacterPopup
            );
        }

        Transform popup = FindDescendant(
            _rankingCharacterSelector.transform,
            "CharacterPopup"
        );
        _characterPopup = popup != null ? popup.gameObject : null;

        for (int characterId = 0;
             characterId < CharacterCount;
             ++characterId)
        {
            Transform buttonTransform = FindDescendant(
                _rankingCharacterSelector.transform,
                $"CharacterButton{characterId}"
            );
            if (buttonTransform == null)
                continue;

            Button button = buttonTransform.GetComponent<Button>();
            _characterButtons[characterId] = button;
            Transform icon = buttonTransform.Find("Icon");
            _characterSprites[characterId] = icon != null
                ? icon.GetComponent<Image>()?.sprite
                : null;

            int capturedCharacterId = characterId;
            ReplaceButtonListener(
                button,
                () => SelectRankingCharacter(capturedCharacterId)
            );
        }

        if (_characterPopup != null)
            _characterPopup.SetActive(false);
        UpdateSelectedCharacterVisual();
        _rankingCharacterSelector.SetActive(false);
    }

    private void SelectRankingMode(int rankingMode)
    {
        rankingMode = rankingMode == CharacterRankingMode
            ? CharacterRankingMode
            : OverallRankingMode;
        if (_rankingMode == rankingMode)
        {
            UpdateRankingModeVisuals();
            return;
        }

        _rankingMode = rankingMode;
        _rankingPage = 1;
        _rankingTotalPages = 1;
        if (_characterPopup != null)
            _characterPopup.SetActive(false);
        UpdateRankingModeVisuals();
        RequestRanking();
    }

    private void SelectRankingCharacter(int characterId)
    {
        _selectedRankingCharacterId = Mathf.Clamp(
            characterId,
            0,
            CharacterCount - 1
        );
        _rankingPage = 1;
        _rankingTotalPages = 1;
        if (_characterPopup != null)
            _characterPopup.SetActive(false);
        UpdateSelectedCharacterVisual();
        RequestRanking();
    }

    private void ToggleCharacterPopup()
    {
        if (_characterPopup != null)
            _characterPopup.SetActive(!_characterPopup.activeSelf);
    }

    private void UpdateSelectedCharacterVisual()
    {
        int characterId = Mathf.Clamp(
            _selectedRankingCharacterId,
            0,
            CharacterCount - 1
        );
        if (_selectedCharacterIcon != null)
            _selectedCharacterIcon.sprite = _characterSprites[characterId];
        if (_selectedCharacterLabel != null)
            _selectedCharacterLabel.text = _tabsPresentation == null ? $"캐릭터 {characterId}" :
                characterId switch { 0 => "이니시에이터", 1 => "컨트롤러", 2 => "센티널", _ => "듀얼리스트" };

        for (int i = 0; i < _characterButtons.Length; ++i)
        {
            SetButtonColor(
                _characterButtons[i],
                i == characterId ? _buttonPressColor : _defaultColor
            );
        }
    }

    private void UpdateRankingModeVisuals()
    {
        bool isCharacterRanking = _rankingMode == CharacterRankingMode;
        SetButtonColor(
            _overallRankingButton,
            isCharacterRanking ? _defaultColor : _buttonPressColor
        );
        SetButtonColor(
            _characterRankingButton,
            isCharacterRanking ? _buttonPressColor : _defaultColor
        );

        if (_rankingCharacterSelector != null)
            _rankingCharacterSelector.SetActive(isCharacterRanking);

        SetDirectChildText(
            _rankingHeader,
            "Text (TMP) (2)",
            isCharacterRanking ? "캐릭터 점수" : "랭크"
        );
        SetDirectChildText(
            _rankingHeader,
            "Text (TMP) (3)",
            "승리 횟수"
        );
        SetDirectChildText(
            _rankingHeader,
            "Text (TMP) (4)",
            _tabsPresentation != null ? "승률" : isCharacterRanking ? string.Empty : "슈퍼스트링"
        );

        foreach (Transform row in _rankingRows)
            SetSuperstringVisible(row, _tabsPresentation != null || !isCharacterRanking);
        UpdateRankingPageControls();
    }

    private void OnRankingSearchChanged(string value)
    {
        _pendingRankingSearch = (value ?? string.Empty).Trim();
        CancelInvoke(nameof(SendPendingRankingSearch));
        Invoke(nameof(SendPendingRankingSearch), 0.15f);
    }

    private void SendPendingRankingSearch()
    {
        _rankingSearchQuery = _pendingRankingSearch;
        _rankingPage = 1;
        _rankingTotalPages = 1;
        UpdateRankingPageControls();
        RequestRanking();
    }

    private void MoveRankingPage(int page)
    {
        int nextPage = Mathf.Clamp(page, 1, _rankingTotalPages);
        if (nextPage == _rankingPage)
            return;

        _rankingPage = nextPage;
        UpdateRankingPageControls();
        RequestRanking();
    }

    private void RequestRanking()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[CAREER RANKING] NetworkManager is null");
            return;
        }

        NetworkManager.Instance.SendCareerRankingRequest(
            _rankingMode,
            _selectedRankingCharacterId,
            _rankingPage,
            _rankingSearchQuery
        );
    }

    private void UpdateRankingPageControls()
    {
        _rankingPage = Mathf.Clamp(_rankingPage, 1, _rankingTotalPages);
        if (_rankingPageText != null)
            _rankingPageText.text = $"{_rankingPage}/{_rankingTotalPages}";

        bool canMoveBackward = _rankingPage > 1;
        bool canMoveForward = _rankingPage < _rankingTotalPages;
        if (_firstRankingPageButton != null)
            _firstRankingPageButton.interactable = canMoveBackward;
        if (_previousRankingPageButton != null)
            _previousRankingPageButton.interactable = canMoveBackward;
        if (_nextRankingPageButton != null)
            _nextRankingPageButton.interactable = canMoveForward;
        if (_lastRankingPageButton != null)
            _lastRankingPageButton.interactable = canMoveForward;
    }

    private void CacheMatchViews()
    {
        _matchMapRows.Clear();
        _friendlyPlayerRows.Clear();
        _opponentPlayerRows.Clear();

        if (_careerRecordPanel == null)
            return;

        Transform modeDropdownTransform = FindDescendant(
            _careerRecordPanel.transform,
            "Mode Dropdown"
        );
        _matchModeDropdown = modeDropdownTransform != null
            ? modeDropdownTransform.GetComponent<TMP_Dropdown>()
            : null;
        if (_matchModeDropdown != null)
        {
            _matchModeDropdown.ClearOptions();
            _matchModeDropdown.AddOptions(new List<string>
            {
                "전체 경기",
                "일반/사설방",
                "랭크 전용"
            });
            _matchModeDropdown.SetValueWithoutNotify(0);
            _matchModeDropdown.RefreshShownValue();
            _matchModeDropdown.onValueChanged.RemoveListener(OnMatchFilterChanged);
            _matchModeDropdown.onValueChanged.AddListener(OnMatchFilterChanged);
            _matchFilterType = 0;
        }

        Transform mapGroup = FindDescendant(_careerRecordPanel.transform, "Map Group");
        Transform content = mapGroup != null ? FindDescendant(mapGroup, "Content") : null;
        CacheNamedDirectChildren(content, "Map Item", _matchMapRows);

        for (int i = 0; i < _matchMapRows.Count; ++i)
        {
            int index = i;
            Button button = _matchMapRows[i].GetComponent<Button>();
            if (button == null)
                continue; // Authored by VectorCareerPresentationEditor before Play.
            button.targetGraphic = _matchMapRows[i].GetComponent<Image>();
            button.onClick.AddListener(() => ShowMatch(index));
        }

        Transform friendly = FindDescendant(
            _careerRecordPanel.transform,
            "Friendly forces"
        );
        Transform opponent = FindDescendant(
            _careerRecordPanel.transform,
            "Opponent"
        );
        CacheNamedDirectChildren(friendly, "Player Item", _friendlyPlayerRows);
        CacheNamedDirectChildren(opponent, "Player Item", _opponentPlayerRows);

        foreach (Transform child in _careerRecordPanel.transform)
        {
            if (FindText(child, "Text (TMP) (5)") != null)
            {
                _matchHeader = child;
                break;
            }
        }
    }

    private void OnMatchFilterChanged(int filterType)
    {
        _matchFilterType = Mathf.Clamp(filterType, 0, 2);
        SetMatchHistory(new List<CareerMatchData>());

        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[CAREER MATCH HISTORY] NetworkManager is null");
            return;
        }

        NetworkManager.Instance.SendCareerMatchHistoryRequest(_matchFilterType);
    }

    private void CacheInventoryViews()
    {
        _inventorySlots.Clear();
        if (_inventoryPanel == null)
            return;

        Transform grid = _tabsPresentation != null ? _tabsPresentation.inventoryGrid : null;
        if (grid == null) foreach (Transform transform in
                 _inventoryPanel.GetComponentsInChildren<Transform>(true))
        {
            if (grid == null || transform.childCount > grid.childCount)
                grid = transform;
        }

        if (grid != null)
        {
            foreach (Transform child in grid)
            {
                if (child.GetComponent<Image>() != null)
                    _inventorySlots.Add(child);
            }

            for (int i = 0; i < _inventorySlots.Count; ++i)
            {
                int index = i;
                Button button = _inventorySlots[i].GetComponent<Button>();
                if (button == null)
                    continue; // Authored by VectorCareerPresentationEditor before Play.
                button.targetGraphic = _inventorySlots[i].GetComponent<Image>();
                button.onClick.AddListener(() => ShowInventoryItem(index));
            }
        }

        foreach (Transform child in _inventoryPanel.transform)
        {
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
                continue;

            if (child.name == "Text (TMP)")
                _inventoryNameText = text;
            else if (child.name == "Text (TMP) (1)")
                _inventoryDescriptionText = text;
        }
    }

    private void ClearServerDrivenViews()
    {
        _rankName = "-";
        _currentPoint = 0;
        _minPoint = 0;
        _maxPoint = 100;
        _totalRankPoints = 0;
        _isUncappedRank = false;
        _rankCount = 0;
        _roundKillCount = 0f;
        _winRate = 0f;
        _avgDamage = 0f;
        _mvp = "MVP\n0";
        _remainingSeasonDays = 0;
        UpdateRankPanelData();
        _rankPresentation?.Clear();
        ClearRankingRows();
        UpdateRankingPageControls();
        SetMatchHistory(new List<CareerMatchData>());
        ApplyInventory(new List<CareerInventoryEntryData>());
        _tabsPresentation?.Clear();
    }

    private void ClearRankingRows()
    {
        foreach (Transform row in _rankingRows)
        {
            if (row != null)
                row.gameObject.SetActive(false);
        }
    }

    private void ShowMatch(int index)
    {
        if (index < 0 || index >= _matchHistory.Count)
            return;

        CareerMatchData match = _matchHistory[index];

        SetNestedText(_matchHeader, "Text (TMP) (1)", match.mapName);
        SetNestedText(
            _matchHeader,
            "Text (TMP) (3)",
            TimeSpan.FromSeconds(Math.Max(0, match.durationSeconds)).ToString(@"mm\:ss")
        );
        SetNestedText(
            _matchHeader,
            "Text (TMP) (5)",
            $"매치 번호: {match.matchId}"
        );

        List<CareerMatchPlayerData> friendly = new();
        List<CareerMatchPlayerData> opponent = new();

        foreach (CareerMatchPlayerData player in match.players ?? new List<CareerMatchPlayerData>())
        {
            if (player.team == match.myTeam)
                friendly.Add(player);
            else
                opponent.Add(player);
        }

        BindPlayerRows(_friendlyPlayerRows, friendly);
        BindPlayerRows(_opponentPlayerRows, opponent);
        _tabsPresentation?.SelectMatch(match, index, _friendlyPlayerRows, _opponentPlayerRows);
    }

    private void ClearMatchDetails()
    {
        SetNestedText(_matchHeader, "Text (TMP) (1)", "전적 없음");
        SetNestedText(_matchHeader, "Text (TMP) (3)", "00:00");
        SetNestedText(_matchHeader, "Text (TMP) (5)", "매치 번호: -");
        BindPlayerRows(_friendlyPlayerRows, new List<CareerMatchPlayerData>());
        BindPlayerRows(_opponentPlayerRows, new List<CareerMatchPlayerData>());
    }

    private static void BindPlayerRows(
        List<Transform> rows,
        List<CareerMatchPlayerData> players)
    {
        for (int i = 0; i < rows.Count; ++i)
        {
            bool hasData = i < players.Count;
            rows[i].gameObject.SetActive(hasData);
            if (!hasData)
                continue;

            CareerMatchPlayerData player = players[i];
            SetDirectChildText(rows[i], "Text (TMP)", player.nickname);
            SetDirectChildText(rows[i], "Text (TMP) (1)", player.combatScore.ToString());
            SetDirectChildText(rows[i], "Text (TMP) (2)", player.attackScore.ToString());
            SetDirectChildText(
                rows[i],
                "Text (TMP) (3)",
                $"{player.kills}/{player.deaths}/{player.assists}"
            );
            SetDirectChildText(rows[i], "Text (TMP) (4)", player.damage.ToString());
            SetDirectChildText(rows[i], "Text (TMP) (5)", player.skillScore.ToString());
            SetDirectChildText(rows[i], "Text (TMP) (6)", player.teamworkScore.ToString());
            SetDirectChildText(rows[i], "Text (TMP) (7)", player.isMvp ? "MVP" : string.Empty);
        }
    }

    private void ShowInventoryItem(int index)
    {
        if (index < 0 || index >= _inventory.Count)
            return;

        CareerInventoryEntryData entry = _inventory[index];
        bool starter = entry.itemId == 1 && entry.itemName == "Career Starter Badge";
        if (_inventoryNameText != null)
            _inventoryNameText.text = $"{(starter ? "커리어 시작 배지" : entry.itemName)} x{entry.quantity}";
        if (_inventoryDescriptionText != null)
            _inventoryDescriptionText.text = starter && entry.description == "Default career inventory catalog item"
                ? "커리어의 시작을 기념하는 배지입니다." : entry.description;
        _tabsPresentation?.SelectInventory(entry, index);
    }

    private static void CacheNamedDirectChildren(
        Transform parent,
        string baseName,
        List<Transform> result)
    {
        if (parent == null)
            return;

        foreach (Transform child in parent)
        {
            if (child.name == baseName ||
                child.name.StartsWith(baseName + " (", StringComparison.Ordinal))
            {
                result.Add(child);
            }
        }

        result.Sort((lhs, rhs) =>
            lhs.GetSiblingIndex().CompareTo(rhs.GetSiblingIndex()));
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == objectName)
                return transform;
        }

        return null;
    }

    private static Button FindButtonByLabel(Transform root, string label)
    {
        if (root == null)
            return null;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            TextMeshProUGUI text =
                button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null && text.text.Trim() == label)
                return button;
        }

        return null;
    }

    private static void ReplaceButtonListener(
        Button button,
        UnityEngine.Events.UnityAction listener)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(listener);
    }

    private static void SetSuperstringVisible(Transform row, bool visible)
    {
        if (row == null)
            return;

        foreach (Transform child in row)
        {
            if (child.GetComponent<Image>() != null && child.childCount >= 3)
            {
                child.gameObject.SetActive(visible);
                return;
            }
        }
    }

    private static TextMeshProUGUI FindText(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.transform.name == objectName)
                return text;
        }

        return null;
    }

    private static void SetNestedText(
        Transform root,
        string objectName,
        string value)
    {
        TextMeshProUGUI text = FindText(root, objectName);
        if (text != null)
            text.text = value;
    }

    private static void SetDirectChildText(
        Transform root,
        string objectName,
        string value)
    {
        if (root == null)
            return;

        Transform child = root.Find(objectName);
        if (child != null && child.TryGetComponent(out TextMeshProUGUI text))
            text.text = value;
    }

    private static void SetRankingProfileIcon(Transform row, int playerIconId)
    {
        if (row == null)
            return;

        Transform iconTransform = row.Find("Image");
        if (iconTransform == null ||
            !iconTransform.TryGetComponent(out Image iconImage))
        {
            return;
        }

        int safeIconId = Mathf.Max(0, playerIconId);
        Sprite icon = Resources.Load<Sprite>($"PlayerIcon/{safeIconId}");
        if (icon == null && safeIconId != 0)
            icon = Resources.Load<Sprite>("PlayerIcon/0");

        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.enabled = icon != null;
    }

}
