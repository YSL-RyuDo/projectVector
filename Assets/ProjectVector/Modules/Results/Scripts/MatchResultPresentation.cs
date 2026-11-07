using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only presentation of the existing authoritative result cache.
// Never awards currency, sends packets, creates UI or changes scene navigation.
public sealed class MatchResultPresentation : MonoBehaviour
{
    public GameObject overlay;
    public CanvasGroup[] stages;
    public TMP_Text outcome, score, mode, spotlightName, spotlightCaption, spotlightKda;
    public TMP_Text personalName, personalKda, freeReward, victoryReward;
    public TMP_Text continueLabel;
    public Image portrait, accent, sweep;
    public GameObject equippedIdentity;
    public Image profileIcon, playerCard, playerCardBorder, equippedRank;
    public Image[] equippedMedals;
    public TMP_Text equippedLevel;
    public Sprite[] characters;
    public Sprite[] spotlightCharacters;
    public Button continueButton, replayButton;
    public float duration = 2.8f;
    private Vector2[] positions;
    private Coroutine sequence;
    private bool finished;
    private uint playerId;
    private readonly Color winColor = new Color32(34, 35, 39, 255);
    private readonly Color lossColor = new Color32(193, 40, 60, 255);

    private void Start()
    {
        continueButton.onClick.AddListener(Continue);
        replayButton.onClick.AddListener(Replay);
        Show(NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayerId : 0);
    }
    private void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Dismiss();
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Continue();
    }
    public void Replay() => Show(playerId);
    public void Show(uint localPlayerId)
    {
        playerId = localPlayerId;
        StopSequence();
        if (MatchResultCache.members.Count == 0) { Dismiss(); return; }
        if (positions == null) positions = stages.Select(s => ((RectTransform)s.transform).anchoredPosition).ToArray();
        var me = MatchResultCache.members.FirstOrDefault(m => m.playerId == playerId && m.team < 2);
        bool draw = MatchResultCache.scoreA == MatchResultCache.scoreB;
        bool won = me != null && me.team == MatchResultCache.winnerTeam;
        outcome.text = me == null ? "MATCH COMPLETE" : draw ? "DRAW" : won ? "VICTORY" : "DEFEAT";
        outcome.color = draw || me == null || won ? winColor : lossColor;
        accent.color = lossColor;
        score.text = $"{MatchResultCache.scoreA:00}  :  {MatchResultCache.scoreB:00}";
        mode.text = MatchResultCache.matchType == 1 ? "랭크전 · 경기 결과" : "경기 결과";
        var star = MatchResultCache.members.FirstOrDefault(m => m.isMvp);
        // Never invent an MVP when the server has not awarded one.
        bool isMvp = star != null;
        star ??= me;
        spotlightCaption.text = isMvp ? "MATCH MVP" : "PLAYER SPOTLIGHT";
        spotlightName.richText = personalName.richText = false;
        spotlightName.text = star?.nickname ?? "전투 종료";
        spotlightKda.text = star != null ? $"{star.kills}  /  {star.deaths}  /  {star.assists}" : "—";
        ApplyIdentity(star?.appearance);
        var artwork = spotlightCharacters != null && spotlightCharacters.Length > 0 ? spotlightCharacters : characters;
        portrait.sprite = star != null && artwork != null && star.characterId >= 0 && star.characterId < artwork.Length ? artwork[star.characterId] : null;
        portrait.enabled = portrait.sprite != null;
        personalName.text = me != null ? me.nickname : "관전한 경기";
        personalKda.text = me != null ? $"{me.kills}  /  {me.deaths}  /  {me.assists}" : "—";
        overlay.SetActive(true); replayButton.gameObject.SetActive(false); finished = false;
        continueLabel.text = "연출 건너뛰기  ›";
        Evaluate(0);
        if (Application.isPlaying && isActiveAndEnabled) sequence = StartCoroutine(Play());
    }
    public void ApplyIdentity(MatchAppearanceData appearance)
    {
        if (equippedIdentity == null) return;
        equippedIdentity.SetActive(appearance != null);
        if (appearance == null) return;
        // The same ID selects both images, matching the existing profile/save rule.
        int selectedId = appearance.playerIconId;
        profileIcon.sprite = Resources.Load<Sprite>($"PlayerIcon/{selectedId}");
        playerCard.sprite = Resources.Load<Sprite>($"IconCard/icon{selectedId}_card");
        playerCardBorder.sprite = Resources.Load<Sprite>("IconBorder/border");
        string[] ranks = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" };
        equippedRank.sprite = appearance.rankImageId >= 0 && appearance.rankImageId < ranks.Length
            ? Resources.Load<Sprite>("Rank/" + ranks[appearance.rankImageId]) : null;
        profileIcon.enabled = profileIcon.sprite != null; playerCard.enabled = playerCard.sprite != null;
        equippedRank.enabled = equippedRank.sprite != null;
        equippedLevel.text = $"Lv.{appearance.level}";
        for (int i = 0; equippedMedals != null && i < equippedMedals.Length; i++)
        {
            var image = equippedMedals[i]; var preset = appearance.preset;
            bool visible = preset != null && i < preset.medals.Count && i < preset.caseId + 1;
            image.gameObject.SetActive(visible);
            if (!visible) continue;
            var medal = preset.medals[i]; image.sprite = Resources.Load<Sprite>("Rank/" + ranks[Mathf.Clamp(medal.medalId,0,5)]);
            image.rectTransform.anchoredPosition = new Vector2(medal.x * playerCard.rectTransform.rect.width, medal.y * playerCard.rectTransform.rect.height);
            image.rectTransform.localRotation = Quaternion.Euler(0,0,medal.rotation);
            image.rectTransform.localScale = new Vector3(medal.flipped ? -medal.scale : medal.scale,medal.scale,1);
        }
    }
    private IEnumerator Play()
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; Evaluate(elapsed / Mathf.Max(.01f, duration)); yield return null;
        }
        Finish();
    }
    // Shared by runtime and editor verification; only animates saved objects.
    public void Evaluate(float progress)
    {
        if (positions == null) return;
        float elapsed = Mathf.Clamp01(progress) * duration;
        for (int i = 0; i < stages.Length; ++i)
        {
            float delay = i == 0 ? 0 : i == 1 ? .28f : i == 2 ? .95f : 1.55f;
            float revealDuration = i == 1 ? .65f : .35f;
            float t = Mathf.Clamp01((elapsed - delay) / revealDuration);
            float eased = 1 - Mathf.Pow(1 - t, 3);
            stages[i].alpha = eased;
            Vector2 offset = i == 1 ? Vector2.left * 110 : Vector2.down * 24;
            ((RectTransform)stages[i].transform).anchoredPosition = positions[i] + offset * (1 - eased);
            stages[i].transform.localScale = Vector3.one * (i == 1 ? Mathf.Lerp(1.045f,1,eased) : 1);
        }
        float rewardProgress = Mathf.Clamp01((progress - .55f) / .4f);
        bool hasPersonalResult = MatchResultCache.members.Any(m => m.playerId == playerId && m.team < 2);
        freeReward.text = hasPersonalResult ? $"+{Mathf.RoundToInt(MatchResultCache.freeCurrencyReward * rewardProgress):N0}" : "—";
        victoryReward.text = hasPersonalResult ? $"+{Mathf.RoundToInt(MatchResultCache.victoryCurrencyReward * rewardProgress):N0}" : "—";
        if (sweep != null)
        {
            var c = sweep.color; c.a = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * .14f; sweep.color = c;
            sweep.rectTransform.localScale = new Vector3(Mathf.Lerp(.01f, 1, Mathf.Clamp01(progress * 2)), 1, 1);
        }
    }
    private void Finish() { Evaluate(1); finished = true; continueLabel.text = "상세 결과 보기  ›"; sequence = null; }
    public void Continue()
    {
        if (!finished) { StopSequence(); Finish(); }
        else Dismiss();
    }
    public void Dismiss()
    {
        StopSequence(); Evaluate(1);
        if (overlay != null) overlay.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(MatchResultCache.members.Count > 0);
    }
    private void StopSequence() { if (sequence != null) StopCoroutine(sequence); sequence = null; }
    private void OnDisable() => StopSequence();
}
