using TMPro;
using UnityEngine;

public class ChatPanelController : MonoBehaviour
{
    [Header("Chat")]
    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

    [Header("Chat Panel")]
    [SerializeField] private GameObject _chatPanel;
    [SerializeField] private GameObject _chatButton;

    private void Start()
    {
        NetworkManager.Instance.SetChatPanelController(this);
    }

    public void OnRecvLobbyChat(uint playerId, string nickname, string message)
    {
        if (ChatPrefeb == null || ChatSlotContent == null)
            return;

        GameObject item = Instantiate(ChatPrefeb, ChatSlotContent);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.text = nickname + " : " + message;
    }

    public void OnClickLobbyChat()
    {
        string msg = ChatInputField.text.Trim();

        if (msg == "")
            return;

        NetworkManager.Instance.SendLobbyChat(msg);
        ChatInputField.text = "";
    }

    public void OnChatPanel()
    {
        if (_chatPanel.activeSelf)
        {
            _chatPanel.GetComponent<UIAnimator>().Close();
            _chatButton.GetComponent<UIAnimator>().Show();
        }
        else
        {
            _chatPanel.GetComponent<UIAnimator>().Show();
            _chatButton.GetComponent<UIAnimator>().Close();
        }
    }
}
