using UnityEngine;

public class DecoyProjectile : MonoBehaviour
{
    public uint DecoyId { get; private set; }
    public uint OwnerPlayerId { get; private set; }
    public bool IsMine { get; private set; }

    private bool _activated;

    public void InitFromServer(uint decoyId, uint ownerPlayerId, bool isMine)
    {
        DecoyId = decoyId;
        OwnerPlayerId = ownerPlayerId;
        IsMine = isMine;
        _activated = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_activated) return;
        if (collision.contactCount <= 0) return;

        ContactPoint contact = collision.contacts[0];

        if (contact.normal.y < 0.55f)
            return;

        _activated = true;

        if (IsMine && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendControllerDecoyActivate(DecoyId, contact.point, contact.normal);
            Debug.Log($"[CONTROLLER DECOY ACTIVATE REQUEST] decoyId={DecoyId}, pos={contact.point}, normal={contact.normal}");
        }
    }
}