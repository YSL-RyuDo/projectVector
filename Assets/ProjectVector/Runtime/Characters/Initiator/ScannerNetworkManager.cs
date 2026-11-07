using System.Collections.Generic;
using UnityEngine;

public class ScannerNetworkManager : MonoBehaviour
{
    public static ScannerNetworkManager Instance { get; private set; }

    [SerializeField] private ScannerProjectile _scannerPrefab;

    private readonly Dictionary<uint, ScannerProjectile> _scanners = new();
    private readonly Dictionary<uint, uint> _scannerOwners = new();
    private readonly HashSet<ulong> _confirmedZaps = new();

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
        _scannerOwners[scannerId] = ownerPlayerId;
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
        FindPlayerIdentityById(ownerPlayerId)?.GetComponent<InitiatorSkill_C>()?.ConfirmSpawn();

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
        float duration, Vector3? confirmedTarget = null)
    {
        if (!_scannerOwners.TryGetValue(scannerId, out uint ownerId)) return;
        if (!_confirmedZaps.Add(((ulong)scannerId << 32) | targetPlayerId)) return;
        uint localId = NetworkManager.Instance ? NetworkManager.Instance.MyPlayerId : 0;
        // Notify the owner/victim even if a presentation object has disappeared.
        bool personalCue = ScannerFeedbackUI.Instance && localId != 0 && (localId == ownerId || localId == targetPlayerId);
        ScannerFeedbackUI.Instance?.ConfirmZap(localId, ownerId, scannerId, targetPlayerId);
        _scanners.TryGetValue(scannerId, out ScannerProjectile scanner);

        PlayerIdentity target =
            FindPlayerIdentityById(targetPlayerId);

        if (target == null && !confirmedTarget.HasValue) return;

        Vector3 targetPos =
            confirmedTarget ?? (target.transform.position + Vector3.up * 1.2f);

        // Owner and victim already hear a local cue; observers hear the spatial zap.
        if (scanner) scanner.PlayZap(targetPos, !personalCue);
        if (!target) return;

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

    public void OnScannerDestroy(uint scannerId, bool expired=false)
    {
        if (!_scanners.TryGetValue(scannerId, out ScannerProjectile scanner))
            return;

        if (scanner != null)
            scanner.End(expired);

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
        _scannerOwners.Clear();
        _confirmedZaps.Clear();
        ScannerFeedbackUI.Instance?.ResetFeedback();
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
