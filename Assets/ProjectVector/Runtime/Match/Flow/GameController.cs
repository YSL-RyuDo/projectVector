using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance
    {
        get;
        private set;
    }

    public enum GameFlowState
    {
        OutsideGame,
        Pick,
        Loading,
        Playing
    }

    [Header("Scene")]
    [SerializeField]
    private string gameSceneName = "Game_TestVersion";

    public GameObject settingsUI;

    public bool isLoading;

    public bool IsPaused
    {
        get;
        private set;
    }

    public GameFlowState CurrentState
    {
        get;
        private set;
    } = GameFlowState.OutsideGame;

    /*
     * ESC 설정창은 실제 인게임 상태에서만 사용할 수 있다.
     */
    public bool CanOpenSettings =>
        CurrentState == GameFlowState.Playing &&
        !isLoading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        isLoading = false;
        IsPaused = false;
        Time.timeScale = 1f;

        ApplyStateFromCurrentScene();
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (ProjectVector.Core.SceneCatalog.IsMatch(scene.name) || scene.name == gameSceneName)
        {
            /*
             * GameScene 진입 직후에는 아직 픽 또는 로딩 단계다.
             * GAME_ALL_READY가 오기 전까지 Playing으로 두지 않는다.
             */
            EnterPickPhase();
        }
        else
        {
            EnterOutsideGamePhase();
        }
    }

    private void ApplyStateFromCurrentScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        if (ProjectVector.Core.SceneCatalog.IsMatch(currentScene.name) || currentScene.name == gameSceneName)
        {
            EnterPickPhase();
        }
        else
        {
            EnterOutsideGamePhase();
        }
    }

    public void EnterOutsideGamePhase()
    {
        CurrentState =
            GameFlowState.OutsideGame;

        isLoading = false;
        IsPaused = false;
        Time.timeScale = 1f;

        ShowUICursor();

        Debug.Log(
            "[GAME FLOW] OutsideGame"
        );
    }

    public void EnterPickPhase()
    {
        CurrentState =
            GameFlowState.Pick;

        isLoading = false;
        IsPaused = false;
        Time.timeScale = 1f;

        /*
         * 픽 화면에서는 마우스가 반드시 보여야 한다.
         */
        ShowUICursor();

        Debug.Log(
            "[GAME FLOW] Pick"
        );
    }

    public void EnterLoadingPhase()
    {
        CurrentState =
            GameFlowState.Loading;

        isLoading = true;
        IsPaused = false;
        Time.timeScale = 1f;

        /*
         * 로딩 화면에서도 커서를 잠그지 않는다.
         */
        ShowUICursor();

        Debug.Log(
            "[GAME FLOW] Loading"
        );
    }

    public void EnterPlayingPhase()
    {
        CurrentState =
            GameFlowState.Playing;

        isLoading = false;
        IsPaused = false;
        Time.timeScale = 1f;

        /*
         * 설정창이 닫혀 있을 때만 게임 커서를 잠근다.
         */
        bool settingsOpen =
            SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen;

        if (!settingsOpen)
        {
            LockGameplayCursor();
        }

        Debug.Log(
            "[GAME FLOW] Playing"
        );
    }

    public void EnsureGameplayRunning()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void LockGameplayCursor()
    {
        if (SettingsMenu.PreserveOpenMenuCursor())
            return;

        if (!CanOpenSettings)
            return;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    public void ShowUICursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
