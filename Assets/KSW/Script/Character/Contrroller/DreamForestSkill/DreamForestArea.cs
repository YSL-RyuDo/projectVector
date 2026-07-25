using UnityEngine;

public class DreamForestArea : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private float defaultMaxRadius = 15f;
    [SerializeField] private float expandSpeed = 5f;
    [SerializeField] private bool destroyAfterDuration = true;

    [Header("디버그")]
    [SerializeField] private bool drawDebugGizmos = true;

    private uint ultimateId;
    private uint ownerPlayerId;
    private float maxRadius;
    private float duration;
    private float currentRadius;
    private float lifeTimer;
    private SphereCollider triggerCollider;
    private bool initialized;

    private void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<SphereCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.5f;

        currentRadius = 0f;
        transform.localScale = Vector3.zero;
    }

    public void InitFromServer(
        uint ultimateId,
        uint ownerPlayerId,
        Vector3 position,
        float radius,
        float duration)
    {
        this.ultimateId = ultimateId;
        this.ownerPlayerId = ownerPlayerId;
        this.maxRadius = radius > 0f ? radius : defaultMaxRadius;
        this.duration = duration;
        this.lifeTimer = 0f;
        this.initialized = true;

        transform.position = position;
        currentRadius = 0f;
        transform.localScale = Vector3.zero;

        Debug.Log(
            $"[DREAM FOREST INIT] id={ultimateId}, owner={ownerPlayerId}, pos={position}, radius={this.maxRadius}, duration={duration}"
        );
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (currentRadius < maxRadius)
        {
            currentRadius = Mathf.MoveTowards(
                currentRadius,
                maxRadius,
                expandSpeed * Time.deltaTime
            );

            float diameter = currentRadius * 2f;
            transform.localScale = new Vector3(diameter, diameter, diameter);
        }

        lifeTimer += Time.deltaTime;

        if (destroyAfterDuration && duration > 0f && lifeTimer >= duration)
        {
            if (ControllerUltimateNetworkManager.Instance != null)
                ControllerUltimateNetworkManager.Instance.OnUltimateDestroy(ultimateId);
            else
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerIdentity identity = other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        bool isLocalPlayer =
            NetworkManager.Instance != null &&
            identity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (!isLocalPlayer)
            return;

        if (DreamForestPostProcessFeedback.Instance != null)
            DreamForestPostProcessFeedback.Instance.SetInsideDreamForest(true);

        Debug.Log($"[DREAM FOREST LOCAL ENTER] playerId={identity.PlayerId}");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerIdentity identity = other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        bool isLocalPlayer =
            NetworkManager.Instance != null &&
            identity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (!isLocalPlayer)
            return;

        if (DreamForestPostProcessFeedback.Instance != null)
            DreamForestPostProcessFeedback.Instance.SetInsideDreamForest(false);

        Debug.Log($"[DREAM FOREST LOCAL EXIT] playerId={identity.PlayerId}");
    }

    public bool IsEnemyUltimateForLocalPlayer()
    {
        if (NetworkManager.Instance == null)
            return false;

        PlayerIdentity localPlayer = NetworkManager.Instance.MyPlayer;
        if (localPlayer == null)
            return false;

        PlayerIdentity ownerIdentity = FindPlayerIdentityById(ownerPlayerId);
        if (ownerIdentity == null)
            return false;

        return ownerIdentity.Team != localPlayer.Team;
    }

    public static bool ShouldHideOutlineForLocalViewer(
        PlayerIdentity targetIdentity,
        Vector3 viewerPosition,
        Vector3 targetPosition)
    {
        if (targetIdentity == null)
            return false;

        DreamForestArea[] areas =
            FindObjectsByType<DreamForestArea>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (DreamForestArea area in areas)
        {
            if (area == null)
                continue;

            if (!area.initialized)
                continue;

            if (!area.IsEnemyUltimateForLocalPlayer())
                continue;

            if (!area.IsLineBlockedByDreamForest(viewerPosition, targetPosition))
                continue;

            return true;
        }

        return false;
    }

    private bool IsLineBlockedByDreamForest(
        Vector3 viewerPosition,
        Vector3 targetPosition)
    {
        Vector3 center = transform.position;

        Vector3 line = targetPosition - viewerPosition;
        float length = line.magnitude;

        if (length <= 0.01f)
            return false;

        Vector3 dir = line / length;
        Vector3 toCenter = center - viewerPosition;

        float projected = Vector3.Dot(toCenter, dir);

        if (projected <= 0f || projected >= length)
            return false;

        Vector3 closestPoint = viewerPosition + dir * projected;
        float distanceToCenter = Vector3.Distance(closestPoint, center);

        return distanceToCenter <= currentRadius;
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId == playerId)
                return identity;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (DreamForestPostProcessFeedback.Instance != null)
            DreamForestPostProcessFeedback.Instance.ForceHide();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        Gizmos.DrawWireSphere(transform.position, defaultMaxRadius);
    }
}