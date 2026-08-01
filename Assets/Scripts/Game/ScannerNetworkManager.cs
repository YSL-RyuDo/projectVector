using System.Collections.Generic;
using UnityEngine;

public class ScannerNetworkManager : MonoBehaviour
{
    public static ScannerNetworkManager Instance { get; private set; }

    [SerializeField] private ScannerProjectile _scannerPrefab;

    private readonly Dictionary<uint, ScannerProjectile> _scanners = new();

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

    public void OnScannerSpawn(
        uint scannerId,
        uint ownerPlayerId,
        float x,
        float y,
        float z,
        float vx,
        float vy,
        float vz)
    {
        if (_scannerPrefab == null)
        {
            Debug.LogError("[SCANNER SPAWN] scanner prefab is null");
            return;
        }

        if (_scanners.TryGetValue(scannerId, out ScannerProjectile oldScanner))
        {
            if (oldScanner != null)
                Destroy(oldScanner.gameObject);

            _scanners.Remove(scannerId);
        }

        Vector3 pos = new Vector3(x, y, z);
        Vector3 velocity = new Vector3(vx, vy, vz);

        ScannerProjectile scanner =
            Instantiate(
                _scannerPrefab,
                pos,
                Quaternion.identity
            );

        bool isMine =
            NetworkManager.Instance != null &&
            ownerPlayerId == NetworkManager.Instance.MyPlayerId;

        scanner.Init(
            scannerId,
            ownerPlayerId,
            isMine,
            velocity
        );

        _scanners[scannerId] = scanner;

        Debug.Log(
            $"[SCANNER SPAWN APPLY] " +
            $"scannerId={scannerId}, owner={ownerPlayerId}, " +
            $"isMine={isMine}, pos={pos}, velocity={velocity}"
        );
    }

    public void OnScannerAnchored(
        uint scannerId,
        float x,
        float y,
        float z)
    {
        if (!_scanners.TryGetValue(scannerId, out ScannerProjectile scanner))
            return;

        if (scanner == null)
        {
            _scanners.Remove(scannerId);
            return;
        }

        scanner.ApplyAnchored(
            new Vector3(x, y, z)
        );
    }

    public void OnScannerZap(
        uint scannerId,
        uint targetPlayerId,
        byte stack,
        float duration)
    {
        if (!_scanners.TryGetValue(scannerId, out ScannerProjectile scanner))
            return;

        if (scanner == null)
        {
            _scanners.Remove(scannerId);
            return;
        }

        PlayerIdentity target =
            FindPlayerIdentityById(targetPlayerId);

        if (target == null)
            return;

        Vector3 targetPos =
            target.transform.position + Vector3.up * 1.2f;

        scanner.PlayZap(targetPos);

        ElectricShock shock =
            target.GetComponent<ElectricShock>();

        if (shock == null)
            shock = target.GetComponentInChildren<ElectricShock>(true);

        if (shock != null)
            shock.Shock(duration);

        Debug.Log(
            $"[SCANNER ZAP APPLY] " +
            $"scannerId={scannerId}, target={targetPlayerId}, " +
            $"stack={stack}, duration={duration}"
        );
    }

    public void OnScannerDestroy(uint scannerId)
    {
        if (!_scanners.TryGetValue(scannerId, out ScannerProjectile scanner))
            return;

        if (scanner != null)
            Destroy(scanner.gameObject);

        _scanners.Remove(scannerId);
    }

    public void ClearAll()
    {
        foreach (var pair in _scanners)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        _scanners.Clear();
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
}