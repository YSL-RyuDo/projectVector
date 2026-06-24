using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatFeedbackUI : MonoBehaviour
{
    public static CombatFeedbackUI Instance { get; private set; }

    [Header("Spike Progress")]
    [SerializeField] private GameObject spikeProgressPanel;
    [SerializeField] private Slider spikeProgressSlider;
    [SerializeField] private TMP_Text spikeProgressText;

    [Header("Objective Notice")]
    [SerializeField] private GameObject objectiveNoticePanel;
    [SerializeField] private GameObject objectiveIcon;
    [SerializeField] private TMP_Text objectiveNoticeText;
    [SerializeField] private float objectiveNoticeDuration = 2f;

    private bool keepObjectiveIconVisible;

    [Header("Round Result")]
    [SerializeField] private GameObject roundResultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultReasonText;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private float roundResultDuration = 4f;

    [Header("Colors")]
    [SerializeField] private Color victoryColor = new Color(0.2f, 1f, 0.85f, 1f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.25f, 0.25f, 1f);

    private Coroutine objectiveNoticeCoroutine;
    private Coroutine roundResultCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResetAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowSpikeProgress(string message, float progress)
    {
        if (spikeProgressPanel != null)
        {
            spikeProgressPanel.SetActive(true);
            spikeProgressPanel.transform.SetAsLastSibling();
        }

        if (spikeProgressText != null)
            spikeProgressText.text = message;

        if (spikeProgressSlider != null)
            spikeProgressSlider.value = Mathf.Clamp01(progress);
    }

    public void HideSpikeProgress()
    {
        if (spikeProgressSlider != null)
            spikeProgressSlider.value = 0f;

        if (spikeProgressPanel != null)
            spikeProgressPanel.SetActive(false);
    }

    public void ShowObjectiveNotice(string message, bool keepIcon)
    {
        if (objectiveNoticeCoroutine != null)
        {
            StopCoroutine(objectiveNoticeCoroutine);
            objectiveNoticeCoroutine = null;
        }

        keepObjectiveIconVisible = keepIcon;

        objectiveNoticeCoroutine = StartCoroutine(
            ShowObjectiveNoticeRoutine(message, keepIcon)
        );
    }

    private IEnumerator ShowObjectiveNoticeRoutine(string message, bool keepIcon)
    {
        if (objectiveNoticePanel != null)
        {
            objectiveNoticePanel.SetActive(true);
            objectiveNoticePanel.transform.SetAsLastSibling();
        }

        if (objectiveIcon != null)
            objectiveIcon.SetActive(keepIcon);

        if (objectiveNoticeText != null)
        {
            objectiveNoticeText.gameObject.SetActive(true);
            objectiveNoticeText.text = message;
        }

        yield return new WaitForSecondsRealtime(objectiveNoticeDuration);

        if (objectiveNoticeText != null)
            objectiveNoticeText.gameObject.SetActive(false);

        if (!keepObjectiveIconVisible)
        {
            if (objectiveIcon != null)
                objectiveIcon.SetActive(false);

            if (objectiveNoticePanel != null)
                objectiveNoticePanel.SetActive(false);
        }

        objectiveNoticeCoroutine = null;
    }

    public void ClearObjectiveState()
    {
        keepObjectiveIconVisible = false;

        if (objectiveNoticeCoroutine != null)
        {
            StopCoroutine(objectiveNoticeCoroutine);
            objectiveNoticeCoroutine = null;
        }

        if (objectiveIcon != null)
            objectiveIcon.SetActive(false);

        if (objectiveNoticeText != null)
        {
            objectiveNoticeText.text = string.Empty;
            objectiveNoticeText.gameObject.SetActive(false);
        }

        if (objectiveNoticePanel != null)
            objectiveNoticePanel.SetActive(false);
    }

    public void ShowRoundResult(
        byte localTeam,
        byte winnerTeam,
        RoundWinReason winReason,
        byte scoreA,
        byte scoreB)
    {
        if (roundResultCoroutine != null)
            StopCoroutine(roundResultCoroutine);

        roundResultCoroutine = StartCoroutine(
            ShowRoundResultRoutine(
                localTeam,
                winnerTeam,
                winReason,
                scoreA,
                scoreB
            )
        );
    }

    private IEnumerator ShowRoundResultRoutine(
        byte localTeam,
        byte winnerTeam,
        RoundWinReason winReason,
        byte scoreA,
        byte scoreB)
    {
        HideSpikeProgress();

        if (objectiveNoticePanel != null)
            objectiveNoticePanel.SetActive(false);

        bool teamKnown = localTeam == 0 || localTeam == 1;
        bool victory = teamKnown && localTeam == winnerTeam;

        if (resultTitleText != null)
        {
            if (!teamKnown)
            {
                resultTitleText.text = "라운드 종료";
                resultTitleText.color = Color.white;
            }
            else if (victory)
            {
                resultTitleText.text = "라운드 승리";
                resultTitleText.color = victoryColor;
            }
            else
            {
                resultTitleText.text = "라운드 패배";
                resultTitleText.color = defeatColor;
            }
        }

        if (resultReasonText != null)
            resultReasonText.text = GetRoundReasonText(winReason);

        if (resultScoreText != null)
        {
            byte ourScore;
            byte enemyScore;

            if (localTeam == 0)
            {
                ourScore = scoreA;
                enemyScore = scoreB;
            }
            else if (localTeam == 1)
            {
                ourScore = scoreB;
                enemyScore = scoreA;
            }
            else
            {
                ourScore = scoreA;
                enemyScore = scoreB;
            }

            resultScoreText.text = $"{ourScore} : {enemyScore}";
        }

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(true);
            roundResultPanel.transform.SetAsLastSibling();
        }

        yield return new WaitForSecondsRealtime(roundResultDuration);

        if (roundResultPanel != null)
            roundResultPanel.SetActive(false);

        roundResultCoroutine = null;
    }

    private string GetRoundReasonText(RoundWinReason reason)
    {
        switch (reason)
        {
            case RoundWinReason.ATTACK_ELIMINATION:
                return "수비팀 전멸";

            case RoundWinReason.ATTACK_SPIKE_EXPLODE:
                return "스파이크 폭파";

            case RoundWinReason.DEFENSE_ELIMINATION:
                return "공격팀 전멸";

            case RoundWinReason.DEFENSE_TIME_OUT:
                return "시간 초과";

            case RoundWinReason.DEFENSE_SPIKE_DEFUSE:
                return "스파이크 해체";

            default:
                return "라운드 종료";
        }
    }

    public void ResetAll()
    {
        if (objectiveNoticeCoroutine != null)
        {
            StopCoroutine(objectiveNoticeCoroutine);
            objectiveNoticeCoroutine = null;
        }

        if (roundResultCoroutine != null)
        {
            StopCoroutine(roundResultCoroutine);
            roundResultCoroutine = null;
        }

        if (spikeProgressSlider != null)
            spikeProgressSlider.value = 0f;

        if (spikeProgressPanel != null)
            spikeProgressPanel.SetActive(false);

        ClearObjectiveState();

        if (roundResultPanel != null)
            roundResultPanel.SetActive(false);
    }
}