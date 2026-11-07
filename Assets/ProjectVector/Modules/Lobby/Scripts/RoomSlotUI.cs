using TMPro;
using UnityEngine;

public class RoomSlotUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text mapNameText;
    public TMP_Text hostNameText;
    public TMP_Text playerCountText;
    public TMP_Text stateText;

    public void SetData(RoomListData room)
    {
        roomNameText.text = room.roomName;
        mapNameText.text = room.mapName;
        hostNameText.text = room.hostName;
        playerCountText.text = room.currentPlayerCount + " / " + room.maxPlayerCount;
        stateText.text = room.state == 0 ? "대기중" : "게임중";
    }
}