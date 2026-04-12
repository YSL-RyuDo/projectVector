using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField] private int MaxHP = 100;
    public int CurrentHP { get; private set; }

    public bool IsDead => CurrentHP <= 0;

    private void Awake()
    {
        CurrentHP = MaxHP;
    }

    public void SetHP(int hp)
    {
        CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
    }

    public void ResetHP()
    {
        CurrentHP = MaxHP;
    }
}