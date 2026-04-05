using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using System.Text.RegularExpressions;
using System.Collections;


public class LoginUI : MonoBehaviour
{

    [Header("Panels")]
    public GameObject LoginPanel;
    public GameObject RegisterPanel;

    [Header("Login")]
    public TMP_InputField loginIdInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register")]
    public TMP_InputField RegisterIdInput;
    public TMP_InputField RegisterNicknameInput;
    public TMP_InputField RegisterPasswordInput;
    public TMP_InputField RegisterConfirmPasswordInput;

    [Header("Texts")]
    public TMP_Text resultText;
    public TMP_Text statusText;

    [Header("Loading")]
    public GameObject loadingObject;

    public void SetResult(string message)
    {
        resultText.text = message;
    }

    public void SetStatus(string message)
    {
        statusText.text = message;
    }

    public void OnClickLogin()
    {


        string ID = loginIdInput.text.Trim();
        string PW = loginPasswordInput.text; //공백여부 몰름아직 

        if (ID == "")
        {
            resultText.text = "아이디를 입력해주세요.";
            return;
        }

        if (PW == "")
        {
            resultText.text = "비밀번호를 입력해주세요.";
            return;
        }

        if (ID.Length < 4)
        {
            resultText.text = "입력값이 올바르지 않습니다";
            return;
        }

        if (PW.Length < 8 || PW.Length > 64)
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        if (!(Regex.IsMatch(PW, @"[a-zA-Z]")))
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        if (!(Regex.IsMatch(PW, @"[0-9]")))
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }


        //형식 검사 부분
        if (ID.Length < 4 || ID.Length > 20)
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        for (int i = 0; i < ID.Length; i++)
        {
            char c = ID[i];

            bool ABC = (c >= 'A' && c <= 'Z');
            bool abc = (c >= 'a' && c <= 'z');
            bool number = (c >= '0' && c <= '9');
            bool underbar = (c == '_');

            if (!(ABC || abc || number || underbar))
            {
                resultText.text = "입력값이 올바르지 않습니다.";
                return;
            }
        }

        SetLoading(true);
        NetworkManager.Instance.SendLogin(ID, PW);
    }

    public void OnClickRegister()
    {

        string ID = RegisterIdInput.text.Trim();
        string Nickname = RegisterNicknameInput.text.Trim();
        string Password = RegisterPasswordInput.text;   //공백여부몰름
        string Confirm = RegisterConfirmPasswordInput.text;


        if (ID == "")
        {
            resultText.text = "아이디를 입력해주세요.";
            return;
        }

        if (Nickname == "")
        {
            resultText.text = "닉네임을 입력해주세요.";
            return;
        }

        if (Password == "")
        {
            resultText.text = "비밀번호를 입력해주세요.";
            return;
        }

        if (Confirm == "")
        {
            resultText.text = "비밀번호 확인을 입력해주세요.";
            return;
        }

        if (Confirm != Password)
        {
            resultText.text = "비밀번호가 일치하지 않습니다.";
            return;
        }

        //형식 검사 부분
        if (ID.Length < 4 || ID.Length > 20)
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        for (int i = 0; i < ID.Length; i++)
        {
            char c = ID[i];

            bool ABC = (c >= 'A' && c <= 'Z');
            bool abc = (c >= 'a' && c <= 'z');
            bool number = (c >= '0' && c <= '9');
            bool underbar = (c == '_');

            if (!(ABC || abc || number || underbar))
            {
                resultText.text = "입력값이 올바르지 않습니다.";
                return;
            }
        }

        if (Nickname.Length < 2 || Nickname.Length > 16)
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        if (Password.Length < 8 || Password.Length > 64)
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        if (!(Regex.IsMatch(Password, @"[a-zA-Z]")))
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        if (!(Regex.IsMatch(Password, @"[0-9]")))
        {
            resultText.text = "입력값이 올바르지 않습니다.";
            return;
        }

        SetLoading(true);
        NetworkManager.Instance.SendRegister(ID, Nickname, Password, Confirm);

    }

    public void SwitchToLoginMode()
    {
        LoginPanel.SetActive(true);
        RegisterPanel.SetActive(false);
        statusText.text = "로그인모드";
        //SetResult("");
    }

    public void SwitchToRegisterMode()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(true);
        statusText.text = "회원가입 모드";
        //SetResult("");
    }


    public void SetLoading(bool active)
    {
        
        if (loadingObject != null)
        {
            loadingObject.SetActive(active);
        }
    }

    public void ClearInputs()
    {
        loginIdInput.text = "";
        loginPasswordInput.text = "";
        RegisterIdInput.text = "";
        RegisterNicknameInput.text = "";
        RegisterPasswordInput.text = "";
        RegisterConfirmPasswordInput.text = "";
    }

    public void MoveToLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(false);
        Debug.Log("LoginUI Start, SetLoginUI 호출");
        NetworkManager.Instance.SetLoginUI(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoginPanel.SetActive(false);
            RegisterPanel.SetActive(false);
            statusText.text = "";
            resultText.text = "";
        }
    }
}
