using System.Collections;
using TMPro;
using UnityEngine;

public class TopInfoUI : MonoBehaviour
{
    [Header("Top Info Texts")]
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private TMP_Text ourTeamRoleText;
    [SerializeField] private TMP_Text enemyTeamRoleText;

    [SerializeField] private TMP_Text gameDescriptionText;

    [Header("Score Texts")]
    [SerializeField] private TMP_Text ourTeamScoreText;
    [SerializeField] private TMP_Text enemyTeamScoreText;

    public static TopInfoUI Instance;

    private Coroutine timerRoutine;

    // 0 = A팀, 1 = B팀
    private byte myTeam = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyPickInfo(PickStateData state)
    {
        if (state == null)
            return;

        SetMapText(state.mapName);
        SetMyTeam((byte)state.myTeam);

        if (ourTeamRoleText != null)
        {
            if (state.myTeam == RoomSlotTeam.A)
                ourTeamRoleText.text = "선 공격 / 후 수비";
            else
                ourTeamRoleText.text = "선 수비 / 후 공격";
        }

        if (enemyTeamRoleText != null)
        {
            if (state.myTeam == RoomSlotTeam.A)
                enemyTeamRoleText.text = "선 수비 / 후 공격";
            else
                enemyTeamRoleText.text = "선 공격 / 후 수비";
        }

        SetScoreByTeam(state.blueScore, state.redScore);
        SetGameDescription("캐릭터 선택");
    }

    public void ApplyGameStartInfo()
    {
        SetGameDescription("5판 3선승");
    }

    public void SetMyTeam(byte team)
    {
        myTeam = team;
    }

    // 서버 원본 점수: scoreA = A팀, scoreB = B팀
    // 화면 출력: 왼쪽 = 우리팀, 오른쪽 = 상대팀
    public void SetScoreByTeam(int scoreA, int scoreB)
    {
        int ourScore = myTeam == 0 ? scoreA : scoreB;
        int enemyScore = myTeam == 0 ? scoreB : scoreA;

        if (ourTeamScoreText != null)
            ourTeamScoreText.text = ourScore.ToString();

        if (enemyTeamScoreText != null)
            enemyTeamScoreText.text = enemyScore.ToString();
    }

    public void SetRoundWinnerText(byte winnerTeam)
    {
        bool ourTeamWin = winnerTeam == myTeam;
        SetGameDescription(ourTeamWin ? "라운드 승리" : "라운드 패배");
    }

    public void SetMatchWinnerText(byte winnerTeam)
    {
        bool ourTeamWin = winnerTeam == myTeam;
        SetGameDescription(ourTeamWin ? "경기 승리" : "경기 패배");
    }

    public void SetMapText(string mapName)
    {
        if (mapText != null)
            mapText.text = mapName;
    }

    public void SetGameDescription(string text)
    {
        if (gameDescriptionText != null)
            gameDescriptionText.text = text;
    }

    public void SetTimer(ushort remainSeconds)
    {
        int min = remainSeconds / 60;
        int sec = remainSeconds % 60;

        if (timerText != null)
            timerText.text = $"{min:00}:{sec:00}";
    }

    public void ResetMatchScore()
    {
        SetScoreByTeam(0, 0);
    }

    public void HideRoleTextsForGame()
    {
        if (ourTeamRoleText != null)
            ourTeamRoleText.text = "";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = "";
    }

    public void StartLocalCountdown(ushort seconds)
    {
        StopLocalCountdown();
        timerRoutine = StartCoroutine(LocalCountdownRoutine(seconds));
    }

    public void StopLocalCountdown()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }
    }

    private IEnumerator LocalCountdownRoutine(ushort seconds)
    {
        int remain = seconds;

        while (remain >= 0)
        {
            SetTimer((ushort)remain);

            if (remain == 0)
                break;

            yield return new WaitForSeconds(1f);
            remain--;
        }

        timerRoutine = null;
    }

    public void SetCurrentRoundRole(byte currentRound)
    {
        bool aTeamAttack = IsATeamAttackRound(currentRound);

        bool myTeamIsAttack = myTeam == 0
            ? aTeamAttack
            : !aTeamAttack;

        if (ourTeamRoleText != null)
            ourTeamRoleText.text = myTeamIsAttack ? "공격" : "수비";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = myTeamIsAttack ? "수비" : "공격";
    }

    private bool IsATeamAttackRound(byte currentRound)
    {
        if (currentRound <= 2)
            return true;

        if (currentRound < 5)
            return false;

        return ((currentRound - 5) % 2) == 0;
    }

    public void SetUnknownTimer()
    {
        StopLocalCountdown();

        if (timerText != null)
            timerText.text = "??:??";
    }
}