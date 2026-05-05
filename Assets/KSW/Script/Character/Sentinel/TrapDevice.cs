using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrapDevice : MonoBehaviour
{
    public float health = 100f;
    public float effectRadius = 5f;
    public float expandSpeed = 20f;

    public LayerMask enemyLayer;
    public GameObject energyEffectPrefab;

    private void Start()
    {
        ActivateSkill();
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void ActivateSkill()
    {
        if (energyEffectPrefab != null)
        {
            StartCoroutine(ExpandAndDetect());
        }
    }

    private IEnumerator ExpandAndDetect()
    {
        GameObject sphere = Instantiate(energyEffectPrefab, transform.position, Quaternion.identity);
        sphere.transform.localScale = Vector3.zero;

        float targetScale = effectRadius * 2f;

        HashSet<EnemyStatus> hitEnemies = new HashSet<EnemyStatus>();

        while (sphere.transform.localScale.x < targetScale * 0.98f)
        {
            sphere.transform.localScale = Vector3.MoveTowards(
                sphere.transform.localScale,
                new Vector3(targetScale, targetScale, targetScale),
                expandSpeed * Time.deltaTime
            );

            float currentRadius = sphere.transform.localScale.x / 2f;
            Collider[] enemies = Physics.OverlapSphere(transform.position, currentRadius, enemyLayer);

            foreach (Collider col in enemies)
            {
                EnemyStatus status = col.GetComponent<EnemyStatus>();
                if (status != null && !hitEnemies.Contains(status))
                {
                    ApplyEffect(status);
                    hitEnemies.Add(status);
                }
            }

            yield return null;
        }

        Destroy(sphere, 0.1f);
    }

    private void ApplyEffect(EnemyStatus status)
    {
        if (status.IsSlowed)
        {
            status.ApplyConcussion(3f);
        }
        else
        {
            status.ApplySlow();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}