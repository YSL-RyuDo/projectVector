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

    [Header("애니메이터 참조 및 파라미터")]
    [SerializeField] private Animator armAnimator;
    [SerializeField] private string weaponTypeParam = "WeaponType";
    [SerializeField] private string reloadBoolParam = "IsReloading";

    private Vector3 targetPos;
    private Quaternion targetRot;
    private bool isADS = false;

    private PlayerItem playerItem;
    private WeaponData.WeaponType currentWeaponType = WeaponData.WeaponType.AR;

    private Weapon lastWeapon = null;
    private int lastWeaponTypeValue = -1;
    private WeaponArmTransform currentSetting;

    [SerializeField] private float knifeAttackCooldown = 0.7f;
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
    }

    private void Update()
    {
        UpdateWeaponTypeAndTransform();

        if (InGameChatUI.IsChatOpen)
        {
            isADS = false;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }

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

        if (Mouse.current != null)
        {
            // 칼 공격
            if (Mouse.current.leftButton.wasPressedThisFrame &&
                currentWeaponType == WeaponData.WeaponType.Knife &&
                knifeAttackTimer <= 0f)
            {
                if (armAnimator != null)
                {
                    armAnimator.SetTrigger("Knife_Attack");
                    knifeAttackTimer = knifeAttackCooldown;
                }
            }

            // 칼 공격 쿨타임 감소
            if (knifeAttackTimer > 0f)
            {
                knifeAttackTimer -= Time.deltaTime;
            }

            // 우클릭 ADS
            if (Mouse.current.rightButton.isPressed)
            {
                isADS = CanAim();
            }
            else
            {
                isADS = false;
            }
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

    public void Reload()
    {
        if (currentWeaponType == WeaponData.WeaponType.Knife)
            return;

        if (armAnimator != null)
        {
            armAnimator.SetBool(reloadBoolParam, true);
        }

        isADS = false;
    }

    public void EndReload()
    {
        if (armAnimator != null)
        {
            armAnimator.SetBool(reloadBoolParam, false);
        }
    }

    private void UpdateWeaponTypeAndTransform()
    {
        Weapon currentWeapon = null;
        if (playerItem != null)
        {
            currentWeapon = playerItem.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentWeaponType = currentWeapon.GetWeaponType();
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
                animWeaponTypeValue = 1;
                break;

            case WeaponData.WeaponType.AR:
            default:
                currentSetting = arTransform;
                animWeaponTypeValue = 0;
                break;
        }

        if (armAnimator != null && lastWeaponTypeValue != animWeaponTypeValue)
        {
            lastWeaponTypeValue = animWeaponTypeValue;
            armAnimator.SetInteger(weaponTypeParam, animWeaponTypeValue);
        }

        if (currentWeapon != null && currentWeapon != lastWeapon)
        {
            lastWeapon = currentWeapon;

            Transform wTransform = currentWeapon.transform;
            wTransform.localPosition = currentSetting.weaponPositionOffset;
            wTransform.localRotation = Quaternion.Euler(currentSetting.weaponRotationOffset);
        }

        if (isADS)
        {
            targetPos = currentSetting.adsPosition;
            targetRot = Quaternion.Euler(currentSetting.adsRotation);
        }
        else
        {
            targetPos = currentSetting.defaultPosition;
            targetRot = Quaternion.Euler(currentSetting.defaultRotation);
        }
    }

    public bool CanAim()
    {
        if (playerItem == null)
            return false;

        Weapon currentWeapon = playerItem.GetCurrentWeapon();
        if (currentWeapon == null)
            return false;

        WeaponData.WeaponType weaponType = currentWeapon.GetWeaponType();

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

    public bool IsADS => isADS;
}