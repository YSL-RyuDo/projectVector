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
        EnsureThirdPersonWeaponHolder();

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
        RefreshThirdPersonWeapon();
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

    private void EnsureThirdPersonWeaponHolder()
    {
        if (thirdPersonWeaponHolder != null)
        {
            EnsureWeaponMirror(thirdPersonWeaponHolder, false);
            return;
        }

        // Reuse the authored third-person hierarchy. Searching every hand can
        // select the first-person rig and create a second gun next to the camera.
        var model = transform.Find("Model");
        var mirrors = model ? model.GetComponentsInChildren<WeaponHolderModel>(true) : new WeaponHolderModel[0];
        if (mirrors.Length == 1)
            thirdPersonWeaponHolder = mirrors[0].gameObject;
        else
            Debug.LogError($"[WEAPON VIEW] Assign the authored third-person holder. object={name}");
    }

    private void EnsureWeaponMirror(GameObject holder, bool useRuntimeDefaults)
    {
        if (holder == null)
            return;

        WeaponHolderModel mirror = holder.GetComponent<WeaponHolderModel>();

        if (mirror == null)
            mirror = holder.AddComponent<WeaponHolderModel>();

        if (useRuntimeDefaults)
        {
            mirror.ConfigureRuntimeDefaults(
                new Vector3(0f, 0.1f, 0f),
                new Vector3(13f, 90f, -90f)
            );
        }
    }

    public void RefreshThirdPersonWeapon()
    {
        if (thirdPersonWeaponHolder == null)
            return;

        WeaponHolderModel mirror =
            thirdPersonWeaponHolder.GetComponent<WeaponHolderModel>();

        if (mirror != null)
            mirror.RefreshNow();
    }

    private Transform FindRightHand()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.isHuman)
                continue;

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null)
                return hand;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            string normalizedName = child.name
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (normalizedName.Contains("righthand") ||
                normalizedName.Contains("handright") ||
                normalizedName == "rhand")
            {
                return child;
            }
        }

        return null;
    }
}
