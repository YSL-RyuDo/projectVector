using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Respawn")]
    public float respawnDelay = 3f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // 외부(무기, 서버 패킷 등)에서 호출
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"[Health] Damage: {damage}, Current: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("[PlayerHealth] Player Died");

        gameObject.SetActive(false);

        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    void Respawn()
    {
        Debug.Log("[PlayerHealth] Respawn");

        currentHealth = maxHealth;
        isDead = false;

        gameObject.SetActive(true);
    }
}
