using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    [SerializeField] private float _baseSensitivity = 0.2f;
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

    private PlayerIdentity _playerIdentity;

    private float xRotation = 0f;
    private float yaw = 0f;
    private float recoilOffset = 0f;
    private bool _isADS = false;

    private Transform playerRoot;
    private bool isLocalPlayer = false;

    public bool CanMove = true;

    private void Awake()
    {
        _playerIdentity = GetComponentInParent<PlayerIdentity>();
        enabled = false;
    }

    public void SetLocalPlayer(bool isLocal, Transform root)
    {
        isLocalPlayer = isLocal;
        playerRoot = root;

        CanMove = isLocal;
        enabled = isLocal;

        if (!isLocal)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_playerIdentity == null)
            _playerIdentity = GetComponentInParent<PlayerIdentity>();

        MiniMapUI ui = FindFirstObjectByType<MiniMapUI>();

        if (ui != null && playerRoot != null)
        {
            ui.RegisterLocalPlayerCamera(this);
            ui.RegisterPlayer(playerRoot.gameObject);
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
        if (!isLocalPlayer) return;
        if (!CanMove) return;
        if (playerRoot == null) return;

        if (Cursor.lockState == CursorLockMode.None) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        if (Mouse.current == null) return;

        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * MouseSettings.MouseSensitivity * sensitivity;
        float mouseY = mouseDelta.y * MouseSettings.MouseSensitivity * sensitivity;

        yaw += mouseX;
        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        recoilOffset = Mathf.Lerp(
            recoilOffset,
            0f,
            recoilReturnSpeed * Time.deltaTime
        );

        transform.localRotation =
            Quaternion.Euler(xRotation + recoilOffset, 0f, 0f);
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
        _isADS = ads;
    }

    public void AddPitch(float amount)
    {
        xRotation += amount;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
    }

    public void AddYaw(float amount)
    {
        yaw += amount;
    }
}