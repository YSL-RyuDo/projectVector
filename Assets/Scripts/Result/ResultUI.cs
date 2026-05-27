using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum ResultViewType
{
    Total,
    FirstHalf,
    SecondHalf,
    Overtime
}

public class ResultUI : MonoBehaviour
{
    [Header("Top Score")]
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text blueTeamScoreText;
    [SerializeField] private TMP_Text redTeamScoreText;

    [Header("Buttons")]
    [SerializeField] private GameObject overtimeButton;
    [SerializeField] private TMP_Text arrayButtonText;

    [Header("Team Sorted Rows")]
    [SerializeField] private ResultUserRowUI[] teamARows;
    [SerializeField] private ResultUserRowUI[] teamBRows;

    [Header("All KDA Sorted Rows")]
    [SerializeField] private Transform allRowParent;
    [SerializeField] private ResultUserRowUI blueTeamRowPrefab;
    [SerializeField] private ResultUserRowUI redTeamRowPrefab;

    [Header("Character Sprites")]
    [SerializeField] private Sprite[] characterSprites;

    private ResultViewType currentViewType = ResultViewType.Total;
    private bool arrayByKdaOnly = false;

    private readonly List<ResultUserRowUI> spawnedAllRows = new();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        bool hasOvertime = HasOvertimeData();

        if (overtimeButton != null)
            overtimeButton.SetActive(hasOvertime);

        Draw();
    }

    public void OnClickTotal()
    {
        currentViewType = ResultViewType.Total;
        Draw();
    }

    public void OnClickFirstHalf()
    {
        currentViewType = ResultViewType.FirstHalf;
        Draw();
    }

    public void OnClickSecondHalf()
    {
        currentViewType = ResultViewType.SecondHalf;
        Draw();
    }

    public void OnClickOvertime()
    {
        currentViewType = ResultViewType.Overtime;
        Draw();
    }

    public void OnClickArrayButton()
    {
        arrayByKdaOnly = !arrayByKdaOnly;

        if (arrayButtonText != null)
            arrayButtonText.text = arrayByKdaOnly ? "팀별 KDA 정렬" : "전체 KDA 정렬";

        Draw();
    }

    public void OnClickLeave()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendResultLeave();
    }

    private void Draw()
    {
        if (resultTitleText != null)
        {
            bool myTeamWin = MatchResultCache.winnerTeam == GetMyTeam();
            resultTitleText.text = myTeamWin ? "승리" : "패배";
        }

        if (blueTeamScoreText != null)
            blueTeamScoreText.text = MatchResultCache.scoreA.ToString();

        if (redTeamScoreText != null)
            redTeamScoreText.text = MatchResultCache.scoreB.ToString();

        ClearTeamRows();
        ClearSpawnedAllRows();

        if (arrayByKdaOnly)
            DrawAllSorted();
        else
            DrawTeamSorted();
    }

    private void DrawTeamSorted()
    {
        List<MatchResultMemberData> teamA = new();
        List<MatchResultMemberData> teamB = new();

        foreach (MatchResultMemberData member in MatchResultCache.members)
        {
            if (member.team == 0)
                teamA.Add(member);
            else if (member.team == 1)
                teamB.Add(member);
        }

        SortMembers(teamA);
        SortMembers(teamB);

        for (int i = 0; i < teamA.Count && i < teamARows.Length; i++)
            ApplyRow(teamARows[i], teamA[i]);

        for (int i = 0; i < teamB.Count && i < teamBRows.Length; i++)
            ApplyRow(teamBRows[i], teamB[i]);
    }

    private void DrawAllSorted()
    {
        List<MatchResultMemberData> all = new(MatchResultCache.members);
        SortMembers(all);

        foreach (MatchResultMemberData data in all)
        {
            ResultUserRowUI prefab = data.team == 0
                ? blueTeamRowPrefab
                : redTeamRowPrefab;

            if (prefab == null || allRowParent == null)
                continue;

            ResultUserRowUI row = Instantiate(prefab, allRowParent);
            spawnedAllRows.Add(row);

            ApplyRow(row, data);
        }
    }

    private void ApplyRow(ResultUserRowUI row, MatchResultMemberData data)
    {
        if (row == null)
            return;

        Sprite sprite = GetCharacterSprite(data.characterId);

        uint myPlayerId = NetworkManager.Instance != null
            ? NetworkManager.Instance.MyPlayerId
            : 0;

        row.SetData(data, currentViewType, sprite, myPlayerId);
    }

    private void ClearTeamRows()
    {
        if (teamARows != null)
        {
            foreach (ResultUserRowUI row in teamARows)
            {
                if (row != null)
                    row.Clear();
            }
        }

        if (teamBRows != null)
        {
            foreach (ResultUserRowUI row in teamBRows)
            {
                if (row != null)
                    row.Clear();
            }
        }
    }

    private void ClearSpawnedAllRows()
    {
        foreach (ResultUserRowUI row in spawnedAllRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedAllRows.Clear();
    }

    private void SortMembers(List<MatchResultMemberData> list)
    {
        list.Sort((a, b) =>
        {
            int scoreA = GetKdaScore(a);
            int scoreB = GetKdaScore(b);

            int compare = scoreB.CompareTo(scoreA);

            if (compare != 0)
                return compare;

            return a.slotIndex.CompareTo(b.slotIndex);
        });
    }

    private int GetKdaScore(MatchResultMemberData data)
    {
        int k = data.kills;
        int d = data.deaths;
        int a = data.assists;

        if (currentViewType == ResultViewType.FirstHalf)
        {
            k = data.firstHalfKills;
            d = data.firstHalfDeaths;
            a = data.firstHalfAssists;
        }
        else if (currentViewType == ResultViewType.SecondHalf)
        {
            k = data.secondHalfKills;
            d = data.secondHalfDeaths;
            a = data.secondHalfAssists;
        }
        else if (currentViewType == ResultViewType.Overtime)
        {
            k = data.overtimeKills;
            d = data.overtimeDeaths;
            a = data.overtimeAssists;
        }

        return k * 100 + a * 50 - d * 20;
    }

    private bool HasOvertimeData()
    {
        foreach (MatchResultMemberData member in MatchResultCache.members)
        {
            if (member.overtimeKills > 0 ||
                member.overtimeDeaths > 0 ||
                member.overtimeAssists > 0)
            {
                return true;
            }
        }

        return false;
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }

    private byte GetMyTeam()
    {
        uint myPlayerId = NetworkManager.Instance != null
            ? NetworkManager.Instance.MyPlayerId
            : 0;

        foreach (MatchResultMemberData member in MatchResultCache.members)
        {
            if (member.playerId == myPlayerId)
                return member.team;
        }

        return 0;
    }
}