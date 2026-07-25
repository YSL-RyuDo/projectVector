using System.Collections.Generic;
using UnityEngine;

public class SentinelCNetworkManager : MonoBehaviour
{
    public static SentinelCNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private GameObject qTrapPrefab;

    private readonly Dictionary<uint, Sentinel_Trap> traps = new();
    private readonly Dictionary<uint, Sentinel_SlowTrap> qTraps = new();

    private void Awake()
    {
        Instance = this;
    }

    public void OnSpawn(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        Vector3 velocity,
        float hp)
    {
        if (trapPrefab == null)
        {
            Debug.LogError("[SENTINEL C NET] trapPrefab 없음");
            return;
        }

        if (traps.TryGetValue(trapId, out Sentinel_Trap oldTrap) && oldTrap != null)
            Destroy(oldTrap.gameObject);

        GameObject obj = Instantiate(
            trapPrefab,
            position,
            Quaternion.LookRotation(velocity.normalized)
        );

        Sentinel_Trap trap = obj.GetComponent<Sentinel_Trap>();

        if (trap == null)
        {
            Debug.LogError("[SENTINEL C NET] Sentinel_Trap 없음");
            Destroy(obj);
            return;
        }

        bool isOwner =
            NetworkManager.Instance != null &&
            NetworkManager.Instance.MyPlayerId == ownerPlayerId;

        trap.InitNetwork(
            trapId,
            ownerPlayerId,
            ownerTeam,
            isOwner,
            hp
        );

        trap.LaunchVelocity(velocity);

        traps[trapId] = trap;

        Debug.Log($"[SENTINEL C NET SPAWN] trapId={trapId}, owner={ownerPlayerId}, isOwner={isOwner}");
    }

    public void OnAnchor(uint trapId, Vector3 position, Vector3 normal)
    {
        if (!traps.TryGetValue(trapId, out Sentinel_Trap trap))
            return;

        if (trap == null)
            return;

        trap.ApplyAnchorFromServer(position, normal);
    }

    public void OnTrigger(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        float radius,
        float revealDuration)
    {
        if (!traps.TryGetValue(trapId, out Sentinel_Trap trap))
            return;

        if (trap == null)
            return;

        trap.PlayScanFromServer(
            ownerPlayerId,
            ownerTeam,
            position,
            radius,
            revealDuration
        );
    }

    public void OnDestroyTrap(uint trapId)
    {
        if (!traps.TryGetValue(trapId, out Sentinel_Trap trap))
            return;

        if (trap != null)
            trap.ApplyDestroyFromServer();

        traps.Remove(trapId);

        Debug.Log($"[SENTINEL C NET DESTROY] trapId={trapId}");
    }

    public void OnQSpawn(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        float hp,
        float detectionRadius,
        float zoneRadius)
    {
        if (qTrapPrefab == null)
        {
            Debug.LogError("[SENTINEL Q NET] qTrapPrefab 없음");
            return;
        }

        if (qTraps.TryGetValue(trapId, out Sentinel_SlowTrap oldTrap) &&
            oldTrap != null)
        {
            oldTrap.DestroyFromServer();
        }

        GameObject obj = Instantiate(
            qTrapPrefab,
            position,
            Quaternion.identity
        );
        obj.SetActive(true);

        Sentinel_SlowTrap trap =
            obj.GetComponent<Sentinel_SlowTrap>();

        if (trap == null)
        {
            trap =
                obj.GetComponentInChildren<Sentinel_SlowTrap>(true);
        }

        if (trap == null)
        {
            Debug.LogError("[SENTINEL Q NET] Sentinel_SlowTrap 없음");
            Destroy(obj);
            return;
        }

        bool isOwner =
            NetworkManager.Instance != null &&
            NetworkManager.Instance.MyPlayerId == ownerPlayerId;

        trap.InitNetwork(
            trapId,
            ownerPlayerId,
            ownerTeam,
            isOwner,
            position,
            hp,
            detectionRadius,
            zoneRadius
        );

        qTraps[trapId] = trap;

        Debug.Log(
            $"[SENTINEL Q NET SPAWN] trapId={trapId}, " +
            $"owner={ownerPlayerId}, isOwner={isOwner}"
        );
    }

    public void OnQActivate(
        uint trapId,
        float duration,
        float slowMultiplier)
    {
        if (!qTraps.TryGetValue(trapId, out Sentinel_SlowTrap trap) ||
            trap == null)
        {
            return;
        }

        trap.ActivateFromServer(duration, slowMultiplier);
    }

    public void OnQDestroy(uint trapId)
    {
        if (!qTraps.TryGetValue(trapId, out Sentinel_SlowTrap trap))
            return;

        if (trap != null)
            trap.DestroyFromServer();

        qTraps.Remove(trapId);

        Debug.Log($"[SENTINEL Q NET DESTROY] trapId={trapId}");
    }

    public void ClearAllTraps()
    {
        foreach (Sentinel_Trap trap in traps.Values)
        {
            if (trap != null)
                trap.ApplyDestroyFromServer();
        }

        traps.Clear();

        foreach (Sentinel_SlowTrap trap in qTraps.Values)
        {
            if (trap != null)
                trap.DestroyFromServer();
        }

        qTraps.Clear();

        Debug.Log("[SENTINEL C NET CLEAR ALL]");
    }

    private void OnDestroy()
    {
        ClearAllTraps();

        if (Instance == this)
            Instance = null;
    }
}
