using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerSlotUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject leaderImage;

    public void Clear()
    {
        if (image != null)
            image.enabled = false;

        if (nicknameText != null)
            nicknameText.text = "";

        if (levelText != null)
            levelText.text = "";

        if (leaderImage != null)
            leaderImage.SetActive(false);
    }

    public void SetData(RoomMemberData data)
    {
        if (image != null)
            image.enabled = true;

        if (nicknameText != null)
            nicknameText.text = data.nickname;

        if (levelText != null)
            levelText.text = "Lv. " + data.level;

        if (leaderImage != null)
            leaderImage.SetActive(data.isLeader);
    }
}