using System.Collections.Generic;
using UnityEngine;

public class DroneNetworkManager : MonoBehaviour
{
    public static DroneNetworkManager Instance { get; private set; }

    [SerializeField] private Drone _dronePrefab;

    private readonly Dictionary<uint, Drone> _drones = new();
    private readonly Dictionary<uint, uint> _ownerDroneIds = new();

    // droneId -> hp
    private readonly Dictionary<uint, int> _droneHps = new();
    private readonly Dictionary<uint, int> _droneMaxHps = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnDroneSpawn(
        uint droneId,
        uint ownerPlayerId,
        float x,
        float y,
        float z,
        float yaw,
        float pitch)
    {
        if (_dronePrefab == null)
        {
            Debug.LogError("[DRONE SPAWN] drone prefab is null");
            return;
        }

        // 같은 droneId가 이미 있으면 정리
        if (_drones.TryGetValue(droneId, out Drone oldDrone))
        {
            if (oldDrone != null)
                Destroy(oldDrone.gameObject);

            _drones.Remove(droneId);
            _droneHps.Remove(droneId);
            _droneMaxHps.Remove(droneId);
        }

        // 같은 owner가 이전 드론을 가지고 있으면 정리
        if (_ownerDroneIds.TryGetValue(ownerPlayerId, out uint oldDroneId))
        {
            if (oldDroneId != droneId)
            {
                if (_drones.TryGetValue(oldDroneId, out Drone previousOwnerDrone))
                {
                    if (previousOwnerDrone != null)
                        Destroy(previousOwnerDrone.gameObject);

                    _drones.Remove(oldDroneId);
                }

                _droneHps.Remove(oldDroneId);
                _droneMaxHps.Remove(oldDroneId);
            }
        }

        Vector3 pos = new Vector3(x, y, z);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        Drone drone = Instantiate(_dronePrefab, pos, rot);

        bool isMine =
            NetworkManager.Instance != null &&
            ownerPlayerId == NetworkManager.Instance.MyPlayerId;

        InitiatorSkill_Q ownerSkill = null;

        if (isMine)
        {
            PlayerIdentity ownerIdentity =
                FindPlayerIdentityById(ownerPlayerId);

            if (ownerIdentity != null)
                ownerSkill = ownerIdentity.GetComponent<InitiatorSkill_Q>();
        }

        drone.Init(
            droneId,
            ownerPlayerId,
            isMine,
            ownerSkill
        );

        _drones[droneId] = drone;
        _ownerDroneIds[ownerPlayerId] = droneId;

        // 서버 HP 패킷이 오기 전까지 기본값 100
        _droneHps[droneId] = 100;
        _droneMaxHps[droneId] = 100;

        // 생성 시점 회전값도 바로 반영
        if (!isMine)
        {
            drone.ApplyRemoteState(
                pos,
                yaw,
                pitch
            );
        }

        if (isMine && ownerSkill != null)
        {
            ownerSkill.OnOwnerDroneSpawned(drone, droneId);

            if (DroneHUDUI.Instance != null)
                DroneHUDUI.Instance.SetDroneHp(100, 100);
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyDroneStateChanged(ownerPlayerId);
        }

        Debug.Log(
            $"[DRONE SPAWN APPLY] " +
            $"droneId={droneId}, owner={ownerPlayerId}, " +
            $"isMine={isMine}, pos={pos}, yaw={yaw}, pitch={pitch}"
        );
    }

    public void OnDroneState(
        uint droneId,
        uint ownerPlayerId,
        float x,
        float y,
        float z,
        float yaw,
        float pitch)
    {
        if (!_drones.TryGetValue(droneId, out Drone drone))
            return;

        if (drone == null)
        {
            _drones.Remove(droneId);
            _ownerDroneIds.Remove(ownerPlayerId);
            _droneHps.Remove(droneId);
            _droneMaxHps.Remove(droneId);
            return;
        }

        if (drone.IsMine)
            return;

        drone.ApplyRemoteState(
            new Vector3(x, y, z),
            yaw,
            pitch
        );
    }

