using UnityEngine;

public class DroppedSpikePickup : MonoBehaviour
{
    private uint ignorePlayerId;
    private float ignoreUntilTime;

// 생성 직후 모든 플레이어가 잠시 못 줍도록 한다.
private float pickupEnableTime;

    // 서버 요청 중 중복 전송 방지
    private bool pickupRequested;

    [Header("Request")]
    [SerializeField] private float requestCooldown = 0.5f;

    public void Init(uint dropperPlayerId, float ignoreSeconds)
    {
        ignorePlayerId = dropperPlayerId;
        ignoreUntilTime = Time.time + ignoreSeconds;

        // 버리자마자 몸과 겹쳐 즉시 다시 먹는 문제 방지
        pickupEnableTime = Time.time + 0.35f;

        pickupRequested = false;

        CancelInvoke(nameof(ResetPickupRequest));

        // 새로 생성된 스파이크는 항상 보이도록 설정
        SetVisible(true);

        Collider col = GetComponent<Collider>();

        if (col == null)
            col = GetComponentInChildren<Collider>(true);

        if (col != null)
            col.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < pickupEnableTime)
            return;

        if (pickupRequested)
            return;

        if (NetworkManager.Instance == null)
            return;

        PlayerIdentity identity =
            other.GetComponent<PlayerIdentity>();

        if (identity == null)
        {
            identity =
                other.GetComponentInParent<PlayerIdentity>();
        }

        if (identity == null)
            return;

        // 이 클라이언트의 로컬 플레이어 충돌만 처리
        if (identity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        // 버린 플레이어는 일정 시간 다시 줍지 못함
        if (identity.PlayerId == ignorePlayerId &&
            Time.time < ignoreUntilTime)
        {
            Debug.Log(
                "[SPIKE PICKUP BLOCK] dropper cannot repickup yet"
            );

            return;
        }

        PlayerHP hp =
            identity.GetComponent<PlayerHP>();

        if (hp == null)
        {
            hp =
                identity.GetComponentInChildren<PlayerHP>(true);
        }

        if (hp != null && hp.IsDead)
            return;

        PlayerDeathController death =
            identity.GetComponent<PlayerDeathController>();

        if (death == null)
        {
            death =
                identity.GetComponentInChildren<PlayerDeathController>(true);
        }

        if (death != null && death.IsDead)
            return;

        pickupRequested = true;

        // 중요:
        // 여기서 Collider를 끄거나 스파이크를 숨기지 않는다.
        // 서버가 성공 통지를 보낸 뒤 NetworkManager에서 삭제한다.
        NetworkManager.Instance.SendSpikePickupRequest();

        // 서버가 요청을 거절한 경우 다시 요청할 수 있도록 잠금 해제
        Invoke(
            nameof(ResetPickupRequest),
            requestCooldown
        );

        Debug.Log(
            $"[SPIKE PICKUP REQUEST SENT] playerId={identity.PlayerId}"
        );
    }

    private void ResetPickupRequest()
    {
        pickupRequested = false;
    }

    private void SetVisible(bool visible)
    {
        Rigidbody rb =
            GetComponentInParent<Rigidbody>();

        Transform root =
            rb != null ? rb.transform : transform;

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    public void DestroyDroppedSpike()
    {
        CancelInvoke(nameof(ResetPickupRequest));

        Rigidbody rb =
            GetComponentInParent<Rigidbody>();

        if (rb != null)
            Destroy(rb.gameObject);
        else
            Destroy(gameObject);
    }
}
