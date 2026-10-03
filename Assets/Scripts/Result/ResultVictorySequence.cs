using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reference video #2: banner -> local 3D performance -> rank -> rewards -> table.
// Read-only cache presentation. Does not grant rewards, queue or leave a room.
public sealed class ResultVictorySequence : MonoBehaviour
{
    public enum Page { Banner, Character, Rank, Rewards, Details }
    public GameObject overlay, rankPage, rewardPage, header, identityRoot;
    public CanvasGroup banner, curtain, pageFade;
    public TMP_Text bannerTitle, outcome, scoreA, scoreB, localName, mvpName, nextLabel;
    public TMP_Text combatReward, winReward, balance, kda, level;
    public Image profileIcon, card, rankBadge;
    public Button next, replay, viewMvp;
    public TMP_Text featuredName;
    public ResultCharacterStage characterStage;
    public RankResultController rank;
    public MatchResultPresentation identityBinder;
    public Page CurrentPage { get; private set; }
    public uint FeaturedPlayerId { get; private set; }
    private MatchResultMemberData me;
    private uint localId;
    private float elapsed;
    private bool ranked;
    private bool celebrate;
    private bool watchingMvp;
    private bool bound;
    private void Start()
    {
        BindControls();
        Show(NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayerId : 0);
    }
    public void BindControls()
    {
        if (bound) return;
        bound = true;
        next.onClick.AddListener(Advance);
        replay.onClick.AddListener(Replay);
        viewMvp.onClick.AddListener(ToggleMvp);
        rank.Completed += ShowRewards;
    }
    private void OnDestroy() { if (rank != null) rank.Completed -= ShowRewards; }
    public void Replay() => Show(localId);
    public void Show(uint playerId)
    {
        localId = playerId;
        me = MatchResultCache.members.FirstOrDefault(m => m.playerId == playerId && m.team < 2);
        var mvp = MatchResultCache.members.FirstOrDefault(m => m.isMvp && m.team < 2);
        // Spectators may watch the server's MVP; participants always see their own model first.
        var featured = me ?? mvp;
        FeaturedPlayerId = featured?.playerId ?? 0;
        watchingMvp = false;
        featuredName.richText = false;
        featuredName.text = featured == null ? "" : featured.nickname;
        viewMvp.gameObject.SetActive(mvp != null && me != null && mvp.playerId != me.playerId);
        viewMvp.GetComponentInChildren<TMP_Text>().text = "MVP 보기";
        ranked = me != null && MatchResultCache.matchType == 1;
        bool draw = MatchResultCache.scoreA == MatchResultCache.scoreB;
        bool won = me != null && me.team == MatchResultCache.winnerTeam && !draw;
        outcome.text = bannerTitle.text = me == null ? "경기 종료" : draw ? "무승부" : won ? "승리" : "패배";
        scoreA.text = MatchResultCache.scoreA.ToString(); scoreB.text = MatchResultCache.scoreB.ToString();
        localName.richText = mvpName.richText = false;
        localName.text = featured?.nickname ?? "";
        mvpName.text = mvp == null ? "" : $"MVP  {mvp.nickname}   {mvp.kills} / {mvp.deaths} / {mvp.assists}";
        kda.text = me == null ? "관전한 경기" : $"{me.kills}   /   {me.deaths}   /   {me.assists}";
        combatReward.text = me == null ? "—" : $"+{MatchResultCache.freeCurrencyReward:N0}";
        winReward.text = me == null ? "—" : $"+{MatchResultCache.victoryCurrencyReward:N0}";
        balance.text = me == null ? "" : $"보유 재화   {MatchResultCache.totalFreeCurrency:N0}  /  {MatchResultCache.totalVictoryCurrency:N0}";
        identityBinder.ApplyIdentity(featured?.appearance);
        var appearance = me?.appearance;
        profileIcon.sprite = appearance == null ? null : Resources.Load<Sprite>($"PlayerIcon/{appearance.playerIconId}");
        card.sprite = appearance == null ? null : Resources.Load<Sprite>($"IconCard/icon{appearance.playerIconId}_card");
        profileIcon.enabled = profileIcon.sprite != null; card.enabled = card.sprite != null;
        level.text = appearance == null ? "" : $"Lv.{appearance.level}";
        celebrate = won || (me == null && mvp != null);
        characterStage.SelectMap(MatchResultCache.mapName);
        characterStage.Select(featured?.characterId ?? -1, false);
        replay.gameObject.SetActive(false); overlay.SetActive(true);
        SetPage(Page.Banner);
        if (MatchResultCache.members.Count == 0) ShowDetails();
    }
    private void Update()
    {
        if (CurrentPage == Page.Details) return;
        Tick(Time.unscaledDeltaTime);
        if (Input.GetKeyDown(KeyCode.Escape)) ShowDetails();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Advance();
    }
    // Deterministic editor tests use this same transition path.
    public void Tick(float delta)
    {
        elapsed += Mathf.Max(0, delta);
        if (CurrentPage == Page.Banner)
        {
            banner.alpha = Mathf.Clamp01(elapsed / .18f) * (1 - Mathf.Clamp01((elapsed - 1.25f) / .3f));
            curtain.alpha = Mathf.Clamp01((elapsed - 1.25f) / .3f);
            if (elapsed >= 1.6f) SetPage(Page.Character);
        }
        else if (CurrentPage == Page.Character)
        {
            curtain.alpha = 1 - Mathf.Clamp01(elapsed / .2f);
            characterStage.SetFraming((elapsed - 3.15f) / .85f);
            if (elapsed >= 4) ShowProgress();
        }
        else pageFade.alpha = Mathf.Clamp01(elapsed / .25f);
    }
    private void SetPage(Page page)
    {
        CurrentPage = page; elapsed = 0;
        pageFade.gameObject.SetActive(page == Page.Rank || page == Page.Rewards);
        if (page == Page.Character) characterStage.Select(characterStage.SelectedCharacter, celebrate);
        banner.gameObject.SetActive(page == Page.Banner);
        banner.alpha = page == Page.Banner ? 1 : 0;
        curtain.alpha = page == Page.Character ? 1 : 0;
        header.SetActive(page == Page.Rank || page == Page.Rewards);
        rankPage.SetActive(page == Page.Rank); rewardPage.SetActive(page == Page.Rewards);
        identityRoot.SetActive(page == Page.Rewards && identityBinder.equippedIdentity.activeSelf);
        nextLabel.text = page == Page.Banner || page == Page.Character ? "건너뛰기" : page == Page.Rewards ? "상세 결과" : "다음";
        pageFade.alpha = page == Page.Rank || page == Page.Rewards ? 0 : 1;
    }
    private void ShowProgress()
    {
        characterStage.SetFraming(1);
        if (!ranked) { ShowRewards(); return; }
        SetPage(Page.Rank);
        if (Application.isPlaying) rank.BeginPresentation();
    }
    public void ShowRewards() { characterStage.SetFraming(1); SetPage(Page.Rewards); }
    public void ToggleMvp()
    {
        var mvp = MatchResultCache.members.FirstOrDefault(m => m.isMvp && m.team < 2);
        if (CurrentPage != Page.Rewards || mvp == null || me == null) return;
        watchingMvp = !watchingMvp;
        var featured = watchingMvp ? mvp : me;
        FeaturedPlayerId = featured.playerId;
        featuredName.text = watchingMvp ? $"MVP  {featured.nickname}" : featured.nickname;
        characterStage.Select(featured.characterId, watchingMvp || celebrate);
        characterStage.SetFraming(1);
        viewMvp.GetComponentInChildren<TMP_Text>().text = watchingMvp ? "내 캐릭터" : "MVP 보기";
        // The right-hand rewards/identity remain the local account's actual settlement.
    }
    public void Advance()
    {
        switch (CurrentPage)
        {
            case Page.Banner: SetPage(Page.Character); break;
            case Page.Character: ShowProgress(); break;
            case Page.Rank: rank.AdvanceEmbedded(); break;
            case Page.Rewards: ShowDetails(); break;
        }
    }
    public void ShowDetails()
    {
        SetPage(Page.Details); characterStage.HideCharacters();
        overlay.SetActive(false); replay.gameObject.SetActive(MatchResultCache.members.Count > 0);
        if (Application.isPlaying)
            foreach (var motion in GetComponentInParent<ResultUI>().GetComponentsInChildren<AuthoredPanelMotion>())
                if (motion.name == "MvpText") motion.Play();
    }
}
