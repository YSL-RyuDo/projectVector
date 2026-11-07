using System.Collections;
using UnityEngine;

public class EntryDashMotor : MonoBehaviour
{
    [Header("Camera Height")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _dashCameraY = 0.75f;
    [SerializeField] private float _standCameraY = 1.2f;
    [SerializeField] private float _cameraSmoothSpeed = 15f;

    private CharacterController _cc;
    private Animator _animator;
    private PlayerMovement _movement;

    private Coroutine _dashCoroutine;
    private bool _isDashing;
    private bool _movesLocally;
    private bool _previousCanMove;

    private float _originalAnimatorSpeed = 1f;
    private Vector3 _originalCameraLocalPosition;
    private bool _hasCameraOriginal;

    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _cc =
            GetComponent<CharacterController>();

        _animator =
            GetComponentInChildren<Animator>(true);

        _movement =
            GetComponent<PlayerMovement>();

        if (_cameraTransform == null)
        {
            FPSCameraLook camLook =
                GetComponentInChildren<FPSCameraLook>(true);

            if (camLook != null)
                _cameraTransform = camLook.transform;
        }

        if (_cameraTransform != null)
        {
            _originalCameraLocalPosition =
                _cameraTransform.localPosition;

            _hasCameraOriginal = true;
        }
    }

    public void PlayDash(
        Vector3 direction,
        float dashForce,
        float dashDuration)
    {
        GetComponent<EntrySkillPresentation>()?.DashConfirmed();
        if (_dashCoroutine != null)
            ForceEndDash();

        // RemoteMotionBuffer is the sole position writer for remote actors.
        // Replaying the impulse remotely fights received snapshots and snaps back.
        _movesLocally = _movement != null && _movement.IsMine;

        _dashCoroutine =
            StartCoroutine(
                DashRoutine(
                    direction,
                    dashForce,
                    dashDuration
                )
            );
    }

    public void ForceEndDash()
    {
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
            _dashCoroutine = null;
        }

        EndDashState();
    }

    private IEnumerator DashRoutine(
        Vector3 direction,
        float dashForce,
        float dashDuration)
    {
        if (_movesLocally && _cc == null)
            yield break;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        direction.Normalize();

        _isDashing = true;

        if (_movesLocally && _movement != null)
        {
            _previousCanMove = _movement.CanMove;
            _movement.CanMove = false;
        }

        if (_animator != null)
        {
            _originalAnimatorSpeed =
                _animator.speed;

            _animator.SetBool("Crouch", true);
            _animator.SetBool("Crouch_Walk", true);
            _animator.speed = 2f;
        }

        float timer = 0f;

        while (timer < dashDuration)
        {
            float deltaTime =
                Time.deltaTime;

            if (_movesLocally && _cc.enabled)
            {
                // A purely horizontal Move clears CharacterController.isGrounded,
                // falsely broadcasting a jump during a ground dash. Maintain the
                // same small ground-contact velocity as normal locomotion; never
                // force a grounded animation while actually airborne.
                Vector3 displacement = direction * dashForce * deltaTime;
                if (_cc.isGrounded) displacement += Vector3.down * (2f * deltaTime);
                _cc.Move(displacement);
            }
            else if (_movesLocally)
            {
                transform.position +=
                    direction *
                    dashForce *
                    deltaTime;
            }

            if (_movesLocally) UpdateCameraHeight(true);

            timer += deltaTime;

            yield return null;
        }

        EndDashState();
    }

    private void EndDashState()
    {
        if (!_isDashing) return;
        _isDashing = false;

        if (_movesLocally && _movement != null)
            _movement.CanMove = _previousCanMove;

        if (_animator != null)
        {
            _animator.speed =
                _originalAnimatorSpeed;

            _animator.SetBool("Crouch", false);
            _animator.SetBool("Crouch_Walk", false);
        }

        if (_movesLocally && _hasCameraOriginal &&
            _cameraTransform != null)
        {
            _cameraTransform.localPosition =
                _originalCameraLocalPosition;
        }

        _dashCoroutine = null;

        Debug.Log(
            $"[ENTRY DASH END LOCAL] object={gameObject.name}"
        );
    }

    private void OnDisable() => ForceEndDash();

    private void UpdateCameraHeight(bool dash)
    {
        if (_cameraTransform == null)
            return;

        Vector3 current =
            _cameraTransform.localPosition;

        float targetY =
            dash
                ? _dashCameraY
                : _standCameraY;

        float nextY =
            Mathf.Lerp(
                current.y,
                targetY,
                Time.deltaTime * _cameraSmoothSpeed
            );

        _cameraTransform.localPosition =
            new Vector3(
                current.x,
                nextY,
                current.z
            );
    }
}
