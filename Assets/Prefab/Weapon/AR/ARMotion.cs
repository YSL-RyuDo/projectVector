using System.Collections;
using UnityEngine;

public class ARMotion : MonoBehaviour
{
    private static readonly int ReloadHash =
        Animator.StringToHash("ReLoad");

    private Animator animator;
    private WeaponSkinSlot skinSlot;
    private Coroutine reloadCoroutine;

    public bool IsReloading { get; private set; }

    private void Awake()
    {
        skinSlot = GetComponent<WeaponSkinSlot>();
        ResolveVisualAnimator();
    }

    private void Start()
    {
        ResolveVisualAnimator();

        if (animator == null)
        {
            Debug.LogWarning(
                $"[AR MOTION] Animator를 찾을 수 없습니다. " +
                $"object={name}",
                this
            );
        }
    }

    public void SetAnimator(Animator visualAnimator)
    {
        animator = visualAnimator;

        if (animator != null)
            animator.SetBool(ReloadHash, IsReloading);
    }

    private void ResolveVisualAnimator()
    {
        if (skinSlot == null)
            skinSlot = GetComponent<WeaponSkinSlot>();

        if (skinSlot != null && skinSlot.ActiveVisualAnimator != null)
        {
            SetAnimator(skinSlot.ActiveVisualAnimator);
            return;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator candidate in animators)
        {
            if (candidate == null || candidate.transform == transform)
                continue;

            SetAnimator(candidate);
            return;
        }

        animator = null;
    }

    public void PlayReload(float reloadTime)
    {
        if (animator == null)
            return;

        if (reloadTime <= 0f)
            return;

        // SpectatorManager의 상태 갱신이 반복되어도
        // 장전 애니메이션을 처음부터 다시 재생하지 않는다.
        if (IsReloading)
            return;

        reloadCoroutine =
            StartCoroutine(
                ReloadRoutine(reloadTime)
            );
    }

    public void StopReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        IsReloading = false;

        if (animator != null)
            animator.SetBool(ReloadHash, false);
    }

    private IEnumerator ReloadRoutine(float reloadTime)
    {
        IsReloading = true;
        animator.SetBool(ReloadHash, true);

        yield return new WaitForSeconds(reloadTime);

        animator.SetBool(ReloadHash, false);

        IsReloading = false;
        reloadCoroutine = null;
    }

    private void OnDisable()
    {
        StopReload();
    }
}
