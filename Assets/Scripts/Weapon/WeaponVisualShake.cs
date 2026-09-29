using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponVisualShake : MonoBehaviour
{
    public enum WeaponType
    {
        Pistol,
        AR,
        SMG,
        LMG,
        DMR,
        SR,
        Shotgun,
        Knife
    }

    [Header("Weapon Type")]
    [SerializeField] private WeaponType weaponType = WeaponType.AR;

    [Header("Recoil")]
    [SerializeField] private float snappiness = 10f;
    [SerializeField] private float returnSpeed = 5f;
    [SerializeField] private float recoveryDelay = 0.15f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private float lastFireTime = -999f;
    private Quaternion baseLocalRotation;

    private float rotationAmount;
    private float hipMultiplier;
    private float adsMultiplier;

    private void Awake()
    {
        baseLocalRotation = transform.localRotation;

        ApplyWeaponSettings();
    }

    private void Update()
    {
        // 마우스를 꾹 누르는 동안 계속 진동
        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            FireShake(false);
        }

        bool isRecovering =
            Time.time - lastFireTime > recoveryDelay;

        if (isRecovering)
        {
            targetRotation = Vector3.Lerp(
                targetRotation,
                Vector3.zero,
                returnSpeed * Time.deltaTime
            );
        }

        currentRotation = Vector3.Slerp(
            currentRotation,
            targetRotation,
            snappiness * Time.deltaTime
        );

        // 기본 회전 유지
        // X = 0
        // Y = 좌우 회전
        // Z = 좌우 기울기
        transform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(currentRotation);
    }

    private void FireShake(bool isADS)
    {
        lastFireTime = Time.time;

        float multiplier =
            isADS
                ? adsMultiplier
                : hipMultiplier;

        // WeaponRecoil의 무기 진동 구조 그대로 사용
        // 단, X 회전은 완전히 제거
        targetRotation += new Vector3(
            0f,
            Random.Range(
                -rotationAmount,
                rotationAmount
            ),
            Random.Range(
                -rotationAmount,
                rotationAmount
            )
        ) * multiplier;
    }

    private void ApplyWeaponSettings()
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                rotationAmount = 1.0f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 12f;
                returnSpeed = 7f;
                recoveryDelay = 0.15f;
                break;

            case WeaponType.AR:
                rotationAmount = 0.65f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 14f;
                returnSpeed = 8f;
                recoveryDelay = 0.12f;
                break;

            case WeaponType.SMG:
                rotationAmount = 0.45f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 16f;
                returnSpeed = 9f;
                recoveryDelay = 0.10f;
                break;

            case WeaponType.LMG:
                rotationAmount = 0.8f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 11f;
                returnSpeed = 6f;
                recoveryDelay = 0.13f;
                break;

            case WeaponType.DMR:
                rotationAmount = 0.9f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 10f;
                returnSpeed = 6f;
                recoveryDelay = 0.18f;
                break;

            case WeaponType.SR:
                rotationAmount = 1.5f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 8f;
                returnSpeed = 5f;
                recoveryDelay = 0.3f;
                break;

            case WeaponType.Shotgun:
                rotationAmount = 1.8f;
                hipMultiplier = 1.0f;
                adsMultiplier = 0.15f;
                snappiness = 8f;
                returnSpeed = 5f;
                recoveryDelay = 0.35f;
                break;

            case WeaponType.Knife:
                rotationAmount = 2.0f;
                hipMultiplier = 1.0f;
                adsMultiplier = 1.0f;
                snappiness = 10f;
                returnSpeed = 7f;
                recoveryDelay = 0.3f;
                break;
        }
    }

    public void SetWeaponType(WeaponType type)
    {
        weaponType = type;
        ApplyWeaponSettings();
    }

    public void StopShake()
    {
        targetRotation = Vector3.Lerp(
            targetRotation,
            Vector3.zero,
            returnSpeed * Time.deltaTime
        );
    }
}