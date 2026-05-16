using UnityEngine;

public class MapInfo : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform[] attackSpawnPoints;
    public Transform[] defendSpawnPoints;
    public Transform spikeSpawnPoint;

    [Header("Round Objects")]
    public GameObject barriers;

    public Transform GetSpawnPoint(PlayerState.BattleState battleState, int index)
    {
        if (battleState == PlayerState.BattleState.Attack)
        {
            if (attackSpawnPoints == null || attackSpawnPoints.Length == 0)
            {
                Debug.LogError("[MAP INFO] attackSpawnPoints is empty");
                return null;
            }

            return attackSpawnPoints[index % attackSpawnPoints.Length];
        }

        if (defendSpawnPoints == null || defendSpawnPoints.Length == 0)
        {
            Debug.LogError("[MAP INFO] defendSpawnPoints is empty");
            return null;
        }

        return defendSpawnPoints[index % defendSpawnPoints.Length];
    }

    public Transform GetTeamSlotSpawnPoint(byte team, byte slotIndex)
    {
        if (team == 0)
        {
            if (attackSpawnPoints == null || slotIndex >= attackSpawnPoints.Length)
            {
                Debug.LogError($"[MAP INFO] Invalid attack spawn slot={slotIndex}");
                return null;
            }

            return attackSpawnPoints[slotIndex];
        }

        if (team == 1)
        {
            if (defendSpawnPoints == null || slotIndex >= defendSpawnPoints.Length)
            {
                Debug.LogError($"[MAP INFO] Invalid defend spawn slot={slotIndex}");
                return null;
            }

            return defendSpawnPoints[slotIndex];
        }

        Debug.LogError($"[MAP INFO] Invalid team={team}");
        return null;
    }

    public void SetBarriersActive(bool active)
    {
        if (barriers != null)
            barriers.SetActive(active);
    }
}