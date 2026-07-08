using System.Collections.Generic;
using UnityEngine;

public class DroneNetworkManager : MonoBehaviour
{
    public static DroneNetworkManager Instance { get; private set; }

    [SerializeField] private Drone _dronePrefab;

    private readonly Dictionary<uint, Drone> _drones = new();

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

        if (_drones.TryGetValue(droneId, out Drone oldDrone))
        {
            if (oldDrone != null)
                return;

            _drones.Remove(droneId);
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
            {
                ownerSkill =
                    ownerIdentity.GetComponent<InitiatorSkill_Q>();
            }
        }

        drone.Init(
            droneId,
            ownerPlayerId,
            isMine,
            ownerSkill
        );

        _drones.Add(droneId, drone);

        if (isMine && ownerSkill != null)
        {
            ownerSkill.OnOwnerDroneSpawned(drone, droneId);
        }

        Debug.Log(
            $"[DRONE SPAWN APPLY] " +
            $"droneId={droneId}, owner={ownerPlayerId}, " +
            $"isMine={isMine}, pos={pos}"
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
            return;

        bool isMine = drone != null && drone.IsMine;

        if (drone != null)
            Destroy(drone.gameObject);

        _drones.Remove(droneId);

        if (!isMine)
            return;

        PlayerIdentity ownerIdentity =
            FindPlayerIdentityById(ownerPlayerId);

        if (ownerIdentity == null)
            return;

        InitiatorSkill_Q skill =
            ownerIdentity.GetComponent<InitiatorSkill_Q>();

        if (skill != null)
            skill.OnOwnerDroneDestroyed(droneId);
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
    }
}