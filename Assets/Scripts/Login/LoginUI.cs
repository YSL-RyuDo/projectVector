using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField idInput;
    public TMP_InputField nickInput;
    public TMP_InputField pwInput;
    public TMP_InputField pwCheckInput;

    public Toggle changeToggle;
    public Button sendButton;
    public TextMeshProUGUI resultText;

    void Start()
    {
        NetworkManager.Instance.SetLoginUI(this);
        sendButton.onClick.AddListener(OnClickSend);
        changeToggle.onValueChanged.AddListener(OnToggleChanged);

        OnToggleChanged(changeToggle.isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        nickInput.gameObject.SetActive(isOn);
        pwCheckInput.gameObject.SetActive(isOn);
    }

    void OnClickSend()
    {
        if (changeToggle.isOn)
        {
            NetworkManager.Instance.SendRegister(
                idInput.text,
                nickInput.text,
                pwInput.text,
                pwCheckInput.text
            );
        }
        else
        {
            NetworkManager.Instance.SendLogin(
                idInput.text,
                pwInput.text
            );
        }
    }

    public void SetResult(string msg)
    {
        resultText.text = msg;
    }
}