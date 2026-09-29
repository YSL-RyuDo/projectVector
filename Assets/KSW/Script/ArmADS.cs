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

    private PlayerItem playerItem;


    private void Awake()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;

        targetAdsRotation = Quaternion.Euler(adsRotation);

        cameraController = GetComponent<PlayerCameraController>();

        // PlayerItem 찾기
        playerItem = GetComponentInParent<PlayerItem>(true);

        if (playerItem == null)
        {
            Debug.LogError("[ArmADS] PlayerItem을 찾을 수 없습니다.");
        }


        if (cameraController != null &&
            cameraController.PlayerCamera != null)
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
            isADS = false;
            UpdateCameraZoom();
            return;
        }


        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                // 현재 무기가 조준 가능한 경우에만 ADS
                isADS = CanAim();
            }
            else
            {
                isADS = false;
            }
        }

        UpdateCameraZoom();
    }


    private void LateUpdate()
    {
        Vector3 targetPos =
            isADS ? adsPosition : defaultPosition;

        Quaternion targetRot =
            isADS ? targetAdsRotation : defaultRotation;


        transform.localPosition =
            Vector3.Lerp(
                transform.localPosition,
                targetPos,
                adsSpeed * Time.deltaTime
            );


        transform.localRotation =
            Quaternion.Slerp(
                transform.localRotation,
                targetRot,
                adsSpeed * Time.deltaTime
            );
    }


    private void UpdateCameraZoom()
    {
        if (targetCamera == null)
            return;


        float targetFOV =
            isADS ? adsFOV : defaultFOV;


        float nextFOV =
            Mathf.Lerp(
                targetCamera.fieldOfView,
                targetFOV,
                fovZoomSpeed * Time.deltaTime
            );


        if (cameraController != null)
        {
            cameraController.SetFOV(nextFOV);
        }
        else
        {
            targetCamera.fieldOfView = nextFOV;
        }
    }

    // 현재 들고 있는 무기가 조준 가능한지 확인

    private bool CanAim()
    {
        if (playerItem == null)
            return false;

        Weapon currentWeapon = playerItem.GetCurrentWeapon();

        if (currentWeapon == null)
            return false;

        WeaponData.WeaponType weaponType =
            currentWeapon.GetWeaponType();

        if (weaponType == WeaponData.WeaponType.Pistol ||
            weaponType == WeaponData.WeaponType.Knife)
        {
            return false;
        }

        return true;
    }


    public void SetADS(bool ads)
    {
        if (ads && !CanAim())
        {
            isADS = false;
            return;
        }

        isADS = ads;
    }
}