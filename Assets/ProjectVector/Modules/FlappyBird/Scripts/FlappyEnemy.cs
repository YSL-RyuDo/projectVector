using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlappyEnemy : MonoBehaviour
{
    public int maxHP = 30;
    public Slider hpSlider;
    public bool isObstacle;     // 파괴 가능 장애물이면 체크
    Material mat;

    private int currentHP;
    private Color originalColor;
    private Coroutine hitCoroutine;
    private bool isDead;

    void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
            hpSlider.gameObject.SetActive(false);
        }

        mat = GetComponentInChildren<Renderer>().material;
        originalColor = mat.color;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitEffect());

        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(true);
            hpSlider.value = Mathf.Max(0, currentHP);
        }

        if (currentHP <= 0)
        {
            isDead = true;

            if (isObstacle)
                ScoreManager.Instance.AddObstacleDestroy();
            else
                ScoreManager.Instance.AddEnemyKill();

            Destroy(gameObject);
        }
    }

    IEnumerator HitEffect()
    {
        mat.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        mat.color = originalColor;
        hitCoroutine = null;
    }
}