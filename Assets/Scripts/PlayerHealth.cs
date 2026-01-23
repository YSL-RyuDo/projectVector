using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Respawn")]
    public float respawnDelay = 3f;
    public Transform respawnPoint;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (respawnPoint == null)
        {
            Debug.LogWarning("[PlayerHealth] Respawn Point not assigned!");
        }
    }

    // 외부에서 데미지 줄 때 호출
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

        // 이동/입력 막고 싶으면 여기서 처리 가능
        GetComponent<FPSPlayerMovement>().enabled = false;

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

        // 위치 이동
        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        // 체력 회복
        currentHealth = maxHealth;
        isDead = false;

        // 다시 이동 가능
        GetComponent<FPSPlayerMovement>().enabled = true;
    }
}
