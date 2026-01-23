using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    public float baseSensitivity = 0.02f;
    public float adsSensitivityMultiplier = 0.5f;
    public Transform playerBody;

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

    private float xRotation = 0f;
    private float recoilOffset = 0f;
    private bool isADS = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float sensitivity = baseSensitivity * (isADS ? adsSensitivityMultiplier : 1f);

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        recoilOffset = Mathf.Lerp(recoilOffset, 0f, recoilReturnSpeed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(xRotation + recoilOffset, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void AddRecoil(float amount)
    {
        recoilOffset -= amount;
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
    }
}
