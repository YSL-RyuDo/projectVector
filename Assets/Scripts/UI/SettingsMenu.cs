using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    // 상용화 게임처럼 직관적인 UI 관리를 위한 구조체 정의
    [Serializable]
    public struct SensitivityUIElements
    {
        public SensitivityType type;
        public Slider slider;
        public TMP_InputField inputField;
    }

    [Header("Sensitivity Settings UI")]
    [SerializeField] private List<SensitivityUIElements> sensitivityUIList;

    // 값 변경 시 무한 루프(이벤트 핑퐁)를 방지하기 위한 플래그
    private bool isUpdatingUI = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(transform.root.gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeUI();
    }

    private void OnDisable()
    {
        CloseSettingsMenu();
    }

    private void InitializeUI()
    {
        foreach (var element in sensitivityUIList)
        {
            // 저장된 데이터 로드
            float savedValue = MouseSettings.GetSensitivity(element.type);

            // UI 초기화 (이벤트 실행 방지를 위해 플래그 설정)
            isUpdatingUI = true;
            element.slider.value = savedValue;
            element.inputField.text = savedValue.ToString("F3");
            isUpdatingUI = false;

            // 리스너 등록 (람다식을 활용해 어떤 타입의 UI가 변경되었는지 전달)
            SensitivityType currentType = element.type;
            Slider currentSlider = element.slider;
            TMP_InputField currentInputField = element.inputField;

            currentSlider.onValueChanged.AddListener((value) =>
                OnSliderValueChanged(currentType, value, currentInputField));

            currentInputField.onEndEdit.AddListener((text) =>
                OnInputFieldEndEdit(currentType, text, currentSlider, currentInputField));
        }
    }

    // 슬라이더 조작 시 호출
    private void OnSliderValueChanged(SensitivityType type, float value, TMP_InputField inputField)
    {
        if (isUpdatingUI) return;

        float roundedValue = Mathf.Round(value * 1000f) / 1000f;

        // 데이터 저장
        MouseSettings.SetSensitivity(type, roundedValue);

        // 인풋필드 동기화
        isUpdatingUI = true;
        inputField.text = roundedValue.ToString("F3");
        isUpdatingUI = false;
    }

    // 인풋필드 입력 종료 시 호출
    private void OnInputFieldEndEdit(SensitivityType type, string text, Slider slider, TMP_InputField inputField)
    {
        if (isUpdatingUI) return;

        if (float.TryParse(text, out float newValue))
        {
            // 슬라이더의 Min/Max 범위를 기준으로 클램핑
            newValue = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
            newValue = Mathf.Round(newValue * 1000f) / 1000f;

            // 데이터 저장
            MouseSettings.SetSensitivity(type, newValue);

            // 슬라이더 및 인풋필드 UI 동기화
            isUpdatingUI = true;
            slider.value = newValue;
            inputField.text = newValue.ToString("F3");
            isUpdatingUI = false;
        }
        else
        {
            // 잘못된 문자 입력 시 기존에 저장되어 있던 값으로 복구
            isUpdatingUI = true;
            inputField.text = MouseSettings.GetSensitivity(type).ToString("F3");
            isUpdatingUI = false;
        }
    }

    // 설정 창을 닫을 때 호출하는 함수 (배경 클릭, X 버튼, ESC 키 입력 등)
    public void CloseSettingsMenu()
    {
        // 변경된 모든 세팅값을 실제 디스크에 동기화 (최적화)
        MouseSettings.SaveSettings();

        // UI 비활성화 등의 연출 추가 가능
        gameObject.SetActive(false);
    }
}