using System;
using UnityEngine;

public class WeaponRotate : MonoBehaviour
{
    public static event Action RotateInit;
    public static void CallRotateInit()
    {
        RotateInit?.Invoke();
    }

    private void OnEnable()
    {
        RotateInit += RotateEvent;
        RotateEvent();
    }

    private void OnDisable()
    {
        RotateInit -= RotateEvent;
    }

    void Update()
    {
        transform.Rotate(0, 0.5f, 0);
    }

    private void RotateEvent()
    {
        transform.rotation = Quaternion.identity;
    }
}