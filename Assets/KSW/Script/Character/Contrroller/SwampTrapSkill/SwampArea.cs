using System.Collections.Generic;
using UnityEngine;

public class SwampArea : MonoBehaviour
{
    [SerializeField] private float _slowMultiplier = 0.2f;

    private uint _ownerPlayerId;
    private readonly Dictionary<PlayerMovement, int> _affectedPlayers = new Dictionary<PlayerMovement, int>();

    public void InitFromServer(uint ownerPlayerId, float slowMultiplier)
    {
        _ownerPlayerId = ownerPlayerId;
        _slowMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
    }

    public void Init(float radius, float duration, float slowMultiplier)
    {
        _slowMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null) sphere = GetComponentInChildren<SphereCollider>(true);

        if (sphere != null)
        {
            sphere.enabled = true;
            sphere.isTrigger = true;
            sphere.radius = radius;
        }

        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();

        if (movement == null)
            return;

        if (_affectedPlayers.ContainsKey(movement))
            _affectedPlayers[movement]++;
        else
            _affectedPlayers.Add(movement, 1);

        movement.SetExternalSpeedMultiplier(_slowMultiplier);

        Debug.Log($"[SWAMP SLOW ENTER] player={movement.name}, multiplier={_slowMultiplier}");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();

        if (movement == null)
            return;

        if (!_affectedPlayers.ContainsKey(movement))
            return;

        _affectedPlayers[movement]--;

        if (_affectedPlayers[movement] > 0)
            return;

        _affectedPlayers.Remove(movement);
        movement.SetExternalSpeedMultiplier(1f);

        Debug.Log($"[SWAMP SLOW EXIT] player={movement.name}");
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<PlayerMovement, int> pair in _affectedPlayers)
        {
            if (pair.Key != null)
                pair.Key.SetExternalSpeedMultiplier(1f);
        }

        _affectedPlayers.Clear();
    }
}