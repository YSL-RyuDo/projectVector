using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoundHistoryUI : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private Transform firstHalfGroup;
    [SerializeField] private Transform secondHalfGroup;
    [SerializeField] private Transform overtimeGroup;

    [Header("Prefab")]
    [SerializeField] private RoundHistoryColumnUI roundColumnPrefab;

    [Header("Role Texts")]
    [SerializeField] private TMP_Text ourTeamRoleText;
    [SerializeField] private TMP_Text enemyTeamRoleText;
    [SerializeField] private TMP_Text ourTeamScoreText;
    [SerializeField] private TMP_Text enemyTeamScoreText;

    [Header("Our Team Icons")]
    [SerializeField] private Sprite ourTeamEliminationIcon;
    [SerializeField] private Sprite ourTeamSpikeExplodeIcon;
    [SerializeField] private Sprite ourTeamDefuseIcon;
    [SerializeField] private Sprite ourTeamTimeOutIcon;

    [Header("Enemy Team Icons")]
    [SerializeField] private Sprite enemyTeamEliminationIcon;
    [SerializeField] private Sprite enemyTeamSpikeExplodeIcon;
    [SerializeField] private Sprite enemyTeamDefuseIcon;
    [SerializeField] private Sprite enemyTeamTimeOutIcon;

    [Header("Round Setting")]
    [SerializeField] private int firstHalfRoundCount = 2;
    [SerializeField] private int secondHalfRoundCount = 2;

    private readonly Dictionary<int, RoundHistoryColumnUI> columns = new();

    private byte myTeam = 0;

    public void Init(byte myTeamValue)
    {
        myTeam = myTeamValue;
        ClearAll();
        SetRole(1);
        SetScore(0, 0);
    }

    public void SetRoundResult(
        byte currentRound,
        byte winnerTeam,
        RoundWinReason reason,
        byte scoreA,
        byte scoreB)
    {
        RoundHistoryColumnUI column = GetOrCreateColumn(currentRound);

        bool ourTeamWin = winnerTeam == myTeam;
        Sprite icon = GetReasonSprite(reason, ourTeamWin);

        column.SetResult(ourTeamWin, icon);

        SetCurrentRound((byte)(currentRound + 1));
        SetScore(scoreA, scoreB);
        SetRole((byte)(currentRound + 1));
    }

    public void SetCurrentRound(byte currentRound)
    {
        foreach (var pair in columns)
        {
            pair.Value.SetCurrent(pair.Key == currentRound);
        }

        GetOrCreateColumn(currentRound).SetCurrent(true);
        SetRole(currentRound);
    }

    private RoundHistoryColumnUI GetOrCreateColumn(int round)
    {
        if (columns.TryGetValue(round, out RoundHistoryColumnUI column))
            return column;

        Transform parent = GetGroupByRound(round);

        RoundHistoryColumnUI created =
            Instantiate(roundColumnPrefab, parent);

        created.SetEmpty();

        columns[round] = created;

        return created;
    }

    private Transform GetGroupByRound(int round)
    {
        if (round <= firstHalfRoundCount)
            return firstHalfGroup;

        if (round <= firstHalfRoundCount + secondHalfRoundCount)
            return secondHalfGroup;

        return overtimeGroup;
    }

    private void SetScore(byte scoreA, byte scoreB)
    {
        int ourScore = myTeam == 0 ? scoreA : scoreB;
        int enemyScore = myTeam == 0 ? scoreB : scoreA;

        if (ourTeamScoreText != null)
            ourTeamScoreText.text = ourScore.ToString();

        if (enemyTeamScoreText != null)
            enemyTeamScoreText.text = enemyScore.ToString();
    }

    private void SetRole(byte currentRound)
    {
        bool aTeamAttack = IsATeamAttackRound(currentRound);

        bool ourTeamAttack;

        if (myTeam == 0) // Aぜ
            ourTeamAttack = aTeamAttack;
        else             // Bぜ
            ourTeamAttack = !aTeamAttack;

        if (ourTeamRoleText != null)
            ourTeamRoleText.text = ourTeamAttack ? "奢問" : "熱綠";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = ourTeamAttack ? "熱綠" : "奢問";
    }

    private bool IsATeamAttackRound(byte currentRound)
    {
        if (currentRound <= 2)
            return true;

        if (currentRound <= 4)
            return false;

        return ((currentRound - 5) % 2) == 0;
    }

    private Sprite GetReasonSprite(RoundWinReason reason, bool ourTeamWin)
    {
        switch (reason)
        {
            case RoundWinReason.ATTACK_ELIMINATION:
            case RoundWinReason.DEFENSE_ELIMINATION:
                return ourTeamWin ? ourTeamEliminationIcon : enemyTeamEliminationIcon;

            case RoundWinReason.ATTACK_SPIKE_EXPLODE:
                return ourTeamWin ? ourTeamSpikeExplodeIcon : enemyTeamSpikeExplodeIcon;

            case RoundWinReason.DEFENSE_SPIKE_DEFUSE:
                return ourTeamWin ? ourTeamDefuseIcon : enemyTeamDefuseIcon;

            case RoundWinReason.DEFENSE_TIME_OUT:
                return ourTeamWin ? ourTeamTimeOutIcon : enemyTeamTimeOutIcon;

            default:
                return null;
        }
    }

    private void ClearAll()
    {
        foreach (var pair in columns)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        columns.Clear();
    }
}