using System;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager Instance { get; private set; }

    public static event Action OnEnemyHit;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(Instance);
    }

    public static void TriggerEnemyHit()
    {
        OnEnemyHit?.Invoke();
    }
}
