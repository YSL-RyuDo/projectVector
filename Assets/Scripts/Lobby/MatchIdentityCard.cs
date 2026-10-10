using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-authored card. Reuses the exact profile icon/card ID and saved medal placement.
public sealed class MatchIdentityCard : MonoBehaviour
{
    public Image card, icon, border;
    public Image[] medals;
    public TMP_Text nickname, caption;
    public CanvasGroup identity;
    public GameObject concealed;
    public float revealSeconds = .35f;
    float reveal;
    bool shown;
    public void Present(string name, MatchAppearanceData appearance, bool accepted, bool isMe)
    {
        bool visible = accepted && appearance != null;
        if (shown != visible) { shown = visible; reveal = 0; }
        concealed.SetActive(!visible);
        nickname.text = visible ? name : "";
        nickname.color = isMe ? new Color32(255,218,112,255) : Color.white;
        caption.text = visible ? (isMe ? "나 · 수락 완료" : "수락 완료") : "수락 대기";
        card.sprite = appearance == null ? null : Resources.Load<Sprite>($"IconCard/icon{appearance.playerIconId}_card");
        icon.sprite = appearance == null ? null : Resources.Load<Sprite>($"PlayerIcon/{appearance.playerIconId}");
        card.enabled = card.sprite != null; icon.enabled = icon.sprite != null;
        string[] ranks = {"Bronze","Silver","Gold","Platinum","Diamond","Master"};
        for(int i=0;i<medals.Length;i++)
        {
            var preset=appearance?.preset;
            bool active=visible && preset!=null && i<preset.medals.Count && i<preset.caseId+1;
            medals[i].gameObject.SetActive(active);
            if(!active) continue;
            var m=preset.medals[i]; var r=medals[i].rectTransform;
            medals[i].sprite=Resources.Load<Sprite>("Rank/"+ranks[Mathf.Clamp(m.medalId,0,5)]);
            r.anchoredPosition=new Vector2(m.x*card.rectTransform.rect.width,m.y*card.rectTransform.rect.height);
            r.localRotation=Quaternion.Euler(0,0,m.rotation);
            r.localScale=new Vector3(m.flipped?-m.scale:m.scale,m.scale,1);
        }
        Evaluate(Application.isPlaying ? reveal : 1);
    }
    public void Evaluate(float value)
    {
        float t=shown?Mathf.SmoothStep(0,1,Mathf.Clamp01(value)):0;
        identity.alpha=t;
        identity.transform.localScale=new Vector3(Mathf.Lerp(.85f,1,t),1,1);
    }
    void Update() { if(shown && reveal<1) { reveal+=Time.unscaledDeltaTime/Mathf.Max(.01f,revealSeconds); Evaluate(reveal); } }
}
