using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float snappiness = 10f;
    [SerializeField] private float returnSpeed = 5f;
    [SerializeField] private float defaultRecoveryDelay = 0.15f; // fireRate가 안 넘어올 때 대비 fallback

    [Header("Hip Fire Recoil")]
    [SerializeField] private float hipWeaponRecoilMultiplier = 1f;
    [SerializeField] private float hipCameraRecoilMultiplier = 1f;
    [SerializeField] private float hipShakeMultiplier = 1f;

    [Header("ADS Recoil")]
    [SerializeField] private float adsWeaponRecoilMultiplier = 0.15f;
    [SerializeField] private float adsCameraRecoilMultiplier = 0.75f;
    [SerializeField] private float adsShakeMultiplier = 0.2f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private Vector2 targetCameraRecoil;
    private Vector2 currentCameraRecoil;

    private Vector2 previousAppliedCameraRecoil;

    private float lastFireTime = -999f;
    private float currentRecoveryDelay;

    private FPSCameraLook cameraLook;
    private GeneralCameraShake cameraShake;

    void Awake()
    {
        currentRecoveryDelay = defaultRecoveryDelay;

        PlayerCameraController cameraController =
            GetComponentInParent<PlayerCameraController>();

        if (cameraController != null)
        {
            cameraLook = cameraController.CameraLook;

            if (cameraController.PlayerCamera != null)
                cameraShake = cameraController.PlayerCamera.GetComponent<GeneralCameraShake>();
        }

        if (cameraLook == null)
            cameraLook = GetComponentInParent<FPSCameraLook>();

        if (cameraShake == null)
            cameraShake = GetComponentInChildren<GeneralCameraShake>(true);

        if (cameraShake == null)
            Debug.LogWarning($"[RECOIL] GeneralCameraShake not found. object={name}", this);
    }

    void Update()
    {
        bool isRecovering = Time.time - lastFireTime > currentRecoveryDelay;

        if (isRecovering)
        {
            targetRotation = Vector3.Lerp(
                targetRotation,
                Vector3.zero,
                returnSpeed * Time.deltaTime);

            targetCameraRecoil = Vector2.Lerp(
                targetCameraRecoil,
                Vector2.zero,
                returnSpeed * Time.deltaTime);
        }

        currentRotation = Vector3.Slerp(
            currentRotation,
            targetRotation,
            snappiness * Time.deltaTime
        );

        //transform.localRotation = Quaternion.Euler(currentRotation);

        currentCameraRecoil = Vector2.Lerp(
            currentCameraRecoil,
            targetCameraRecoil,
            snappiness * Time.deltaTime);

        Vector2 delta = currentCameraRecoil - previousAppliedCameraRecoil;

        previousAppliedCameraRecoil = currentCameraRecoil;

        if (cameraLook != null)
        {
            cameraLook.AddYaw(delta.x);
            cameraLook.AddPitch(-delta.y);
        }
    }

    public void FireRecoil(Vector2 recoilPattern, float multiplier, bool isADS, float recoveryDelay = -1f)
    {
        lastFireTime = Time.time;
        currentRecoveryDelay = recoveryDelay > 0f ? recoveryDelay : defaultRecoveryDelay;

        float weaponMultiplier = isADS
            ? adsWeaponRecoilMultiplier
            : hipWeaponRecoilMultiplier;

        float cameraMultiplier = isADS
            ? adsCameraRecoilMultiplier
            : hipCameraRecoilMultiplier;

        float shakeMultiplier = isADS
            ? adsShakeMultiplier
            : hipShakeMultiplier;

        targetRotation += new Vector3(
            recoilPattern.y,
            recoilPattern.x,
            Random.Range(-1f, 1f)
        ) * (multiplier * weaponMultiplier);

        /*
        if (cameraLook != null)
        {
            cameraLook.AddYaw(
                recoilPattern.x * multiplier * cameraMultiplier
            );

            cameraLook.AddPitch(
                -recoilPattern.y * multiplier * cameraMultiplier
            );
        }
        */

        targetCameraRecoil += new Vector2(
            recoilPattern.x,
            recoilPattern.y
        ) * multiplier * cameraMultiplier;

        if (cameraShake != null)
        {
            cameraShake.Fire(
                new Vector3(0f, -0.1f, -0.1f) * shakeMultiplier
            );
        }
    }

    public void StopRecoil()
    {
        targetRotation = Vector3.Lerp(
            targetRotation,
            Vector3.zero,
            returnSpeed * Time.deltaTime
        );
    }
}