using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    [SerializeField] private float _baseSensitivity = 0.2f;
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Test Settings")]
    [SerializeField] private bool _isTestMode = false; //Æ©Åä¸®¾ó

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

    private PlayerIdentity _playerIdentity;

    private float xRotation;
    private float yaw;
    private float recoilOffset;
    private bool _isADS;

    private Transform playerRoot;
    private bool isLocalPlayer;

    public bool CanMove = true;
    private bool lastSentADS;

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    [SerializeField] private float downDistance = 0.5f;
    [SerializeField] private float moveSpeed = 5f;

    private void Awake()
    {
        _playerIdentity =
            GetComponentInParent<PlayerIdentity>();

        if (_isTestMode)//Æ©Åä¸®¾ó
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            SetLocalPlayer(true, root);
            return;
        }

        enabled = false;
    }

    public void SetLocalPlayer(
        bool isLocal,
        Transform root)
    {
        isLocalPlayer = isLocal;
        playerRoot = root;

        CanMove = isLocal;
        enabled = isLocal;

        if (!isLocal)
            return;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        if (_playerIdentity == null)
        {
            _playerIdentity =
                GetComponentInParent<PlayerIdentity>();
        }

        Debug.Log(
            $"[FPSCameraLook LOCAL INIT] " +
            $"cameraRoot={gameObject.name}, " +
            $"player={playerRoot.name}, " +
            $"playerId={_playerIdentity?.PlayerId}, " +
            $"userId={_playerIdentity?.UserId}"
        );
    }



    private void Update()
    {

        if (_isTestMode && !TESTInitiatorSkill_Q.IsLocalDroneMode)//Æ©Åä¸®¾ó
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null && !cam.enabled)
                cam.enabled = true;

            var listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null && !listener.enabled)
                listener.enabled = true;
        }

        if (!isLocalPlayer)
            return;


        if (!isLocalPlayer)
            return;

        if (!CanMove)
            return;

        if (playerRoot == null)
            return;

        if (InGameChatUI.IsChatOpen)
            return;

        if (Cursor.lockState ==
            CursorLockMode.None)
        {
            return;
        }

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
        {
            return;
        }

        if (Mouse.current == null)
            return;

        // Ping wheel consumes look input; keep recoil, movement and networking updates.
        Vector2 mouseDelta = Game.Pings.PingSystem.WheelOpen
            ? Vector2.zero
            : Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        float generalSens =
            MouseSettings.GetSensitivity(
                SensitivityType.General
            );

        float horizontalSens =
            MouseSettings.GetSensitivity(
                SensitivityType.Horizontal
            );

        float verticalSens =
            MouseSettings.GetSensitivity(
                SensitivityType.Vertical
            );

        float adsMultiplier =
            _isADS
                ? _adsSensitivityMultiplier
                : 1f;

        float finalHorizontalDelta =
            mouseX *
            _baseSensitivity *
            generalSens *
            horizontalSens *
            adsMultiplier;

        float finalVerticalDelta =
            mouseY *
            _baseSensitivity *
            generalSens *
            verticalSens *
            adsMultiplier;

        yaw += finalHorizontalDelta;

        playerRoot.rotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );

        xRotation -=
            finalVerticalDelta;

        xRotation =
            Mathf.Clamp(
                xRotation,
                -80f,
                80f
            );

        recoilOffset =
            Mathf.Lerp(
                recoilOffset,
                0f,
                recoilReturnSpeed *
                Time.deltaTime
            );

        transform.localRotation =
            Quaternion.Euler(
                xRotation + recoilOffset,
                0f,
                0f
            );
    }

    private void LateUpdate() //Æ©Åä¸®¾ó
    {
        if (_isTestMode)
        {
            var camTransform = GetComponentInChildren<Camera>()?.transform;
            if (camTransform != null)
            {
                camTransform.localPosition = new Vector3(0f, 0f, 0.25f);
            }
        }
    }


    public float GetYaw()
    {
        return yaw;
    }

    public void SetYaw(float value)
    {
        yaw = value;
    }

    public void AddRecoil(float amount)
    {
        recoilOffset -= amount;
    }

    public void SetADS(bool ads)
    {
        SetADSLocal(ads);
    }

    public void AddPitch(float amount)
    {
        xRotation += amount;

        xRotation =
            Mathf.Clamp(
                xRotation,
                -80f,
                80f
            );
    }

    public void AddYaw(float amount)
    {
        yaw += amount;
    }

    public void ResetLookRotation(
        float yawValue)
    {
        xRotation = 0f;
        yaw = yawValue;
        recoilOffset = 0f;

        transform.localRotation =
            Quaternion.identity;

        if (playerRoot == null)
        {
            playerRoot =
                GetComponentInParent<PlayerIdentity>()
                    ?.transform;
        }

        if (playerRoot != null)
        {
            playerRoot.rotation =
                Quaternion.Euler(
                    0f,
                    yaw,
                    0f
                );
        }
    }

    public void ForceExitADS()
    {
        SetADSLocal(false);

        PlayerMovement movement =
            GetComponentInParent<PlayerMovement>();

        if (movement != null)
            movement.SetADS(false);

        Debug.Log(
            "[ADS] force exit"
        );
    }

    public float GetPitch()
    {
        return xRotation;
    }

    public void SetADSFromServer(
        bool isADS)
    {
        _isADS = isADS;

        Camera cam =
            GetComponent<Camera>();

        if (cam == null)
        {
            cam =
                GetComponentInChildren<Camera>(
                    true
                );
        }

        if (cam != null)
        {
            cam.fieldOfView =
                isADS ? 40f : 60f;
        }

        Debug.Log(
            $"[ADS] set from server isADS={isADS}"
        );
    }


    private void SetADSLocal(bool value)
    {
        if (_isADS == value)
            return;

        _isADS = value;

        if (NetworkManager.Instance != null &&
            lastSentADS != _isADS)
        {
            lastSentADS = _isADS;

            NetworkManager.Instance.SendADSState(_isADS);
        }
    }
}