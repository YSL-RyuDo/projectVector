using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Data binding only. Every graphic, slot, button and position is saved in the scene.
// These seasonal milestones are read-only views of server totals, not claimable rewards.
public sealed class CareerTabsPresentation : MonoBehaviour
{
    [Serializable] public sealed class Milestone
    {
        public string title, description;
        public int metric, target;
        public Button button;
        public Image icon, progress;
        public TMP_Text status;
    }
    [Serializable] public sealed class RankingRow
    {
        public Image surface, tier;
        public TMP_Text rate;
    }
    [Serializable] public sealed class MatchCard
    {
        public Image surface, artwork, accent;
        public TMP_Text summary;
    }
    [Serializable] public sealed class InventorySlot
    {
        public Transform root;
        public Image surface, icon;
        public TMP_Text quantity, fallback;
    }
    public Color ink = new Color32(39, 41, 45, 255), accent = new Color32(177, 28, 61, 255);
    public Sprite[] portraits, tiers;
    public RankingRow[] rankingRows;
    public TMP_Text rankingCount, rankingEmpty, ownRank;
    public MatchCard[] matchCards;
    public Image matchArtwork;
    public TMP_Text matchResult, matchDate, historyEmpty;
    public Sprite desertArtwork;
    public Transform inventoryGrid;
    public InventorySlot[] inventorySlots;
    public Button[] inventorySortButtons;
    public Image inventoryPreview, inventoryFallback;
    public TMP_Text inventoryCount, inventoryHint, inventoryMeta, inventoryEmpty;
    public Sprite starterBadge;
    public Milestone[] milestones;
    public TMP_Text milestoneSeason, milestoneSummary, milestoneTitle, milestoneDescription, milestoneValue, milestoneState;
    public Image milestoneIcon, milestoneProgress;
    public Button[] milestoneFilters;
    CareerOverviewData overview;
    CareerPanelController controller;
    int selectedMilestone, milestoneFilter;
    bool bound;

    public void Bind(CareerPanelController owner)
    {
        controller = owner;
        if (bound) return;
        bound = true;
        for (int i = 0; i < milestones.Length; ++i)
        {
            int index = i; milestones[i].button.onClick.AddListener(() => SelectMilestone(index));
        }
        for (int i = 0; i < milestoneFilters.Length; ++i)
        {
            int index = i; milestoneFilters[i].onClick.AddListener(() => FilterMilestones(index));
        }
        for (int i = 0; i < inventorySortButtons.Length; ++i)
        {
            int index = i; inventorySortButtons[i].onClick.AddListener(() => controller.SortCareerInventory(index));
        }
    }

    public void Clear()
    {
        overview = null;
        milestoneSeason.text = "시즌 기록 불러오는 중";
        milestoneSummary.text = "기록을 불러오면 진행도가 표시됩니다.";
        rankingCount.text = "랭킹 불러오는 중";
        rankingEmpty.gameObject.SetActive(true); rankingEmpty.text = "랭킹을 불러오는 중입니다.";
        ownRank.text = "내 순위는 현재 목록에 표시되면 강조됩니다.";
        RefreshMilestones();
    }

