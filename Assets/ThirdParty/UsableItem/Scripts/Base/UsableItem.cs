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

        // 임시: A팀, B팀 플레이어 캐릭터와 충돌했을 때
        if (other.CompareTag("Player A") || other.CompareTag("Player B"))
        {
            isCollected = true;
            OnCollected(other.gameObject);
            DeactivateAndScheduleRespawn();
        }
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
