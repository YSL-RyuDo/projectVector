
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ReportUI : MonoBehaviour
{
    public static ReportUI Instance { get; private set; }

    [Header("Root & Panels")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject summaryView;
    [SerializeField] private GameObject detailView;
    [SerializeField] private GameObject emptyNotice;
    [SerializeField] private GameObject failedNotice;
    [SerializeField] private GameObject limitWarningNotice;

    [Header("Toggle Button (Optional)")]
    [SerializeField] private Button toggleExpandButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("Summary View Lines")]
    [SerializeField] private TextMeshProUGUI opponentInfoText;
    [SerializeField] private TextMeshProUGUI damageSummaryText;
    [SerializeField] private TextMeshProUGUI hitsDealtText;
    [SerializeField] private TextMeshProUGUI hitsTakenText;

    [Header("Detail View")]
    [SerializeField] private Transform detailItemContainer;
    [SerializeField] private GameObject detailItemPrefab;
    [SerializeField] private bool spawnPerWeaponLine = true;

    [Header("Timeout Settings")]
    [SerializeField] private float requestTimeoutSeconds = 3.0f;

    private bool isExpanded = false;
    private bool hasValidData = false;
    private bool isWaitingResponse = false;
    private float requestSentTime = 0f;

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

    private void Update()
    {
        if (rootPanel == null || !rootPanel.activeSelf)
            return;

        if (isWaitingResponse &&
            Time.unscaledTime - requestSentTime >= requestTimeoutSeconds)
        {
            Debug.LogWarning("[ReportUI] 전투 리포트 응답 타임아웃");
            isWaitingResponse = false;
            ShowFailed();
            return;
        }

        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        if (!isSpectating || GameplayActionPolicy.MenuBlocksInput)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleExpand();
        }
    }

    public void Open()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);

        isExpanded = false;
        hasValidData = false;
        isWaitingResponse = true;
        requestSentTime = Time.unscaledTime;

        if (failedNotice != null) failedNotice.SetActive(false);
        if (emptyNotice != null) emptyNotice.SetActive(false);
        if (limitWarningNotice != null) limitWarningNotice.SetActive(false);
        if (summaryView != null) summaryView.SetActive(false);
        if (detailView != null) detailView.SetActive(false);

        UpdateToggleButtonLabel();
    }

    public void NotifyPageReceived()
    {
        requestSentTime = Time.unscaledTime;
    }

    public void CloseAndReset()
    {
        cachedSummaries.Clear();

        hasValidData = false;
        isWaitingResponse = false;
        isExpanded = false;

        if (failedNotice != null) failedNotice.SetActive(false);
        if (emptyNotice != null) emptyNotice.SetActive(false);
        if (limitWarningNotice != null) limitWarningNotice.SetActive(false);
        if (summaryView != null) summaryView.SetActive(false);
        if (detailView != null) detailView.SetActive(false);

        ClearDetailItems();

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void ShowFailed()
    {
        isWaitingResponse = false;
        hasValidData = false;

        if (rootPanel != null) rootPanel.SetActive(true);
        if (failedNotice != null) failedNotice.SetActive(true);
        if (emptyNotice != null) emptyNotice.SetActive(false);
        if (limitWarningNotice != null) limitWarningNotice.SetActive(false);
        if (summaryView != null) summaryView.SetActive(false);
        if (detailView != null) detailView.SetActive(false);
    }

    public void ApplyData(
        List<CombatReportRawEntry> rawEntries,
        uint killerId,
        bool limitReached)
    {
        Debug.Log(
            $"[ReportUI] ApplyData 호출: " +
            $"entries={rawEntries?.Count ?? 0}, " +
            $"killerId={killerId}, limit={limitReached}");

        isWaitingResponse = false;
        cachedSummaries.Clear();
        hasValidData = false;

        if (failedNotice != null)
            failedNotice.SetActive(false);

        if (limitWarningNotice != null)
            limitWarningNotice.SetActive(limitReached);

        if (rawEntries == null || rawEntries.Count == 0)
        {
            if (emptyNotice != null)
                emptyNotice.SetActive(true);

            if (summaryView != null)
                summaryView.SetActive(false);

            if (detailView != null)
                detailView.SetActive(false);

            return;
        }

        Dictionary<uint, OpponentCombatSummary> dict = new();

        foreach (var entry in rawEntries)
        {
            if (!dict.TryGetValue(entry.opponentId, out var summary))
            {
                summary = new OpponentCombatSummary
                {
                    opponentId = entry.opponentId,
                    characterId = entry.characterId,
                    nickname = string.IsNullOrWhiteSpace(entry.nickname)
                        ? "알 수 없음"
                        : entry.nickname,
                    isKiller = killerId != 0 &&
                               entry.opponentId == killerId
                };

                dict[entry.opponentId] = summary;
            }
            else if (killerId != 0 &&
                     entry.opponentId == killerId)
            {
                summary.isKiller = true;
            }

            summary.details.Add(entry);

            uint entryDamage = entry.hpDamage + entry.barrierDamage;

            if (entry.direction == 0)
            {
                summary.damageDealt += entryDamage;

                if (entry.sourceKind == 0)
                {
                    summary.hitsDealtHead += entry.headHits;
                    summary.hitsDealtBody += entry.bodyHits;
                    summary.hitsDealtLeg += entry.legHits;
                }
            }
            else
            {
                summary.damageTaken += entryDamage;

                if (entry.sourceKind == 0)
                {
                    summary.hitsTakenHead += entry.headHits;
                    summary.hitsTakenBody += entry.bodyHits;
                    summary.hitsTakenLeg += entry.legHits;
                }
            }
        }

        foreach (var summary in dict.Values)
        {
            summary.details.Sort((a, b) =>
            {
                int kindCmp = a.sourceKind.CompareTo(b.sourceKind);
                if (kindCmp != 0)
                    return kindCmp;

                int dirCmp = a.direction.CompareTo(b.direction);
                if (dirCmp != 0)
                    return dirCmp;

                uint dmgA = a.hpDamage + a.barrierDamage;
                uint dmgB = b.hpDamage + b.barrierDamage;

                return dmgB.CompareTo(dmgA);
            });
        }

        cachedSummaries.AddRange(dict.Values);

        cachedSummaries.Sort((a, b) =>
        {
            int killerCmp = b.isKiller.CompareTo(a.isKiller);
            if (killerCmp != 0)
                return killerCmp;

            uint totalA = a.damageDealt + a.damageTaken;
            uint totalB = b.damageDealt + b.damageTaken;

            return totalB.CompareTo(totalA);
        });

        hasValidData = true;

        if (emptyNotice != null)
            emptyNotice.SetActive(false);

        RefreshUI();
        UpdateViewVisibility();
    }

    public void ToggleExpand()
    {
        if (!hasValidData)
            return;

        isExpanded = !isExpanded;
        UpdateViewVisibility();
    }

    private void UpdateViewVisibility()
    {
        UpdateToggleButtonLabel();

        if (!hasValidData)
        {
            if (summaryView != null)
                summaryView.SetActive(false);

            if (detailView != null)
                detailView.SetActive(false);

            return;
        }

        if (summaryView != null)
            summaryView.SetActive(!isExpanded);

        if (detailView != null)
            detailView.SetActive(isExpanded);
    }

    private void UpdateToggleButtonLabel()
    {
        if (toggleButtonText != null)
            toggleButtonText.text = isExpanded ? "요약 [E]" : "상세 [E]";
    }


    private void RefreshUI()
    {
        if (cachedSummaries.Count == 0)
            return;

        OpponentCombatSummary mainOpponent = cachedSummaries[0];

        if (opponentInfoText != null)
        {
            opponentInfoText.text = mainOpponent.isKiller
                ? $"{mainOpponent.nickname} - 나를 처치한 상대"
                : mainOpponent.nickname;
        }

        if (damageSummaryText != null)
        {
            damageSummaryText.text =
                $"준 피해 {mainOpponent.damageDealt} / " +
                $"받은 피해 {mainOpponent.damageTaken}";
        }

        if (hitsDealtText != null)
        {
            hitsDealtText.text =
                $"내가 맞힌 부위: 머리 {mainOpponent.hitsDealtHead}회, " +
                $"몸통 {mainOpponent.hitsDealtBody}회, " +
                $"다리 {mainOpponent.hitsDealtLeg}회";
        }

        if (hitsTakenText != null)
        {
            hitsTakenText.text =
                $"내가 맞은 부위: 머리 {mainOpponent.hitsTakenHead}회, " +
                $"몸통 {mainOpponent.hitsTakenBody}회, " +
                $"다리 {mainOpponent.hitsTakenLeg}회";
        }

        ClearDetailItems();

        if (detailItemPrefab == null || detailItemContainer == null)
        {
            Debug.LogError("[ReportUI] 상세 항목 프리팹 또는 컨테이너가 연결되지 않았습니다.");
            return;
        }

        foreach (OpponentCombatSummary summary in cachedSummaries)
        {
            foreach (CombatReportRawEntry detail in summary.details)
            {
                GameObject itemObject = Instantiate(
                    detailItemPrefab,
                    detailItemContainer
                );

                CombatReportDetailItemUI itemUI =
                    itemObject.GetComponent<CombatReportDetailItemUI>();

                if (itemUI == null)
                {
                    Debug.LogError(
                        "[ReportUI] 프리팹에 CombatReportDetailItemUI가 없습니다."
                    );

                    Destroy(itemObject);
                    continue;
                }

                string sourceName = GetSourceDisplayName(
                    detail.sourceKind,
                    detail.sourceId,
                    detail.characterId
                );

                uint totalDamage = detail.hpDamage + detail.barrierDamage;

                if (detail.sourceKind == 0)
                {
                    itemUI.SetWeaponData(
                        sourceName,
                        totalDamage,
                        detail.hpDamage,
                        detail.barrierDamage,
                        detail.headHits,
                        detail.bodyHits,
                        detail.legHits
                    );
                }
                else if (detail.sourceKind == 1)
                {
                    uint skillHitCount =
                        detail.headHits +
                        detail.bodyHits +
                        detail.legHits +
                        detail.unlocalizedHits;

                    itemUI.SetSkillData(
                        sourceName,
                        totalDamage,
                        detail.hpDamage,
                        detail.barrierDamage,
                        skillHitCount
                    );
                }

                itemObject.SetActive(true);
            }
        }
    }

    private void ClearDetailItems()
    {
        if (detailItemContainer == null)
            return;

        for (int i = detailItemContainer.childCount - 1; i >= 0; i--)
            Destroy(detailItemContainer.GetChild(i).gameObject);
    }

    public static string GetSourceDisplayName(
        byte sourceKind,
        int sourceId,
        int characterId)
    {
        if (sourceKind == 0)
        {
            string weaponName =
                WeaponDropSpawner.Instance?.GetWeaponData(sourceId)?.name;

            return !string.IsNullOrEmpty(weaponName)
                ? weaponName
                : $"무기 #{sourceId}";
        }

        return GetSkillDisplayName(characterId, sourceId);
    }

    private static string GetSkillDisplayName(int characterId, int sourceId)
    {
        switch (sourceId)
        {
            case 1:
                return "이니시에이터 드론 테이저";

            case 2:
                return "이니시에이터 궁극기";

            case 3:
                return "엔트리 블레이드";

            case 4:
                return "센티널 궁극기";

            default:
                return $"알 수 없는 스킬 (ID {sourceId})";
        }
    }

    [ContextMenu("[테스트] 무기별 피해량 샘플 데이터 적용")]
    public void TestWithSampleData()
    {
        Open();

        List<CombatReportRawEntry> sample = new()
        {
            new CombatReportRawEntry
            {
                opponentId = 101,
                characterId = 0,
                nickname = "상대 A",
                sourceKind = 0,
                direction = 0,
                sourceId = 1,
                hpDamage = 80,
                barrierDamage = 0,
                headHits = 1,
                bodyHits = 2,
                legHits = 0,
                unlocalizedHits = 0
            },
            new CombatReportRawEntry
            {
                opponentId = 101,
                characterId = 0,
                nickname = "상대 A",
                sourceKind = 0,
                direction = 0,
                sourceId = 0,
                hpDamage = 15,
                barrierDamage = 25,
                headHits = 0,
                bodyHits = 1,
                legHits = 1,
                unlocalizedHits = 0
            },
            new CombatReportRawEntry
            {
                opponentId = 101,
                characterId = 0,
                nickname = "상대 A",
                sourceKind = 0,
                direction = 1,
                sourceId = 2,
                hpDamage = 70,
                barrierDamage = 50,
                headHits = 1,
                bodyHits = 3,
                legHits = 0,
                unlocalizedHits = 0
            }
        };

        ApplyData(sample, killerId: 101, limitReached: false);
    }
}
