using System;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager Instance { get; private set; }

    public static event Action OnEnemyHit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void TriggerEnemyHit()
    {
        int listenerCount = OnEnemyHit?.GetInvocationList().Length ?? 0;
        Debug.Log($"[HIT MANAGER] TriggerEnemyHit, listeners={listenerCount}");

        OnEnemyHit?.Invoke();
    }
}