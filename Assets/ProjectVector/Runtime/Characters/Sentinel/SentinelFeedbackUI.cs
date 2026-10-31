using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout, images and audio are serialized in ElectricStatusHUD.prefab. No runtime UI construction.
public sealed class SentinelFeedbackUI : MonoBehaviour
{
    public static SentinelFeedbackUI Instance { get; private set; }
    [SerializeField] private CanvasGroup ownerNotice, statusNotice;
    [SerializeField] private TMP_Text ownerText, statusText;
    [SerializeField] private Image statusFill;
    [SerializeField] private AudioSource noticeAudio;
    private float ownerUntil, concussionUntil, slowUntil;
    private uint trackingTrap;
    private PlayerMovement slowedMovement;
    public float ConcussionRemaining => Mathf.Max(0, concussionUntil-Time.unscaledTime);
    public static bool LocalConcussed => Instance && Instance.ConcussionRemaining>0;
    private void Awake(){Instance=this;Clear();}
    public void Track(uint trap,float seconds)
    {
        if(seconds<=0){if(trackingTrap==trap)ownerUntil=0;return;}
        trackingTrap=trap;ownerUntil=Time.unscaledTime+seconds;
        if(ownerText)ownerText.text="감지됨 · 스캔 준비";
        if(noticeAudio)noticeAudio.Play();
    }
    public void Notice(string text,float duration=1.4f)
    {trackingTrap=0;ownerUntil=Time.unscaledTime+duration;if(ownerText)ownerText.text=text;if(noticeAudio)noticeAudio.Play();}
    public void ApplyStatus(float slow,float multiplier,float concussion)
    {
        var actor=NetworkManager.Instance?NetworkManager.Instance.MyPlayer:null;
        if(!actor || actor.GetComponent<PlayerHP>()?.IsDead==true)return;
        if(slow>0){
            ReleaseSlow();slowedMovement=actor.GetComponentInChildren<PlayerMovement>(true);
            if(slowedMovement){slowedMovement.ApplySwampSlow(multiplier);slowUntil=Time.unscaledTime+slow;}
        }
        // A new hit does not extend a still-running concussion.
        if(concussion>0 && ConcussionRemaining<=0)concussionUntil=Time.unscaledTime+concussion;
    }
    public void Present(float remaining)
    {
        if(statusNotice){statusNotice.gameObject.SetActive(remaining>0);statusNotice.alpha=1;}
        if(statusText)statusText.text=$"진탕  {remaining:0.0}초";
        if(statusFill)statusFill.fillAmount=Mathf.Clamp01(remaining/3);
    }
    private void Update()
    {
        var net=NetworkManager.Instance;
        if(!net || !net.IsConnected || !net.IsAuthenticated || !net.MyPlayer || net.MyPlayer.GetComponent<PlayerHP>()?.IsDead==true){Clear();return;}
        if(slowedMovement && Time.unscaledTime>=slowUntil)ReleaseSlow();
        if(ownerNotice){ownerNotice.gameObject.SetActive(Time.unscaledTime<ownerUntil);ownerNotice.alpha=Mathf.Clamp01((ownerUntil-Time.unscaledTime)*5);}
        if(trackingTrap!=0 && ownerText)ownerText.text=$"감지됨 · 스캔 {Mathf.Max(0,ownerUntil-Time.unscaledTime):0.0}초";
        Present(ConcussionRemaining);
    }
    private void ReleaseSlow(){if(slowedMovement)slowedMovement.RemoveSwampSlow();slowedMovement=null;slowUntil=0;}
    public void Clear(){ReleaseSlow();ownerUntil=concussionUntil=0;trackingTrap=0;if(ownerNotice)ownerNotice.gameObject.SetActive(false);Present(0);}
    private void OnDisable(){Clear();}
    private void OnDestroy(){Clear();if(Instance==this)Instance=null;}
}
