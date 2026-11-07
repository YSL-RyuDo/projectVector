using System.Collections.Generic;
using UnityEngine;

public class EntrySmokeNetworkManager : MonoBehaviour
{
    public static EntrySmokeNetworkManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject _smokeGrenadePrefab;
    [SerializeField] private GameObject _smokeEffectPrefab;

    private readonly Dictionary<uint, SmokeGrenade> _grenades =
        new Dictionary<uint, SmokeGrenade>();

    private readonly Dictionary<uint, GameObject> _smokes =
        new Dictionary<uint, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void OnSmokeSpawn(
        uint smokeId,
        uint ownerPlayerId,
        Vector3 position,
        Vector3 velocity)
    {
        if (_smokeGrenadePrefab == null)
        {
            Debug.LogError("[ENTRY SMOKE] smoke grenade prefab is null");
            return;
        }

        if (_grenades.ContainsKey(smokeId))
            return;

        GameObject obj =
            Instantiate(
                _smokeGrenadePrefab,
                position,
                Quaternion.identity
            );

        SmokeGrenade grenade =
            obj.GetComponent<SmokeGrenade>();

        if (grenade == null)
            grenade = obj.GetComponentInChildren<SmokeGrenade>(true);

        if (grenade == null)
        {
            Debug.LogError("[ENTRY SMOKE] SmokeGrenade component missing");
            Destroy(obj);
            return;
        }

        bool isMine =
            NetworkManager.Instance != null &&
            ownerPlayerId == NetworkManager.Instance.MyPlayerId;

        grenade.InitFromServer(
            smokeId,
            ownerPlayerId,
            isMine,
            velocity
        );

        _grenades[smokeId] = grenade;

        Debug.Log(
            $"[ENTRY SMOKE SPAWN APPLY] smokeId={smokeId}, " +
            $"owner={ownerPlayerId}, isMine={isMine}"
        );
    }

    public void OnSmokeState(
        uint smokeId,
        uint ownerPlayerId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!_grenades.TryGetValue(smokeId, out SmokeGrenade grenade))
            return;

        if (grenade == null)
            return;

        grenade.ApplyRemoteState(
            position,
            rotation
        );
    }

    public void OnSmokeActivate(
        uint smokeId,
        uint ownerPlayerId,
        Vector3 position,
        Vector3 normal,
        float radius,
        float duration)
    {
        if (_grenades.TryGetValue(smokeId, out SmokeGrenade grenade))
        {
            if (grenade != null)
                Destroy(grenade.gameObject);

            _grenades.Remove(smokeId);
        }

        if (_smokeEffectPrefab == null)
        {
            Debug.LogError("[ENTRY SMOKE] smoke effect prefab is null");
            return;
        }

        if (_smokes.TryGetValue(smokeId, out GameObject oldSmoke))
        {
            if (oldSmoke != null)
                Destroy(oldSmoke);

            _smokes.Remove(smokeId);
        }

        GameObject smoke =
            Instantiate(
                _smokeEffectPrefab,
                position,
                Quaternion.identity
            );

        smoke.transform.localScale =
            Vector3.one * radius;

        smoke.GetComponent<ControllerSmokeVfx>()?.Begin(duration);

        ParticleSystem[] particles =
            smoke.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play();
        }

        EntrySmokeArea smokeArea =
            smoke.GetComponent<EntrySmokeArea>();

        if (smokeArea == null)
            smokeArea = smoke.GetComponentInChildren<EntrySmokeArea>(true);

        if (smokeArea != null)
        {
            smokeArea.Init(
                smokeId,
                ownerPlayerId,
                radius,
                duration
            );
        }
        else
        {
            Destroy(smoke, duration);
        }

        _smokes[smokeId] = smoke;

        Debug.Log(
            $"[ENTRY SMOKE ACTIVATE APPLY] smokeId={smokeId}, " +
            $"owner={ownerPlayerId}, pos={position}, radius={radius}, duration={duration}"
        );
    }

    public void OnSmokeDestroy(uint smokeId)
    {
        if (_grenades.TryGetValue(smokeId, out SmokeGrenade grenade))
        {
            if (grenade != null)
                Destroy(grenade.gameObject);

            _grenades.Remove(smokeId);
        }

        if (_smokes.TryGetValue(smokeId, out GameObject smoke))
        {
            if (smoke != null)
                Destroy(smoke);

            _smokes.Remove(smokeId);
        }

        Debug.Log($"[ENTRY SMOKE DESTROY APPLY] smokeId={smokeId}");
    }
}
