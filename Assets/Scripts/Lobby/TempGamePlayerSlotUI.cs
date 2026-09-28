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
    [SerializeField] private Image[] medalImages;

    private static readonly string[] MedalSprites =
        { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" };

    public string Nickname => nicknameText != null ? nicknameText.text : string.Empty;

    public void SetCardSprite(Sprite card)
    {
        if (iconImage == null) return;
        iconImage.sprite = card;
        iconImage.enabled = card != null;
    }

    public void SetData(Sprite icon, Sprite border, Sprite rank, string nickname)
    {
        SetData(icon, border, rank, nickname, null);
    }

    public void SetData(Sprite icon, Sprite border, Sprite rank, string nickname, ChipCraftPresetData preset)
    {
        gameObject.SetActive(true);

        SetCardSprite(icon);

        if (borderImage != null)
            borderImage.sprite = border;

        if (rankImage != null)
            rankImage.sprite = rank;

        if (nicknameText != null)
            nicknameText.text = nickname;

        SetChipCraftPreset(preset);
    }

    public void SetChipCraftPreset(ChipCraftPresetData preset)
    {
        if (medalImages == null || iconImage == null) return;
        RectTransform card = iconImage.rectTransform;
        for (int i = 0; i < medalImages.Length; ++i)
        {
            Image image = medalImages[i];
            if (image == null) continue;
            bool visible = preset != null && preset.medals != null && i < preset.medals.Count;
            image.gameObject.SetActive(visible);
            if (!visible) continue;
            ChipCraftMedalData medal = preset.medals[i];
            image.sprite = Resources.Load<Sprite>("Rank/" + MedalSprites[Mathf.Clamp(medal.medalId, 0, MedalSprites.Length - 1)]);
            image.enabled = image.sprite != null;
            image.color = Color.white;
            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = new Vector2(medal.x * card.rect.width, medal.y * card.rect.height);
            rect.localRotation = Quaternion.Euler(0, 0, medal.rotation);
            rect.localScale = new Vector3(medal.flipped ? -medal.scale : medal.scale, medal.scale, 1);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
