using UnityEngine;

public class PlayerWeaponViewController : MonoBehaviour
{
    private enum WeaponViewMode
    {
        FirstPerson,
        ThirdPerson
    }

    [Header("Weapon Holders")]
    [Tooltip("CameraRoot 아래에 있는 1인칭 WeaponHolder")]
    [SerializeField] private GameObject firstPersonWeaponHolder;

    [Tooltip("Model의 손 본 아래에 있는 3인칭 WeaponHolder(Model)")]
    [SerializeField] private GameObject thirdPersonWeaponHolder;

    private WeaponViewMode lastViewMode =
        WeaponViewMode.ThirdPerson;

    private bool hidden;

    private void Awake()
    {
        // 플레이어 소유권이 확정되기 전에 두 총이 동시에 보이는 것을 방지한다.
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

    private void ApplyViewMode()
    {
        if (hidden)
        {
            SetHolderActive(firstPersonWeaponHolder, false);
            SetHolderActive(thirdPersonWeaponHolder, false);
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
            $"mode={lastViewMode}, hidden={hidden}"
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