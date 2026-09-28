using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct HonorChangeData
{
    public readonly long appliedAtUnix;
    public readonly int delta;
    public readonly byte reason;

    public HonorChangeData(long appliedAtUnix, int delta, byte reason)
    {
        this.appliedAtUnix = appliedAtUnix;
        this.delta = delta;
        this.reason = reason;
    }
}

// All layout and graphics are saved in Lobby.unity. This component only binds state and input.
public sealed class MyPageHonorController : MonoBehaviour
{
    private static readonly int[] Thresholds = { 0, 0, 301, 601, 901, 1201, 1601 };
    private static readonly string[] GradeNames = { "", "위험", "주의 필요", "보통", "양호", "우수", "탁월" };
    private static readonly string[] Perks =
    {
        "사용자 지정 방/난투", "챌린지", "채팅/음성", "랭크 폭파",
        "전용 스킨", "전용 훈장", "전용 케이스"
    };
    private static readonly int[] PerkGrade = { 1, 2, 3, 3, 5, 6, 6 };

    [SerializeField] private RewardReceiptPopup receiptPopup;
    [SerializeField] private Sprite currency0Icon;
    [SerializeField] private Sprite currency1Icon;
    private readonly Button[] sectionButtons = new Button[4];
    private readonly GameObject[] sections = new GameObject[4];
    private bool initialized;
    private int section;
    private int score = 601;
    private int hoveredGrade;
    private int claimedGrade;
    private int page = 1;
    private int pageCount = 1;
    private bool loaded;
    private bool claiming;
    private float requestDeadline;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        for (int i = 0; i < 4; ++i)
        {
            int target = i;
            sectionButtons[i] = Find($"HonorSectionButton{i}")?.GetComponent<Button>();
            sections[i] = Find($"HonorSection{i}")?.gameObject;
            if (sectionButtons[i] != null)
                sectionButtons[i].onClick.AddListener(() => ShowSection(target));
        }
        Bind("HonorClaim5", () => Claim(5));
        Bind("HonorClaim6", () => Claim(6));
        Bind("HonorFirstPage", () => RequestPage(1));
        Bind("HonorPreviousPage", () => RequestPage(page - 1));
        Bind("HonorNextPage", () => RequestPage(page + 1));
        Bind("HonorLastPage", () => RequestPage(pageCount));
        if (receiptPopup == null)
            receiptPopup = GetComponentInParent<MyPagePanelController>(true)
                ?.GetComponentInChildren<RewardReceiptPopup>(true);
        if (currency0Icon == null)
            currency0Icon = Resources.Load<Sprite>("LevelRewards/Currency0Ticket");
        if (currency1Icon == null)
            currency1Icon = Resources.Load<Sprite>("LevelRewards/Currency1Crystal");
        ShowSection(0);
    }

    public void Open()
    {
        Initialize();
        gameObject.SetActive(true);
        loaded = false;
        claiming = false;
        hoveredGrade = 0;
        RefreshCurrentGrade();
        RefreshRewards();
        requestDeadline = Time.unscaledTime + 15f;
        SetText("HonorStatus", "명예 정보를 불러오는 중입니다.");
        ShowSection(0);
        NetworkManager.Instance?.SendHonorRequest(1);
    }

    public void Close() => gameObject.SetActive(false);

    public void ApplyState(bool success, int updatedScore, int updatedClaimedGrade,
        int updatedPage, int updatedPageCount, IReadOnlyList<HonorChangeData> history)
    {
        Initialize();
        loaded = success;
        requestDeadline = 0;
        if (!success)
        {
            SetText("HonorStatus", "명예 정보를 불러오지 못했습니다.");
            return;
        }
        score = Mathf.Clamp(updatedScore, 0, 1800);
        claimedGrade = Mathf.Clamp(updatedClaimedGrade, 0, 6);
        page = Mathf.Clamp(updatedPage, 1, 5);
        pageCount = Mathf.Clamp(updatedPageCount, 1, 5);
        SetText("HonorStatus", string.Empty);
        RefreshCurrentGrade();
        RefreshGradeDescription(hoveredGrade > 0 ? hoveredGrade : GradeForScore(score));
        RefreshRewards();
        RefreshHistory(history);
    }

    public void ApplyClaim(bool success, int updatedGrade, int freeAward, int victoryAward)
    {
        claiming = false;
        requestDeadline = 0;
        RefreshRewards();
        if (!success)
        {
            receiptPopup?.ShowMessage("명예 등급 보상을 수령할 수 없습니다.");
            return;
        }
        claimedGrade = updatedGrade;
        RefreshRewards();
        var rewards = new List<RewardReceiptPopup.RewardItem>();
        if (freeAward > 0) rewards.Add(new RewardReceiptPopup.RewardItem("재화 0", currency0Icon, freeAward));
        if (victoryAward > 0) rewards.Add(new RewardReceiptPopup.RewardItem("재화 1", currency1Icon, victoryAward));
        receiptPopup?.Show(rewards);
        NetworkManager.Instance?.SendHonorRequest(page);
    }

    public void HoverGrade(int grade)
    {
        if (section != 0) return;
        hoveredGrade = Mathf.Clamp(grade, 1, 6);
        RefreshGradeDescription(hoveredGrade);
    }

    public void LeaveGrade()
    {
        hoveredGrade = 0;
        if (section == 0) RefreshGradeDescription(GradeForScore(score));
    }

    private void RefreshCurrentGrade()
    {
        int grade = GradeForScore(score);
        SetText("HonorCurrentBadgeNumber", grade.ToString());
        HonorEmblemGraphic emblem = Find("HonorCurrentBadge")?.GetComponent<HonorEmblemGraphic>();
        if (emblem != null) emblem.Grade = grade;
        Image badgeEdge = Find("HonorCurrentBadgeMid")?.GetComponent<Image>();
        if (badgeEdge != null) badgeEdge.color = grade >= 4
            ? new Color32(236, 179, 61, 255) : new Color32(190, 172, 137, 255);
        SetText("HonorCurrentGradeTitle", $"{grade}  {GradeNames[grade]}");
        SetText("HonorCurrentScore", $"{score:N0} / 1,800");
        Image fill = Find("HonorProgressFill")?.GetComponent<Image>();
        if (fill != null) fill.fillAmount = score / 1800f;
        SetText("HonorVoiceStatus", "보통");
        SetText("HonorVoiceDetail", "음성 매너 평가는 준비 중입니다.\n기본 상태로 표시합니다.");
        for (int i = 1; i <= 6; ++i)
            Find($"HonorCurrentMarker{i}")?.gameObject.SetActive(i == grade);
    }

    private void RefreshGradeDescription(int viewedGrade)
    {
        int currentGrade = GradeForScore(score);
        for (int i = 1; i <= 6; ++i)
        {
            Find($"HonorGradeHighlight{i}")?.gameObject.SetActive(i == viewedGrade);
            Find($"HonorGradeCurrent{i}")?.gameObject.SetActive(i == currentGrade);
        }
        SetText("HonorViewedGrade", $"{viewedGrade}  {GradeNames[viewedGrade]}");
        SetText("HonorViewedThreshold", $"{Thresholds[viewedGrade]:N0}점부터");
        for (int i = 0; i < Perks.Length; ++i)
        {
            SetText($"HonorPerk{i}Name", Perks[i]);
            Image item = Find($"HonorPerk{i}")?.GetComponent<Image>();
            if (item != null) item.color = viewedGrade >= PerkGrade[i]
                ? new Color32(104, 124, 164, 255) : new Color32(88, 91, 99, 220);
            SetText($"HonorPerk{i}State", viewedGrade >= PerkGrade[i] ? "해제" : "잠김");
            TMP_Text state = Find($"HonorPerk{i}State")?.GetComponent<TMP_Text>();
            if (state != null) state.color = viewedGrade >= PerkGrade[i]
                ? new Color32(90, 230, 112, 255) : new Color32(198, 203, 211, 255);
            Graphic glyph = Find($"HonorPerk{i}Icon")?.GetComponent<Graphic>();
            if (glyph != null) glyph.color = viewedGrade >= PerkGrade[i]
                ? Color.white : new Color32(164, 172, 183, 255);
        }
    }

    private void RefreshRewards()
    {
        int grade = GradeForScore(score);
        for (int milestone = 5; milestone <= 6; ++milestone)
        {
            bool claimed = claimedGrade >= milestone;
            bool unlocked = grade >= milestone;
            SetText($"HonorReward{milestone}State", claimed ? "수령 완료" : unlocked ? "수령 가능" : "잠김");
            Button button = Find($"HonorClaim{milestone}")?.GetComponent<Button>();
            if (button != null) button.interactable = loaded && !claiming && unlocked && !claimed;
        }
    }

    private void RefreshHistory(IReadOnlyList<HonorChangeData> history)
    {
        SetText("HonorHistoryPage", $"{page} / {pageCount}");
        Find("HonorHistoryEmpty")?.gameObject.SetActive(history == null || history.Count == 0);
        foreach (string name in new[] { "HonorFirstPage", "HonorPreviousPage" })
        {
            Button control = Find(name)?.GetComponent<Button>();
            if (control != null) control.interactable = page > 1;
        }
        foreach (string name in new[] { "HonorNextPage", "HonorLastPage" })
        {
            Button control = Find(name)?.GetComponent<Button>();
            if (control != null) control.interactable = page < pageCount;
        }
        for (int i = 0; i < 10; ++i)
        {
            bool has = history != null && i < history.Count;
            GameObject row = Find($"HonorHistoryRow{i}")?.gameObject;
            if (row != null) row.SetActive(has);
            if (!has) continue;
            var change = history[i];
            SetText($"HonorHistoryReason{i}", ReasonName(change.reason));
            string date;
            try { date = DateTimeOffset.FromUnixTimeSeconds(change.appliedAtUnix).ToLocalTime().ToString("yyyy-MM-dd HH:mm"); }
            catch { date = "-"; }
            SetText($"HonorHistoryTime{i}", date);
            TMP_Text delta = Find($"HonorHistoryDelta{i}")?.GetComponent<TMP_Text>();
            if (delta != null)
            {
                delta.text = change.delta > 0 ? $"+{change.delta}" : change.delta.ToString();
                delta.color = change.delta < 0 ? new Color32(220, 79, 99, 255) : Color.white;
            }
        }
    }

    private void ShowSection(int index)
    {
        section = index;
        for (int i = 0; i < sections.Length; ++i)
        {
            if (sections[i] != null) sections[i].SetActive(i == index);
            if (sectionButtons[i]?.image != null)
                sectionButtons[i].image.color = i == index
                    ? new Color32(190, 43, 57, 255) : new Color32(61, 67, 77, 255);
        }
        hoveredGrade = 0;
        if (index == 0) LeaveGrade();
    }

    private void Claim(int grade)
    {
        if (loaded && !claiming && GradeForScore(score) >= grade && claimedGrade < grade)
        {
            claiming = true;
            requestDeadline = Time.unscaledTime + 15f;
            RefreshRewards();
            NetworkManager.Instance?.SendHonorClaim(grade);
        }
    }

    private void Update()
    {
        if (requestDeadline <= 0 || Time.unscaledTime < requestDeadline) return;
        requestDeadline = 0;
        claiming = false;
        RefreshRewards();
        SetText("HonorStatus", "서버 응답이 지연됩니다. 명예 화면을 다시 열어 확인해 주세요.");
    }

    private void RequestPage(int requested)
    {
        if (requested < 1 || requested > pageCount || requested == page) return;
        NetworkManager.Instance?.SendHonorRequest(requested);
    }

    private static int GradeForScore(int value)
    {
        for (int i = 6; i >= 2; --i)
            if (value >= Thresholds[i]) return i;
        return 1;
    }

    private static string ReasonName(byte reason) => reason switch
    {
        1 => "게임 완료", 2 => "경기 중 이탈", 3 => "입장 확인 거절",
        4 => "플레이 방해", 5 => "채팅 위반", 6 => "매치 조작", _ => "명예 변경"
    };

    private void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        Button button = Find(name)?.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(action);
    }

    private Transform Find(string name)
    {
        foreach (Transform item in GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }

    private void SetText(string name, string value)
    {
        TMP_Text item = Find(name)?.GetComponent<TMP_Text>();
        if (item != null) item.text = value;
    }
}
