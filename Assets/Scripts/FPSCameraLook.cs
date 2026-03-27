using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    // 기본 마우스 감도
    [SerializeField] private float _baseSensitivity = 0.2f;
    // ADS(조준) 상태일 때 감도 배율 (0.5면 절반으로 감소)
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

    private float xRotation = 0f;   // Camera Pitch
    private float yaw = 0f;         // Player Yaw
    private float recoilOffset = 0f;
    private bool isADS = false;

    Transform player;

    public bool CanMove = true;

    public static FPSCameraLook Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        player = transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!CanMove) return;

        // ADS(조준) 상태면 감도 낮춤
        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);
        
        // 마우스 이동값 읽기
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

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
        isADS = ads;
    }

    public void AddPitch(float amount)
    {
        xRotation += amount;
    }

    internal void AddYaw(float amount)
    {
        yaw += amount;
        //throw new NotImplementedException();
    }
}