    public void ApplyOverview(CareerOverviewData data)
    {
        overview = data;
        milestoneSeason.text = $"{data.seasonName} · 랭크 업적";
        milestoneSummary.text = $"달성 {milestones.Count(m => Value(m.metric) >= m.target)} / {milestones.Length}";
        RefreshMilestones();
    }
    int Value(int metric)
    {
        if (overview == null) return 0;
        return Math.Max(0, metric switch { 0 => overview.rankedMatches, 1 => overview.wins, 2 => overview.kills,
            3 => overview.totalDamage, 4 => overview.mvpCount, _ => overview.roundsPlayed });
    }
    public void FilterMilestones(int filter)
    {
        milestoneFilter = Mathf.Clamp(filter, 0, 2);
        RefreshMilestones();
    }
    bool Matches(Milestone m) => milestoneFilter == 0 || (milestoneFilter == 1 ? Value(m.metric) < m.target : overview != null && Value(m.metric) >= m.target);
    void RefreshMilestones()
    {
        for (int i = 0; i < milestones.Length; ++i)
        {
            var m = milestones[i]; int value = Value(m.metric); bool complete = overview != null && value >= m.target;
            m.button.gameObject.SetActive(Matches(m));
            m.progress.fillAmount = overview == null ? 0 : Mathf.Clamp01((float)value / Math.Max(1, m.target));
            m.status.text = overview == null ? "—" : complete ? "달성" : $"{value:N0} / {m.target:N0}";
            m.status.color = complete ? accent : ink;
        }
        for (int i = 0; i < milestoneFilters.Length; ++i) milestoneFilters[i].image.color = i == milestoneFilter ? accent : ink;
        if (!Matches(milestones[selectedMilestone])) selectedMilestone = Array.FindIndex(milestones, Matches);
        if (selectedMilestone < 0) { selectedMilestone = 0; ShowMilestoneEmpty(); return; }
        SelectMilestone(selectedMilestone);
    }
    void ShowMilestoneEmpty()
    {
        milestoneTitle.text = "표시할 업적이 없습니다"; milestoneDescription.text = "다른 필터를 선택해 주세요.";
        milestoneValue.text = "—"; milestoneState.text = ""; milestoneIcon.enabled = false; milestoneProgress.fillAmount = 0;
    }
    public void SelectMilestone(int index)
    {
        if (index < 0 || index >= milestones.Length) return;
        selectedMilestone = index; var m = milestones[index]; int value = Value(m.metric);
        milestoneTitle.text = m.title; milestoneDescription.text = m.description + "\n\n현재 시즌의 랭크 경기 기록으로 계산됩니다.";
        milestoneIcon.sprite = m.icon.sprite; milestoneIcon.enabled = true;
        milestoneValue.text = overview == null ? "—" : $"{value:N0} <size=24>/ {m.target:N0}</size>";
        milestoneProgress.fillAmount = overview == null ? 0 : Mathf.Clamp01((float)value / Math.Max(1, m.target));
        milestoneState.text = overview == null ? "기록 불러오는 중" : value >= m.target ? "달성 완료" : $"목표까지 {m.target - value:N0}";
        for (int i = 0; i < milestones.Length; ++i)
            milestones[i].button.image.color = i == index ? new Color32(255, 255, 255, 230) : new Color32(222, 224, 223, 170);
    }

    public void ApplyRanking(CareerRankingPageData page, string myUserId)
    {
        var entries = page.entries ?? new List<CareerRankingEntryData>();
        rankingCount.text = $"{(page.rankingType == 0 ? "시즌 랭크" : "캐릭터 활약")} · {Math.Max(0, page.totalEntries):N0}명";
        rankingEmpty.gameObject.SetActive(entries.Count == 0); rankingEmpty.text = "조건에 맞는 플레이어가 없습니다.";
        var mine = entries.FirstOrDefault(e => !string.IsNullOrEmpty(myUserId) && e.userId == myUserId);
        ownRank.text = mine == null ? "현재 페이지에 내 순위가 없습니다." : $"내 순위  {mine.position:N0}위    ·    {mine.nickname}    ·    {mine.wins:N0}승";
        for (int i = 0; i < rankingRows.Length; ++i)
        {
            var row = rankingRows[i]; bool has = i < entries.Count;
            row.tier.enabled = has && page.rankingType == 0 && entries[i].rankImageId >= 0 && entries[i].rankImageId < tiers.Length;
            if (!has) continue;
            var e = entries[i]; if (row.tier.enabled) row.tier.sprite = tiers[e.rankImageId];
            row.rate.text = e.rankedMatches > 0 ? $"{Mathf.Clamp01((float)e.wins / e.rankedMatches) * 100:F0}%" : "—";
            row.surface.color = e == mine ? new Color32(177, 28, 61, 48) : i % 2 == 0 ? new Color32(255, 255, 255, 155) : new Color32(192, 195, 196, 140);
        }
    }

