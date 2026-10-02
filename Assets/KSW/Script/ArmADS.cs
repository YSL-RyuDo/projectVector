using UnityEngine;
using UnityEngine.InputSystem;

public class ArmADS : MonoBehaviour
{
    [System.Serializable]
    public struct WeaponArmTransform
    {
        [Header("팔(Arm) 기본 위치 & 회전")]
        public Vector3 defaultPosition;
        public Vector3 defaultRotation;

        [Header("팔(Arm) 조준(ADS) 위치 & 회전")]
        public Vector3 adsPosition;
        public Vector3 adsRotation;

        [Header("장착 무기(Weapon) 위치 & 회전 오프셋")]
        public Vector3 weaponPositionOffset;
        public Vector3 weaponRotationOffset;
    }

    [Header("무기 타입별 위치 및 오프셋 설정")]
    [SerializeField]
    private WeaponArmTransform arTransform = new WeaponArmTransform
    {
        defaultPosition = Vector3.zero,
        defaultRotation = Vector3.zero,
        adsPosition = new Vector3(0f, -0.1f, 0.2f),
        adsRotation = Vector3.zero,
        weaponPositionOffset = Vector3.zero,
        weaponRotationOffset = Vector3.zero
    };

    [SerializeField]
    private WeaponArmTransform pistolTransform = new WeaponArmTransform
    {
        defaultPosition = Vector3.zero,
        defaultRotation = Vector3.zero,
        adsPosition = new Vector3(0f, -0.05f, 0.15f),
        adsRotation = Vector3.zero,
        weaponPositionOffset = Vector3.zero,
        weaponRotationOffset = Vector3.zero
    };

    [SerializeField]
    private WeaponArmTransform knifeTransform = new WeaponArmTransform
    {
        defaultPosition = Vector3.zero,
        defaultRotation = Vector3.zero,
        adsPosition = Vector3.zero,
        adsRotation = Vector3.zero,
        weaponPositionOffset = Vector3.zero,
        weaponRotationOffset = Vector3.zero
    };

    [Header("조준 속도")]
    [SerializeField] private float adsSpeed = 15f;

    [Header("재장전 애니메이션")]
    [Tooltip("재장전 애니메이션의 원본 길이(초)")]
    [SerializeField] private float reloadAnimationLength = 1.5f;

    [Header("애니메이터 참조 및 파라미터")]
    [SerializeField] private Animator armAnimator;
    [SerializeField] private string weaponTypeParam = "WeaponType";
    [SerializeField] private string reloadBoolParam = "IsReloading";

    [Header("칼 공격")]
    [SerializeField] private float knifeAttackCooldown = 0.7f;

    private Vector3 targetPos;
    private Quaternion targetRot;

    private bool isADS = false;

    private PlayerItem playerItem;
    private WeaponData.WeaponType currentWeaponType =
        WeaponData.WeaponType.AR;

    private Weapon lastWeapon = null;
    private int lastWeaponTypeValue = -1;

    private WeaponArmTransform currentSetting;

    private float knifeAttackTimer = 0f;

    private void Awake()
    {
        if (armAnimator == null)
        {
            armAnimator = GetComponent<Animator>();

            if (armAnimator == null)
            {
                armAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        playerItem = GetComponentInParent<PlayerItem>(true);

        if (playerItem == null)
        {
            Debug.LogError("[ArmADS] PlayerItem을 찾을 수 없습니다.");
        }

        if (armAnimator != null)
        {
            armAnimator.speed = 1f;
        }
    }

    private void Update()
    {
        UpdateWeaponTypeAndTransform();

        if (InGameChatUI.IsChatOpen)
        {
            isADS = false;
            return;
        }

        // 재장전

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }

        // ADS

        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                isADS = CanAim();
            }
            else
            {
                isADS = false;
            }
        }

