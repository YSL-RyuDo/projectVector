using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomPlayerSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject leaderImage;
    [SerializeField] private Button slotButton;
    public GameObject emptyLabel;

    private RoomSlotTeam slotTeam;
    private byte slotIndex;

    private Action<RoomSlotTeam, byte> onSlotLeftClick;
    private Action<RoomSlotTeam, byte> onSlotRightClick;

    public void InitSlot(
        RoomSlotTeam team,
        byte index,
        Action<RoomSlotTeam, byte> leftCallback,
        Action<RoomSlotTeam, byte> rightCallback)
    {
        slotTeam = team;
        slotIndex = index;
        onSlotLeftClick = leftCallback;
        onSlotRightClick = rightCallback;

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotButton == null)
            slotButton = GetComponentInChildren<Button>(true);

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
            {
                Debug.Log($"[ROOM SLOT LEFT CLICK] team={slotTeam}, slot={slotIndex}");
                onSlotLeftClick?.Invoke(slotTeam, slotIndex);
            });
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"[ROOM SLOT RIGHT CLICK] team={slotTeam}, slot={slotIndex}");
            onSlotRightClick?.Invoke(slotTeam, slotIndex);
        }
    }

    public void Clear()
    {
        if(emptyLabel!=null)emptyLabel.SetActive(true);
        gameObject.SetActive(true);

        if (image != null)
        {
            image.gameObject.SetActive(false);
            image.enabled = true;
        }

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(false);
            nicknameText.text = "";
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
            levelText.text = "";
        }

        if (leaderImage != null)
            leaderImage.SetActive(false);
    }

    public void SetData(RoomMemberData data)
    {
        if(emptyLabel!=null)emptyLabel.SetActive(false);
        gameObject.SetActive(true);

        if (image != null)
        {
            image.gameObject.SetActive(true);
            Sprite icon = Resources.Load<Sprite>($"PlayerIcon/{Mathf.Clamp(data.playerIconId, 0, 2)}");
            if (icon == null) icon = Resources.Load<Sprite>("PlayerIcon/0");
            image.sprite = icon;
            image.enabled = icon != null;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(true);
            nicknameText.text = data.nickname;
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = "Lv. " + data.level;
        }

        if (leaderImage != null)
            leaderImage.SetActive(data.isLeader);
    }
}
