using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardRowUI : MonoBehaviour
{
    [SerializeField] private Image charImage;
    [SerializeField] private TMP_Text nickNameText;
    [SerializeField] private TMP_Text kdaText;

    [SerializeField] private GameObject ultTextObject;
    [SerializeField] private GameObject weaponImageObject;
    [SerializeField] private GameObject creditTextObject;

    private TMP_Text creditText;

    private void Awake()
    {
        CacheCreditText();
    }

    private void CacheCreditText()
    {
        if (creditTextObject == null)
            return;

        creditText = creditTextObject.GetComponent<TMP_Text>();

        if (creditText == null)
            creditText = creditTextObject.GetComponentInChildren<TMP_Text>(true);
    }

    public void SetData(ScoreboardMemberData data, Sprite characterSprite, uint myPlayerId)
    {
        gameObject.SetActive(true);

        if (charImage != null)
        {
            charImage.sprite = characterSprite;
            charImage.enabled = characterSprite != null;
        }

        if (nickNameText != null)
        {
            nickNameText.text = data.nickname;

            if (data.playerId == myPlayerId)
                nickNameText.color = Color.yellow;
            else
                nickNameText.color = Color.white;
        }

        if (kdaText != null)
            kdaText.text = $"{data.kills} / {data.deaths} / {data.assists}";

        if (ultTextObject != null)
            ultTextObject.SetActive(false);

        if (weaponImageObject != null)
            weaponImageObject.SetActive(false);

        if (creditTextObject != null)
            creditTextObject.SetActive(true);

        if (creditText == null)
            CacheCreditText();

        if (creditText != null)
            creditText.text = data.credits.ToString();
    }

    public void Clear()
    {
        if (charImage != null)
        {
            charImage.sprite = null;
            charImage.enabled = false;
        }

        if (nickNameText != null)
            nickNameText.text = "";

        if (kdaText != null)
            kdaText.text = "";

        if (creditText == null)
            CacheCreditText();

        if (creditText != null)
            creditText.text = "";

        if (creditTextObject != null)
            creditTextObject.SetActive(false);

        if (ultTextObject != null)
            ultTextObject.SetActive(false);

        if (weaponImageObject != null)
            weaponImageObject.SetActive(false);

        gameObject.SetActive(false);
    }
}