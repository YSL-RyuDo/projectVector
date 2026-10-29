using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamPickSlotUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text nicknameText;

    [Header("Images")]
    [SerializeField] private Image slotImage;

    [Header("Buttons")]
    [SerializeField] private Button changeButton;
    [SerializeField] private Button shareButton;

    [Header("Nickname Color")]
    [SerializeField] private Color normalNicknameColor = Color.black;
    [SerializeField] private Color myNicknameColor = new Color(1f, 0.85f, 0f);

    private Sprite defaultSlotSprite;
    private uint actionPlayer;
    public void BindActions(System.Action<uint> exchange, System.Action share)
    {
        changeButton.onClick.AddListener(() => exchange(actionPlayer));
        shareButton.onClick.AddListener(() => share());
    }
    public void SetActions(uint player, bool mine, bool locked, bool enabled, bool busy)
    {
        actionPlayer = player;
        changeButton.gameObject.SetActive(!mine && locked);
        shareButton.gameObject.SetActive(mine && locked);
        changeButton.interactable = shareButton.interactable = enabled && !busy;
    }

    private void Awake()
    {
        if (slotImage == null)
            slotImage = GetComponent<Image>();

        if (slotImage != null)
            defaultSlotSprite = slotImage.sprite;
    }

    public void Clear()
    {
        gameObject.SetActive(true);

        if (slotImage != null)
        {
            slotImage.gameObject.SetActive(true);
            slotImage.enabled = true;
            slotImage.sprite = defaultSlotSprite;
        }

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(true);
            nicknameText.text = "";
            nicknameText.color = normalNicknameColor;
        }

        if (changeButton != null)
            changeButton.gameObject.SetActive(false);

        if (shareButton != null)
            shareButton.gameObject.SetActive(false);
    }

    public void SetWaitingSlot(string fallbackName)
    {
        gameObject.SetActive(true);

        if (slotImage != null)
        {
            slotImage.gameObject.SetActive(true);
            slotImage.enabled = true;
            slotImage.sprite = defaultSlotSprite;
        }

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(true);
            nicknameText.text = fallbackName;
            nicknameText.color = normalNicknameColor;
        }

        if (changeButton != null)
            changeButton.gameObject.SetActive(false);

        if (shareButton != null)
            shareButton.gameObject.SetActive(false);
    }

    public void SetData(string nickname, Sprite characterSprite, bool isLocked, bool isMe)
    {
        gameObject.SetActive(true);

        if (slotImage != null)
        {
            slotImage.gameObject.SetActive(true);
            slotImage.enabled = true;

            if (characterSprite != null)
                slotImage.sprite = characterSprite;
            else
                slotImage.sprite = defaultSlotSprite;
        }

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(true);
            nicknameText.text = nickname;
            nicknameText.color = isMe ? myNicknameColor : normalNicknameColor;
        }

        if (changeButton != null)
            changeButton.gameObject.SetActive(isLocked && !isMe);

        if (shareButton != null)
            shareButton.gameObject.SetActive(isLocked && isMe);
    }
}