using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RankResultController : MonoBehaviour
{
    private const byte ModifierUnderdog = 1 << 0;
    private const byte ModifierFavorite = 1 << 1;
    private const byte ModifierCalibrating = 1 << 2;
    private const byte ModifierConvergingUp = 1 << 3;
    private const byte ModifierConvergingDown = 1 << 4;
    private const byte ModifierDecisive = 1 << 5;
    private const byte ModifierFloorProtection = 1 << 6;
    private const byte ModifierPartyDisadvantage = 1 << 7;

    [Header("Rank Presentation")]
    [SerializeField] private Image rankImage;
    [SerializeField] private TMP_Text rankNameText;
    [SerializeField] private TMP_Text rankPointText;
    [SerializeField] private TMP_Text rankDeltaText;
    [SerializeField] private TMP_Text rankChangeText;
    [SerializeField] private Slider rankProgressSlider;
    [SerializeField] private GameObject rankProgressRoot;
    [SerializeField] private Button continueButton;

    [Header("Rank Sprites (Bronze to Master)")]
    [SerializeField] private Sprite[] rankSprites;

    [Header("Animation")]
    [SerializeField] private float introDelay = 0.6f;
    [SerializeField] private float pointAnimationDuration = 1.6f;
    [SerializeField] private Color increaseColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color decreaseColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralColor = Color.white;

    private bool canContinue;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (MatchResultCache.matchType != 1)
        {
            SceneManager.LoadScene("Result");
            return;
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        int beforePoints = Mathf.Max(0, MatchResultCache.rankPointsBefore);
        int beforeTier = Mathf.Clamp(MatchResultCache.rankTierIdBefore, 0, 25);
        ApplyRankPresentation(beforePoints, beforeTier);
        ApplyChangeText(beforeTier, MatchResultCache.rankTierIdAfter);

        if (rankDeltaText != null)
        {
            int delta = MatchResultCache.rankPointDelta;
            rankDeltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
            rankDeltaText.color = delta > 0
                ? increaseColor
                : delta < 0 ? decreaseColor : neutralColor;
        }

        StartCoroutine(PlayRankPointAnimation());
    }

    private void Update()
    {
        if (canContinue &&
            (Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.Space)))
        {
            OnClickContinue();
        }
    }

    private IEnumerator PlayRankPointAnimation()
    {
        yield return new WaitForSecondsRealtime(introDelay);

        int beforePoints = Mathf.Max(0, MatchResultCache.rankPointsBefore);
        int afterPoints = Mathf.Max(0, MatchResultCache.rankPointsAfter);
        int beforeTier = Mathf.Clamp(MatchResultCache.rankTierIdBefore, 0, 25);
        int afterTier = Mathf.Clamp(MatchResultCache.rankTierIdAfter, 0, 25);
        float duration = Mathf.Max(0.05f, pointAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            int animatedPoints = Mathf.RoundToInt(
                Mathf.Lerp(beforePoints, afterPoints, eased));
            int animatedTier = ResolveAnimatedTier(
                animatedPoints,
                beforeTier,
                afterTier,
                normalized);

            ApplyRankPresentation(animatedPoints, animatedTier);
            yield return null;
        }

        ApplyRankPresentation(afterPoints, afterTier);
        canContinue = true;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.Select();
        }
    }

    public void OnClickContinue()
    {
        if (!canContinue)
            return;

        canContinue = false;
        SceneManager.LoadScene("Result");
    }

    private void ApplyRankPresentation(int totalPoints, int tierId)
    {
        tierId = Mathf.Clamp(tierId, 0, 25);

        if (rankNameText != null)
            rankNameText.text = GetRankName(tierId);

        if (rankImage != null)
            rankImage.sprite = GetRankSprite(tierId);

        bool usesLeaderboardScore = tierId >= 24;
        if (rankProgressRoot != null)
            rankProgressRoot.SetActive(!usesLeaderboardScore);

        if (usesLeaderboardScore)
        {
            if (rankPointText != null)
                rankPointText.text = $"랭크 점수  {totalPoints:N0}";
            return;
        }

        int divisionPoints = Mathf.Clamp(totalPoints - tierId * 100, 0, 100);
        if (rankProgressSlider != null)
            rankProgressSlider.value = divisionPoints / 100f;

        if (rankPointText != null)
            rankPointText.text = $"{divisionPoints} / 100";
    }

    private void ApplyChangeText(int beforeTier, int afterTier)
    {
        if (rankChangeText == null)
            return;

        int delta = MatchResultCache.rankPointDelta;
        string headline;
        if (afterTier > beforeTier)
            headline = "승급";
        else if (afterTier < beforeTier)
            headline = "강등";
        else if (delta > 0)
            headline = "랭크 점수 상승";
        else if (delta < 0)
            headline = "랭크 점수 하락";
        else
            headline = "랭크 변동 없음";

        List<string> modifiers = BuildModifierLabels(
            MatchResultCache.rankModifierFlags,
            delta);

        if (MatchResultCache.rankExpectedWinRatePermille > 0)
        {
            float expectedWinRate =
                MatchResultCache.rankExpectedWinRatePermille / 10f;
            modifiers.Insert(
                0,
                $"매치 예상 승률 {expectedWinRate:0.#}%");
        }

        rankChangeText.text = modifiers.Count == 0
            ? headline
            : $"{headline}\n<size=45%><color=#8DE8FF>" +
                string.Join("  ·  ", modifiers) +
                "</color></size>";
    }

    private List<string> BuildModifierLabels(byte flags, int delta)
    {
        List<string> labels = new();

        if ((flags & ModifierUnderdog) != 0)
            labels.Add(delta > 0 ? "열세 극복" : "위로 보정");
        else if ((flags & ModifierFavorite) != 0)
            labels.Add(delta > 0 ? "예상 승리" : "역전패");

        if ((flags & ModifierCalibrating) != 0)
            labels.Add("랭크 보정 중");
        if ((flags & ModifierConvergingUp) != 0)
            labels.Add("상향 보정");
        else if ((flags & ModifierConvergingDown) != 0)
            labels.Add("하향 보정");
        if ((flags & ModifierDecisive) != 0)
            labels.Add("큰 점수 차");
        if ((flags & ModifierFloorProtection) != 0)
            labels.Add("최저점 보호");
        if ((flags & ModifierPartyDisadvantage) != 0)
            labels.Add("파티 열세 보정");

        return labels;
    }

    private int ResolveAnimatedTier(
        int points,
        int beforeTier,
        int afterTier,
        float normalized)
    {
        if (beforeTier == 25 || afterTier == 25)
            return normalized < 0.72f ? beforeTier : afterTier;

        return Mathf.Clamp(points / 100, 0, 24);
    }

    private Sprite GetRankSprite(int tierId)
    {
        if (rankSprites == null || rankSprites.Length == 0)
            return null;

        int imageIndex;
        if (tierId >= 25)
            imageIndex = 5;
        else
            imageIndex = Mathf.Clamp(tierId / 5, 0, 4);

        return imageIndex < rankSprites.Length
            ? rankSprites[imageIndex]
            : rankSprites[0];
    }

    private string GetRankName(int tierId)
    {
        if (tierId >= 25)
            return "마스터";

        string[] rankGroups =
        {
            "브론즈",
            "실버",
            "골드",
            "플레티넘",
            "다이아몬드"
        };

        int group = Mathf.Clamp(tierId / 5, 0, rankGroups.Length - 1);
        int division = 5 - tierId % 5;
        return $"{rankGroups[group]} {division}";
    }
}