    public static string Outcome(CareerMatchData match) => match.winnerTeam > 1 ? "무승부" : match.winnerTeam == match.myTeam ? "승리" : "패배";
    static string Date(long unix)
    {
        if (unix <= 0) return "날짜 정보 없음";
        try { return DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy.MM.dd  HH:mm"); }
        catch (ArgumentOutOfRangeException) { return "날짜 정보 없음"; }
    }
    Sprite MapArt(string name) => string.Equals(name, "Desert", StringComparison.OrdinalIgnoreCase) ? desertArtwork : null;
    public void ApplyHistory(List<CareerMatchData> matches)
    {
        int count = matches?.Count ?? 0;
        historyEmpty.gameObject.SetActive(count == 0);
        for (int i = 0; i < matchCards.Length; ++i)
        {
            if (i >= count) continue;
            var m = matches[i]; var card = matchCards[i];
            card.artwork.sprite = MapArt(m.mapName); card.artwork.enabled = card.artwork.sprite != null;
            card.summary.text = $"{(m.myTeam == 0 ? m.scoreA : m.scoreB)} : {(m.myTeam == 0 ? m.scoreB : m.scoreA)}   ·   {m.durationSeconds / 60:D2}:{m.durationSeconds % 60:D2}\n<size=17>{Date(m.startedAtUnix)}</size>";
            card.accent.color = m.winnerTeam == m.myTeam ? new Color32(78, 117, 108, 255) : accent;
        }
        if (count == 0)
        {
            matchArtwork.enabled = false; matchResult.text = "경기 기록 없음"; matchDate.text = "경기를 완료하면 전적을 확인할 수 있습니다.";
        }
    }
    public void SelectMatch(CareerMatchData match, int index, List<Transform> friendly, List<Transform> opponents)
    {
        matchResult.text = $"{Outcome(match)}   {(match.myTeam == 0 ? match.scoreA : match.scoreB)} : {(match.myTeam == 0 ? match.scoreB : match.scoreA)}";
        matchResult.color = match.winnerTeam == match.myTeam ? new Color32(55, 101, 88, 255) : accent;
        matchDate.text = Date(match.startedAtUnix);
        matchArtwork.sprite = MapArt(match.mapName); matchArtwork.enabled = matchArtwork.sprite != null;
        for (int i = 0; i < matchCards.Length; ++i) matchCards[i].surface.color = i == index ? Color.white : new Color32(209, 212, 211, 230);
        BindPortraits(friendly, match.players?.Where(p => p.team == match.myTeam).ToList());
        BindPortraits(opponents, match.players?.Where(p => p.team != match.myTeam).ToList());
    }
    void BindPortraits(List<Transform> rows, List<CareerMatchPlayerData> players)
    {
        for (int i = 0; i < rows.Count && i < (players?.Count ?? 0); ++i)
        {
            var image = rows[i].Find("Image").GetComponent<Image>(); int id = players[i].characterId;
            image.enabled = id >= 0 && id < portraits.Length; image.sprite = image.enabled ? portraits[id] : null;
        }
    }

    Sprite InventoryArt(CareerInventoryEntryData item) => item.iconId == 0 ? starterBadge : null;
    public void ApplyInventory(List<CareerInventoryEntryData> items, int sort)
    {
        inventoryCount.text = $"보유 {items.Count:N0}종";
        inventoryEmpty.gameObject.SetActive(items.Count == 0);
        for (int i = 0; i < inventorySortButtons.Length; ++i) inventorySortButtons[i].image.color = i == sort ? accent : ink;
        for (int i = 0; i < inventorySlots.Length; ++i)
        {
            bool has = i < items.Count; var slot = inventorySlots[i]; slot.root.gameObject.SetActive(has);
            if (!has) continue;
            slot.icon.sprite = InventoryArt(items[i]); slot.icon.enabled = slot.icon.sprite != null;
            slot.fallback.text = slot.icon.enabled ? "" : items[i].itemName;
            slot.quantity.text = $"×{items[i].quantity:N0}";
        }
        if (items.Count == 0)
        {
            inventoryPreview.enabled = false; inventoryFallback.enabled = true;
            inventoryHint.text = "보유 아이템 없음"; inventoryMeta.text = "";
        }
    }
    public void SelectInventory(CareerInventoryEntryData item, int index)
    {
        inventoryPreview.sprite = InventoryArt(item); inventoryPreview.enabled = inventoryPreview.sprite != null;
        inventoryFallback.enabled = !inventoryPreview.enabled;
        inventoryHint.text = inventoryPreview.enabled
            ? item.itemId == 1 && item.itemName == "Career Starter Badge" ? "커리어 시작 배지" : item.itemName
            : "미리보기 이미지 없음";
        inventoryMeta.text = $"보유 수량    {item.quantity:N0}\n\n획득일    {Date(item.acquiredAtUnix)}";
        for (int i = 0; i < inventorySlots.Length; ++i)
            inventorySlots[i].surface.color = i == index ? new Color32(177, 28, 61, 255) : ink;
    }
}
