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
    }

    public void OnDead()
    {
        IsDead = true;
        _playerHp.SetHp(0);

        if (movement != null)
        {
            movement.CanMove = false;
            movement.SetLocalPlayer(false);
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
            movement.CanMove = true;
            movement.SetLocalPlayer(isMine);
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