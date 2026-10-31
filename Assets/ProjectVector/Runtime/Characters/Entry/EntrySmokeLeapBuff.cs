using System.Collections;
using UnityEngine;

public class EntrySmokeLeapBuff : MonoBehaviour
{
    private PlayerMovement _movement;
    private Coroutine _buffCoroutine;

    private void Awake()
    {
        _movement =
            GetComponent<PlayerMovement>();
    }

    public void ApplyLeapBuff(
        float duration,
        float speedMultiplier,
        float jumpMultiplier,
        float upForce)
    {
        if (_movement == null)
            _movement =
                GetComponent<PlayerMovement>();

        if (_buffCoroutine != null)
            StopCoroutine(_buffCoroutine);

        _buffCoroutine =
            StartCoroutine(
                BuffRoutine(
                    duration,
                    speedMultiplier
                )
            );
    }

    private IEnumerator BuffRoutine(
        float duration,
        float speedMultiplier)
    {
        if (_movement != null)
        {
            _movement.SetExternalSpeedMultiplier(
                speedMultiplier
            );
        }

        Debug.Log(
            $"[ENTRY SMOKE SPEED BUFF START] duration={duration}, speed={speedMultiplier}"
        );

        yield return new WaitForSeconds(duration);

        if (_movement != null)
        {
            _movement.SetExternalSpeedMultiplier(1.0f);
        }

        Debug.Log("[ENTRY SMOKE SPEED BUFF END]");

        _buffCoroutine = null;
    }
}