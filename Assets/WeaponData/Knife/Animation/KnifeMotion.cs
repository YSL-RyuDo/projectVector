using System.Collections;
using UnityEngine;

public class KnifeMotion : MonoBehaviour
{
    private Animator _animator;

    private float _attackCooldown = 0.5f;

    private bool _isAttacking = false;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !_isAttacking)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        if (_animator != null) _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(_attackCooldown);

        _isAttacking = false;
    }
}