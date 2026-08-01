using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Sentinel_Trap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject trapModel;
    [SerializeField] private GameObject scanSphereVisual;
    [SerializeField] private Slider hpUI;

    [Header("HP")]
    [SerializeField] private float maxHp = 40f;
    [SerializeField] private float currentHp = 40f;

    [Header("Detect")]
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float scanDelay = 1.0f;
    [SerializeField] private float scanCooldown = 1.5f;
    [SerializeField] private float scanSpeed = 35f;
    [SerializeField] private float revealDuration = 2.5f;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private LayerMask visionBlockLayer = ~0;

    [Header("Placement")]
    [SerializeField] private float placeOffset = 0.03f;
    [SerializeField] private float ignoreCollisionTime = 0.2f;

    private uint trapId;
    private uint ownerPlayerId;
    private int ownerTeam = -1;

    private bool isOwnerTrap;
    private bool isPlaced;
    private bool isScanning;
    private bool isDestroyed;
    private bool suppressAnchorSend;

    private Rigidbody rb;
    private Collider myCollider;
    private int detectionMask;
    private int visibilityMask;

    private Coroutine scanCoroutine;
    private Coroutine scanVisualCoroutine;

    private readonly Dictionary<Outline, OutlineBackup> outlineBackups = new();

    public uint TrapId => trapId;
    public uint OwnerPlayerId => ownerPlayerId;
    public int OwnerTeam => ownerTeam;

    private struct OutlineBackup
    {
        public bool enabled;
        public Color color;
        public float width;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        detectionMask = BuildDetectionMask();
        visibilityMask = visionBlockLayer.value | detectionMask;

        currentHp = maxHp;

        if (scanSphereVisual != null)
        {
            scanSphereVisual.SetActive(false);
            scanSphereVisual.transform.localScale = Vector3.zero;
        }

        if (hpUI != null)
            hpUI.gameObject.SetActive(false);

        StartCoroutine(IgnoreCollisionRoutine());
    }

    private void Start()
    {
        if (hpUI != null)
        {
            hpUI.maxValue = maxHp;
            hpUI.value = currentHp;
            hpUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDestroyed)
            return;

        if (!isPlaced)
            return;

        UpdateHpUI();

        if (currentHp <= 0f)
        {
            DestroyTrapLocal();
            return;
        }

        // 서버 연결 구조에서는 함정 주인 클라만 감지 판정을 한다.
        if (!isOwnerTrap)
            return;

        PlayerIdentity target = FindVisibleEnemy();

        if (target == null)
            return;

        RotateTowardsTarget(target.transform);

        if (!isScanning)
            scanCoroutine = StartCoroutine(ScanSequence());
    }

    public void InitNetwork(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        bool isOwnerTrap,
        float hp)
    {
        this.trapId = trapId;
        this.ownerPlayerId = ownerPlayerId;
        this.ownerTeam = ownerTeam;
        this.isOwnerTrap = isOwnerTrap;

        currentHp = hp;
        isPlaced = false;
        isScanning = false;
        isDestroyed = false;
        suppressAnchorSend = false;

        UpdateHpUI();

        Debug.Log(
            $"[SENTINEL C INIT NET] trapId={trapId}, owner={ownerPlayerId}, team={ownerTeam}, isOwner={isOwnerTrap}"
        );
    }

    // 로컬 테스트용 Init이 남아있어도 컴파일 깨지지 않게 유지.
    public void Init(uint ownerPlayerId, int ownerTeam)
    {
        this.trapId = 0;
        this.ownerPlayerId = ownerPlayerId;
        this.ownerTeam = ownerTeam;
        this.isOwnerTrap = true;

        currentHp = maxHp;
        isPlaced = false;
        isScanning = false;
        isDestroyed = false;

        Debug.Log($"[SENTINEL C INIT LOCAL] owner={ownerPlayerId}, team={ownerTeam}");
    }

    // 로컬 테스트용. 서버 연결 후에는 되도록 LaunchVelocity를 사용.
    public void Launch(Vector3 direction, float force)
    {
        LaunchVelocity(direction.normalized * force);
    }

    public void LaunchVelocity(Vector3 velocity)
    {
        if (rb == null)
        {
            Debug.LogError("[SENTINEL C] Rigidbody 없음");
            return;
        }

        isPlaced = false;
        isScanning = false;
        isDestroyed = false;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[SENTINEL C LAUNCH VELOCITY] trapId={trapId}, velocity={velocity}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isPlaced)
            return;

        if (collision.collider.CompareTag("Player"))
            return;

        if (collision.contactCount <= 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        PlaceTrap(contact.point, contact.normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlaced)
            return;

        if (other.CompareTag("Player"))
            return;

        if (Physics.Raycast(
                transform.position + Vector3.up * 0.2f,
                Vector3.down,
                out RaycastHit hit,
                1.5f,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            PlaceTrap(hit.point, hit.normal);
        }
        else
        {
            PlaceTrap(transform.position, Vector3.up);
        }
    }

    private void PlaceTrap(Vector3 point, Vector3 normal)
    {
        if (isPlaced)
            return;

        isPlaced = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = point + normal * placeOffset;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal);

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        transform.rotation = Quaternion.LookRotation(forward.normalized, normal);

        Debug.Log($"[SENTINEL C PLACED] trapId={trapId}, pos={transform.position}, normal={normal}");

        // 함정 주인 클라만 서버에 안착 위치를 보낸다.
        if (isOwnerTrap &&
            !suppressAnchorSend &&
            trapId != 0 &&
            NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSentinelCAnchor(
                trapId,
                transform.position,
                normal
            );
        }
    }

    public void ApplyAnchorFromServer(Vector3 position, Vector3 normal)
    {
        suppressAnchorSend = true;

        isPlaced = true;
        isScanning = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = position;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal);

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        transform.rotation = Quaternion.LookRotation(forward.normalized, normal);

        suppressAnchorSend = false;

        Debug.Log($"[SENTINEL C APPLY ANCHOR SERVER] trapId={trapId}, pos={position}, normal={normal}");
    }

    private IEnumerator IgnoreCollisionRoutine()
    {
        if (myCollider == null)
            yield break;

        myCollider.enabled = false;

        yield return new WaitForSeconds(ignoreCollisionTime);

        if (myCollider != null)
            myCollider.enabled = true;
    }

    private IEnumerator ScanSequence()
    {
        isScanning = true;

        if (CharacterSkillUI.Instance != null && isOwnerTrap)
            CharacterSkillUI.Instance.SentinelSkillText(true, $"적 감지: {scanDelay:0.0}초");

        Debug.Log($"[SENTINEL C DETECT] trapId={trapId}, countdown={scanDelay}");

        float timer = 0f;

        while (timer < scanDelay)
        {
            if (!HasVisibleEnemy())
            {
                if (CharacterSkillUI.Instance != null && isOwnerTrap)
                    CharacterSkillUI.Instance.SentinelSkillText(false, "");

                isScanning = false;
                yield break;
            }

            timer += Time.deltaTime;

            if (CharacterSkillUI.Instance != null && isOwnerTrap)
            {
                float remaining = Mathf.Max(0f, scanDelay - timer);
                CharacterSkillUI.Instance.SentinelSkillText(
                    true,
                    $"적 감지: {remaining:0.0}초"
                );
            }

            yield return null;
        }

        if (HasVisibleEnemy())
        {
            if (trapId != 0 && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendSentinelCTrigger(
                    trapId,
                    transform.position,
                    detectRadius,
                    revealDuration
                );
            }
            else
            {
                // 서버 없이 로컬 테스트할 때만 직접 재생.
                PlayScanFromServer(
                    ownerPlayerId,
                    ownerTeam,
                    transform.position,
                    detectRadius,
                    revealDuration
                );
            }
        }

        if (CharacterSkillUI.Instance != null && isOwnerTrap)
            CharacterSkillUI.Instance.SentinelSkillText(false, "");

        yield return new WaitForSeconds(scanCooldown);

        isScanning = false;
    }

    public void PlayScanFromServer(
        uint ownerPlayerId,
        int ownerTeam,
        Vector3 position,
        float radius,
        float revealDuration)
    {
        this.ownerPlayerId = ownerPlayerId;
        this.ownerTeam = ownerTeam;

        transform.position = position;

        if (scanVisualCoroutine != null)
            StopCoroutine(scanVisualCoroutine);

        scanVisualCoroutine = StartCoroutine(
            ExpandWaveFromServer(radius, revealDuration)
        );

        Debug.Log($"[SENTINEL C PLAY SCAN SERVER] trapId={trapId}, owner={ownerPlayerId}, team={ownerTeam}");
    }

    private IEnumerator ExpandWaveFromServer(float radius, float revealDuration)
    {
        if (scanSphereVisual == null)
            yield break;

        RestoreAllOutlines();

        scanSphereVisual.SetActive(true);
        scanSphereVisual.transform.localScale = Vector3.zero;

        float currentRadius = 0f;

        while (currentRadius < radius)
        {
            currentRadius += Time.deltaTime * scanSpeed;

            scanSphereVisual.transform.localScale =
                Vector3.one * currentRadius * 2f;

            RevealEnemiesForOwnerTeam(currentRadius, radius);

            yield return null;
        }

        yield return new WaitForSeconds(revealDuration);

        RestoreAllOutlines();

        scanSphereVisual.transform.localScale = Vector3.zero;
        scanSphereVisual.SetActive(false);

        scanVisualCoroutine = null;
    }

    private void RevealEnemiesForOwnerTeam(float currentRadius, float maxRadius)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.MyPlayer == null)
            return;

        PlayerIdentity localPlayer = NetworkManager.Instance.MyPlayer;

        // 스캔을 볼 수 있는 건 함정 설치자와 같은 팀뿐.
        if (localPlayer.Team != ownerTeam)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            maxRadius,
            detectionMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            PlayerIdentity identity = hit.GetComponentInParent<PlayerIdentity>();

            if (identity == null)
                identity = hit.GetComponentInChildren<PlayerIdentity>(true);

            if (identity == null)
                continue;

            if (identity.Team == ownerTeam)
                continue;

            Vector3 targetCenter = GetTargetCenter(identity);
            float distance = Vector3.Distance(transform.position, targetCenter);

            if (distance > currentRadius)
                continue;

            Outline outline = identity.GetComponent<Outline>();

            if (outline == null)
                outline = identity.GetComponentInChildren<Outline>(true);

            if (outline == null)
                continue;

            BackupOutline(outline);

            outline.enabled = true;
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 5f;
        }
    }

    private PlayerIdentity FindVisibleEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            detectionMask,
            QueryTriggerInteraction.Ignore
        );

        PlayerIdentity closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            PlayerIdentity identity = hit.GetComponentInParent<PlayerIdentity>();

            if (identity == null)
                identity = hit.GetComponentInChildren<PlayerIdentity>(true);

            if (identity == null)
                continue;

            if (!IsEnemy(identity))
                continue;

            Vector3 targetCenter = GetTargetCenter(identity);
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            Vector3 dir = targetCenter - origin;
            float distance = dir.magnitude;

            if (distance <= 0.01f)
                continue;

            if (!HasLineOfSight(identity, origin, dir.normalized, distance))
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = identity;
            }
        }

        return closest;
    }

    private bool HasLineOfSight(
        PlayerIdentity target,
        Vector3 origin,
        Vector3 direction,
        float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            visibilityMask,
            QueryTriggerInteraction.Ignore
        );

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // 트랩 본체와 자식 모델 콜라이더는 시야를 가리지 않는다.
            if (hit.collider.transform.root == transform.root)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            closestCollider = hit.collider;
        }

        if (closestCollider == null)
            return true;

        PlayerIdentity hitIdentity =
            closestCollider.GetComponentInParent<PlayerIdentity>();

        if (hitIdentity == null)
        {
            hitIdentity =
                closestCollider.GetComponentInChildren<PlayerIdentity>(true);
        }

        return hitIdentity == target;
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

    private static void IncludeLayer(ref int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
            mask |= 1 << layer;
    }

    private bool HasVisibleEnemy()
    {
        return FindVisibleEnemy() != null;
    }

    private bool IsEnemy(PlayerIdentity identity)
    {
        if (identity == null)
            return false;

        if (identity.PlayerId == ownerPlayerId)
            return false;

        if (ownerTeam < 0)
            return true;

        return identity.Team != ownerTeam;
    }

    private void RotateTowardsTarget(Transform target)
    {
        if (target == null || trapModel == null)
            return;

        Vector3 targetDir = target.position - trapModel.transform.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDir.normalized);

        trapModel.transform.rotation = Quaternion.Slerp(
            trapModel.transform.rotation,
            targetRotation,
            Time.deltaTime * 15f
        );
    }

    private void BackupOutline(Outline outline)
    {
        if (outline == null)
            return;

        if (outlineBackups.ContainsKey(outline))
            return;

        outlineBackups.Add(
            outline,
            new OutlineBackup
            {
                enabled = outline.enabled,
                color = outline.OutlineColor,
                width = outline.OutlineWidth
            }
        );
    }

    private void RestoreOutline(Outline outline)
    {
        if (outline == null)
            return;

        if (!outlineBackups.TryGetValue(outline, out OutlineBackup backup))
            return;

        outline.enabled = backup.enabled;
        outline.OutlineColor = backup.color;
        outline.OutlineWidth = backup.width;

        PlayerOutline playerOutline = outline.GetComponent<PlayerOutline>();

        if (playerOutline == null)
            playerOutline = outline.GetComponentInParent<PlayerOutline>();

        if (playerOutline == null)
            playerOutline = outline.GetComponentInChildren<PlayerOutline>(true);

        if (playerOutline != null)
            playerOutline.RefreshOutline();

        outlineBackups.Remove(outline);
    }

    private void RestoreAllOutlines()
    {
        List<Outline> targets = new(outlineBackups.Keys);

        foreach (Outline outline in targets)
            RestoreOutline(outline);

        outlineBackups.Clear();
    }

    private Vector3 GetTargetCenter(PlayerIdentity identity)
    {
        CharacterController cc = identity.GetComponent<CharacterController>();

        if (cc == null)
            cc = identity.GetComponentInChildren<CharacterController>(true);

        if (cc != null)
            return cc.bounds.center;

        Collider col = identity.GetComponent<Collider>();

        if (col == null)
            col = identity.GetComponentInChildren<Collider>(true);

        if (col != null)
            return col.bounds.center;

        return identity.transform.position + Vector3.up * 1.2f;
    }

    private void UpdateHpUI()
    {
        if (hpUI == null)
            return;

        hpUI.value = currentHp;
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed)
            return;

        // 서버 연결 상태면 HP는 서버가 깎고 Destroy 패킷으로 제거한다.
        if (trapId != 0 && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSentinelCHit(trapId, amount);
            return;
        }

        // 로컬 테스트용.
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        UpdateHpUI();

        if (currentHp <= 0f)
            DestroyTrapLocal();
    }

    public void ApplyDestroyFromServer()
    {
        DestroyTrapLocal();
    }

    private void DestroyTrapLocal()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
        }

        if (scanVisualCoroutine != null)
        {
            StopCoroutine(scanVisualCoroutine);
            scanVisualCoroutine = null;
        }

        RestoreAllOutlines();

        if (CharacterSkillUI.Instance != null && isOwnerTrap)
            CharacterSkillUI.Instance.SentinelSkillText(false, "");

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        RestoreAllOutlines();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
