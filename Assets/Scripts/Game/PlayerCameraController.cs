using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private FPSCameraLook cameraLook;

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
    }

    public void SetLocalPlayer(bool isLocal)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = isLocal;
            playerCamera.tag = isLocal ? "MainCamera" : "Untagged";
        }

        if (audioListener != null)
            audioListener.enabled = isLocal;

        if (cameraLook != null)
            cameraLook.SetLocalPlayer(isLocal, transform);
    }

    public void SetFOV(float fov)
    {
        if (playerCamera == null) return;
        playerCamera.fieldOfView = fov;
    }
}