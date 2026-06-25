using System.Collections;
using UnityEngine;

public class ARMotion : MonoBehaviour
{
    private Animator _animator;
    private bool isReloading = false;
    private Weapon _weapon;

    private void Start()
    {
        _weapon = GetComponent<Weapon>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartCoroutine(reLoadTime(_weapon.GetWeaponReLoadTime()));
        }
    }

    private IEnumerator reLoadTime(float time)
    {
        isReloading = true;
        _animator.SetBool("ReLoad", true);

        yield return new WaitForSeconds(time);

        _animator.SetBool("ReLoad", false);
        isReloading = false;
    }
}