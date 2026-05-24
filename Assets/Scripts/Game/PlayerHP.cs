using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    public int CurrentHP { get; private set; }
    public int CurrentBarrier { get; private set; }

    public int MaxHP { get; private set; }
    public int MaxBarrier { get; private set; }

    public bool IsDead => CurrentHP <= 0;

    private void Awake()
    {
        MaxHP = 100;
        CurrentHP = 100;

        MaxBarrier = 0;
        CurrentBarrier = 0;
    }

    public void SetServerHP(
        int hp,
        int barrier,
        int maxHp,
        int maxBarrier)
    {
        MaxHP = Mathf.Max(1, maxHp);
        MaxBarrier = Mathf.Max(0, maxBarrier);

        CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
        CurrentBarrier = Mathf.Clamp(barrier, 0, MaxBarrier);
    }
}