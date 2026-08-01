using System.Collections.Generic;
using UnityEngine;

public class ControllerSmokeNetworkManager : MonoBehaviour
{
    public static ControllerSmokeNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject _smokePrefab;

    private readonly Dictionary<uint, GameObject> _smokes = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void OnSmokeSpawn(uint smokeId, uint ownerPlayerId, Vector3 position, float radius, float duration)
    {
        if (_smokePrefab == null)
        {
            Debug.LogError("[CONTROLLER SMOKE NET] Smoke Prefab is null");
            return;
        }

        if (_smokes.TryGetValue(smokeId, out GameObject oldSmoke))
        {
            if (oldSmoke != null) Destroy(oldSmoke);
            _smokes.Remove(smokeId);
        }

        GameObject smoke = Instantiate(_smokePrefab, position, Quaternion.identity);
        smoke.transform.localScale = Vector3.one * radius;

        _smokes[smokeId] = smoke;

        Destroy(smoke, duration);

        Debug.Log($"[CONTROLLER SMOKE SPAWN] smokeId={smokeId}, owner={ownerPlayerId}, pos={position}, radius={radius}, duration={duration}");
    }

    public void OnSmokeDestroy(uint smokeId)
    {
        if (_smokes.TryGetValue(smokeId, out GameObject smoke))
        {
            if (smoke != null) Destroy(smoke);
            _smokes.Remove(smokeId);
        }

        Debug.Log($"[CONTROLLER SMOKE DESTROY] smokeId={smokeId}");
    }
}