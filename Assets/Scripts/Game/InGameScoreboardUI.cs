using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameScoreboardUI : MonoBehaviour
{
    public static InGameScoreboardUI Instance;

    [SerializeField] private GameObject rootPanel;

    [Header("Rows")]
    [SerializeField] private ScoreboardRowUI[] ourTeamRows;
    [SerializeField] private ScoreboardRowUI[] enemyTeamRows;

    [Header("Round History")]
    [SerializeField] private RoundHistoryUI roundHistoryUI;

    [Header("Character Sprites")]
    [SerializeField] private Sprite[] characterSprites;

    private ScoreboardData currentData;
    private byte myTeam;
    private bool roundHistoryInitialized = false;

    private bool isScoreboardOpen = false;

    private void Awake()
    {
        Instance = this;

        if (rootPanel == null)
        {
            Debug.LogError("[SCOREBOARD] Root Panel is null.");
            return;
        }

        if (rootPanel == gameObject)
        {
            Debug.LogError("[SCOREBOARD] Root Panel에 자기 자신을 넣으면 안 됩니다.");
            return;
        }

        rootPanel.SetActive(false);
        isScoreboardOpen = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (rootPanel == null)
            return;

        bool tabPressed = IsTabPressed();

        if (tabPressed && !isScoreboardOpen)
            Open();

        if (!tabPressed && isScoreboardOpen)
            Close();

        if (isScoreboardOpen)
            Draw();
    }

    private bool IsTabPressed()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
            return true;

        return false;
    }

    public void Open()
    {
        if (rootPanel == null)
            return;

        isScoreboardOpen = true;
        rootPanel.SetActive(true);

        // 상점 패널이 떠 있어도 스코어보드가 위로 오게 함
        rootPanel.transform.SetAsLastSibling();

        Draw();

        Debug.Log("[SCOREBOARD] OPEN");
    }

    public void Close()
    {
        if (rootPanel == null)
            return;

        isScoreboardOpen = false;
        rootPanel.SetActive(false);

        Debug.Log("[SCOREBOARD] CLOSE");
    }

    public bool IsOpen()
    {
        return isScoreboardOpen;
    }

    public void OnRecvScoreboardUpdate(ScoreboardData data)
    {
        currentData = data;
        myTeam = data.myTeam;

        if (!roundHistoryInitialized)
        {
            if (roundHistoryUI != null)
                roundHistoryUI.Init(myTeam);

            roundHistoryInitialized = true;
        }

        if (isScoreboardOpen)
            Draw();
    }

    public void OnRoundResult(
        byte currentRound,
        byte winnerTeam,
        RoundWinReason reason,
        byte scoreA,
        byte scoreB)
    {
        if (roundHistoryUI == null)
            return;

        roundHistoryUI.SetRoundResult(
            currentRound,
            winnerTeam,
            reason,
            scoreA,
            scoreB
        );
    }

    public void InitRoundHistory(byte myTeamValue)
    {
        myTeam = myTeamValue;

        if (roundHistoryUI != null)
            roundHistoryUI.Init(myTeam);
    }

    private void Draw()
    {
        ClearRows();

        if (currentData == null)
            return;

        List<ScoreboardMemberData> ourTeam = new();
        List<ScoreboardMemberData> enemyTeam = new();

        foreach (ScoreboardMemberData member in currentData.members)
        {
            if (member.team == myTeam)
                ourTeam.Add(member);
            else
                enemyTeam.Add(member);
        }

        SortByKda(ourTeam);
        SortByKda(enemyTeam);

        uint myPlayerId = NetworkManager.Instance != null
            ? NetworkManager.Instance.MyPlayerId
            : 0;

        for (int i = 0; i < ourTeamRows.Length && i < ourTeam.Count; i++)
        {
            if (ourTeamRows[i] == null)
                continue;

            ourTeamRows[i].SetData(
                ourTeam[i],
                GetCharacterSprite(ourTeam[i].characterId),
                myPlayerId
            );
        }

        for (int i = 0; i < enemyTeamRows.Length && i < enemyTeam.Count; i++)
        {
            if (enemyTeamRows[i] == null)
                continue;

            enemyTeamRows[i].SetData(
                enemyTeam[i],
                GetCharacterSprite(enemyTeam[i].characterId),
                myPlayerId
            );
        }
    }

    private void SortByKda(List<ScoreboardMemberData> list)
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

    private int GetKdaScore(ScoreboardMemberData data)
    {
        return data.kills * 100 + data.assists * 50 - data.deaths * 20;
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }

    private void ClearRows()
    {
        if (ourTeamRows != null)
        {
            foreach (ScoreboardRowUI row in ourTeamRows)
            {
                if (row != null)
                    row.Clear();
            }
        }

        if (enemyTeamRows != null)
        {
            foreach (ScoreboardRowUI row in enemyTeamRows)
            {
                if (row != null)
                    row.Clear();
            }
        }
    }
}