using TMPro;
using UnityEngine;

public class ChatPanelController : MonoBehaviour
{
    [Header("Chat")]
    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

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
}
