using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TempGamePlayerSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image rankImage;
    [SerializeField] private TMP_Text nicknameText;

    public void SetData(Sprite icon, Sprite border, Sprite rank, string nickname)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
            iconImage.sprite = icon;

        if (borderImage != null)
            borderImage.sprite = border;

        if (rankImage != null)
            rankImage.sprite = rank;

        if (nicknameText != null)
            nicknameText.text = nickname;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}