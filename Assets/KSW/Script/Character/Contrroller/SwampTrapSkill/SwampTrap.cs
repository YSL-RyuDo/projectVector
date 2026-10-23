using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwampTrap : MonoBehaviour
{
#if UNITY_EDITOR
    public System.Action<uint,Vector3> EditorCaptureActivate;
#endif
    private uint _trapId;
    private uint _ownerPlayerId;
    private bool _isMine;
    private bool _initialized;
    private bool _activated;

    public uint TrapId => _trapId;
    public uint OwnerPlayerId => _ownerPlayerId;

    public void InitFromServer(uint trapId, uint ownerPlayerId, bool isMine)
    {
        _trapId = trapId;
        _ownerPlayerId = ownerPlayerId;
        _isMine = isMine;
        _initialized = true;
        _activated = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_initialized) return;
        if (_activated) return;
        if (!_isMine) return;
        if (collision.contactCount <= 0) return;
        if (collision.collider.GetComponentInParent<CharacterController>() || collision.collider.GetComponentInParent<PlayerIdentity>()) return;

        ContactPoint bestContact = collision.contacts[0];

        for (int i = 1; i < collision.contactCount; i++)
        {
            if (collision.contacts[i].normal.y > bestContact.normal.y)
                bestContact = collision.contacts[i];
        }

        if (bestContact.normal.y < 0.75f)
            return;

        _activated = true;

#if UNITY_EDITOR
        if (Application.isBatchMode && EditorCaptureActivate != null) { EditorCaptureActivate(_trapId,bestContact.point); return; }
#endif

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendControllerSwampActivate(_trapId, bestContact.point, Vector3.up);
    }
}
