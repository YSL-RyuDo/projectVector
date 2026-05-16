using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public enum RoundState
    {
        Waiting,
        Preparation,
        MainRound,
        SpikePlanted,
        RoundEnd,
        GameOver
    }

    public RoundState currentState = RoundState.Waiting;

    public bool CanAttack =>
        currentState == RoundState.MainRound ||
        currentState == RoundState.SpikePlanted;

    private MapInfo currentMapInfo;

    private void Awake()
    {
        Instance = this;
    }

    public void SetMapInfo(MapInfo mapInfo)
    {
        currentMapInfo = mapInfo;

        if (currentMapInfo == null)
        {
            Debug.LogError("[ROUND] MapInfo is null");
            return;
        }

        Debug.Log("[ROUND] MapInfo registered");
    }

    public void StartGame()
    {
        Debug.Log("[ROUND] StartGame ignored. Round flow is controlled by server packets.");
    }

    public void OnServerPrepareStarted()
    {
        currentState = RoundState.Preparation;

        if (currentMapInfo != null)
            currentMapInfo.SetBarriersActive(true);

        Debug.Log("[ROUND] Server prepare started.");
    }

    public void OnServerPrepareFinished()
    {
        currentState = RoundState.MainRound;

        if (currentMapInfo != null)
            currentMapInfo.SetBarriersActive(false);

        Debug.Log("[ROUND] Server prepare finished. Main round start.");
    }

    public void OnSpikePlanted()
    {
        if (currentState != RoundState.MainRound)
            return;

        currentState = RoundState.SpikePlanted;

        Debug.Log("[ROUND] Spike planted.");
    }

    public void RequestRoundEnd(bool attackWin)
    {
        Debug.Log(attackWin
            ? "[ROUND] Attack round end requested."
            : "[ROUND] Defense round end requested.");

        // 지금은 임시 클라이언트 처리.
        // 나중에는 서버에 라운드 종료 요청 패킷을 보내고,
        // 서버가 라운드 결과 / 점수 갱신 패킷을 내려주는 구조로 바꾸면 된다.
        OnServerRoundEnd(attackWin);
    }

    public void OnServerRoundEnd(bool attackWin)
    {
        currentState = RoundState.RoundEnd;

        if (currentMapInfo != null)
            currentMapInfo.SetBarriersActive(true);

        Debug.Log(attackWin
            ? "[ROUND] Attack round win."
            : "[ROUND] Defense round win.");

        ResetRound();
    }

    public void OnServerGameOver(bool attackWin)
    {
        currentState = RoundState.GameOver;

        Debug.Log(attackWin
            ? "[ROUND] Attack game win."
            : "[ROUND] Defense game win.");
    }

    public void ResetRound()
    {
        Debug.Log("[ROUND] Reset round.");

        CleanUpDroppedItems();

        ResetTeam("Player");
        ResetTeam("Enemy");

        if (SpikeSpawner.Instance != null && currentMapInfo != null)
            SpikeSpawner.Instance.ResetSpike(currentMapInfo.spikeSpawnPoint);
    }

    private void ResetTeam(string targetTag)
    {
        if (currentMapInfo == null)
        {
            Debug.LogError("[ROUND] Cannot reset team. currentMapInfo is null.");
            return;
        }

        GameObject[] units = GameObject.FindGameObjectsWithTag(targetTag);

        int attackIndex = 0;
        int defendIndex = 0;

        foreach (GameObject unit in units)
        {
            if (unit == null)
                continue;

            PlayerState state = unit.GetComponent<PlayerState>();
            if (state == null)
                continue;

            Transform spawnPoint = null;

            if (state.state == PlayerState.BattleState.Attack)
            {
                spawnPoint = currentMapInfo.GetSpawnPoint(
                    PlayerState.BattleState.Attack,
                    attackIndex
                );

                attackIndex++;
            }
            else
            {
                spawnPoint = currentMapInfo.GetSpawnPoint(
                    PlayerState.BattleState.Defence,
                    defendIndex
                );

                defendIndex++;
            }

            if (spawnPoint == null)
                continue;

            if (state.IsDead)
                state.ReviveAndResetInfo(spawnPoint.position);
            else
                state.Relocate(spawnPoint.position);
        }
    }

    private void CleanUpDroppedItems()
    {
        DroppedItem[] droppedWeapons =
            FindObjectsByType<DroppedItem>(FindObjectsSortMode.InstanceID);

        foreach (DroppedItem weapon in droppedWeapons)
        {
            if (weapon != null)
                Destroy(weapon.gameObject);
        }

        Spike[] spikes =
            FindObjectsByType<Spike>(FindObjectsSortMode.InstanceID);

        foreach (Spike spike in spikes)
        {
            if (spike != null)
                Destroy(spike.gameObject);
        }
    }
}