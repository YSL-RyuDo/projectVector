using UnityEngine;
using UnityEngine.UI;

public class TESTPlayerHP : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;

    [Header("HP UI")]
    [SerializeField] private Slider hpSlider;

    private int currentHP;

    public int CurrentHP => currentHP;

    private void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    public void Damage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (hpSlider != null)
            hpSlider.value = currentHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
    }
}