using System.Collections;
using UnityEngine;

public class KnifeMotion : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.5f;

    private Animator animator;
    private PlayerIdentity ownerIdentity;
    private Coroutine attackCoroutine;
    private bool isAttacking;

    private void Awake()
    {
        animator =
            GetComponentInChildren<Animator>(true);

        ownerIdentity =
            GetComponentInParent<PlayerIdentity>();
    }

    private void OnEnable()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.SetBool(
                "AttackReady",
                true
            );
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
    }

    private void Update()
    {
        if (!IsLocalOwner())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            PlayAttack();
        }
    }

    private bool IsLocalOwner()
    {
        if (ownerIdentity == null)
        {
            ownerIdentity =
                GetComponentInParent<PlayerIdentity>();
        }

        if (ownerIdentity == null)
            return false;

        if (NetworkManager.Instance == null)
            return false;

        return ownerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    public void PlayAttack()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (isAttacking)
            return;

        attackCoroutine =
            StartCoroutine(
                AttackRoutine()
            );
    }


    public void PlayAttackFromServer()
    {
        PlayAttack();
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.ResetTrigger(
                "Attack"
            );

            animator.SetTrigger(
                "Attack"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[KNIFE MOTION] Animator not found. object={name}"
            );
        }

        yield return new WaitForSeconds(
            attackCooldown
        );

        isAttacking = false;
        attackCoroutine = null;
    }
}