        // 칼 공격

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentWeaponType == WeaponData.WeaponType.Knife &&
                knifeAttackTimer <= 0f)
            {
                if (armAnimator != null)
                {
                    armAnimator.SetTrigger("Knife_Attack");

                    knifeAttackTimer = knifeAttackCooldown;
                }
            }
        }

        // 칼 공격 쿨타임
        if (knifeAttackTimer > 0f)
        {
            knifeAttackTimer -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            adsSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            adsSpeed * Time.deltaTime
        );
    }

    // RELOAD

    public void Reload()
    {
        // 칼은 재장전 불가능
        if (currentWeaponType == WeaponData.WeaponType.Knife)
            return;

        if (playerItem == null)
            return;

        Weapon currentWeapon = playerItem.GetCurrentWeapon();

        if (currentWeapon == null)
            return;

        // 실제 재장전 가능 여부 확인
        if (!currentWeapon.CanReloadNow())
            return;

        // Weapon.cs에 이미 존재하는 함수 사용
        float reloadTime = currentWeapon.GetWeaponReLoadTime();

        // reloadTime에 맞춰 Animator 속도 변경
        SetReloadAnimationSpeed(reloadTime);

        if (armAnimator != null)
        {
            armAnimator.SetBool(reloadBoolParam, true);
        }

        // 재장전 중 ADS 해제
        isADS = false;
    }

    public void EndReload()
    {
        if (armAnimator != null)
        {
            armAnimator.SetBool(reloadBoolParam, false);

            // Animator 속도 원상복구
            armAnimator.speed = 1f;
        }

        isADS = false;
    }

    // RELOAD ANIMATION SPEED

    private void SetReloadAnimationSpeed(float reloadTime)
    {
        if (armAnimator == null)
            return;

        if (reloadTime <= 0f)
        {
            armAnimator.speed = 1f;
            return;
        }

        if (reloadAnimationLength <= 0f)
        {
            armAnimator.speed = 1f;
            return;
        }

        /*
         * 예시
         *
         * Reload 애니메이션 원본 길이 = 1.5초
         *
         * 무기 A
         * reloadTime = 1.5
         * Animator.speed = 1.0
         *
         * 무기 B
         * reloadTime = 2.0
         * Animator.speed = 0.75
         *
         * 무기 C
         * reloadTime = 1.0
         * Animator.speed = 1.5
         */

        armAnimator.speed =
            reloadAnimationLength / reloadTime;
    }

    // WEAPON TYPE

    private void UpdateWeaponTypeAndTransform()
    {
        Weapon currentWeapon = null;

        if (playerItem != null)
        {
            currentWeapon = playerItem.GetCurrentWeapon();

            if (currentWeapon != null)
            {
                currentWeaponType =
                    currentWeapon.GetWeaponType();
            }
        }

        int animWeaponTypeValue;

        switch (currentWeaponType)
        {
            case WeaponData.WeaponType.Pistol:
                currentSetting = pistolTransform;
                animWeaponTypeValue = 1;
                break;

            case WeaponData.WeaponType.Knife:
                currentSetting = knifeTransform;
                animWeaponTypeValue = 2;
                break;

            case WeaponData.WeaponType.AR:
            default:
                currentSetting = arTransform;
                animWeaponTypeValue = 0;
                break;
        }

        // Animator WeaponType 변경
        if (armAnimator != null &&
            lastWeaponTypeValue != animWeaponTypeValue)
        {
            lastWeaponTypeValue = animWeaponTypeValue;

            armAnimator.SetInteger(
                weaponTypeParam,
                animWeaponTypeValue
            );
        }

        // 무기가 변경됐을 때
        if (currentWeapon != null &&
            currentWeapon != lastWeapon)
        {
            lastWeapon = currentWeapon;

            Transform weaponTransform =
                currentWeapon.transform;

            weaponTransform.localPosition =
                currentSetting.weaponPositionOffset;

            weaponTransform.localRotation =
                Quaternion.Euler(
                    currentSetting.weaponRotationOffset
                );

            // 무기 변경 시 Animator 속도 초기화
            if (armAnimator != null)
            {
                armAnimator.speed = 1f;
            }
        }

        // ADS 위치 / 기본 위치
        if (isADS)
        {
            targetPos =
                currentSetting.adsPosition;

            targetRot =
                Quaternion.Euler(
                    currentSetting.adsRotation
                );
        }
        else
        {
            targetPos =
                currentSetting.defaultPosition;

            targetRot =
                Quaternion.Euler(
                    currentSetting.defaultRotation
                );
        }
    }

    // CAN AIM

    public bool CanAim()
    {
        if (playerItem == null)
            return false;

        Weapon currentWeapon =
            playerItem.GetCurrentWeapon();

        if (currentWeapon == null)
            return false;

        WeaponData.WeaponType weaponType =
            currentWeapon.GetWeaponType();

        // Pistol / Knife는 ADS 불가능
        if (weaponType == WeaponData.WeaponType.Pistol ||
            weaponType == WeaponData.WeaponType.Knife)
        {
            return false;
        }

        return true;
    }

    // EXTERNAL ADS

    public void SetADS(bool ads)
    {
        if (ads && !CanAim())
        {
            isADS = false;
            return;
        }

        isADS = ads;
    }

    public bool IsADS => isADS;
}