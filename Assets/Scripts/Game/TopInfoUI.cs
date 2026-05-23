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

    [SerializeField] private TMP_Text blueTeamScoreText;
    [SerializeField] private TMP_Text redTeamScoreText;

    public static TopInfoUI Instance;
    private Coroutine timerRoutine;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyPickInfo(PickStateData state)
    {
        if (state == null)
            return;

        SetMapText(state.mapName);

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

        SetScore(state.blueScore, state.redScore);
        SetGameDescription("캐릭터 선택");
    }

    public void ApplyGameStartInfo()
    {
        SetGameDescription("5판 3선승");
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

    public void SetScore(int blueScore, int redScore)
    {
        if (blueTeamScoreText != null)
            blueTeamScoreText.text = blueScore.ToString();

        if (redTeamScoreText != null)
            redTeamScoreText.text = redScore.ToString();
    }

    public void ResetMatchScore()
    {
        SetScore(0, 0);
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

    public void SetCurrentRoundRole(byte myTeam, byte currentRound)
    {
        bool aTeamAttack;

        if (currentRound <= 2)
        {
            aTeamAttack = true;
        }
        else if (currentRound < 5)
        {
            aTeamAttack = false;
        }
        else
        {
            // 5R A공격, 6R A수비, 7R A공격 ...
            aTeamAttack = ((currentRound - 5) % 2) == 0;
        }

        bool myTeamIsAttack;

        if (myTeam == 0)
            myTeamIsAttack = aTeamAttack;
        else
            myTeamIsAttack = !aTeamAttack;

        if (ourTeamRoleText != null)
            ourTeamRoleText.text = myTeamIsAttack ? "공격" : "수비";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = myTeamIsAttack ? "수비" : "공격";
    }

    private string RoleToText(PickRole role)
    {
        if (role == PickRole.ATTACK)
            return "선 공격";

        if (role == PickRole.DEFENSE)
            return "후 수비";

        return "";
    }

    private PickRole GetOppositeRole(PickRole role)
    {
        if (role == PickRole.ATTACK)
            return PickRole.DEFENSE;

        return PickRole.ATTACK;
    }
}