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

    // Ä®À» ¼±ÅÃ(È°¼ºÈ­)µÆÀ» ¶§ AttackReady·Î º¯°æ
    private void OnEnable()
    {
        if (_animator != null) _animator.SetBool("AttackReady", true);
    }

    // Ä® ÈÖµÎ¸£±â ÄðÅ¸ÀÓ
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        if (_animator != null) _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(_attackCooldown);

        _isAttacking = false;
    }
}