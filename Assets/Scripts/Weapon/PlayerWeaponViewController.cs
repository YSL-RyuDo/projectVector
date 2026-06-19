using UnityEngine;

public class PlayerWeaponViewController : MonoBehaviour
{
    private enum WeaponViewMode
    {
        FirstPerson,
        ThirdPerson
    }

    [Header("Weapon Holders")]
    [Tooltip("CameraRoot 아래에 있는 자기 시점 전용 WeaponHolder")]
    [SerializeField] private GameObject firstPersonWeaponHolder;

    [Tooltip("Model 손 본 아래에 있는 다른 플레이어 표시용 WeaponHolder")]
    [SerializeField] private GameObject thirdPersonWeaponHolder;

    private WeaponViewMode lastViewMode =
        WeaponViewMode.ThirdPerson;

    private bool hidden = true;

    private void Awake()
    {
        // 소유권과 시점이 확정되기 전에는 둘 다 숨긴다.
        SetHolderActive(firstPersonWeaponHolder, false);
        SetHolderActive(thirdPersonWeaponHolder, false);
    }

    public void ShowFirstPerson()
    {
        hidden = false;
        lastViewMode = WeaponViewMode.FirstPerson;

        ApplyViewMode();
    }
    public void ShowThirdPerson()
    {
        hidden = false;
        lastViewMode = WeaponViewMode.ThirdPerson;

        ApplyViewMode();
    }
    public void HideAll()
    {
        hidden = true;
        ApplyViewMode();
    }

    public void RestoreLastView()
    {
        hidden = false;
        ApplyViewMode();
    }

    public void ResetToThirdPerson()
    {
        hidden = false;
        lastViewMode = WeaponViewMode.ThirdPerson;

        ApplyViewMode();
    }

    public void ResetToFirstPerson()
    {
        hidden = false;
        lastViewMode = WeaponViewMode.FirstPerson;

        ApplyViewMode();
    }

    private void ApplyViewMode()
    {
        if (hidden)
        {
            SetHolderActive(
                firstPersonWeaponHolder,
                false
            );

            SetHolderActive(
                thirdPersonWeaponHolder,
                false
            );

            Debug.Log(
                $"[WEAPON VIEW] object={name}, hidden=true"
            );

            return;
        }

        bool firstPerson =
            lastViewMode == WeaponViewMode.FirstPerson;

        SetHolderActive(
            firstPersonWeaponHolder,
            firstPerson
        );

        SetHolderActive(
            thirdPersonWeaponHolder,
            !firstPerson
        );

        Debug.Log(
            $"[WEAPON VIEW] object={name}, " +
            $"mode={lastViewMode}, " +
            $"firstHolder={firstPerson}, " +
            $"thirdHolder={!firstPerson}"
        );
    }

    private void SetHolderActive(
        GameObject holder,
        bool active)
    {
        if (holder == null)
            return;

        if (holder.activeSelf == active)
            return;

        holder.SetActive(active);
    }
}