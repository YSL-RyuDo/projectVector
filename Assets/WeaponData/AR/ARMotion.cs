using System.Collections;
using UnityEngine;

public class ARMotion : MonoBehaviour
{
    private Animator _animator;
    private bool isReloading = false;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartCoroutine(reLoadTime(2.4f));
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