using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private FPSCameraLook cameraLook;

    private PlayerWeaponViewController weaponViewController;
    private PlayerDeathController deathController;
    private PlayerItem playerItem;

    private bool isLocalView;
    private bool isSpectatorView;
    private bool isDroneBodyView;

    public Camera PlayerCamera => playerCamera;
    public FPSCameraLook CameraLook => cameraLook;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>(true);
        }

        if (audioListener == null &&
            playerCamera != null)
        {
            audioListener =
                playerCamera.GetComponent<AudioListener>();
        }

        if (cameraLook == null)
        {
            cameraLook =
                GetComponentInChildren<FPSCameraLook>(true);
        }

        weaponViewController =
            GetComponent<PlayerWeaponViewController>();

        if (weaponViewController == null)
        {
            weaponViewController =
                GetComponentInChildren<PlayerWeaponViewController>(true);
        }

        deathController =
            GetComponent<PlayerDeathController>();

        if (deathController == null)
        {
            deathController =
                GetComponentInChildren<PlayerDeathController>(true);
        }

        playerItem =
            GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                GetComponentInChildren<PlayerItem>(true);
        }

        isLocalView = false;
        isSpectatorView = false;

        SetCameraActive(false);
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

        Debug.Log(
            $"[LOCAL VIEW] player={name}, " +
            $"isLocal={isLocal}, " +
            $"cameraEnabled={(playerCamera != null && playerCamera.enabled)}"
        );
    }

    public void SetSpectatorView(bool active)
    {
        isLocalView = false;
        isSpectatorView = active;

        // 관전 대상의 실제 카메라는 끈 상태로 유지한다.
        SetCameraActive(false);

        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(
                false,
                transform
            );
        }

        RefreshViewVisuals();

        Debug.Log(
            $"[SPECTATOR TARGET VIEW] " +
            $"player={name}, active={active}"
        );
    }

    /// <summary>
    /// 일반 원격 플레이어 상태로 강제 초기화한다.
    /// </summary>
    public void ResetToRemoteView()
    {
        isLocalView = false;
        isSpectatorView = false;

        SetCameraActive(false);

        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(
                false,
                transform
            );
        }

        RefreshViewVisuals();

        Debug.Log(
            $"[RESET REMOTE VIEW] player={name}"
        );
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
        {
            if (active &&
                !playerCamera.gameObject.activeSelf)
            {
                playerCamera.gameObject.SetActive(true);
            }

            playerCamera.enabled = active;

            playerCamera.tag =
                active
                    ? "MainCamera"
                    : "Untagged";
        }

        if (audioListener != null)
        {
            audioListener.enabled = active;
        }
    }

    public void RefreshViewVisuals()
    {
        bool firstPersonView =
            isLocalView || isSpectatorView;

        bool isDead =
            deathController != null &&
            deathController.IsDead;

        if (isDroneBodyView && !isDead)
        {
            // Show the authored world body, never a duplicate prefab or FPS arms.
            deathController?.SetModelVisible(true);
            weaponViewController?.HideAll();
            return;
        }

        // 죽은 플레이어는 모델과 양쪽 WeaponHolder 모두 숨긴다.
        if (isDead)
        {
            if (weaponViewController != null)
            {
                weaponViewController.HideAll();
            }

            if (playerItem != null)
            {
                playerItem.SetWeaponVisibility(false);
            }

            if (deathController != null)
            {
                deathController.SetModelVisible(false);
            }

            return;
        }

        // 살아 있는 플레이어의 현재 무기 프리팹을 활성화한다.
        if (playerItem != null)
        {
            playerItem.SetWeaponVisibility(true);
        }

        if (weaponViewController != null)
        {
            if (firstPersonView)
            {
                // 내 캐릭터 또는 현재 관전 대상
                weaponViewController.ShowFirstPerson();
            }
            else
            {
                // 일반 원격 플레이어
                weaponViewController.ShowThirdPerson();
            }
        }

        if (deathController != null)
        {
            // 내 캐릭터와 관전 대상은 모델을 숨긴다.
            // 일반 원격 플레이어는 모델을 표시한다.
            deathController.SetModelVisible(
                !firstPersonView
            );
        }

        Debug.Log(
            $"[VIEW VISUAL REFRESH] " +
            $"player={name}, " +
            $"local={isLocalView}, " +
            $"spectator={isSpectatorView}, " +
            $"firstPerson={firstPersonView}, " +
            $"dead={isDead}"
        );
    }

    public void SetDroneBodyView(bool active)
    {
        isDroneBodyView = active;
        RefreshViewVisuals();
        var drone = GetComponent<InitiatorSkill_Q>();
        if (!active && drone != null && drone.BlocksWeaponPresentation)
            playerItem?.SetWeaponVisibility(drone.ShowTransitionWeapon);
    }

    public void SetFOV(float fov)
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView = fov;
    }
}
