using UnityEngine;

public class PlayerDeathController : MonoBehaviour
{
    private PlayerMovement movement;
    private PlayerItem playerItem;
    private PlayerSpike playerSpike;
    private CharacterController characterController;
    private PlayerIdentity identity;
    private PlayerHP _playerHp;

    [SerializeField] private GameObject modelRoot;

    private Renderer[] modelRenderers;
    private Animator animator;

    public bool IsDead { get; private set; }
    private PlayerWeaponViewController weaponViewController;
    private PlayerCameraController cameraController;
    private FPSCameraLook cameraLook;
    private readonly System.Collections.Generic.List<CharacterSkill> suspendedSkills = new();

    private void SuspendSkills()
    {
        // Input-mode singletons belong only to the local actor. Remote deaths must not clear them.
        if (identity == null || NetworkManager.Instance == null || identity.PlayerId != NetworkManager.Instance.MyPlayerId) return;
        foreach (var skill in GetComponents<CharacterSkill>())
            if (skill.enabled) { suspendedSkills.Add(skill); skill.enabled = false; }
    }

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
        movement = GetComponent<PlayerMovement>();
        playerItem = GetComponent<PlayerItem>();
        playerSpike = GetComponent<PlayerSpike>();
        characterController = GetComponent<CharacterController>();
        weaponViewController = GetComponent<PlayerWeaponViewController>();
        cameraController = GetComponent<PlayerCameraController>();
        _playerHp = GetComponent<PlayerHP>();

        // 이름이 Model인 자식 오브젝트를 직접 찾음
        if (modelRoot == null)
        {
            Transform model = transform.Find("Model");

            if (model != null)
                modelRoot = model.gameObject;
        }

        // 찾아낸 modelRoot에서 Renderer와 Animator를 각각 가져옴
        if (modelRoot != null)
        {
            modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

            // Model 오브젝트 본체 또는 그 바로 하위에 있는 Animator 가져오기
            animator = modelRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelRoot.GetComponentInChildren<Animator>(true);
            }
        }

        // 모델이 하나로 바뀌면 GetComponentInChildren으로 Animator 불러오면 됩니다.

        cameraLook = GetComponentInChildren<FPSCameraLook>(true);
    }

    public void OnDead()
    {
        GetComponent<ElectricShock>()?.ClearElectricState();
        GetComponent<EmpCastVfx>()?.ResetPresentation();
        IsDead = true;
        _playerHp.SetHp(0);
        SuspendSkills();

        if (movement != null)
        {
            movement.ResetMoveSpeedState();
            movement.CanMove = false;
            movement.SetFirstPersonArmsVisible(false);
            movement.enabled = false;
        }

        if (cameraLook != null)
        {
            cameraLook.ForceExitADS();
            cameraLook.enabled = false;
        }

        if (playerItem != null)
            playerItem.DisableOnDeath();

        if (playerSpike != null)
            playerSpike.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (weaponViewController != null)
            weaponViewController.HideAll(this);

        // 사망 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 기존 SetModelVisible(false); 를 호출하면 모델이 바로 사라져서 애니메이션이 안 보임.
        // 타인 시점(3인칭)에서도 죽는 모습을 봐야 하므로 렌더러를 끄지 않거나, 애니메이션이 끝난 후 끄도록 변경해야 함.
        // SetModelVisible(false);

        if (MatchManager.Instance != null && identity != null)
        {
            MatchManager.Instance.SetPlayerDeadVisual(
                identity.PlayerId,
                true
            );
        }

        Debug.Log($"[DEAD] disabled object={name}, playerId={identity?.PlayerId}");
    }

    public void OnRespawn()
    {
        GetComponent<ElectricShock>()?.ClearElectricState();
        GetComponent<EmpCastVfx>()?.ResetPresentation();
        // Alive survivors also need pending casts/input modes cleared at the round boundary.
        SuspendSkills();
        IsDead = false;
        foreach (var skill in suspendedSkills) if (skill) skill.enabled = true;
        suspendedSkills.Clear();

        bool isMine =
            NetworkManager.Instance != null &&
            identity != null &&
            identity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isMine) { NetworkManager.Instance.ResetLocalCombatReport();
            if (SpectatorManager.Instance && SpectatorManager.Instance.IsSpectating) SpectatorManager.Instance.StopSpectating(); }

        // 리스폰 시 애니메이션 상태 초기화
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (characterController != null)
            characterController.enabled = true;

        if (movement != null)
        {
            movement.enabled = true;
            movement.SetLocalPlayer(isMine);
            movement.ResetMoveSpeedState();
            movement.CanMove = true;

            movement.ResetAfterRespawn(
                transform.eulerAngles.y
            );
        }

        if (cameraLook != null)
        {
            cameraLook.enabled = isMine;

            if (isMine)
            {
                cameraLook.ResetLookRotation(
                    transform.eulerAngles.y
                );
            }
        }

        if (playerItem != null)
        {
            if (isMine)
                playerItem.EnableOnRespawn();
            else
                playerItem.enabled = false;
        }

        if (playerSpike != null)
            playerSpike.enabled = isMine;

        if (weaponViewController != null)
            weaponViewController.ResetVisibilityForRespawn();

        if (cameraController != null)
            cameraController.RefreshViewVisuals();
        else
            SetModelVisible(!isMine);

        if (MatchManager.Instance != null && identity != null)
        {
            MatchManager.Instance.SetPlayerDeadVisual(
                identity.PlayerId,
                false
            );
        }

        Debug.Log($"[RESPAWN] restored object={name}, playerId={identity?.PlayerId}, isMine={isMine}");
    }

    public void SetModelVisible(bool visible)
    {
        if (modelRenderers == null)
            return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            // Respawn/view changes must not activate dormant skill geometry.
            if (modelRenderers[i] is LineRenderer || modelRenderers[i] is TrailRenderer || modelRenderers[i] is ParticleSystemRenderer)
            {
                modelRenderers[i].forceRenderingOff = !visible;
                continue;
            }
            if (modelRenderers[i] != null)
                modelRenderers[i].enabled = visible;
        }
    }
}
