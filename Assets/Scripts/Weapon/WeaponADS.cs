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

    private void Awake()
    {
        hipRot = Quaternion.Euler(hipRotation);
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

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                hipRot,
                adsMoveSpeed * Time.deltaTime
            );

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