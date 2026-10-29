using UnityEngine;
using UnityEngine.InputSystem;

public class TESTWeaponADS : MonoBehaviour
{
    [Header("Hip")]
    [SerializeField] private Vector3 hipPosition = new Vector3(0.4f, -0.15f, 0.4f);
    [SerializeField] private Vector3 hipRotation = Vector3.zero;

    [Header("ADS")]
    [SerializeField] private Transform adsPoint;
    [SerializeField] private Transform adsTarget;
    [SerializeField] private float adsMoveSpeed = 15f;

    [Header("FOV")]
    [SerializeField] private Camera cam;
    [SerializeField] private float hipFOV = 60f;
    [SerializeField] private float adsFOV = 40f;
    [SerializeField] private float fovSpeed = 10f;

    private bool isADS = false;
    private Quaternion hipRot;

    private void Awake()
    {
        hipRot = Quaternion.Euler(hipRotation);

        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        bool adsInput = Mouse.current.rightButton.isPressed;

        if (adsInput != isADS)
            isADS = adsInput;
    }

    private void LateUpdate()
    {
        HandleFOV();

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

    private void HandleFOV()
    {
        if (cam == null) return;

        float targetFOV = isADS ? adsFOV : hipFOV;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            fovSpeed * Time.deltaTime
        );
    }
}