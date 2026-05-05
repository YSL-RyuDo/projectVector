using UnityEngine;
using System.Collections;

public class X_Area : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float damagePerSecond = 50f;
    [SerializeField] private float radius = 7f;
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private LineRenderer lightningLine;
    [SerializeField] private int segments = 12;
    [SerializeField] private float lightningHeight = 15f;
    [SerializeField] private float jitterAmount = 1.2f;

    private void Start()
    {
        if (lightningLine == null) lightningLine = GetComponentInChildren<LineRenderer>();

        if (lightningLine != null)
        {
            lightningLine.useWorldSpace = true;
            lightningLine.positionCount = segments;
            if (lightningLine.startWidth == 0) lightningLine.startWidth = 0.2f;
            lightningLine.enabled = false;
        }

        Destroy(gameObject, duration);
        StartCoroutine(TickDamage());
        StartCoroutine(LightningVisualRoutine());
    }

    IEnumerator LightningVisualRoutine()
    {
        if (lightningLine == null) yield break;

        while (true)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 targetPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 startPos = targetPos + Vector3.up * lightningHeight;

            DrawLightning(startPos, targetPos);

            lightningLine.enabled = true;
            yield return new WaitForSeconds(0.06f);
            lightningLine.enabled = false;

            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }

    void DrawLightning(Vector3 start, Vector3 end)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            if (i > 0 && i < segments - 1)
            {
                pos += Random.insideUnitSphere * jitterAmount;
            }
            lightningLine.SetPosition(i, pos);
        }
    }

    IEnumerator TickDamage()
    {
        float tick = 0.2f;
        while (true)
        {
            // Àû °¨Áö
            Collider[] targets = Physics.OverlapSphere(transform.position, radius, enemyLayer);
            foreach (var t in targets)
            {
                EnemyStatus status = t.GetComponent<EnemyStatus>();
                if (status != null)
                {
                    status.TakeDamage(damagePerSecond * tick);
                    status.ApplySlow();
                }
            }
            yield return new WaitForSeconds(tick);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}