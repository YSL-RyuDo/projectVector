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
            ourTeamRoleText.text =
                RoleToText(state.firstRole) + " / " +
                RoleToText(state.secondRole);
        }

        if (enemyTeamRoleText != null)
        {
            enemyTeamRoleText.text =
                RoleToText(GetOppositeRole(state.firstRole)) + " / " +
                RoleToText(GetOppositeRole(state.secondRole));
        }

        SetScore(state.blueScore, state.redScore);
        SetGameDescription("캐릭터 선택");
    }

    public void ApplyGameStartInfo()
    {
        SetGameDescription("9라운드 승리");

        if (ourTeamRoleText != null)
            ourTeamRoleText.text = "";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = "";
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

    public void ResetMatchScore()
    {
        SetScore(0, 0);
    }
}