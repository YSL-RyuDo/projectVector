using System;
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

    private Transform player;

    public bool CanMove = true;

    void Start()
    {
        player = transform.parent;

        if (player == null)
        {
            Debug.LogError($"[FPSCameraLook] parent is null. object={gameObject.name}");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerIdentity = GetComponentInParent<PlayerIdentity>();

        if (_playerIdentity == null)
        {
            Debug.LogError($"[FPSCameraLook] PlayerIdentity not found in parent. object={gameObject.name}");
            enabled = false;
            return;
        }

        MiniMapUI ui = FindFirstObjectByType<MiniMapUI>();

        if (ui != null)
        {
            ui.RegisterLocalPlayerCamera(this);
            ui.RegisterPlayer(player.gameObject);
        }

        Debug.Log(
            $"[FPSCameraLook INIT] " +
            $"camera={gameObject.name}, " +
            $"player={player.name}, " +
            $"playerId={_playerIdentity.PlayerId}, " +
            $"userId={_playerIdentity.UserId}"
        );
    }

    void Update()
    {
        if (!CanMove) return;

        if (Cursor.lockState == CursorLockMode.None) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        if (_playerIdentity == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (_playerIdentity.UserId != NetworkManager.Instance.MyUserId)
            return;

        if (Mouse.current == null)
            return;

        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * MouseSettings.MouseSensitivity * sensitivity;
        float mouseY = mouseDelta.y * MouseSettings.MouseSensitivity * sensitivity;

        yaw += mouseX;
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

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
    }

    internal void AddYaw(float amount)
    {
        yaw += amount;
    }
}