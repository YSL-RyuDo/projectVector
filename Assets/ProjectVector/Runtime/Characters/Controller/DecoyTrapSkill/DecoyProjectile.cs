using UnityEngine;

public class DecoyProjectile : MonoBehaviour
{
    public uint DecoyId { get; private set; }
    public uint OwnerPlayerId { get; private set; }
    public bool IsMine { get; private set; }

    private bool _activated;
#if UNITY_EDITOR
    public System.Action<uint, Vector3, Vector3> EditorCaptureActivate;
#endif

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
        if (collision.collider.GetComponentInParent<CharacterController>() ||
            collision.collider.GetComponentInParent<PlayerIdentity>() ||
            collision.collider.GetComponentInParent<DecoyProjectile>()) return;

        ContactPoint contact = collision.contacts[0];

        _activated = true;
        // Preserve the surface normal for walls as well as floors and ramps.
        var body = GetComponent<Rigidbody>();
        if (body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true; }
        transform.SetPositionAndRotation(contact.point + contact.normal * .02f,
            Quaternion.FromToRotation(Vector3.up, contact.normal));
#if UNITY_EDITOR
        if (Application.isBatchMode && IsMine && EditorCaptureActivate != null)
        { EditorCaptureActivate(DecoyId, contact.point, contact.normal); return; }
#endif

        if (IsMine && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendControllerDecoyActivate(DecoyId, contact.point, contact.normal);
            Debug.Log($"[CONTROLLER DECOY ACTIVATE REQUEST] decoyId={DecoyId}, pos={contact.point}, normal={contact.normal}");
        }
    }
}
