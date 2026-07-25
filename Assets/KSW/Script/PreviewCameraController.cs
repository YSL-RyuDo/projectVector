using System.Collections;
using UnityEngine;

public class PreviewCameraController : MonoBehaviour
{
    [Header("Target Positions")]
    [SerializeField] private Transform fullBodyViewPoint;

    [Header("Weapon View Offset")]
    [SerializeField] private Vector3 weaponOffset = new Vector3(0f, 0.1f, -0.8f);

    [Header("Transition Settings")]
    [SerializeField] private float smoothTime = 0.25f;
    [Tooltip("캐릭터 회전 완료를 기다릴 시간(초)")]
    [SerializeField] private float characterResetWaitTime = 0.25f;

    private bool isWeaponView = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector3 currentVelocity;
    private bool isMoving = false;

    private Coroutine currentFocusCoroutine;

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

    /// <summary>
    /// UI 버튼에 연결할 메인 토글 함수
    /// </summary>
    public void ToggleCameraViewWithRotation()
    {
        isWeaponView = !isWeaponView;

        if (currentFocusCoroutine != null)
        {
            StopCoroutine(currentFocusCoroutine);
        }

        if (isWeaponView)
        {
            currentFocusCoroutine = StartCoroutine(RoutineFocusOnSocket("RightHandSocket"));
        }
        else
        {
            SetFullBodyView();
        }
    }

    public void ToggleCameraView()
    {
        ToggleCameraViewWithRotation();
    }

    /// <summary>
    /// 코루틴: 캐릭터 Y축 회전값을 체크하여 조건부 대기 후 카메라 이동
    /// </summary>
    private IEnumerator RoutineFocusOnSocket(string socketName)
    {
        GameObject handSocket = GameObject.Find(socketName);

        if (handSocket == null)
        {
            SetFullBodyView();
            yield break;
        }

        Transform handTransform = handSocket.transform;

        CharacterPanelController characterController =
            FindFirstObjectByType<CharacterPanelController>();

        if (characterController != null)
        {
            // 이미 정면이면 바로 넘어감
            if (!characterController.IsCharacterFacingFront)
            {
                characterController.ResetCharacterRotationSmooth();

                // 회전 완료까지 대기
                yield return new WaitUntil(() => characterController.IsCharacterFacingFront);
            }
        }

        Quaternion refRotation = fullBodyViewPoint != null
            ? fullBodyViewPoint.rotation
            : transform.rotation;

        Vector3 worldOffset =
            refRotation * Vector3.right * weaponOffset.x +
            refRotation * Vector3.up * weaponOffset.y +
            refRotation * Vector3.forward * weaponOffset.z;

        targetPosition = handTransform.position + worldOffset;

        if (fullBodyViewPoint != null)
            targetRotation = fullBodyViewPoint.rotation;

        isWeaponView = true;
        isMoving = true;

        currentFocusCoroutine = null;
    }

    public void FocusOnSocket(string socketName)
    {
        if (currentFocusCoroutine != null)
        {
            StopCoroutine(currentFocusCoroutine);
        }
        currentFocusCoroutine = StartCoroutine(RoutineFocusOnSocket(socketName));
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