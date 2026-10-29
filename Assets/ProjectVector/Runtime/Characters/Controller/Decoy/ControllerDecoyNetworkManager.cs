using System.Collections.Generic;
using UnityEngine;

public class ControllerDecoyNetworkManager : MonoBehaviour
{
    public static ControllerDecoyNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject _decoyProjectilePrefab;
    [SerializeField] private GameObject _decoyAreaPrefab;

    private readonly Dictionary<uint, GameObject> _projectiles = new Dictionary<uint, GameObject>();
    private readonly Dictionary<uint, GameObject> _areas = new Dictionary<uint, GameObject>();
    private readonly Dictionary<uint, uint> _owners = new Dictionary<uint, uint>();
    private readonly HashSet<uint> _playing = new HashSet<uint>();
    private readonly Dictionary<uint, float> _requests = new Dictionary<uint, float>();
#if UNITY_EDITOR
    public System.Action<uint> EditorCaptureTrigger;
#endif

    public bool TryTriggerOwned(uint owner)
    {
        foreach (var pair in _owners)
        {
            if (pair.Value != owner) continue;
            uint id = pair.Key;
            if (!_areas.ContainsKey(id) || _playing.Contains(id)) return true;
            if (_requests.TryGetValue(id, out float last) && Time.unscaledTime - last < .35f) return true;
            _requests[id] = Time.unscaledTime;
#if UNITY_EDITOR
            if (Application.isBatchMode && EditorCaptureTrigger != null) { EditorCaptureTrigger(id); return true; }
#endif
            NetworkManager.Instance?.SendControllerDecoyTrigger(id);
            return true;
        }
        return false;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void OnDecoySpawn(uint decoyId, uint ownerPlayerId, Vector3 position, Vector3 velocity)
    {
        _owners[decoyId] = ownerPlayerId;
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
        if (_areas.TryGetValue(decoyId, out var existing) && existing != null) return;
        _owners[decoyId] = ownerPlayerId;
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

        normal = normal.sqrMagnitude > .01f ? normal.normalized : Vector3.up;
        Vector3 spawnPos = position + normal * 0.02f;
        Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, normal.normalized);

        GameObject area = Instantiate(_decoyAreaPrefab, spawnPos, spawnRot);
        area.SetActive(true);

        DecoyTrapArea trapArea = area.GetComponent<DecoyTrapArea>();
        if (trapArea == null) trapArea = area.GetComponentInChildren<DecoyTrapArea>(true);

        if (trapArea != null)
            trapArea.Arm();

        _areas[decoyId] = area;

        // No expiry while armed. The server trigger starts playback and the six-second clock.

        Debug.Log($"[CONTROLLER DECOY NET ACTIVATE] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, normal={normal}, duration={duration}");
    }

    public void OnDecoyDestroy(uint decoyId)
    {
        _owners.Remove(decoyId); _playing.Remove(decoyId); _requests.Remove(decoyId);
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

    private System.Collections.IEnumerator DestroyAreaRoutine(uint decoyId, GameObject instance, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_areas.TryGetValue(decoyId, out var current) && current == instance) OnDecoyDestroy(decoyId);
    }

    public void OnDecoyTrigger(uint id, float duration)
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0 ||
            !_areas.TryGetValue(id, out var area) || !area || !_playing.Add(id)) return;
        area.GetComponentInChildren<DecoyTrapArea>()?.PlayDecoy(duration, id);
        StartCoroutine(DestroyAreaRoutine(id, area, duration));
    }

    private void OnDestroy()
    {
        foreach (var obj in _projectiles.Values) if (obj) Destroy(obj);
        foreach (var obj in _areas.Values) if (obj) Destroy(obj);
        _projectiles.Clear(); _areas.Clear();
        _owners.Clear(); _playing.Clear(); _requests.Clear();
        if (Instance == this) Instance = null;
    }

    private void IgnoreOwnerCollision(GameObject projectile, uint ownerPlayerId)
    {
        if (projectile == null) return;
        if (NetworkManager.Instance == null) return;
        if (ownerPlayerId != NetworkManager.Instance.MyPlayerId) return;

        PlayerIdentity myPlayer = NetworkManager.Instance.MyPlayer;
        if (myPlayer == null) return;

        Collider[] ownerCols = myPlayer.GetComponentsInChildren<Collider>(true);
        Collider[] projectileCols = projectile.GetComponentsInChildren<Collider>(true);

        foreach (Collider projectileCol in projectileCols)
        {
            foreach (var ownerCol in ownerCols)
                if (ownerCol != null && projectileCol != null)
                    Physics.IgnoreCollision(ownerCol, projectileCol, true);
        }
    }
}
