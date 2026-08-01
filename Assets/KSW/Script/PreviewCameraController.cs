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

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );

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

    private IEnumerator RoutineFocusOnSocket(string socketName)
    {
        CharacterPanelController characterController = FindFirstObjectByType<CharacterPanelController>();
        Transform handTransform = null;

        if (characterController != null && characterController.ObjectSpawnPoint != null)
        {
            handTransform = FindChildRecursive(characterController.ObjectSpawnPoint, socketName);
        }

        if (handTransform == null)
        {
            GameObject handSocket = GameObject.Find(socketName);
            if (handSocket != null) handTransform = handSocket.transform;
        }

        if (handTransform == null)
        {
            SetFullBodyView();
            yield break;
        }

        if (characterController != null)
        {
            if (!characterController.IsCharacterFacingFront)
            {
                characterController.ResetCharacterRotationSmooth();
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

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
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