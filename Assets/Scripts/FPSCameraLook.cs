using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    public float baseSensitivity = 0.02f;
    public float adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

    private float xRotation = 0f;   // Camera Pitch
    private float yaw = 0f;         // Player Yaw
    private float recoilOffset = 0f;
    private bool isADS = false;

    Transform player;

    void Start()
    {
        player = transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float sensitivity = baseSensitivity * (isADS ? adsSensitivityMultiplier : 1f);

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
