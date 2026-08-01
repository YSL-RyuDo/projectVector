using System.Collections.Generic;
using UnityEngine;

public class ControllerDecoyNetworkManager : MonoBehaviour
{
    public static ControllerDecoyNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject _decoyProjectilePrefab;
    [SerializeField] private GameObject _decoyAreaPrefab;

    private readonly Dictionary<uint, GameObject> _projectiles = new Dictionary<uint, GameObject>();
    private readonly Dictionary<uint, GameObject> _areas = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void OnDecoySpawn(uint decoyId, uint ownerPlayerId, Vector3 position, Vector3 velocity)
    {
        if (_decoyProjectilePrefab == null)
        {
            Debug.LogError("[CONTROLLER DECOY NET] Projectile prefab is null");
            return;
        }

        if (_projectiles.TryGetValue(decoyId, out GameObject oldProjectile))
        {
            if (oldProjectile != null) Destroy(oldProjectile);
            _projectiles.Remove(decoyId);
        }

        Quaternion rot = velocity.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
            : Quaternion.identity;

        GameObject obj = Instantiate(_decoyProjectilePrefab, position, rot);
        obj.SetActive(true);

        DecoyProjectile projectile = obj.GetComponent<DecoyProjectile>();
        if (projectile == null) projectile = obj.GetComponentInChildren<DecoyProjectile>(true);

        bool isMine = NetworkManager.Instance != null && ownerPlayerId == NetworkManager.Instance.MyPlayerId;

        if (projectile != null)
            projectile.InitFromServer(decoyId, ownerPlayerId, isMine);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        IgnoreOwnerCollision(obj, ownerPlayerId);

        _projectiles[decoyId] = obj;

        Debug.Log($"[CONTROLLER DECOY NET SPAWN] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, velocity={velocity}");
    }

    public void OnDecoyActivate(uint decoyId, uint ownerPlayerId, Vector3 position, Vector3 normal, float duration)
    {
        RemoveProjectile(decoyId);

        if (_decoyAreaPrefab == null)
        {
            Debug.LogError("[CONTROLLER DECOY NET] Area prefab is null");
            return;
        }

        if (_areas.TryGetValue(decoyId, out GameObject oldArea))
        {
            if (oldArea != null) Destroy(oldArea);
            _areas.Remove(decoyId);
        }

        Vector3 spawnPos = position + normal.normalized * 0.05f;
        Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, normal.normalized);

        GameObject area = Instantiate(_decoyAreaPrefab, spawnPos, spawnRot);
        area.SetActive(true);

        DecoyTrapArea trapArea = area.GetComponent<DecoyTrapArea>();
        if (trapArea == null) trapArea = area.GetComponentInChildren<DecoyTrapArea>(true);

        if (trapArea != null)
            trapArea.PlayDecoy();

        _areas[decoyId] = area;

        if (duration > 0.01f)
            StartCoroutine(DestroyAreaRoutine(decoyId, duration));

        Debug.Log($"[CONTROLLER DECOY NET ACTIVATE] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, normal={normal}, duration={duration}");
    }

    public void OnDecoyDestroy(uint decoyId)
    {
        RemoveProjectile(decoyId);

        if (_areas.TryGetValue(decoyId, out GameObject area))
        {
            if (area != null) Destroy(area);
            _areas.Remove(decoyId);
        }

        Debug.Log($"[CONTROLLER DECOY NET DESTROY] decoyId={decoyId}");
    }

    private void RemoveProjectile(uint decoyId)
    {
        if (!_projectiles.TryGetValue(decoyId, out GameObject obj)) return;

        if (obj != null) Destroy(obj);
        _projectiles.Remove(decoyId);
    }

    private System.Collections.IEnumerator DestroyAreaRoutine(uint decoyId, float duration)
    {
        yield return new WaitForSeconds(duration);
        OnDecoyDestroy(decoyId);
    }

    private void IgnoreOwnerCollision(GameObject projectile, uint ownerPlayerId)
    {
        if (projectile == null) return;
        if (NetworkManager.Instance == null) return;
        if (ownerPlayerId != NetworkManager.Instance.MyPlayerId) return;

        PlayerIdentity myPlayer = NetworkManager.Instance.MyPlayer;
        if (myPlayer == null) return;

        Collider ownerCol = myPlayer.GetComponent<Collider>();
        if (ownerCol == null) ownerCol = myPlayer.GetComponentInChildren<Collider>();

        Collider[] projectileCols = projectile.GetComponentsInChildren<Collider>(true);

        foreach (Collider projectileCol in projectileCols)
        {
            if (ownerCol != null && projectileCol != null)
                Physics.IgnoreCollision(ownerCol, projectileCol, true);
        }
    }
}