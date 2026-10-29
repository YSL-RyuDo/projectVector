using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance
    {
        get;
        private set;
    }

    [Serializable]
    public struct SensitivityUIElements
    {
        public SensitivityType type;
        public Slider slider;
        public TMP_InputField inputField;
    }

    [Header("Game Scene")]
    [Tooltip("실제 인게임 씬 이름을 확장자 없이 입력")]
    [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("Settings Panel")]
    [Tooltip("실제로 켜고 끌 자식 설정 패널")]
    [SerializeField]
    private GameObject settingsPanel;

    [Header("Sensitivity Settings UI")]
    [SerializeField]
    private List<SensitivityUIElements>
        sensitivityUIList;

    private bool isUpdatingUI;
    private bool listenersInitialized;

    private Coroutine restoreCursorCoroutine;

    public bool IsOpen
    {
        get
        {
            return settingsPanel != null &&
                   settingsPanel.activeSelf;
        }
    }

    // Respawn/view/skill resets restore gameplay, not the user's menu ownership.
    public static bool PreserveOpenMenuCursor()
    {
        if (Instance == null || !Instance.IsOpen)
            return false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!IsCurrentGameScene())
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            return;
        }

        InitializeUI();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (GameController.Instance != null)
        {
            if (GameController.Instance.CanOpenSettings)
            {
                GameController.Instance
                    .LockGameplayCursor();
            }
            else
            {
                GameController.Instance
                    .ShowUICursor();
            }
        }
    }

    private void Update()
    {
        if (VectorVoicePanel.BlocksGameplayInput) return;
        if (!CanUseSettingsMenu())
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (!keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (InGameChatUI.TryCloseChatByEscape())
            return;

        ToggleSettingsMenu();
    }

    private void OnDestroy()
    {
        MouseSettings.SaveSettings();

        if (restoreCursorCoroutine != null)
        {
            StopCoroutine(
                restoreCursorCoroutine
            );

            restoreCursorCoroutine = null;
        }

        if (GameController.Instance != null &&
            GameController.Instance.settingsUI ==
            settingsPanel)
        {
            GameController.Instance.settingsUI = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        if (Application.isPlaying)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }

    private bool IsCurrentGameScene()
    {
        Scene activeScene =
            SceneManager.GetActiveScene();

        return activeScene.name ==
               gameSceneName;
    }

    private bool CanUseSettingsMenu()
    {
        if (!IsCurrentGameScene())
            return false;

        if (GameController.Instance == null)
            return false;

        // An already-open menu must remain dismissible during state restoration.
        return IsOpen || GameController.Instance.CanOpenSettings;
    }

    public void ToggleSettingsMenu()
    {
        if (!CanUseSettingsMenu())
            return;

        if (IsOpen)
            CloseSettingsMenu();
        else
            OpenSettingsMenu();
    }

    public void OpenSettingsMenu()
    {
        if (!CanUseSettingsMenu())
            return;

        if (settingsPanel == null)
        {
            Debug.LogError(
                "[SETTINGS] Settings Panel이 연결되지 않았습니다."
            );

            return;
        }

        if (restoreCursorCoroutine != null)
        {
            StopCoroutine(
                restoreCursorCoroutine
            );

            restoreCursorCoroutine = null;
        }

        if (GameController.Instance != null)
        {
            GameController.Instance
                .EnsureGameplayRunning();
        }

        settingsPanel.SetActive(true);

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Debug.Log(
            "[SETTINGS] opened"
        );
    }

    public void CloseSettingsMenu()
    {
        MouseSettings.SaveSettings();

        if (EventSystem.current != null)
        {
            GameObject selectedObject =
                EventSystem.current
                    .currentSelectedGameObject;

            if (selectedObject != null)
            {
                TMP_InputField inputField =
                    selectedObject
                        .GetComponent<TMP_InputField>();

                if (inputField != null)
                {
                    inputField
                        .DeactivateInputField();
                }
            }

            EventSystem.current
                .SetSelectedGameObject(null);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (GameController.Instance != null)
        {
            GameController.Instance
                .EnsureGameplayRunning();
        }

        if (restoreCursorCoroutine != null)
        {
            StopCoroutine(
                restoreCursorCoroutine
            );
        }

        restoreCursorCoroutine =
            StartCoroutine(
                RestoreCursorByCurrentState()
            );

        Debug.Log(
            "[SETTINGS] closed"
        );
    }

    private IEnumerator RestoreCursorByCurrentState()
    {
        Time.timeScale = 1f;

        yield return null;

        if (GameController.Instance != null &&
            GameController.Instance.CanOpenSettings)
        {
            GameController.Instance
                .LockGameplayCursor();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }

        restoreCursorCoroutine = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        if (!IsCurrentGameScene())
            return;

        // 설정창이 열려 있으면 UI 커서를 유지한다.
        if (IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // 실제 플레이 상태일 때만 커서를 잠근다.
        // 픽/로딩 상태에서는 커서를 계속 보여준다.
        if (GameController.Instance != null)
        {
            if (GameController.Instance.CanOpenSettings)
            {
                GameController.Instance.LockGameplayCursor();
            }
            else
            {
                GameController.Instance.ShowUICursor();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    private void InitializeUI()
    {
        if (listenersInitialized)
            return;

        if (sensitivityUIList == null)
            return;

        foreach (
            SensitivityUIElements element
            in sensitivityUIList)
        {
            if (element.slider == null ||
                element.inputField == null)
            {
                Debug.LogWarning(
                    $"[SETTINGS] UI reference missing. " +
                    $"type={element.type}"
                );

                continue;
            }

            float savedValue =
                MouseSettings.GetSensitivity(
                    element.type
                );

            savedValue =
                Mathf.Clamp(
                    savedValue,
                    element.slider.minValue,
                    element.slider.maxValue
                );

            savedValue =
                Mathf.Round(
                    savedValue * 1000f
                ) / 1000f;

            element.slider
                .SetValueWithoutNotify(
                    savedValue
                );

            element.inputField
                .SetTextWithoutNotify(
                    savedValue.ToString("F3")
                );

            SensitivityType currentType =
                element.type;

            Slider currentSlider =
                element.slider;

            TMP_InputField currentInputField =
                element.inputField;

            currentSlider
                .onValueChanged
                .AddListener(
                    value =>
                        OnSliderValueChanged(
                            currentType,
                            value,
                            currentInputField
                        )
                );

            currentInputField
                .onEndEdit
                .AddListener(
                    text =>
                        OnInputFieldEndEdit(
                            currentType,
                            text,
                            currentSlider,
                            currentInputField
                        )
                );
        }

        listenersInitialized = true;
    }

    private void OnSliderValueChanged(
        SensitivityType type,
        float value,
        TMP_InputField inputField)
    {
        if (isUpdatingUI)
            return;

        float roundedValue =
            Mathf.Round(
                value * 1000f
            ) / 1000f;

        MouseSettings.SetSensitivity(
            type,
            roundedValue
        );

        isUpdatingUI = true;

        inputField.SetTextWithoutNotify(
            roundedValue.ToString("F3")
        );

        isUpdatingUI = false;
    }

    private void OnInputFieldEndEdit(
        SensitivityType type,
        string text,
        Slider slider,
        TMP_InputField inputField)
    {
        if (isUpdatingUI)
            return;

        if (float.TryParse(
            text,
            out float newValue))
        {
            newValue =
                Mathf.Clamp(
                    newValue,
                    slider.minValue,
                    slider.maxValue
                );

            newValue =
                Mathf.Round(
                    newValue * 1000f
                ) / 1000f;

            MouseSettings.SetSensitivity(
                type,
                newValue
            );

            isUpdatingUI = true;

            slider.SetValueWithoutNotify(
                newValue
            );

            inputField.SetTextWithoutNotify(
                newValue.ToString("F3")
            );

            isUpdatingUI = false;
        }
        else
        {
            float savedValue =
                MouseSettings.GetSensitivity(
                    type
                );

            savedValue =
                Mathf.Clamp(
                    savedValue,
                    slider.minValue,
                    slider.maxValue
                );

            isUpdatingUI = true;

            slider.SetValueWithoutNotify(
                savedValue
            );

            inputField.SetTextWithoutNotify(
                savedValue.ToString("F3")
            );

            isUpdatingUI = false;
        }
    }
}
