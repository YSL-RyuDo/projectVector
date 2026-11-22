using UnityEngine;

public abstract class UsableItem : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("플레이어가 먹은 후 다시 활성화되기까지 걸리는 시간 (초)")]
    [SerializeField] protected float respawnTime = 1f;

    private bool isCollected = false;

    protected virtual void Awake()
    {
    }

    protected virtual void OnEnable()
    {
        isCollected = false;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (IsPlayer(other, out GameObject playerRoot))
        {
            isCollected = true;
            OnCollected(playerRoot);
            DeactivateAndScheduleRespawn();
        }
    }

    /// <summary>
    /// 충돌한 대상이 플레이어인지 확인하고 루트 오브젝트를 반환합니다.
    /// </summary>
    protected virtual bool IsPlayer(Collider other, out GameObject playerRoot)
    {
        playerRoot = null;
        if (other == null) return false;

        // 1. PlayerMovement 컴포넌트 우선 확인 (본인 또는 부모)
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            playerRoot = pm.gameObject;
            return true;
        }

        // 2. 태그 확인 (Player A, Player B, PlayerA, PlayerB, Player)
        if (CheckPlayerTag(other.gameObject))
        {
            playerRoot = other.gameObject;
            return true;
        }

        if (other.transform.parent != null && CheckPlayerTag(other.transform.parent.gameObject))
        {
            playerRoot = other.transform.parent.gameObject;
            return true;
        }

        return false;
    }

    private bool CheckPlayerTag(GameObject obj)
    {
        if (obj == null) return false;
        return obj.CompareTag("Player A") || obj.CompareTag("Player B") ||
               obj.CompareTag("PlayerA") || obj.CompareTag("PlayerB") ||
               obj.CompareTag("Player");
    }

    /// <summary>
    /// 플레이어가 아이템을 획득했을 때 호출됩니다.
    /// 서브클래스에서 효과 적용 로직을 오버라이드할 수 있습니다.
    /// </summary>
    protected virtual void OnCollected(GameObject player)
    {
    }

    /// <summary>
    /// 아이템을 비활성화하고 respawnTime 후 다시 활성화하도록 예약합니다.
    /// </summary>
    protected virtual void DeactivateAndScheduleRespawn()
    {
        ItemRespawnRunner.Instance.Respawn(gameObject, respawnTime);
        gameObject.SetActive(false);
    }
}
