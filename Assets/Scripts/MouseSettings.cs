using UnityEngine;

public enum SensitivityType
{
    General,    // 전체 감도
    Horizontal, // 수평 감도
    Vertical    // 수직 감도
}

public static class MouseSettings
{
    private const float DEFAULT_SENSITIVITY = 1.0f;

    // 감도 타입에 따른 PlayerPrefs 키 반환
    private static string GetKey(SensitivityType type)
    {
        return type switch
        {
            SensitivityType.General => "MouseSensitivity_General",
            SensitivityType.Horizontal => "MouseSensitivity_Horizontal",
            SensitivityType.Vertical => "MouseSensitivity_Vertical",
            _ => "MouseSensitivity_General"
        };
    }

    // 감도 값 가져오기
    public static float GetSensitivity(SensitivityType type)
    {
        return PlayerPrefs.GetFloat(GetKey(type), DEFAULT_SENSITIVITY);
    }

    // 감도 값 설정하기 (중복 코드 제거)
    public static void SetSensitivity(SensitivityType type, float value)
    {
        float roundedValue = Mathf.Round(value * 1000f) / 1000f;
        PlayerPrefs.SetFloat(GetKey(type), roundedValue);
    }

    // 변경된 데이터를 디스크에 최종 저장 (상용화 필수)
    public static void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}