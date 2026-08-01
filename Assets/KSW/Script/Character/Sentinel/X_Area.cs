using System.Collections;
using UnityEngine;

public class X_Area : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float radius = 7f;
    [SerializeField] private float slowMultiplier = 0.5f;

    [SerializeField] private LineRenderer lightningLine;
    [SerializeField] private int segments = 12;
    [SerializeField] private float lightningHeight = 15f;
    [SerializeField] private float jitterAmount = 1.2f;

    private uint ultimateId;
    private int ownerTeam = -1;
    private bool networkInitialized;
    private bool localSlowApplied;
    private PlayerMovement localMovement;

    public uint UltimateId => ultimateId;

    private void Start()
    {
        if (lightningLine == null)
            lightningLine = GetComponentInChildren<LineRenderer>();

        if (lightningLine != null)
        {
            lightningLine.useWorldSpace = true;
            lightningLine.positionCount = segments;

            if (lightningLine.startWidth == 0f)
                lightningLine.startWidth = 0.2f;

            lightningLine.enabled = false;
            StartCoroutine(LightningVisualRoutine());
        }

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        if (!networkInitialized)
            return;

        UpdateLocalSlow();
    }

    public void InitNetwork(
        uint networkUltimateId,
        int networkOwnerTeam,
        Vector3 position,
        float networkRadius,
        float networkDuration,
        float networkSlowMultiplier)
    {
        ultimateId = networkUltimateId;
        ownerTeam = networkOwnerTeam;
        radius = networkRadius;
        duration = networkDuration;
        slowMultiplier =
            Mathf.Clamp(networkSlowMultiplier, 0.05f, 1f);
        networkInitialized = true;

        transform.position = position;
    }

    public void DestroyFromServer()
    {
        RemoveLocalSlow();
        Destroy(gameObject);
    }

    private void UpdateLocalSlow()
    {
        PlayerIdentity localPlayer =
            NetworkManager.Instance != null
                ? NetworkManager.Instance.MyPlayer
                : null;

        bool shouldSlow = false;

        if (localPlayer != null &&
            localPlayer.Team != ownerTeam)
        {
            PlayerHP hp = localPlayer.GetComponent<PlayerHP>();

            shouldSlow =
                (hp == null || !hp.IsDead) &&
                (
                    localPlayer.transform.position -
                    transform.position
                ).sqrMagnitude <= radius * radius;

            if (shouldSlow && localMovement == null)
            {
                localMovement =
                    localPlayer.GetComponent<PlayerMovement>();

                if (localMovement == null)
                {
                    localMovement =
                        localPlayer.GetComponentInChildren<PlayerMovement>(
                            true
                        );
                }
            }
        }

        if (shouldSlow &&
            !localSlowApplied &&
            localMovement != null)
        {
            localMovement.ApplySwampSlow(slowMultiplier);
            localSlowApplied = true;
        }
        else if (!shouldSlow && localSlowApplied)
        {
            RemoveLocalSlow();
        }
    }

    private void RemoveLocalSlow()
    {
        if (!localSlowApplied)
            return;

        if (localMovement != null)
            localMovement.RemoveSwampSlow();

        localSlowApplied = false;
        localMovement = null;
    }

    private IEnumerator LightningVisualRoutine()
    {
        while (true)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle * radius;

            Vector3 targetPosition =
                transform.position +
                new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y
                );

            Vector3 startPosition =
                targetPosition + Vector3.up * lightningHeight;

            DrawLightning(startPosition, targetPosition);

            lightningLine.enabled = true;
            yield return new WaitForSeconds(0.06f);
            lightningLine.enabled = false;

            yield return new WaitForSeconds(
                Random.Range(0.05f, 0.15f)
            );
        }
    }

    private void DrawLightning(Vector3 start, Vector3 end)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 position = Vector3.Lerp(start, end, t);

            if (i > 0 && i < segments - 1)
                position += Random.insideUnitSphere * jitterAmount;

            lightningLine.SetPosition(i, position);
        }
    }

    private void OnDestroy()
    {
        RemoveLocalSlow();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
