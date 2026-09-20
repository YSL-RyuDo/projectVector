using UnityEngine;
using UnityEngine.InputSystem;

public class ArmADS : MonoBehaviour
{
    [Header("조준 위치 설정 (Local)")]
    [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.1f, 0.2f);
    [SerializeField] private Vector3 adsRotation = Vector3.zero;
    [SerializeField] private float adsSpeed = 15f;

    [Header("카메라 확대 (Zoom) 설정")]
    [SerializeField] private float fovZoomSpeed = 15f;
    private float defaultFOV = 60f;
    private float adsFOV = 30f;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Quaternion targetAdsRotation;
    private bool isADS = false;

    private Camera targetCamera;
    private PlayerCameraController cameraController;

    private void Awake()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;

        targetAdsRotation = Quaternion.Euler(adsRotation);
        cameraController = GetComponent<PlayerCameraController>();
        if (cameraController != null && cameraController.PlayerCamera != null)
        {
            targetCamera = cameraController.PlayerCamera;
        }
        else
        {
            GameObject camObj = GameObject.Find("PlayerCamera");
            if (camObj != null)
            {
                targetCamera = camObj.GetComponent<Camera>();
            }
        }

        if (targetCamera != null)
        {
            defaultFOV = targetCamera.fieldOfView;
        }
    }

    private void Update()
    {
        if (InGameChatUI.IsChatOpen)
        {
            if (isADS) isADS = false;
            return;
        }

        if (Mouse.current != null)
        {
            isADS = Mouse.current.rightButton.isPressed;
        }
        UpdateCameraZoom();
    }

    private void LateUpdate()
    {
        Vector3 targetPos = isADS ? adsPosition : defaultPosition;
        Quaternion targetRot = isADS ? targetAdsRotation : defaultRotation;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, adsSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, adsSpeed * Time.deltaTime);
    }

    private void UpdateCameraZoom()
    {
        if (targetCamera == null) return;

        float targetFOV = isADS ? adsFOV : defaultFOV;
        float nextFOV = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, fovZoomSpeed * Time.deltaTime);

        if (cameraController != null)
        {
            cameraController.SetFOV(nextFOV);
        }
        else
        {
            targetCamera.fieldOfView = nextFOV;
        }
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
    }
}