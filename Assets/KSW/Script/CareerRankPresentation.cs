using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All objects and geometry are authored in the scene. Only data, visibility and alpha change here.
public sealed class CareerRankPresentation : MonoBehaviour
{
    public Image emblem;
    public Sprite[] rankSprites;
    public TMP_Text seasonName, seasonNotice, nextRank, totalPoints;
    public TMP_Text[] statisticTexts;
    public Image[] recentCharacters;
    public Sprite[] characterSprites;
    public TMP_Text recentCaption;
    public GameObject rankGuide, seasonGuide;
    public Button rankButton, seasonButton, rankClose, seasonClose;
    public TMP_Text guideCurrentRank, seasonTitle, seasonSummary, seasonPeriod;
    public Image seasonEmblem;
    public CanvasGroup[] revealGroups;
    public Image[] rankGuideHighlights;
    bool bound;

    void Awake() => Bind();
    void OnEnable() { Bind(); PlayIntro(); }
    void OnDisable() { StopAllCoroutines(); CloseOverlays(); SetReveal(1); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseOverlays();
    }

    public void Bind()
    {
        if (bound) return;
        bound = true;
        rankButton?.onClick.AddListener(ShowRankGuide);
        seasonButton?.onClick.AddListener(ShowSeasonGuide);
        rankClose?.onClick.AddListener(CloseOverlays);
        seasonClose?.onClick.AddListener(CloseOverlays);
    }

    public void ShowRankGuide()
    {
        CloseOverlays();
        if (rankGuide != null) rankGuide.SetActive(true);
    }
    public void ShowSeasonGuide()
    {
        CloseOverlays();
        if (seasonGuide != null) seasonGuide.SetActive(true);
    }
    public void CloseOverlays()
    {
        if (rankGuide != null) rankGuide.SetActive(false);
        if (seasonGuide != null) seasonGuide.SetActive(false);
    }

    public void PlayIntro()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(Reveal());
    }
    IEnumerator Reveal()
    {
        float elapsed = 0;
        SetReveal(0);
        while (elapsed < .65f)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < revealGroups.Length; ++i)
                if (revealGroups[i] != null)
                    revealGroups[i].alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - i * .08f) / .4f));
            yield return null;
        }
        SetReveal(1);
    }
    void SetReveal(float alpha)
    {
        if (revealGroups == null) return;
        foreach (var group in revealGroups) if (group != null) group.alpha = alpha;
    }

    public void Clear()
    {
        Set(seasonName, "시즌 정보 불러오는 중");
        Set(seasonNotice, "시즌 정보를 불러오는 중입니다");
        Set(nextRank, "랭크 정보를 불러오는 중입니다");
        Set(totalPoints, "누적 포인트 —");
        Set(guideCurrentRank, "현재 랭크 —");
        Set(seasonTitle, "시즌 정보");
        Set(seasonSummary, "시즌 정보를 불러오는 중입니다.");
        Set(seasonPeriod, "종료 일정 —");
        if (emblem != null) emblem.enabled = false;
        if (seasonEmblem != null) seasonEmblem.enabled = false;
        foreach (var highlight in rankGuideHighlights) highlight.enabled = false;
        string[] labels = { "랭크 경기", "라운드당 처치", "승률", "라운드당 피해", "MVP" };
        for (int i = 0; i < statisticTexts.Length; ++i) Stat(i, "—", labels[i]);
        ApplyHistory(null, "", 0);
    }

    public void Apply(CareerOverviewData data)
    {
        if (data == null) { Clear(); return; }
        Sprite sprite = data.rankImageId >= 0 && data.rankImageId < rankSprites.Length ? rankSprites[data.rankImageId] : null;
        foreach (var image in new[] { emblem, seasonEmblem })
            if (image != null) { image.sprite = sprite; image.enabled = sprite != null; image.preserveAspect = true; }
        for (int i = 0; i < rankGuideHighlights.Length; ++i)
            rankGuideHighlights[i].enabled = i == data.rankImageId;
        string name = string.IsNullOrWhiteSpace(data.seasonName) ? "현재 시즌" : data.seasonName;
        string tier = string.IsNullOrWhiteSpace(data.tierName) ? "—" : data.tierName;
        Set(seasonName, name);
        Set(totalPoints, $"누적 포인트  {Math.Max(0, data.rankPoints):N0}");
        Set(nextRank, data.tierMaxPoints < 0 ? "순위 경쟁 구간" : $"다음 단계까지  {Math.Max(0L, (long)data.tierMaxPoints + 1 - data.rankPoints):N0} 포인트");
        Set(guideCurrentRank, "현재 랭크  " + tier);
        Set(seasonTitle, name + " · 시즌 보상");
        Set(seasonSummary, $"{tier}\n<size=22>누적 {Math.Max(0, data.rankPoints):N0} 포인트 · 랭크 {Math.Max(0, data.rankedMatches):N0}경기</size>");
        string period = data.remainingSeasonDays > 0 ? $"시즌 종료까지 {data.remainingSeasonDays}일" : "시즌 종료";
        if (data.seasonEndUnix > 0 && data.seasonEndUnix <= 253402300799L)
            period += "\n<size=20>" + DateTimeOffset.FromUnixTimeSeconds(data.seasonEndUnix).ToLocalTime().ToString("yyyy.MM.dd HH:mm") + " 종료</size>";
        Set(seasonPeriod, period);
        Stat(0, Math.Max(0, data.rankedMatches).ToString("N0"), "랭크 경기");
        Stat(1, data.roundsPlayed > 0 ? ((double)data.kills / data.roundsPlayed).ToString("F2") : "0.00", "라운드당 처치");
        Stat(2, (data.rankedMatches > 0 ? Math.Clamp(100.0 * data.wins / data.rankedMatches, 0, 100) : 0).ToString("F0") + "%", "승률");
        Stat(3, data.roundsPlayed > 0 ? ((double)data.totalDamage / data.roundsPlayed).ToString("F1") : "0.0", "라운드당 피해");
        Stat(4, Math.Max(0, data.mvpCount).ToString("N0"), "MVP");
    }

    // Match-history data only: do not pretend these are season-wide usage statistics.
    public void ApplyHistory(IReadOnlyList<CareerMatchData> matches, string userId, int filter)
    {
        var appearances = new Dictionary<int, int>();
        if (matches != null && !string.IsNullOrEmpty(userId))
            foreach (var match in matches)
                if (match?.players != null)
                    foreach (var player in match.players)
                        if (player.userId == userId && player.characterId >= 0 && player.characterId < characterSprites.Length)
                        {
                            appearances.TryGetValue(player.characterId, out int count);
                            appearances[player.characterId] = count + 1;
                        }
        int[] ids = appearances.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).Take(recentCharacters.Length).ToArray();
        for (int i = 0; i < recentCharacters.Length; ++i)
        {
            recentCharacters[i].enabled = i < ids.Length;
            if (i < ids.Length) recentCharacters[i].sprite = characterSprites[ids[i]];
        }
        string mode = filter == 2 ? "랭크 전적 기준" : filter == 1 ? "일반·사설 전적" : "전체 전적 기준";
        Set(recentCaption, ids.Length == 0 ? "최근 출전\n<size=17>경기 기록 없음</size>" : "최근 출전\n<size=17>" + mode + "</size>");
    }
    void Stat(int index, string value, string label) => Set(statisticTexts[index], $"<size=38>{value}</size>\n<size=20>{label}</size>");
    static void Set(TMP_Text text, string value) { if (text != null) text.text = value; }
}
