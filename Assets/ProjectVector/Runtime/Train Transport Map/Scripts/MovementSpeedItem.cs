using UnityEngine;

public class MovementSpeedItem : UsableItem
{
    [Header("Speed Buff Settings")]
    [Tooltip("이동 속도 배율 (기본 2배)")]
    [SerializeField] private float speedMultiplier = 2f;

    [Tooltip("버프 지속 시간 (초) (기본 30초)")]
    [SerializeField] private float buffDuration = 30f;

    public MovementSpeedItem()
    {
        respawnTime = 1f;
    }

    private void Reset()
    {
        respawnTime = 1f;
        speedMultiplier = 2f;
        buffDuration = 30f;
    }

    protected override void Awake()
    {
        base.Awake();
        if (respawnTime <= 0f)
        {
            respawnTime = 1f;
        }
        if (speedMultiplier <= 0f)
        {
            speedMultiplier = 2f;
        }
        if (buffDuration <= 0f)
        {
            buffDuration = 30f;
        }
    }

    protected override void OnCollected(GameObject player)
    {
        base.OnCollected(player);

        if (player == null) return;

        // PlayerMovement 컴포넌트 검색 (본인 또는 부모/자식)
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm == null)
            pm = player.GetComponentInParent<PlayerMovement>();
        if (pm == null)
            pm = player.GetComponentInChildren<PlayerMovement>();

        if (pm != null)
        {
            pm.ApplySpeedBuff(speedMultiplier, buffDuration);
            Debug.Log($"[MovementSpeedItem] {player.name}에게 이동 속도 {speedMultiplier}배 버프 적용 ({buffDuration}초 동안)");
        }
    }
}
