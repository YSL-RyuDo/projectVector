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

    public bool IsDead { get; private set; }
    private PlayerWeaponViewController weaponViewController;
    private PlayerCameraController cameraController;
    private FPSCameraLook cameraLook;

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

        if (modelRoot == null)
        {
            Transform model = transform.Find("Model");

            if (model != null)
                modelRoot = model.gameObject;
        }

        if (modelRoot != null)
            modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        cameraLook = GetComponentInChildren<FPSCameraLook>(true);
    }

    public void OnDead()
    {
        IsDead = true;
        _playerHp.SetHp(0);

        if (movement != null)
        {
            movement.ResetMoveSpeedState();
            movement.CanMove = false;
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
            weaponViewController.HideAll();

        SetModelVisible(false);

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
        IsDead = false;

        bool isMine =
            NetworkManager.Instance != null &&
            identity != null &&
            identity.PlayerId == NetworkManager.Instance.MyPlayerId;

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
            weaponViewController.RestoreLastView();

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
            if (modelRenderers[i] != null)
                modelRenderers[i].enabled = visible;
        }
    }
}