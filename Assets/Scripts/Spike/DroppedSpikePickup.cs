using UnityEngine;

public class DroppedSpikePickup : MonoBehaviour
{
    private uint ignorePlayerId;
    private float ignoreUntilTime;

    private bool pickupRequested = false;

    public void Init(uint dropperPlayerId, float ignoreSeconds)
    {
        ignorePlayerId = dropperPlayerId;
        ignoreUntilTime = Time.time + ignoreSeconds;
        pickupRequested = false;
    }

    private void OnTriggerEnter(Collider other)
    {
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

        Renderer[] renderers = GetComponentInParent<Rigidbody>()
            ?.GetComponentsInChildren<Renderer>(true);

        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null)
                    r.enabled = false;
            }
        }

        NetworkManager.Instance.SendSpikePickupRequest();

        Debug.Log("[SPIKE PICKUP REQUEST SENT]");
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