using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CareerPanelController : MonoBehaviour
{
    #region Tab State
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
    #endregion

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

    #region Init
    private void Awake()
    {
        _allButtons = new Button[]
        {
            _rankButton,
            _rankingButton,
            _achievementsButton,
            _careerRecordButton,
            _inventoryButton,
            _cardCollectionButton
        };

        _allPanels = new GameObject[]
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
            if (_allButtons[i] == null) continue;

            TabState targetState = (TabState)i;
            _allButtons[i].onClick.AddListener(() => ChangeTab(targetState));
        }
    }

    private void Start()
    {
        ChangeTab(TabState.Rank);
        UpdateRankPanelData();
        OnAchievementsTabButton();
    }

    public void OnRankButton() => ChangeTab(TabState.Rank);
    public void OnRankingButton() => ChangeTab(TabState.Ranking);
    public void OnAchievementsButton() => ChangeTab(TabState.Achievements);
    public void OnCareerRecordButton() => ChangeTab(TabState.CareerRecord);
    public void OnInventoryButton() => ChangeTab(TabState.Inventory);
    public void OnCardCollectionButton() => ChangeTab(TabState.CardCollection);
    #endregion

    #region Tab Contorl
    private void ChangeTab(TabState newState)
    {
        _currentTabState = newState;
        int targetIndex = (int)newState;

        for (int i = 0; i < _allButtons.Length; i++)
        {
            if (_allButtons[i] != null && _allButtons[i].image != null)
            {
                _allButtons[i].image.color = (i == targetIndex) ? _buttonPressColor : _defaultColor;
            }
        }

        for (int i = 0; i < _allPanels.Length; i++)
        {
            if (_allPanels[i] != null)
            {
                _allPanels[i].SetActive(i == targetIndex);
            }
        }
    }
    #endregion

    #region Rank Panel Control
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
    [SerializeField] private Image[] _favoriteCharacterIcons;
    [SerializeField] private TextMeshProUGUI _seasonNoticeText;

    [Header("Rank Panel - Bottom Buttons")]
    [SerializeField] private Button _rankIconInfoButton;
    [SerializeField] private Button _seasonRewardButton;

    [SerializeField] private string _rankName = "원소 II";
    [SerializeField] private int _currentPoint = 11;
    [SerializeField] private int _maxPoint = 100;

    [SerializeField] private int _rankCount = 5;                // 횟수는 int
    [SerializeField] private float _roundKillCount = 1.24f;     // 소수점 -> float
    [SerializeField] private float _winRate = 0.60f;            // 60% -> float (0.6f)
    [SerializeField] private float _avgDamage = 383.28f;        // 소수점 -> float
    [SerializeField] private int _mvpCount = 3;                 // 횟수는 int
    [SerializeField] private int _remainingSeasonDays = 65;     // 남은 일수는 int

    public void UpdateRankPanelData()
    {
        if (_rankNameText != null) _rankNameText.text = _rankName;

        if (_rankPointSlider != null)
        {
            _rankPointSlider.minValue = 0;
            _rankPointSlider.maxValue = _maxPoint;
            _rankPointSlider.value = _currentPoint;
        }
        if (_rankPointText != null) _rankPointText.text = $"{_currentPoint} / {_maxPoint}";

        if (_rankCountText != null)
            _rankCountText.text = _rankCount.ToString();
        if (_roundKillCountText != null)
            _roundKillCountText.text = _roundKillCount.ToString("F2");
        if (_winRateText != null)
            _winRateText.text = $"{(_winRate * 100f):F0}%";
        if (_avgDamageText != null)
            _avgDamageText.text = _avgDamage.ToString("F2");
        if (_mvpCountText != null)
            _mvpCountText.text = _mvpCount.ToString();
        if (_seasonNoticeText != null)
            _seasonNoticeText.text = $"현재 시즌은 {_remainingSeasonDays}일 후 종료됩니다";
    }
    #endregion

    #region Career Record Panel

    #endregion

    #region Card Collection Panel

    [Header("Buttons")]
    [SerializeField] private Button _achievementsTabButton;
    [SerializeField] private Button _cardCollectionTabButton;

    [Header("Screens")]
    [SerializeField] private GameObject _achievementsScreen;
    [SerializeField] private GameObject _cardCollectionScreen;

    [Header("Colors")]
    [SerializeField] private Color _selectButtonColor = Color.white;
    [SerializeField] private Color _defaultButtonColor = Color.gray;

    public void OnAchievementsTabButton()
    {
        if (_achievementsScreen != null) _achievementsScreen.SetActive(true);
        if (_cardCollectionScreen != null) _cardCollectionScreen.SetActive(false);

        SetButtonColor(_achievementsTabButton, _selectButtonColor);
        SetButtonColor(_cardCollectionTabButton, _defaultButtonColor);
    }

    public void OnCardCollectionTabButton()
    {
        if (_achievementsScreen != null) _achievementsScreen.SetActive(false);
        if (_cardCollectionScreen != null) _cardCollectionScreen.SetActive(true);

        SetButtonColor(_achievementsTabButton, _defaultButtonColor);
        SetButtonColor(_cardCollectionTabButton, _selectButtonColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            button.image.color = color;
        }
    }

    #endregion
}