using UnityEngine;

public class PreviewCameraController : MonoBehaviour
{
    [Header("Target Positions")]
    [SerializeField] private Transform fullBodyViewPoint;

    [Header("Weapon View Offset")]
    [SerializeField] private Vector3 weaponOffset = new Vector3(0f, 0.1f, -0.8f);

    [Header("Transition Settings")]
    [SerializeField] private float smoothTime = 0.25f;

    private bool isWeaponView = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector3 currentVelocity;
    private bool isMoving = false;

    private void Awake()
    {
        if (fullBodyViewPoint != null)
        {
            targetPosition = fullBodyViewPoint.position;
            targetRotation = fullBodyViewPoint.rotation;
        }
        else
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }
    }

    private void Update()
    {
        if (!isMoving) return;

        // 위치 이동 (SmoothDamp)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );

        // 회전 이동 (Slerp)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * (1f / Mathf.Max(smoothTime, 0.01f)) * 2f
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.001f &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            isMoving = false;
        }
    }

    public void ToggleCameraView()
    {
        isWeaponView = !isWeaponView;

        if (isWeaponView)
        {
            FocusOnSocket("RightHandSocket");
        }
        else
        {
            SetFullBodyView();
        }
    }

    public void FocusOnSocket(string socketName)
    {
        GameObject handSocket = GameObject.Find(socketName);

        if (handSocket != null)
        {
            Transform handTransform = handSocket.transform;
            Transform referenceTransform = (fullBodyViewPoint != null) ? fullBodyViewPoint : transform;

            targetPosition = handTransform.position
                           + (referenceTransform.right * weaponOffset.x)
                           + (referenceTransform.up * weaponOffset.y)
                           + (referenceTransform.forward * weaponOffset.z);

            if (fullBodyViewPoint != null)
            {
                targetRotation = fullBodyViewPoint.rotation;
            }

            isWeaponView = true;
            isMoving = true;
        }
        else
        {
            SetFullBodyView();
        }
    }

    public void SetFullBodyView()
    {
        isWeaponView = false;
        if (fullBodyViewPoint != null)
        {
            targetPosition = fullBodyViewPoint.position;
            targetRotation = fullBodyViewPoint.rotation;
            isMoving = true;
        }
    }
}