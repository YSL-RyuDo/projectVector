using UnityEngine;

public class DroppedSpikePickup : MonoBehaviour
{
    private bool requested;

    private void OnTriggerEnter(Collider other)
    {
        if (requested)
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

        requested = true;

        Debug.Log($"[SPIKE CLIENT] pickup trigger playerId={identity.PlayerId}");

        NetworkManager.Instance.SendSpikePickupRequest();
    }
}