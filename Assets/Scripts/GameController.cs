using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    // 외부에서 접근할 수 있는 싱글톤 인스턴스
    public static GameController Instance { get; private set; }

    public GameObject settingsUI;

    public bool isLoading = false;

    // 외부에서는 읽기만 가능 (get), 내부에서만 수정 가능 (private set)
    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        // 싱글톤 세팅
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        TryFindSettingsUI();

        isLoading = false;
    }

    void Update()
    {
        if (isLoading) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 만약 다른 씬으로 넘어가면서 settingsUI 연결이 끊겼을 때를 대비한 안전장치
            if (settingsUI == null)
            {
                TryFindSettingsUI();
            }

            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    // SettingsMenu 싱글톤을 찾아 UI 오브젝트를 안전하게 매핑하는 함수
    private void TryFindSettingsUI()
    {
        if (SettingsMenu.Instance != null)
        {
            settingsUI = SettingsMenu.Instance.transform.root.gameObject;

            // 처음에는 꺼둔 상태로 설정 (켜져 있다면 꺼줌)
            if (!IsPaused && settingsUI.activeSelf)
            {
                settingsUI.SetActive(false);
            }
        }
    }

    void PauseGame()
    {
        IsPaused = true;

        if (settingsUI != null)
            settingsUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        IsPaused = false;

        if (SettingsMenu.Instance != null && settingsUI != null && settingsUI.activeSelf)
        {
            SettingsMenu.Instance.CloseSettingsMenu();
        }
        else if (settingsUI != null)
        {
            settingsUI.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}