using UnityEngine;

public static class MouseSettings
{
    // 기본값 1.0, PlayerPrefs로 저장 및 로드
    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        set => PlayerPrefs.SetFloat("MouseSensitivity", value);
    }
}