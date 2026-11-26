using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlappyEnemy : MonoBehaviour
{
    public int maxHP = 30;
    public Slider hpSlider;
    Material mat;

    private int currentHP;
    private Color originalColor;
    private Coroutine hitCoroutine;

    void Start()
    {
        currentHP = maxHP;

        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
        hpSlider.gameObject.SetActive(false);

        mat = GetComponentInChildren<Renderer>().material;
        originalColor = mat.color;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitEffect());

        hpSlider.gameObject.SetActive(true);
        hpSlider.value = Mathf.Max(0, currentHP);

        if (currentHP <= 0)
        {
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