using System.Collections;
using UnityEngine;

public class Entry_Toenail : MonoBehaviour
{
    private Coroutine _moveCoroutine;

    public void Init(
        Vector3 startPosition,
        Vector3 endPosition,
        float lifeTime)
    {
        InitFire(
            startPosition,
            endPosition,
            lifeTime
        );
    }

    public void InitFire(
        Vector3 startPosition,
        Vector3 endPosition,
        float lifeTime)
    {
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        DisablePhysicsForVisualOnly();

        _moveCoroutine =
            StartCoroutine(
                FireRoutine(
                    startPosition,
                    endPosition,
                    lifeTime
                )
            );
    }

    public void InitFloating(
        Transform owner,
        int index,
        int totalCount,
        float radius,
        float height,
        float rotateSpeed)
    {
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        DisablePhysicsForVisualOnly();

        _moveCoroutine =
            StartCoroutine(
                FloatingRoutine(
                    owner,
                    index,
                    totalCount,
                    radius,
                    height,
                    rotateSpeed
                )
            );
    }

    public void MakeEquippedVisualOnly()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        DisablePhysicsForVisualOnly();
    }

    private IEnumerator FloatingRoutine(
        Transform owner,
        int index,
        int totalCount,
        float radius,
        float height,
        float rotateSpeed)
    {
        if (owner == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float baseAngle =
            totalCount <= 0
                ? 0f
                : 360f / totalCount * index;

        while (owner != null)
        {
            float angle =
                baseAngle +
                Time.time * rotateSpeed;

            float rad =
                angle * Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(rad) * radius,
                    height,
                    Mathf.Sin(rad) * radius
                );

            transform.position =
                owner.position + offset;

            Vector3 lookDir =
                transform.position - owner.position;

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        lookDir.normalized,
                        Vector3.up
                    );
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator FireRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        float lifeTime)
    {
        lifeTime =
            Mathf.Max(
                0.01f,
                lifeTime
            );

        transform.position =
            startPosition;

        Vector3 dir =
            endPosition - startPosition;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    dir.normalized,
                    Vector3.up
                );
        }

        float timer = 0f;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / lifeTime
                );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisablePhysicsForVisualOnly()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody rb =
            GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}