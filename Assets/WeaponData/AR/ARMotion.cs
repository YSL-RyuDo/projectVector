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
            StartCoroutine(reLoadTime(3f));
        }
    }

    private IEnumerator reLoadTime(float time)
    {
        isReloading = true;
        _animator.SetTrigger("ReLoad");

        yield return new WaitForSeconds(time);

        isReloading = false;
    }
}