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
            //ui.RegisterLocalPlayerCamera(this);
            //ui.RegisterPlayer(playerRoot.gameObject);
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

        // 마우스 델타(입력)값 읽기
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        // MouseSettings에서 각각의 감도 가져오기
        float generalSens = MouseSettings.GetSensitivity(SensitivityType.General);
        float horizontalSens = MouseSettings.GetSensitivity(SensitivityType.Horizontal);
        float verticalSens = MouseSettings.GetSensitivity(SensitivityType.Vertical);

        // 조준(ADS) 상태에 따른 공통 배율 계산
        float adsMultiplier = _isADS ? _adsSensitivityMultiplier : 1f;

        // 최종 수평 / 수직 감도 적용 계산 (델타값 * 베이스감도 * 전체감도 * 수평or수직감도 * ADS배율)
        float finalHorizontalDelta = mouseX * _baseSensitivity * generalSens * horizontalSens * adsMultiplier;
        float finalVerticalDelta = mouseY * _baseSensitivity * generalSens * verticalSens * adsMultiplier;

        // 좌우 회전 (Yaw) 적용 및 플레이어 루트 회전
        yaw += finalHorizontalDelta;
        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 상하 회전 (Pitch) 적용 및 80도 제한
        xRotation -= finalVerticalDelta;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // 반동 처리 및 카메라 최종 회전 적용
        recoilOffset = Mathf.Lerp(
            recoilOffset,
            0f,
            recoilReturnSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Euler(xRotation + recoilOffset, 0f, 0f);
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

    public void ResetLookRotation(float yawValue)
    {
        xRotation = 0f;
        yaw = yawValue;
        recoilOffset = 0f;

        transform.localRotation = Quaternion.identity;

        if (playerRoot == null)
            playerRoot = GetComponentInParent<PlayerIdentity>()?.transform;

        if (playerRoot != null)
            playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}