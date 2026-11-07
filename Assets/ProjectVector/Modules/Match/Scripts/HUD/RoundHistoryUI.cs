using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private int firstHalfRoundCount = 5;
    [SerializeField] private int secondHalfRoundCount = 5;

    [SerializeField] private RoundHistoryColumnUI[] authoredColumns;
    private readonly Dictionary<int, (byte winner, RoundWinReason reason)> results = new();
    public const int VisibleRoundCount = 10;
    [SerializeField] private TMP_Text historyPhaseText;
    [SerializeField] private Button previousPageButton, nextPageButton;
    private int page;
    private bool followLatest = true;
    public int PageIndex => page;
    private int LatestPage => (Mathf.Max(1,current)-1)/VisibleRoundCount;
    [SerializeField] private TMP_Text historyRangeText;
    private byte myTeam;
    private int current = 1;
    [SerializeField] private TMP_Text currentRoundText;

    public void Init(byte myTeamValue)
    { myTeam=myTeamValue; ClearAll();SetRole(1);SetScore(0,0); }
    public void SetRoundResult(byte currentRound,byte winnerTeam,RoundWinReason reason,byte scoreA,byte scoreB)
    {
        results[currentRound]=(winnerTeam,reason);
        SetCurrentRound((byte)(currentRound+1));SetScore(scoreA,scoreB);
    }
    public void SetCurrentRound(byte currentRound)
    { current=currentRound;RefreshColumns();SetRole(currentRound); }
    public void ApplyRoundRules(int firstHalf,int secondHalf)
    {
        if(firstHalf<1 || secondHalf<1 || firstHalf+secondHalf>=255)return;
        if(firstHalfRoundCount==firstHalf && secondHalfRoundCount==secondHalf)return;
        firstHalfRoundCount=firstHalf;secondHalfRoundCount=secondHalf;
        RefreshColumns();SetRole((byte)current);
    }
    public void ShowLatestPage()
    { followLatest=true;page=LatestPage;RefreshColumns(); }
    public void PreviousPage() => MovePage(-1);
    public void NextPage() => MovePage(1);
    private void MovePage(int offset)
    {
        page=Mathf.Clamp(page+offset,0,LatestPage);
        followLatest=page==LatestPage;
        RefreshColumns();
    }
    private void RefreshColumns()
    {
        int normal=firstHalfRoundCount+secondHalfRoundCount;
        if(currentRoundText)currentRoundText.text=current<=normal?"라운드 "+current:"연장 "+(current-normal)+" / 라운드 "+current;
        if(authoredColumns==null)return;
        int visible=Mathf.Min(VisibleRoundCount,authoredColumns.Length);
        if(followLatest)page=LatestPage;
        page=Mathf.Clamp(page,0,LatestPage);
        int first=page*VisibleRoundCount+1;
        if(historyPhaseText)historyPhaseText.text=first+visible-1<=normal?"정규전":first>normal?"연장전":"정규/연장";
        if(previousPageButton)previousPageButton.interactable=page>0;
        if(nextPageButton)nextPageButton.interactable=page<LatestPage;
        if(historyRangeText)historyRangeText.text=first+" - "+(first+visible-1)+"R";
        for(int i=0;i<authoredColumns.Length;i++)
        {
            var column=authoredColumns[i];if(!column)continue;
            int round=first+i;
            column.gameObject.SetActive(i<visible);
            column.SetEmpty();column.SetRoundNumber(round);
            if(results.TryGetValue(round,out var result))
            {
                bool won=result.winner==myTeam;
                bool attacking=result.winner==0?IsATeamAttackRound((byte)round):!IsATeamAttackRound((byte)round);
                column.SetResult(won,attacking,result.reason,GetReasonSprite(result.reason,won));
            }
            column.SetCurrent(round==current && !results.ContainsKey(round));
        }
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

        if (myTeam == 0) // A팀
            ourTeamAttack = aTeamAttack;
        else             // B팀
            ourTeamAttack = !aTeamAttack;

        if (ourTeamRoleText != null)
            ourTeamRoleText.text = ourTeamAttack ? "공격" : "수비";

        if (enemyTeamRoleText != null)
            enemyTeamRoleText.text = ourTeamAttack ? "수비" : "공격";
    }

    private bool IsATeamAttackRound(byte currentRound)
    {
        if (currentRound <= firstHalfRoundCount)
            return true;
        int normal=firstHalfRoundCount+secondHalfRoundCount;
        if (currentRound <= normal)
            return false;
        return ((currentRound - normal - 1) % 2) == 0;
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
    { results.Clear();current=1;page=0;followLatest=true;RefreshColumns(); }
}
