using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All graphics, buttons and bounds are authored in the scene by PickInteractionAuthoring.
public sealed class PickInteractionView : MonoBehaviour
{
    public GameObject exchangePanel, noticePanel;
    public TMP_Text exchangeTitle, exchangeNames, countdown, noticeTitle, noticeText;
    public Image myPortrait, peerPortrait, noticePortrait;
    public Button acceptButton, declineButton, cancelButton;
    float exchangeDeadline, noticeUntil;
    public void SetExchange(bool visible, bool outgoing, Sprite mine, Sprite other, string names, float seconds)
    {
        exchangePanel.SetActive(visible);
        if (!visible) return;
        exchangeTitle.text = outgoing ? "교환 응답 대기" : "캐릭터 교환 요청";
        myPortrait.sprite = mine; peerPortrait.sprite = other; exchangeNames.text = names;
        acceptButton.gameObject.SetActive(!outgoing); declineButton.gameObject.SetActive(!outgoing); cancelButton.gameObject.SetActive(outgoing);
        exchangeDeadline = Time.unscaledTime + seconds;
    }
    public void Notice(string title, string content, Sprite portrait = null)
    {
        noticeTitle.text = title; noticeText.text = content;
        noticePortrait.sprite = portrait; noticePortrait.enabled = portrait != null;
        noticePanel.SetActive(true); noticeUntil = Time.unscaledTime + 5f;
    }
    public void Hide() { exchangePanel.SetActive(false); noticePanel.SetActive(false); }
    void Update()
    {
        if (exchangePanel.activeSelf) countdown.text = $"{Mathf.Max(0, Mathf.CeilToInt(exchangeDeadline - Time.unscaledTime))}초";
        if (noticePanel.activeSelf && Time.unscaledTime >= noticeUntil) noticePanel.SetActive(false);
    }
}
