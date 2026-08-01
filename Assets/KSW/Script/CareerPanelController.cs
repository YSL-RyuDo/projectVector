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

    // --- 실제 내부 수치 데이터 (float/int 관리) ---
    private string _rankName = "원소 II";
    private int _currentPoint = 11;
    private int _maxPoint = 100;

    private int _rankCount = 5;               // 횟수는 int
    private float _roundKillCount = 1.24f;     // 소수점 -> float
    private float _winRate = 0.60f;            // 60% -> float (0.6f)
    private float _avgDamage = 383.28f;        // 소수점 -> float
    private int _mvpCount = 3;                 // 횟수는 int
    private int _remainingSeasonDays = 65;     // 남은 일수는 int

    /// <summary>
    /// float/int 수치 데이터를 UI 텍스트 형태로 변환하여 갱신합니다.
    /// </summary>
    public void UpdateRankPanelData()
    {
        // 1. 메인 정보
        if (_rankNameText != null) _rankNameText.text = _rankName;

        if (_rankPointSlider != null)
        {
            _rankPointSlider.minValue = 0;
            _rankPointSlider.maxValue = _maxPoint;
            _rankPointSlider.value = _currentPoint;
        }
        if (_rankPointText != null) _rankPointText.text = $"{_currentPoint} / {_maxPoint}";

        // 2. 통계 정보 (float 포맷팅 적용)
        if (_rankCountText != null)
            _rankCountText.text = _rankCount.ToString();

        // 소수점 둘째 자리까지 표시 ("F2") -> 1.24
        if (_roundKillCountText != null)
            _roundKillCountText.text = _roundKillCount.ToString("F2");

        // float(0.6f)를 백분율 퍼센트 텍스트("P0" 또는 계산)로 출력 -> 60%
        if (_winRateText != null)
            _winRateText.text = $"{(_winRate * 100f):F0}%";

        // 소수점 둘째 자리까지 표시 ("F2") -> 383.28
        if (_avgDamageText != null)
            _avgDamageText.text = _avgDamage.ToString("F2");

        if (_mvpCountText != null)
            _mvpCountText.text = _mvpCount.ToString();

        // 3. 시즌 안내
        if (_seasonNoticeText != null)
            _seasonNoticeText.text = $"현재 시즌은 {_remainingSeasonDays}일 후 종료됩니다";
    }
    #endregion
}