#if UNITY_EDITOR
using System;
using UnityEngine;

// Test scene only: runs before the settings panel so virtual input is observed
// by normal MonoBehaviour.Update calls, outside Input System event processing.
[DefaultExecutionOrder(-11000)]
public sealed class VectorSettingsPlayDriver : MonoBehaviour
{
    public static Action Tick;
    void Awake()=>DontDestroyOnLoad(gameObject);
    void Update()=>Tick?.Invoke();
}
#endif
