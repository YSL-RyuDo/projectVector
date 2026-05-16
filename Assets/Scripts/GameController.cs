using UnityEngine;

public class GameController : MonoBehaviour
{
    // 외부에서 접근할 수 있는 싱글톤 인스턴스
    public static GameController Instance { get; private set; }

    public GameObject settingsUI;

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
        // 수동 연결 대신, 다른 씬으로 넘어온 SettingsMenu의 부모 오브젝트를 자동으로 찾아서 연결합니다.
        if (SettingsMenu.Instance != null)
        {
            // SettingsMenu가 붙어있는 최상위 오브젝트(Canvas)를 지정
            settingsUI = SettingsMenu.Instance.transform.root.gameObject;

            // 시작할 때는 UI를 꺼둡니다.
            settingsUI.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    void PauseGame()
    {
        IsPaused = true;
        settingsUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        IsPaused = false;
        settingsUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}