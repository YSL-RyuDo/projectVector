using UnityEngine;
using System.Collections;

public class EntrySkill_C : CharacterSkill
{
    private CharacterController _cc;
    private Animator _animator;

    [SerializeField] private float _dashWaitTime = 3.0f;
    [SerializeField] private float _dashForce = 35f;
    [SerializeField] private float _dashDuration = 0.3f;

    [SerializeField] private float _crouchY = 0.75f;
    [SerializeField] private float _standY = 1.2f;
    [SerializeField] private float _smoothSpeed = 10f;

    [SerializeField] private Transform _camera;

    private bool _isSkillActive = false;
    private Coroutine _skillCoroutine;

    private void Start()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !_isSkillActive)
        {
            UseSkill();
        }
        handleCameraHeight();

        if (_isSkillActive && _animator != null && _cc != null)
        {
            bool isMoving = _cc.velocity.magnitude > 0.1f;
            _animator.SetBool("Crouch_Walk", isMoving);
        }
    }

    protected override void UseSkill()
    {
        _skillCoroutine = StartCoroutine(SkillSequence());
    }

    private IEnumerator SkillSequence()
    {
        _isSkillActive = true;
        _animator.SetBool("Crouch", true);

        yield return new WaitForSeconds(_dashWaitTime);

        _animator.speed = 2f;

        float timer = 0f;
        Vector3 dashDir = transform.forward;
        dashDir.y = 0;
        dashDir.Normalize();

        while (timer < _dashDuration)
        {
            _cc.Move(dashDir * _dashForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _animator.speed = 1f;

        resetStates();
    }

    private void resetStates()
    {
        _isSkillActive = false;
        _animator.SetBool("Crouch", false);
        _animator.SetBool("Crouch_Walk", false);
        
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }
    }

    private void handleCameraHeight()
    {
        if (_camera == null) return;

        float targetY = _isSkillActive ? _crouchY : _standY;
        Vector3 currentPos = _camera.localPosition;
        float nextY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * _smoothSpeed);

        _camera.localPosition = new Vector3(currentPos.x, nextY, currentPos.z);
    }
}