using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [Header("UI Elements")]
    public Slider sensitivitySlider;
    public TMP_InputField sensitivityInputField; // 텍스트 표시와 입력을 모두 담당

    void Awake()
    {
        // 중복 생성 방지 및 싱글톤 세팅
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            // 이미 다른 씬에서 넘어온 설정창이 있다면, 새로 생성된 것은 파괴합니다.
            Destroy(transform.root.gameObject);
            return;
        }
    }

    void Start()
    {
        float savedSensitivity = MouseSettings.MouseSensitivity;

        // UI의 초기 상태를 불러온 값으로 설정
        sensitivitySlider.value = savedSensitivity;
        UpdateInputField(savedSensitivity);

        // 리스너 등록 (값이 바뀔 때마다 실행될 함수 연결)
        sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        sensitivityInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    // 슬라이더를 움직일 때 호출
    void OnSliderValueChanged(float value)
    {
        // 소수점 3째 자리로 제한하여 저장
        float roundedValue = Mathf.Round(value * 1000f) / 1000f;

        MouseSettings.MouseSensitivity = roundedValue;
        UpdateInputField(roundedValue);
    }

    // 인풋 필드 입력을 마쳤을 때 호출 (Enter 키 입력 또는 다른 곳 클릭 시)
    void OnInputFieldEndEdit(string text)
    {
        if (float.TryParse(text, out float newValue))
        {
            // 범위를 0.01 ~ 10 사이로 제한하고 소수점 3째 자리 반올림
            newValue = Mathf.Clamp(newValue, sensitivitySlider.minValue, sensitivitySlider.maxValue);
            newValue = Mathf.Round(newValue * 1000f) / 1000f;

            // 슬라이더 값 변경 (OnSliderValueChanged가 자동으로 연이어 실행됨)
            sensitivitySlider.value = newValue;
            MouseSettings.MouseSensitivity = newValue;
            UpdateInputField(newValue);
        }
        else
        {
            // 숫자가 아닌 잘못된 입력이 들어오면 기존에 저장되어 있던 값으로 복구
            UpdateInputField(MouseSettings.MouseSensitivity);
        }
    }

    // 인풋 필드의 텍스트만 소수점 3째 자리 포맷("F3")으로 업데이트
    void UpdateInputField(float value)
    {
        if (sensitivityInputField != null)
        {
            sensitivityInputField.text = value.ToString("F3");
        }
    }
}