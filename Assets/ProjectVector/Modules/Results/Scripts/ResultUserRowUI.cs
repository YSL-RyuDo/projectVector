using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUserRowUI : MonoBehaviour
{
    [SerializeField] private Image charImage;
    [SerializeField] private TMP_Text nickNameText;
    [SerializeField] private TMP_Text kdaText;
    [SerializeField] private TMP_Text mvpText;

    [Header("Colors")]
    [SerializeField] private Color myNicknameColor = Color.yellow;
    [SerializeField] private Color normalNicknameColor = Color.white;

    public void SetData(
        MatchResultMemberData data,
        ResultViewType viewType,
        Sprite characterSprite,
        uint myPlayerId)
    {
        if (charImage != null)
            charImage.sprite = characterSprite;

        if (nickNameText != null)
        {
            nickNameText.text = data.nickname;
            nickNameText.color = data.playerId == myPlayerId
                ? myNicknameColor
                : normalNicknameColor;
        }

        int k = data.kills;
        int d = data.deaths;
        int a = data.assists;

        if (viewType == ResultViewType.FirstHalf)
        {
            k = data.firstHalfKills;
            d = data.firstHalfDeaths;
            a = data.firstHalfAssists;
        }
        else if (viewType == ResultViewType.SecondHalf)
        {
            k = data.secondHalfKills;
            d = data.secondHalfDeaths;
            a = data.secondHalfAssists;
        }
        else if (viewType == ResultViewType.Overtime)
        {
            k = data.overtimeKills;
            d = data.overtimeDeaths;
            a = data.overtimeAssists;
        }

        if (kdaText != null)
            kdaText.text = $"{k} / {d} / {a}";

        if (mvpText != null)
        {
            mvpText.gameObject.SetActive(data.isMvp);
            mvpText.text = data.isMvp ? "MVP" : "";
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (charImage != null)
            charImage.sprite = null;

        if (nickNameText != null)
        {
            nickNameText.text = "";
            nickNameText.color = normalNicknameColor;
        }

        if (kdaText != null)
            kdaText.text = "";

        if (mvpText != null)
            mvpText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}