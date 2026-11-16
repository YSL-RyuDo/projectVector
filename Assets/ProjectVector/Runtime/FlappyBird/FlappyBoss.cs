using UnityEngine;

public class FlappyBoss : MonoBehaviour
{
    public int maxHP = 100;

    private int currentHP;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        Debug.Log("보스 남은 체력: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        FlappyGameManager gameManager =
            FindFirstObjectByType<FlappyGameManager>();

        if (gameManager != null)
        {
            gameManager.BossDefeated();
        }

        Destroy(gameObject);
    }
}