using UnityEngine;
using UnityEngine.UI;

public class RoomCreateButton : MonoBehaviour
{
    [SerializeField] private Button createRoomButton;

    private void Awake()
    {
        createRoomButton.onClick.RemoveAllListeners();
        createRoomButton.onClick.AddListener(OnClickCreateRoom);
    }

    private void OnClickCreateRoom()
    {
        NetworkManager.Instance.SendRoomCreate();
    }
}