    public void OnDroneDestroy(
        uint droneId,
        uint ownerPlayerId,
        byte reason)
    {
        if (!_drones.TryGetValue(droneId, out Drone drone))
        {
            _ownerDroneIds.Remove(ownerPlayerId);
            _droneHps.Remove(droneId);
            _droneMaxHps.Remove(droneId);

            if (SpectatorManager.Instance != null)
                SpectatorManager.Instance.NotifyDroneStateChanged(ownerPlayerId);

            return;
        }

        bool isMine = drone != null && drone.IsMine;

        if (drone != null)
        {
            if (reason == 1) drone.GetComponent<DroneCombatVfx>()?.BreakApart();
            Destroy(drone.gameObject);
        }

        _drones.Remove(droneId);
        _ownerDroneIds.Remove(ownerPlayerId);
        _droneHps.Remove(droneId);
        _droneMaxHps.Remove(droneId);

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyDroneStateChanged(ownerPlayerId);
        }

        if (!isMine)
            return;

        PlayerIdentity ownerIdentity =
            FindPlayerIdentityById(ownerPlayerId);

        if (ownerIdentity == null)
            return;

        InitiatorSkill_Q skill =
            ownerIdentity.GetComponent<InitiatorSkill_Q>();

        if (skill != null)
            skill.OnOwnerDroneDestroyed(droneId, reason);
    }

    public void OnDroneHpUpdate(
        uint droneId,
        uint ownerPlayerId,
        int hp,
        int maxHp)
    {
        maxHp = Mathf.Max(1, maxHp);
        hp = Mathf.Clamp(hp, 0, maxHp);

        _droneHps[droneId] = hp;
        _droneMaxHps[droneId] = maxHp;

        if (_drones.TryGetValue(droneId, out Drone drone))
        {
            if (drone != null)
            {
                drone.ApplyHp(hp, maxHp);
            }
            else
            {
                _drones.Remove(droneId);
                _ownerDroneIds.Remove(ownerPlayerId);
                _droneHps.Remove(droneId);
                _droneMaxHps.Remove(droneId);
                return;
            }
        }

        bool isMyDrone =
            NetworkManager.Instance != null &&
            ownerPlayerId == NetworkManager.Instance.MyPlayerId;

        bool isSpectatingThisDrone =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating &&
            SpectatorManager.Instance.CurrentTargetPlayerId == ownerPlayerId;

        if ((isMyDrone || isSpectatingThisDrone) &&
            DroneHUDUI.Instance != null)
        {
            DroneHUDUI.Instance.SetDroneHp(hp, maxHp);
        }

        Debug.Log(
            $"[DRONE HP UPDATE APPLY] " +
            $"droneId={droneId}, owner={ownerPlayerId}, " +
            $"hp={hp}/{maxHp}, " +
            $"isMyDrone={isMyDrone}, spectator={isSpectatingThisDrone}"
        );
    }

    public bool TryGetActiveDroneByOwner(
        uint ownerPlayerId,
        out Drone drone)
    {
        drone = null;

        if (!_ownerDroneIds.TryGetValue(ownerPlayerId, out uint droneId))
            return false;

        if (!_drones.TryGetValue(droneId, out drone))
            return false;

        if (drone == null)
            return false;

        return true;
    }

    public bool TryGetDroneHpByOwner(
        uint ownerPlayerId,
        out int hp,
        out int maxHp)
    {
        hp = 100;
        maxHp = 100;

        if (!_ownerDroneIds.TryGetValue(ownerPlayerId, out uint droneId))
            return false;

        if (_droneHps.TryGetValue(droneId, out int storedHp))
            hp = storedHp;

        if (_droneMaxHps.TryGetValue(droneId, out int storedMaxHp))
            maxHp = Mathf.Max(1, storedMaxHp);

        hp = Mathf.Clamp(hp, 0, maxHp);

        return true;
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null)
                continue;

            if (identities[i].PlayerId == playerId)
                return identities[i];
        }

        return null;
    }

    public void ClearAll()
    {
        foreach (var pair in _drones)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        _drones.Clear();
        _ownerDroneIds.Clear();
        _droneHps.Clear();
        _droneMaxHps.Clear();
    }
}
