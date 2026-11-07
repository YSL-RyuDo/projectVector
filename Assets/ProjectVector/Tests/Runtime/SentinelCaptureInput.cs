#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Only opted-in isolated QA uses virtual devices. Ordinary editor/development input is unchanged.
public static class SentinelCaptureInput
{
    static bool Active => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_OPT_IN")=="local-ranked-qa" &&
        Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SENTINEL")=="1";
    public static bool GetKeyDown(KeyCode key)
    {
        if(!Active)return UnityEngine.Input.GetKeyDown(key);
        var keyboard=Keyboard.current;if(keyboard==null)return false;
        switch(key){case KeyCode.C:return keyboard.cKey.wasPressedThisFrame;case KeyCode.Q:return keyboard.qKey.wasPressedThisFrame;
            case KeyCode.E:return keyboard.eKey.wasPressedThisFrame;case KeyCode.X:return keyboard.xKey.wasPressedThisFrame;
            case KeyCode.Escape:return keyboard.escapeKey.wasPressedThisFrame;default:return false;}
    }
    public static bool GetMouseButtonDown(int button)=>Active ? (button==0?Mouse.current?.leftButton.wasPressedThisFrame:Mouse.current?.rightButton.wasPressedThisFrame)==true : UnityEngine.Input.GetMouseButtonDown(button);
    public static bool GetMouseButton(int button)=>Active ? (button==0?Mouse.current?.leftButton.isPressed:Mouse.current?.rightButton.isPressed)==true : UnityEngine.Input.GetMouseButton(button);
}
#endif
