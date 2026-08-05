using UnityEngine;

public class WeaponADS : MonoBehaviour
{
    [Header("Hip")]
    [SerializeField] private Vector3 hipPosition = new Vector3(0.4f, -0.15f, 0.4f);
    [SerializeField] private Vector3 hipRotation = Vector3.zero;

    [Header("ADS")]
    [SerializeField] private Transform adsPoint;
    [SerializeField] private Transform adsTarget;
    [SerializeField] private float adsMoveSpeed = 15f;

    private bool isADS = false;
    private Quaternion hipRot;
    private Animator rootAnimator;

    private void Awake()
    {
        hipRot = Quaternion.Euler(hipRotation);
        rootAnimator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (!isADS)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                hipPosition,
                adsMoveSpeed * Time.deltaTime
            );

            // Do not fight an Animator that owns this same root transform.
            // WeaponSkinSlot disables that root Animator and moves animation to
            // the visual model; legacy weapons without a skin slot remain stable.
            if (rootAnimator == null || !rootAnimator.enabled)
            {
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    hipRot,
                    adsMoveSpeed * Time.deltaTime
                );
            }

            return;
        }

        if (adsPoint == null || adsTarget == null)
            return;

        Vector3 worldOffset = adsTarget.position - adsPoint.position;
        Vector3 targetWorldPosition = transform.position + worldOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetWorldPosition,
            adsMoveSpeed * Time.deltaTime
        );
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
    }

    public void SetADSTarget(Transform target)
    {
        adsTarget = target;
    }
}
