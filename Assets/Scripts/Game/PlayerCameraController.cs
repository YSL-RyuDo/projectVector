using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private FPSCameraLook cameraLook;

    private PlayerWeaponViewController weaponViewController;
    private PlayerDeathController deathController;

    private bool isLocalView;
    private bool isSpectatorView;

    public Camera PlayerCamera => playerCamera;
    public FPSCameraLook CameraLook => cameraLook;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (audioListener == null && playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();

        if (cameraLook == null)
            cameraLook = GetComponentInChildren<FPSCameraLook>(true);

        weaponViewController =
            GetComponent<PlayerWeaponViewController>();

        deathController =
            GetComponent<PlayerDeathController>();
    }

    public void SetLocalPlayer(bool isLocal)
    {
        isLocalView = isLocal;
        isSpectatorView = false;

        SetCameraActive(isLocal);

        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(
                isLocal,
                transform
            );
        }

        RefreshViewVisuals();
    }

    public void SetSpectatorView(bool active)
    {
        isLocalView = false;
        isSpectatorView = active;

        SetCameraActive(active);

        // 관전 중에는 상대 캐릭터의 시선 입력을 조작하지 않는다.
        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(
                false,
                transform
            );
        }

        RefreshViewVisuals();

        Debug.Log(
            $"[SPECTATOR VIEW] player={name}, active={active}"
        );
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = active;
            playerCamera.tag =
                active ? "MainCamera" : "Untagged";
        }

        if (audioListener != null)
            audioListener.enabled = active;
    }

    public void RefreshViewVisuals()
    {
        bool firstPersonView =
            isLocalView || isSpectatorView;

        if (deathController != null &&
            deathController.IsDead)
        {
            if (weaponViewController != null)
                weaponViewController.HideAll();

            deathController.SetModelVisible(false);
            return;
        }

        if (weaponViewController != null)
        {
            if (firstPersonView)
                weaponViewController.ShowFirstPerson();
            else
                weaponViewController.ShowThirdPerson();
        }

        if (deathController != null)
        {
            deathController.SetModelVisible(
                !firstPersonView
            );
        }
    }

    public void SetFOV(float fov)
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView = fov;
    }
}