using UnityEngine;

// Authored in the scene from the exact MatchManager character prefab, including nested scales.
public sealed class PracticeTargetProfile : MonoBehaviour
{
    public GameObject gameplayPrefab;
    public Transform model;
    public Animator animator;
    public float groundedRootOffset;
    public float worldCapsuleHeight;
    public void SetMoving(bool moving)
    {
        if(animator!=null && animator.runtimeAnimatorController!=null) animator.SetFloat("MoveState",moving?7:6);
    }

    private void OnEnable()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.Rebind();
        // Same standing long-gun state used by PlayerMovement; no network/player scripts on targets.
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == "WeaponType") animator.SetInteger(parameter.nameHash, 1);
            if (parameter.name == "MoveState") animator.SetFloat(parameter.nameHash, 6);
            if (parameter.name == "IsAiming") animator.SetBool(parameter.nameHash, false);
        }
        animator.Update(0);
    }
}
