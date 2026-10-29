using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentinelCNetworkManager : MonoBehaviour
{
    public static SentinelCNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private GameObject qTrapPrefab;
    [SerializeField] private GameObject eTrapPrefab;
    [SerializeField] private GameObject xAreaPrefab;

    private readonly Dictionary<uint, Sentinel_Trap> traps = new();
    private readonly Dictionary<uint, Sentinel_SlowTrap> qTraps = new();
    private readonly Dictionary<uint, TrapDevice> eTraps = new();
    private readonly Dictionary<uint, X_Area> xAreas = new();
    private readonly Dictionary<uint, uint> xProtectedOwners = new();
    private readonly HashSet<ulong> statusEvents = new();
    public bool HasInstalledDevice(uint owner) =>
        System.Linq.Enumerable.Any(traps.Values,t=>t && t.OwnerPlayerId==owner && t.IsPlaced) ||
        System.Linq.Enumerable.Any(qTraps.Values,t=>t && t.OwnerPlayerId==owner) ||
        System.Linq.Enumerable.Any(eTraps.Values,t=>t && t.OwnerPlayerId==owner);
    public void ReceiveTracking(byte[] b)
    {
        if(b==null||b.Length!=12)return;
        uint id=System.BitConverter.ToUInt32(b,0),target=System.BitConverter.ToUInt32(b,4);float seconds=System.BitConverter.ToSingle(b,8);
        if(!float.IsFinite(seconds)||seconds<0||seconds>1||!traps.TryGetValue(id,out var trap)||!trap)return;
        Transform actor=null;
        foreach(var identity in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))if(identity.PlayerId==target){actor=identity.transform;break;}
        trap.GetComponent<SentinelDeviceVfx>()?.Track(actor,seconds);
        if(NetworkManager.Instance && NetworkManager.Instance.MyPlayerId==trap.OwnerPlayerId)SentinelFeedbackUI.Instance?.Track(id,seconds);
    }
    public void ReceiveStatus(byte[] b)
    {
        if(b==null||b.Length!=24||!NetworkManager.Instance)return;
        uint id=System.BitConverter.ToUInt32(b,0),target=System.BitConverter.ToUInt32(b,4),sequence=System.BitConverter.ToUInt32(b,8);
        float slow=System.BitConverter.ToSingle(b,12),multiplier=System.BitConverter.ToSingle(b,16),concussion=System.BitConverter.ToSingle(b,20);
        if(!eTraps.TryGetValue(id,out var trap)||!trap||target!=NetworkManager.Instance.MyPlayerId||
           !float.IsFinite(slow)||slow<0||slow>4||!float.IsFinite(multiplier)||multiplier<.05f||multiplier>1||
           !float.IsFinite(concussion)||concussion<0||concussion>3||!statusEvents.Add(((ulong)sequence<<32)|target))return;
        SentinelFeedbackUI.Instance?.ApplyStatus(slow,multiplier,concussion);
    }
    public void ReceiveReveal(byte[] b)
    {
        if(b==null||b.Length!=12)return;
        uint id=System.BitConverter.ToUInt32(b,0),target=System.BitConverter.ToUInt32(b,4);
        float duration=System.BitConverter.ToSingle(b,8);
        if(!float.IsFinite(duration)||duration<=0||duration>2.5f||!traps.TryGetValue(id,out var trap)||!trap)return;
        trap.ApplyConfirmedReveal(target,duration);
    }

    public bool IsSkillProtected(uint ownerPlayerId)
    {
        foreach (uint protectedOwnerId in xProtectedOwners.Values)
        {
            if (protectedOwnerId == ownerPlayerId)
                return true;
        }

        return false;
    }

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

    public void OnESpawn(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        Vector3 normal,
        float hp,
        float radius)
    {
        if (eTrapPrefab == null)
        {
            Debug.LogError("[SENTINEL E NET] eTrapPrefab 없음");
            return;
        }

        if (eTraps.TryGetValue(trapId, out TrapDevice oldTrap) &&
            oldTrap != null)
        {
            oldTrap.DestroyFromServer();
        }

        Quaternion rotation =
            normal.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;

        GameObject obj = Instantiate(
            eTrapPrefab,
            position,
            rotation
        );
        obj.SetActive(true);

        TrapDevice trap = obj.GetComponent<TrapDevice>();

        if (trap == null)
        {
            trap =
                obj.GetComponentInChildren<TrapDevice>(true);
        }

        if (trap == null)
        {
            Debug.LogError("[SENTINEL E NET] TrapDevice 없음");
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
            normal,
            hp,
            radius
        );

        eTraps[trapId] = trap;

        if (isOwner)
        {
            SentinelSkill_E skill = GetLocalESkill();

            if (skill != null)
                skill.OnNetworkTrapSpawned(trapId);
        }

        Debug.Log(
            $"[SENTINEL E NET SPAWN] trapId={trapId}, " +
            $"owner={ownerPlayerId}, isOwner={isOwner}"
        );
    }

    public void OnEActivate(
        uint trapId,
        float radius,
        float slowDuration,
        float slowMultiplier,
        float stunDuration)
    {
        if (!eTraps.TryGetValue(trapId, out TrapDevice trap) ||
            trap == null)
        {
            return;
        }

        trap.ActivateFromServer(
            radius,
            slowDuration,
            slowMultiplier,
            stunDuration
        );
    }

    public void OnEDestroy(
        uint trapId,
        int reason,
        float cooldownSeconds)
    {
        if (!eTraps.TryGetValue(trapId, out TrapDevice trap))
            return;

        bool isOwner =
            trap != null &&
            NetworkManager.Instance != null &&
            trap.OwnerPlayerId ==
            NetworkManager.Instance.MyPlayerId;

        if (trap != null)
            trap.DestroyFromServer();

        eTraps.Remove(trapId);

        if (isOwner)
        {
            SentinelSkill_E skill = GetLocalESkill();

            if (skill != null)
            {
                skill.OnNetworkTrapRemoved(
                    trapId,
                    cooldownSeconds
                );
            }
        }

        Debug.Log(
            $"[SENTINEL E NET DESTROY] trapId={trapId}, " +
            $"reason={reason}, cooldown={cooldownSeconds}"
        );
    }

    public void OnXSpawn(
        uint ultimateId,
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        float radius,
        float duration,
        float slowMultiplier,
        float protectionDuration)
    {
        if (xAreaPrefab == null)
        {
            Debug.LogError("[SENTINEL X NET] xAreaPrefab 없음");
            return;
        }

        if (xAreas.TryGetValue(ultimateId, out X_Area oldArea) &&
            oldArea != null)
        {
            oldArea.DestroyFromServer();
        }

        GameObject obj = Instantiate(
            xAreaPrefab,
            position,
            Quaternion.identity
        );

        X_Area area = obj.GetComponent<X_Area>();

        if (area == null)
            area = obj.GetComponentInChildren<X_Area>(true);

        if (area == null)
        {
            Debug.LogError("[SENTINEL X NET] X_Area 없음");
            Destroy(obj);
            return;
        }

        area.InitNetwork(
            ultimateId,
            ownerTeam,
            position,
            radius,
            duration,
            slowMultiplier
        );

        if(!IsSkillProtected(ownerPlayerId))
        {
            foreach(var device in traps.Values)if(device && device.OwnerPlayerId==ownerPlayerId)device.GetComponent<SentinelDeviceVfx>()?.Pulse();
            foreach(var device in qTraps.Values)if(device && device.OwnerPlayerId==ownerPlayerId)device.GetComponent<SentinelDeviceVfx>()?.Pulse();
            foreach(var device in eTraps.Values)if(device && device.OwnerPlayerId==ownerPlayerId)device.GetComponent<SentinelDeviceVfx>()?.Pulse();
            foreach(var actor in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))if(actor.PlayerId==ownerPlayerId)actor.GetComponent<SentinelCastMotion>()?.ConfirmOvercharge();
            if(NetworkManager.Instance && NetworkManager.Instance.MyPlayerId==ownerPlayerId)SentinelFeedbackUI.Instance?.Notice("과충전 · 장치 보호 활성화");
        }
        xAreas[ultimateId] = area;
        xProtectedOwners[ultimateId] = ownerPlayerId;

        StartCoroutine(
            RemoveXProtectionAfter(
                ultimateId,
                protectionDuration
            )
        );

        Debug.Log(
            $"[SENTINEL X NET SPAWN] ultimateId={ultimateId}, " +
            $"owner={ownerPlayerId}, radius={radius}, duration={duration}, " +
            $"protection={protectionDuration}"
        );
    }

    public void OnXDestroy(uint ultimateId)
    {
        if (xAreas.TryGetValue(ultimateId, out X_Area area) &&
            area != null)
        {
            area.DestroyFromServer();
        }

        xAreas.Remove(ultimateId);

        Debug.Log(
            $"[SENTINEL X NET DESTROY] ultimateId={ultimateId}"
        );
    }

    private IEnumerator RemoveXProtectionAfter(
        uint ultimateId,
        float duration)
    {
        yield return new WaitForSeconds(duration);
        xProtectedOwners.Remove(ultimateId);
    }

    public void ClearAllTraps()
    {
        statusEvents.Clear();SentinelFeedbackUI.Instance?.Clear();
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

        foreach (TrapDevice trap in eTraps.Values)
        {
            if (trap != null)
                trap.DestroyFromServer();
        }

        eTraps.Clear();

        SentinelSkill_E skill = GetLocalESkill();

        if (skill != null)
            skill.ResetRoundState();

        foreach (X_Area area in xAreas.Values)
        {
            if (area != null)
                area.DestroyFromServer();
        }

        xAreas.Clear();
        xProtectedOwners.Clear();

        Debug.Log("[SENTINEL NET CLEAR ALL]");
    }

    private SentinelSkill_E GetLocalESkill()
    {
        if (NetworkManager.Instance == null ||
            NetworkManager.Instance.MyPlayer == null)
        {
            return null;
        }

        PlayerIdentity player =
            NetworkManager.Instance.MyPlayer;

        SentinelSkill_E skill =
            player.GetComponent<SentinelSkill_E>();

        if (skill == null)
        {
            skill =
                player.GetComponentInChildren<SentinelSkill_E>(true);
        }

        return skill;
    }

    private void OnDestroy()
    {
        ClearAllTraps();

        if (Instance == this)
            Instance = null;
    }
}
