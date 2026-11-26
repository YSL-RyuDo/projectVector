using UnityEngine;

public class MapInfo : MonoBehaviour
{
    [Header("폭파전 스폰 정보")]
    public Transform[] attackSpawnPoints;
    public Transform[] defendSpawnPoints;
    public Transform spikeSpawnPoint;

    [Header("폭파전 라운드 오브젝트")]
    public GameObject barriers;

    [Header("데스매치 부활 정보")]
    [Tooltip("서버와 공유할 순서이므로 사용 중인 지점의 배열 순서를 임의로 바꾸지 않는다.")]
    public Transform[] deathmatchSpawnPoints;

    public bool UsesDemolitionRules => !ClientMatchContext.State.IsDeathmatch;
    public int DeathmatchSpawnPointCount => deathmatchSpawnPoints?.Length ?? 0;
    public Transform ActiveSpikeSpawnPoint => UsesDemolitionRules ? spikeSpawnPoint : null;
    private bool requestedBarriersActive;

    private void Awake()
    {
        // 기존 프리팹에 저장된 폭파전 차단막 상태를 보존한다.
        requestedBarriersActive = barriers != null && barriers.activeSelf;
    }

    private void OnEnable()
    {
        ClientMatchContext.State.Changed += ApplyMatchMode;
        ApplyMatchMode();
    }

    private void OnDisable()
    {
        ClientMatchContext.State.Changed -= ApplyMatchMode;
    }

    private void ApplyMatchMode()
    {
        // 준비 단계라도 데스매치에서는 폭파전 차단막을 사용하지 않는다.
        if (barriers != null)
            barriers.SetActive(UsesDemolitionRules && requestedBarriersActive);
    }

    public Transform GetDeathmatchSpawnPoint(int spawnIndex)
    {
        // 팀 슬롯을 부활 지점으로 환산하지 않는다. 서버가 선택한 공통 배열 인덱스를 사용한다.
        // 잘못된 인덱스를 순환하거나 보정하면 서버와 다른 위치에 부활하므로 실패 처리한다.
        if (UsesDemolitionRules || deathmatchSpawnPoints == null ||
            spawnIndex < 0 || spawnIndex >= deathmatchSpawnPoints.Length)
            return null;

        return deathmatchSpawnPoints[spawnIndex];
    }

    public Transform GetSpawnPointForCurrentMode(PlayerState.BattleState battleState, int spawnIndex)
    {
        // 폭파전은 진영별 인덱스, 데스매치는 전체 후보 배열의 인덱스를 받는다.
        return UsesDemolitionRules ? GetSpawnPoint(battleState, spawnIndex) : GetDeathmatchSpawnPoint(spawnIndex);
    }

    private void OnDrawGizmosSelected()
    {
        if (deathmatchSpawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform point in deathmatchSpawnPoints)
        {
            if (point == null) continue;
            // 씬 편집 시 발 위치와 바라보는 방향을 확인한다. 실행 중 오브젝트를 생성하지 않는다.
            Gizmos.DrawWireSphere(point.position, 0.35f);
            Gizmos.DrawLine(point.position, point.position + point.forward * 1.5f);
            Gizmos.DrawLine(point.position, point.position + Vector3.up * 1.6f);
        }
    }

    public Transform GetSpawnPoint(PlayerState.BattleState battleState, int index)
    {
        // 데스매치에서 기존 팀 스폰을 잘못 사용하는 경로를 차단한다.
        if (!UsesDemolitionRules) return null;

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
        // 데스매치에서 기존 팀 스폰을 잘못 사용하는 경로를 차단한다.
        if (!UsesDemolitionRules) return null;

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
        requestedBarriersActive = active;
        ApplyMatchMode();
    }

    public Transform GetSpawnPoint(byte team, byte slotIndex)
    {
        // 데스매치에서 기존 팀 스폰을 잘못 사용하는 경로를 차단한다.
        if (!UsesDemolitionRules) return null;

        Transform[] points = null;

        // 지금 기준:
        // A팀 = Attack
        // B팀 = Defense
        if (team == 0)
            points = attackSpawnPoints;
        else
            points = defendSpawnPoints;

        if (points == null || points.Length == 0)
            return null;

        int index = Mathf.Clamp(slotIndex, 0, points.Length - 1);

        return points[index];
    }
}