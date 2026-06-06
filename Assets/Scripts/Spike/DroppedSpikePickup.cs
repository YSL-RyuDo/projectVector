using UnityEngine;

public class DroppedSpikePickup : MonoBehaviour
{
    private uint ignorePlayerId;
    private float ignoreUntilTime;

    // 생성 직후에는 모든 플레이어가 잠깐 못 줍게 막는다.
    private float pickupEnableTime;

    private bool pickupRequested = false;

    public void Init(uint dropperPlayerId, float ignoreSeconds)
    {
        ignorePlayerId = dropperPlayerId;
        ignoreUntilTime = Time.time + ignoreSeconds;

        // 버리자마자 몸 콜라이더와 겹쳐서 바로 다시 먹는 문제 방지
        pickupEnableTime = Time.time + 0.35f;

        pickupRequested = false;

        SetVisible(true);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < pickupEnableTime)
            return;

        if (pickupRequested)
            return;

        PlayerIdentity identity = other.GetComponent<PlayerIdentity>();

        if (identity == null)
            identity = other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (identity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        if (identity.PlayerId == ignorePlayerId && Time.time < ignoreUntilTime)
        {
            Debug.Log("[SPIKE PICKUP BLOCK] dropper cannot repickup yet");
            return;
        }

        PlayerHP hp = identity.GetComponent<PlayerHP>();
        if (hp != null && hp.IsDead)
            return;

        PlayerDeathController death = identity.GetComponent<PlayerDeathController>();
        if (death != null && death.IsDead)
            return;

        pickupRequested = true;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        SetVisible(false);

        NetworkManager.Instance.SendSpikePickupRequest();

        Debug.Log("[SPIKE PICKUP REQUEST SENT]");
    }

    private void SetVisible(bool visible)
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();

        Transform root = transform;

        if (rb != null)
            root = rb.transform;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }

    public void DestroyDroppedSpike()
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();

        if (rb != null)
            Destroy(rb.gameObject);
        else
            Destroy(gameObject);
    }
}