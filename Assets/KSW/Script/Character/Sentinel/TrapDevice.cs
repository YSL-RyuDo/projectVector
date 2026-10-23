using System.Collections;
using UnityEngine;

public class TrapDevice : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private float effectRadius = 8f;
    [SerializeField] private float expandSpeed = 20f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject energyEffectPrefab;

    private uint trapId;
    private uint ownerPlayerId;
    private int ownerTeam = -1;

    private bool isOwner;
    private bool isDestroyed;
    private bool enemyWasInside;
    private bool activationRequested;
    private int detectionMask;
    private float nextActivationRetryTime;

    private const float ActivationRetryInterval = 0.25f;

    public uint TrapId => trapId;
    public uint OwnerPlayerId => ownerPlayerId;
    public int OwnerTeam => ownerTeam;

    private void Awake()
    {
        detectionMask = BuildDetectionMask();

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        UnityEngine.UI.Slider slider =
            GetComponentInChildren<UnityEngine.UI.Slider>(true);

        if (slider != null)
            slider.gameObject.SetActive(false);

        if (energyEffectPrefab != null &&
            energyEffectPrefab != gameObject &&
            energyEffectPrefab.transform.IsChildOf(transform))
        {
            energyEffectPrefab.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDestroyed ||
            !isOwner || trapId != 0 ||
            trapId == 0 ||
            NetworkManager.Instance == null)
        {
            return;
        }

        bool enemyInside = HasEnemyInRange();

        bool shouldRequest =
            enemyInside &&
            (
                (!enemyWasInside && !activationRequested) ||
                (activationRequested &&
                 Time.time >= nextActivationRetryTime)
            );

        if (shouldRequest)
        {
            activationRequested = true;
            nextActivationRetryTime =
                Time.time + ActivationRetryInterval;
            NetworkManager.Instance.SendSentinelEActivate(trapId);

            Debug.Log(
                $"[SENTINEL E DETECT] trapId={trapId}"
            );
        }

        if (!enemyInside)
            activationRequested = false;

        enemyWasInside = enemyInside;
    }

    public void InitNetwork(
        uint networkTrapId,
        uint networkOwnerPlayerId,
        int networkOwnerTeam,
        bool owner,
        Vector3 position,
        Vector3 normal,
        float networkHealth,
        float networkEffectRadius)
    {
        trapId = networkTrapId;
        ownerPlayerId = networkOwnerPlayerId;
        GetComponent<SentinelDeviceVfx>()?.Initialize(ownerPlayerId);
        ownerTeam = networkOwnerTeam;
        isOwner = owner;
        health = networkHealth;
        effectRadius = networkEffectRadius;

        isDestroyed = false;
        enemyWasInside = false;
        activationRequested = false;
        nextActivationRetryTime = 0f;

        transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation(normal.normalized)
        );

        Debug.Log(
            $"[SENTINEL E INIT] trapId={trapId}, owner={ownerPlayerId}, " +
            $"team={ownerTeam}, isOwner={isOwner}, radius={effectRadius}"
        );
    }

    public void ActivateFromServer(
        float radius,
        float slowDuration,
        float slowMultiplier,
        float stunDuration)
    {
        if (isDestroyed)
            return;

        activationRequested = false;
        effectRadius = radius;

        StartCoroutine(PlayEnergyPulse());
        GetComponent<SentinelDeviceVfx>()?.Pulse();

        Debug.Log(
            $"[SENTINEL E ACTIVATE] trapId={trapId}, " +
            $"slow={slowDuration}s/{slowMultiplier}, stun={stunDuration}s"
        );
    }

    public void DestroyFromServer()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        Destroy(gameObject);
    }

    private bool HasEnemyInRange()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            effectRadius,
            detectionMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            PlayerIdentity identity =
                hit.GetComponentInParent<PlayerIdentity>();

            if (identity == null ||
                identity.PlayerId == ownerPlayerId ||
                identity.Team == ownerTeam)
            {
                continue;
            }

            PlayerHP hp = identity.GetComponent<PlayerHP>();

            if (hp != null && hp.IsDead)
                continue;

            return true;
        }

        return false;
    }


    private IEnumerator PlayEnergyPulse()
    {
        GameObject pulse;

        if (energyEffectPrefab != null)
        {
            pulse = Instantiate(
                energyEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            pulse.SetActive(true);
        }
        else
        {
            pulse = GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );
            pulse.name = "SentinelEEnergyPulse";
            pulse.transform.position = transform.position;

            Collider pulseCollider = pulse.GetComponent<Collider>();

            if (pulseCollider != null)
                Destroy(pulseCollider);
        }

        pulse.transform.localScale = Vector3.zero;
        Vector3 targetScale =
            Vector3.one * effectRadius * 2f;

        while (pulse != null &&
            pulse.transform.localScale.x <
            targetScale.x * 0.98f)
        {
            pulse.transform.localScale =
                Vector3.MoveTowards(
                    pulse.transform.localScale,
                    targetScale,
                    expandSpeed * Time.deltaTime
                );

            yield return null;
        }

        if (pulse != null)
            Destroy(pulse, 0.1f);
    }

    private int BuildDetectionMask()
    {
        int mask = enemyLayer.value;

        IncludeLayer(ref mask, "Default");
        IncludeLayer(ref mask, "LocalPlayer");
        IncludeLayer(ref mask, "Enemy");
        IncludeLayer(ref mask, "Player");

        return mask;
    }

    private static void IncludeLayer(
        ref int mask,
        string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
            mask |= 1 << layer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            effectRadius
        );
    }
}
