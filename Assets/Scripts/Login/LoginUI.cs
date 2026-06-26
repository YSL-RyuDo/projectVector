using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum LoginMode
{
    Login,
    Register
}

public class LoginUI : MonoBehaviour
{
    // =========================
    // 1. 변수
    // =========================

    public LoginMode mode = LoginMode.Login;

    public GameObject loginImagePanel;

    [Header("Inputs")]
    public TMP_InputField idInput;
    public TMP_InputField nicknameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Buttons")]
    public Button sendButton;
    public Button switchButton;
    public Button enterButton;

    [Header("Images")]
    public Image sendImage;
    public Sprite loginSendSprite;
    public Sprite registerSendSprite;

    public Image switchImage;
    public Sprite toRegisterSprite;
    public Sprite toLoginSprite;

    [Header("Popup")]
    public GameObject popupImage;
    public TMP_Text errorText;

    private bool isLoginSuccess = false;

    // =========================
    // 2. 초기화
    // =========================

    void Start()
    {
        NetworkManager.Instance.SetLoginUI(this);

        SetMode(LoginMode.Login);

        enterButton.gameObject.SetActive(false);
        errorText.gameObject.SetActive(false);

        if (popupImage != null)
            popupImage.SetActive(false);
    }

    // =========================
    // 3. 입력 처리
    // =========================

    void Update()
    {
        // TAB 이동
        if (Input.GetKeyDown(KeyCode.Tab))
            HandleTab();

        // ENTER 실행
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isLoginSuccess)
                OnClickSend();
            else
                OnClickEnter();
        }

        // ESC 팝업 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (popupImage.activeSelf)
                OnClickClosePopup();
        }
    }

    // =========================
    // 4. 모드 제어
    // =========================

    public void SetMode(LoginMode newMode)
    {
        mode = newMode;

        bool isRegister = (mode == LoginMode.Register);

        nicknameInput.gameObject.SetActive(isRegister);
        confirmPasswordInput.gameObject.SetActive(isRegister);

        sendImage.sprite = isRegister ? registerSendSprite : loginSendSprite;
        switchImage.sprite = isRegister ? toLoginSprite : toRegisterSprite;

        errorText.gameObject.SetActive(false);

        FocusFirstInput();
    }

    public void OnClickSwitch()
    {
        LoginMode nextMode = mode == LoginMode.Login
            ? LoginMode.Register
            : LoginMode.Login;

        SetMode(nextMode);

        if (nextMode == LoginMode.Register)
        {
            if (popupImage != null)
                popupImage.SetActive(true);

            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }
    }

    // =========================
    // 5. 버튼 동작
    // =========================

    public void OnClickSend()
    {
        if (mode == LoginMode.Login)
            TryLogin();
        else
            TryRegister();
    }

    public void OnClickEnter()
    {
        if (!isLoginSuccess)
            return;

        SceneManager.LoadScene("Lobby");
    }

    public void OnClickClosePopup()
    {
        popupImage.SetActive(false);
    }

    // =========================
    // 6. 서버 요청
    // =========================

    void TryLogin()
    {
        string id = idInput.text.Trim();
        string pw = passwordInput.text;

        if (id == "" || pw == "")
        {
            ShowError("아이디 / 비밀번호 입력");
            return;
        }

        NetworkManager.Instance.SendLogin(id, pw);
    }

    void TryRegister()
    {
        string id = idInput.text.Trim();
        string nick = nicknameInput.text.Trim();
        string pw = passwordInput.text;
        string confirm = confirmPasswordInput.text;

        if (pw != confirm)
        {
            ShowError("비밀번호 불일치");
            return;
        }

        NetworkManager.Instance.SendRegister(id, nick, pw, confirm);
    }

    // =========================
    // 7. 서버 응답
    // =========================

    public void OnLoginResult(bool success)
    {
        if (!success)
        {
            ShowError("로그인 실패");
            return;
        }

        if (popupImage != null)
            popupImage.SetActive(false);

        isLoginSuccess = true;
        enterButton.gameObject.SetActive(true);

        if (loginImagePanel != null)
            loginImagePanel.SetActive(false);
    }

    public void OnRegisterResult(bool success)
    {
        if (!success)
        {
            ShowError("회원가입 실패");
            return;
        }

        ClearInputs();
        SetMode(LoginMode.Login);
    }

    // =========================
    // 8. UI 처리
    // =========================

    public void ShowError(string msg)
    {
        popupImage.SetActive(true);
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }

    void ClearInputs()
    {
        idInput.text = "";
        nicknameInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }

    void FocusFirstInput()
    {
        idInput.Select();
        idInput.ActivateInputField();
    }

    // =========================
    // 9. TAB 이동
    // =========================

    void HandleTab()
    {
        if (mode == LoginMode.Login)
        {
            if (idInput.isFocused)
            {
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }
            else
            {
                idInput.Select();
                idInput.ActivateInputField();
            }
        }
        else
        {
            if (idInput.isFocused)
            {
                nicknameInput.Select();
                nicknameInput.ActivateInputField();
            }
            else if (nicknameInput.isFocused)
            {
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }
            else if (passwordInput.isFocused)
            {
                confirmPasswordInput.Select();
                confirmPasswordInput.ActivateInputField();
            }
            else
            {
                idInput.Select();
                idInput.ActivateInputField();
            }
        }
    }
}