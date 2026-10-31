using System.Collections;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Input = SentinelCaptureInput;
#endif

public class SentinelSkill_E : CharacterSkill
{
    public static bool IsSelectingSentinelE { get; private set; }

    private static int consumedPlacementInputFrame = -1;

    public static bool ShouldBlockWeaponInput()
    {
        return
            IsSelectingSentinelE ||
            consumedPlacementInputFrame == Time.frameCount;
    }

    [Header("E Wall Trap")]
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float maxWallThickness = 2f;
    [SerializeField] private LayerMask wallLayer;

    private PlayerIdentity playerIdentity;
    private uint placedTrapId;
    private float cooldownEndTime;
    private bool isPlanning;
    private bool recallRequested;
    private Coroutine messageRoutine;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) { if(isPlanning)SetPlanning(false); return; }

        if (Input.GetKeyDown(KeyCode.E))
            UseSkill();

        if (isPlanning)
            HandlePlacement();
    }

    protected override void UseSkill()
    {
        if (RoundManager.Instance != null &&
            !RoundManager.Instance.CanAttack)
        {
            return;
        }

        if (placedTrapId != 0)
        {
            RequestRecall();
            return;
        }

        if (!CanUsePurchasedSkill(SkillSlot.E))
            return;

        float cooldownRemaining = GetCooldownRemaining();

        if (cooldownRemaining > 0f)
        {
            ShowTransientMessage(
                $"E 재사용 대기시간: {Mathf.CeilToInt(cooldownRemaining)}초"
            );
            return;
        }

        SetPlanning(!isPlanning);
    }

    private void HandlePlacement()
    {
        if(GetComponent<SentinelCastMotion>() is { } motion && !motion.ReadyToCast)return;
        if (Camera.main == null)
            return;

        bool placeFront = Input.GetMouseButtonDown(0);
        bool placeBeyond = Input.GetMouseButtonDown(1);

        if (!placeFront && !placeBeyond)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SetPlanning(false);

            return;
        }

        consumedPlacementInputFrame = Time.frameCount;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            )
        );

        if (!TryGetPlacementHit(ray, out RaycastHit wallHit))
        {
            ShowTransientMessage("E 설치 불가: 벽을 조준하세요.");
            return;
        }

        if (!IsWallSurface(wallHit.normal))
        {
            Debug.Log($"[SENTINEL E PLACE BLOCK] hit={wallHit.collider.name} normal={wallHit.normal}");
            ShowTransientMessage("E 설치 불가: 수직 벽에만 설치할 수 있습니다.");
            return;
        }

        if (wallHit.collider.GetComponentInParent<PlayerIdentity>() != null ||
            wallHit.collider.GetComponentInParent<Sentinel_Trap>() != null ||
            wallHit.collider.GetComponentInParent<Sentinel_SlowTrap>() != null ||
            wallHit.collider.GetComponentInParent<TrapDevice>() != null)
        {
            ShowTransientMessage("E 설치 불가: 지형의 벽을 조준하세요.");
            return;
        }

        RaycastHit placementHit = wallHit;

        if (placeBeyond &&
            !TryFindFarWallSurface(ray, wallHit, out placementHit))
        {
            ShowTransientMessage(
                $"E 설치 불가: 벽 두께가 {maxWallThickness:0.#}m를 넘습니다."
            );
            return;
        }

        Vector3 position =
            placementHit.point +
            placementHit.normal * 0.03f;

        Vector3 normal = placementHit.normal.normalized;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSentinelEPlace(
                position,
                normal
            );
            GetComponent<SentinelCastMotion>()?.Release();
        }

        SetPlanning(false);

        Debug.Log(
            $"[SENTINEL E PLACE REQUEST] " +
            $"position={position}, normal={normal}, beyond={placeBeyond}"
        );
    }

    private bool TryGetPlacementHit(Ray ray, out RaycastHit hit)
    {
        hit = default;
        float nearest = float.PositiveInfinity;
        foreach (var candidate in Physics.RaycastAll(ray, maxDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            if (candidate.collider.transform.IsChildOf(transform) || candidate.distance >= nearest) continue;
            nearest = candidate.distance;
            hit = candidate;
        }
        return hit.collider != null;
    }

    private bool TryFindFarWallSurface(
        Ray cameraRay,
        RaycastHit nearHit,
        out RaycastHit farHit)
    {
        Vector3 reverseOrigin =
            nearHit.point +
            cameraRay.direction *
            (maxWallThickness + 0.1f);

        Ray reverseRay = new Ray(
            reverseOrigin,
            -cameraRay.direction
        );

        if (!nearHit.collider.Raycast(
                reverseRay,
                out farHit,
                maxWallThickness + 0.2f))
        {
            return false;
        }

        if (!IsWallSurface(farHit.normal))
            return false;

        float wallThickness =
            Vector3.Distance(
                nearHit.point,
                farHit.point
            );

        return
            wallThickness > 0.02f &&
            wallThickness <= maxWallThickness + 0.05f;
    }

    private static bool IsWallSurface(Vector3 normal)
    {
        return Mathf.Abs(
            Vector3.Dot(normal.normalized, Vector3.up)
        ) <= 0.5f;
    }

    private void RequestRecall()
    {
        if (recallRequested ||
            placedTrapId == 0 ||
            NetworkManager.Instance == null)
        {
            return;
        }

        recallRequested = true;
        NetworkManager.Instance.SendSentinelERecall(placedTrapId);
        ShowTransientMessage("E 함정 회수 요청");

        Debug.Log(
            $"[SENTINEL E RECALL REQUEST] trapId={placedTrapId}"
        );
    }

    public void OnNetworkTrapSpawned(uint trapId)
    {
        placedTrapId = trapId;
        recallRequested = false;
        cooldownEndTime = 0f;
        SetPlanning(false);
        ShowTransientMessage("E 함정 설치 완료 · E를 눌러 회수");
    }

    public void OnNetworkTrapRemoved(
        uint trapId,
        float cooldownSeconds)
    {
        if (placedTrapId != trapId)
            return;

        placedTrapId = 0;
        recallRequested = false;
        cooldownEndTime =
            cooldownSeconds > 0f
                ? Time.time + cooldownSeconds
                : 0f;

        if (cooldownSeconds > 0f)
        {
            ShowTransientMessage(
                $"E 재사용 대기시간: {Mathf.CeilToInt(cooldownSeconds)}초"
            );
        }
    }

    public void ResetRoundState()
    {
        placedTrapId = 0;
        recallRequested = false;
        cooldownEndTime = 0f;
        SetPlanning(false);
    }

    private float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownEndTime - Time.time);
    }

    private void SetPlanning(bool value)
    {
        isPlanning = value;
        IsSelectingSentinelE = value;

        if (CharacterSkillUI.Instance != null)
        {
            CharacterSkillUI.Instance.SentinelSkillText(
                value,
                value
                    ? "E 배치 모드: 좌클릭(앞면), 우클릭(벽 너머)"
                    : ""
            );
        }
    }

    private void ShowTransientMessage(string message)
    {
        // Keep placement instructions intact while confirmations use the saved side notice.
        if(SentinelFeedbackUI.Instance){SentinelFeedbackUI.Instance.Notice(message);return;}
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine =
            StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        if (CharacterSkillUI.Instance != null)
        {
            CharacterSkillUI.Instance.SentinelSkillText(
                true,
                message
            );
        }

        yield return new WaitForSeconds(1.5f);

        if (!isPlanning &&
            CharacterSkillUI.Instance != null)
        {
            CharacterSkillUI.Instance.SentinelSkillText(false, "");
        }

        messageRoutine = null;
    }

    private bool IsMine()
    {
        if (playerIdentity == null)
            playerIdentity = GetComponent<PlayerIdentity>();

        return
            playerIdentity != null &&
            NetworkManager.Instance != null &&
            playerIdentity.PlayerId ==
            NetworkManager.Instance.MyPlayerId;
    }

    private void OnDisable()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (isPlanning)
            SetPlanning(false);
    }
}
