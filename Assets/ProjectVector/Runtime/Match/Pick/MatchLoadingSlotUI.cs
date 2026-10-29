using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchLoadingSlotUI : MonoBehaviour
{
    [Header("Slot References")]
    [SerializeField] private Image playerInfo;
    [SerializeField] private TMP_Text charText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private Image rankImage;

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void AutoBindIfNeeded()
    {
        if (playerInfo == null)
        {
            Transform t = transform.Find("PlayerInfo");
            if (t != null)
                playerInfo = t.GetComponent<Image>();
        }

        if (charText == null)
        {
            Transform t = transform.Find("PlayerInfo/CharText");
            if (t == null)
                t = transform.Find("CharText");

            if (t != null)
                charText = t.GetComponent<TMP_Text>();
        }

        if (nicknameText == null)
        {
            Transform t = transform.Find("PlayerInfo/NicknameText");
            if (t == null)
                t = transform.Find("NicknameText");

            if (t != null)
                nicknameText = t.GetComponent<TMP_Text>();
        }

        if (rankImage == null)
        {
            Transform t = transform.Find("PlayerInfo/RankImage");
            if (t == null)
                t = transform.Find("RankImage");

            if (t != null)
                rankImage = t.GetComponent<Image>();
        }
    }

    public void SetData(Sprite characterSprite, string characterName, string nickname, Sprite rankSprite)
    {
        AutoBindIfNeeded();

        gameObject.SetActive(true);

        if (playerInfo != null)
        {
            playerInfo.gameObject.SetActive(true);
            playerInfo.enabled = true;
            playerInfo.sprite = characterSprite;
            playerInfo.preserveAspect = true;

            Color color = playerInfo.color;
            color.a = 1f;
            playerInfo.color = color;

            Debug.Log(
                $"[MATCH SLOT UI SET] slot={gameObject.name}, " +
                $"playerInfo={playerInfo.gameObject.name}, " +
                $"sprite={(characterSprite != null ? characterSprite.name : "NULL")}"
            );
        }
        else
        {
            Debug.LogError($"[MATCH SLOT UI ERROR] {gameObject.name} playerInfo is null");
        }

        if (charText != null)
            charText.text = characterName;
        else
            Debug.LogError($"[MATCH SLOT UI ERROR] {gameObject.name} charText is null");

        if (nicknameText != null)
            nicknameText.text = nickname;
        else
            Debug.LogError($"[MATCH SLOT UI ERROR] {gameObject.name} nicknameText is null");

        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(true);
            rankImage.enabled = rankSprite != null;
            rankImage.sprite = rankSprite;
            rankImage.preserveAspect = true;
        }
    }

    public void Clear()
    {
        AutoBindIfNeeded();

        gameObject.SetActive(true);

        if (playerInfo != null)
        {
            playerInfo.gameObject.SetActive(true);
            playerInfo.enabled = true;
            playerInfo.sprite = null;
        }

        if (charText != null)
            charText.text = "";

        if (nicknameText != null)
            nicknameText.text = "";

        if (rankImage != null)
        {
            rankImage.enabled = false;
            rankImage.sprite = null;
        }
    }
}