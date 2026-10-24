using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReportUI : MonoBehaviour
{
    public static ReportUI Instance { get; private set; }

    [Header("Root & Panels")]
    [SerializeField] private GameObject rootPanel; // 전체 리포트 루트
    [SerializeField] private GameObject summaryView; // 요약 패널
    [SerializeField] private GameObject detailView; // 상세 패널
    [SerializeField] private GameObject emptyNotice; // 피해 기록 없음 알림
    [SerializeField] private GameObject failedNotice; // 조회 실패 알림
    [SerializeField] private GameObject limitWarningNotice; // 한도 경고

    [Header("Toggle Button")]
    [SerializeField] private Button toggleExpandButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("Summary View Lines (화면 그대로 4줄)")]
    [SerializeField] private TextMeshProUGUI opponentInfoText; // 1줄: A - 나를 처치한 상대
    [SerializeField] private TextMeshProUGUI damageSummaryText; // 2줄: 준 피해 X / 받은 피해 Y
    [SerializeField] private TextMeshProUGUI hitsDealtText; // 3줄: 내가 맞힌 부위: 머리 X회, 몸통 Y회, 다리 Z회
    [SerializeField] private TextMeshProUGUI hitsTakenText; // 4줄: 내가 맞은 부위: 머리 X회, 몸통 Y회, 다리 Z회

    [Header("Detail View Prefab & Container")]
    [SerializeField] private Transform detailItemContainer;
    [SerializeField] private GameObject detailItemPrefab;

    private bool isExpanded = false;
    private readonly List<OpponentCombatSummary> cachedSummaries = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (toggleExpandButton != null)
            toggleExpandButton.onClick.AddListener(ToggleExpand);

        CloseAndReset();
    }

    public void Open()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);

        SetExpanded(false);
    }

    public void CloseAndReset()
    {
        cachedSummaries.Clear();
        isExpanded = false;

        if (failedNotice != null) failedNotice.SetActive(false);
        if (emptyNotice != null) emptyNotice.SetActive(false);
        if (limitWarningNotice != null) limitWarningNotice.SetActive(false);

        ClearDetailItems();

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void ShowFailed()
    {
        if (rootPanel != null) rootPanel.SetActive(true);
        if (failedNotice != null) failedNotice.SetActive(true);
        if (summaryView != null) summaryView.SetActive(false);
        if (detailView != null) detailView.SetActive(false);
    }

    public void ApplyData(List<CombatReportRawEntry> rawEntries, uint killerId, bool limitReached)
    {
        Debug.Log(
    $"[ReportUI] ApplyData 호출: " +
    $"entries={rawEntries?.Count ?? -1}, " +
    $"killerId={killerId}"
);

        cachedSummaries.Clear();

        if (failedNotice != null) failedNotice.SetActive(false);
        if (limitWarningNotice != null) limitWarningNotice.SetActive(limitReached);

        Dictionary<uint, OpponentCombatSummary> dict = new();

        foreach (var entry in rawEntries)
        {
            if (!dict.TryGetValue(entry.opponentId, out var summary))
            {
                summary = new OpponentCombatSummary
                {
                    opponentId = entry.opponentId,
                    characterId = entry.characterId,
                    nickname = string.IsNullOrEmpty(entry.nickname) ? "알 수 없음" : entry.nickname,
                    isKiller = (entry.opponentId == killerId && killerId != 0)
                };
                dict[entry.opponentId] = summary;
            }

            summary.details.Add(entry);

            if (entry.direction == 0) // 내가 준 피해
            {
                summary.damageDealt += (entry.hpDamage + entry.barrierDamage);
                summary.hitsDealtHead += entry.headHits;
                summary.hitsDealtBody += entry.bodyHits;
                summary.hitsDealtLeg += entry.legHits;
            }
            else // 내가 받은 피해
            {
                summary.damageTaken += (entry.hpDamage + entry.barrierDamage);
                summary.hitsTakenHead += entry.headHits;
                summary.hitsTakenBody += entry.bodyHits;
                summary.hitsTakenLeg += entry.legHits;
            }
        }

        cachedSummaries.AddRange(dict.Values);
        cachedSummaries.Sort((a, b) => b.isKiller.CompareTo(a.isKiller));

        if (cachedSummaries.Count == 0)
        {
            if (emptyNotice != null) emptyNotice.SetActive(true);
            if (summaryView != null) summaryView.SetActive(false);
            if (detailView != null) detailView.SetActive(false);
            return;
        }

        if (emptyNotice != null) emptyNotice.SetActive(false);
        RefreshUI();
    }

    public void ToggleExpand()
    {
        SetExpanded(!isExpanded);
    }

    private void SetExpanded(bool expand)
    {
        isExpanded = expand;

        if (summaryView != null) summaryView.SetActive(!isExpanded);
        if (detailView != null) detailView.SetActive(isExpanded);

        if (toggleButtonText != null)
            toggleButtonText.text = isExpanded ? "요약" : "상세";
    }

    private void RefreshUI()
    {
        Debug.Log(
    $"[ReportUI] RefreshUI: " +
    $"summaries={cachedSummaries.Count}"
);

        if (cachedSummaries.Count == 0) return;

        OpponentCombatSummary mainOpponent = cachedSummaries[0];

        if (opponentInfoText != null)
        {
            opponentInfoText.text = mainOpponent.isKiller
                ? $"{mainOpponent.nickname} - 나를 처치한 상대"
                : $"{mainOpponent.nickname}";
        }
        if (damageSummaryText != null)
        {
            damageSummaryText.text = $"준 피해 {mainOpponent.damageDealt} / 받은 피해 {mainOpponent.damageTaken}";
        }
        if (hitsDealtText != null)
        {
            hitsDealtText.text = $"내가 맞힌 부위: 머리 {mainOpponent.hitsDealtHead}회, 몸통 {mainOpponent.hitsDealtBody}회, 다리 {mainOpponent.hitsDealtLeg}회";
        }
        if (hitsTakenText != null)
        {
            hitsTakenText.text = $"내가 맞은 부위: 머리 {mainOpponent.hitsTakenHead}회, 몸통 {mainOpponent.hitsTakenBody}회, 다리 {mainOpponent.hitsTakenLeg}회";
        }
        ClearDetailItems();

        if (detailItemPrefab != null && detailItemContainer != null)
        {
            foreach (var summary in cachedSummaries)
            {
                GameObject itemObj = Instantiate(detailItemPrefab, detailItemContainer);
                var itemView = itemObj.GetComponent<ReportDetailItemView>();
                if (itemView != null)
                {
                    itemView.Bind(summary);
                }
            }
        }
    }

    private void ClearDetailItems()
    {
        if (detailItemContainer == null) return;
        foreach (Transform child in detailItemContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public static string GetSourceDisplayName(byte sourceKind, int sourceId)
    {
        if (sourceKind == 0)
        {
            return WeaponDropSpawner.Instance?.GetWeaponData(sourceId)?.name ?? $"무기 ({sourceId})";
        }

        return sourceId switch
        {
            1 => "드론 테이저",
            2 => "이니시에이터 궁극기",
            3 => "엔트리 블레이드",
            4 => "센티널 궁극기",
            _ => "스킬 피해"
        };
    }
}

public class ReportDetailItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private GameObject killerBadge;
    [SerializeField] private TextMeshProUGUI damageDealtText;
    [SerializeField] private TextMeshProUGUI damageTakenText;
    [SerializeField] private TextMeshProUGUI hitsDealtText;
    [SerializeField] private TextMeshProUGUI hitsTakenText;
    [SerializeField] private TextMeshProUGUI sourceBreakdownText;

    public void Bind(OpponentCombatSummary summary)
    {
        if (opponentNameText != null)
            opponentNameText.text = summary.nickname;

        if (killerBadge != null)
            killerBadge.SetActive(summary.isKiller);

        if (damageDealtText != null)
            damageDealtText.text = $"준 피해: {summary.damageDealt}";

        if (damageTakenText != null)
            damageTakenText.text = $"받은 피해: {summary.damageTaken}";

        if (hitsDealtText != null)
            hitsDealtText.text = $"내가 맞힌 부위: 머리 {summary.hitsDealtHead}회, 몸통 {summary.hitsDealtBody}회, 다리 {summary.hitsDealtLeg}회";

        if (hitsTakenText != null)
            hitsTakenText.text = $"내가 맞은 부위: 머리 {summary.hitsTakenHead}회, 몸통 {summary.hitsTakenBody}회, 다리 {summary.hitsTakenLeg}회";

        if (sourceBreakdownText != null)
        {
            System.Text.StringBuilder sb = new();
            foreach (var detail in summary.details)
            {
                string sourceName = ReportUI.GetSourceDisplayName(detail.sourceKind, detail.sourceId);
                string dir = detail.direction == 0 ? "[준 피해]" : "[받은 피해]";
                sb.AppendLine($"{dir} {sourceName}: {detail.hpDamage + detail.barrierDamage} 데미지 (타격 {detail.bodyHits + detail.headHits + detail.legHits + detail.unlocalizedHits}회)");
            }
            sourceBreakdownText.text = sb.ToString().TrimEnd();
        }
    }
}