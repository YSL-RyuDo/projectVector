using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlappyBoss : MonoBehaviour
{
    public int maxHP = 100;
    public Slider hpSlider;

    private int currentHP;
    private Material mat;
    private Color originalColor;
    private Coroutine hitCoroutine;

    void Start()
    {
        currentHP = maxHP;

        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
        hpSlider.gameObject.SetActive(false);

        mat = GetComponent<Renderer>().material;
        originalColor = mat.color;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        // 체력바 표시
        hpSlider.gameObject.SetActive(true);
        hpSlider.value = Mathf.Max(0, currentHP);

        // 피격 효과
        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitEffect());

        // 사망 처리
        if (currentHP <= 0)
        {
            ScoreManager.Instance.AddEnemyKill();
            Die();
        }
    }

    IEnumerator HitEffect()
    {
        mat.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        mat.color = originalColor;
        hitCoroutine = null;
    }

    void Die()
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