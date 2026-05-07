using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RoomPanelUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Room Info")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text mapNameText;

    [Header("Team Slots")]
    [SerializeField] private RoomPlayerSlotUI[] aTeamSlots;
    [SerializeField] private RoomPlayerSlotUI[] bTeamSlots;
    [SerializeField] private RoomPlayerSlotUI[] observerSlots;

    [Header("Buttons")]
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button settingButton;

    [Header("Leave")]
    [SerializeField] private Button leaveButton;
    [SerializeField] private GameObject leaveConfirmPanel;
    [SerializeField] private Button leaveConfirmButton;
    [SerializeField] private Button leaveCancelButton;

    public Action OnConfirmLeave;

    private void Awake()
    {
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(ShowLeaveConfirmPanel);
        }

        if (leaveConfirmButton != null)
        {
            leaveConfirmButton.onClick.RemoveAllListeners();
            leaveConfirmButton.onClick.AddListener(() =>
            {
                HideLeaveConfirmPanel();
                OnConfirmLeave?.Invoke();
            });
        }

        if (leaveCancelButton != null)
        {
            leaveCancelButton.onClick.RemoveAllListeners();
            leaveCancelButton.onClick.AddListener(HideLeaveConfirmPanel);
        }

        HideLeaveConfirmPanel();
    }

    public void OpenRoom(RoomStateData data)
    {
        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (roomPanel != null)
            roomPanel.SetActive(true);

        ApplyRoomState(data);
    }

    public void ApplyRoomState(RoomStateData data)
    {
        ClearSlots();

        if (roomNameText != null)
            roomNameText.text = data.roomName;

        if (mapNameText != null)
            mapNameText.text = data.mapName;

        foreach (RoomMemberData member in data.members)
        {
            SetMemberSlot(member);
        }

        ApplyGameStartButton(data);
    }

    private void ClearSlots()
    {
        foreach (RoomPlayerSlotUI slot in aTeamSlots)
            slot.Clear();

        foreach (RoomPlayerSlotUI slot in bTeamSlots)
            slot.Clear();

        foreach (RoomPlayerSlotUI slot in observerSlots)
            slot.Clear();
    }

    private void SetMemberSlot(RoomMemberData member)
    {
        RoomPlayerSlotUI[] targetSlots = null;

        if (member.team == RoomSlotTeam.A)
            targetSlots = aTeamSlots;
        else if (member.team == RoomSlotTeam.B)
            targetSlots = bTeamSlots;
        else if (member.team == RoomSlotTeam.Observer)
            targetSlots = observerSlots;

        if (targetSlots == null)
            return;

        if (member.slotIndex >= targetSlots.Length)
            return;

        targetSlots[member.slotIndex].SetData(member);
    }

    private void ApplyGameStartButton(RoomStateData data)
    {
        if (gameStartButton == null)
            return;

        gameStartButton.gameObject.SetActive(data.isLeader);
        gameStartButton.interactable = data.isLeader && data.canStart;
    }

    public void HideRoom()
    {
        HideLeaveConfirmPanel();

        if (roomPanel != null)
            roomPanel.SetActive(false);
    }

    public bool IsRoomOpen()
    {
        return roomPanel != null && roomPanel.activeSelf;
    }

    public bool IsLeaveConfirmOpen()
    {
        return leaveConfirmPanel != null && leaveConfirmPanel.activeSelf;
    }

    public void ShowLeaveConfirmPanel()
    {
        if (leaveConfirmPanel != null)
            leaveConfirmPanel.SetActive(true);
    }

    public void HideLeaveConfirmPanel()
    {
        if (leaveConfirmPanel != null)
            leaveConfirmPanel.SetActive(false);
    }
}