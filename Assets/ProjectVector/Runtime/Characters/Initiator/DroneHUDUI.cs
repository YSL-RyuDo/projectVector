using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Graphics and audio sources are authored in the scene, never built in Play.
public class DroneHUDUI : MonoBehaviour
{
    public static DroneHUDUI Instance { get; private set; }
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _taserStatusText, _droneHpText, _controlGuideText;
    [SerializeField] private GameObject _hitMarker;
    [SerializeField] private Image _fireFlashImage;
    [SerializeField] private TMP_Text _telemetryText, _feedbackText, _modeText;
    [SerializeField] private Image _hpFill;
    [SerializeField] private GameObject _flightInstruments;
    [SerializeField] private Image[] _damageEdges;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioClip _hitSound, _damageSound, _launchSound;
    [SerializeField] private DroneReticleGraphic _reticle;
    [SerializeField] private float _hitMarkerTime = 0.35f, _flashFadeSpeed = 8f;
    private float _hitUntil, _feedbackUntil, _exitUntil, _firePulse, _damagePulse;
    private int _previousHp = -1;
    private bool _spectating;
    private static readonly Color Cyan = new Color(.35f,.91f,1f);
    private static readonly Color Amber = new Color(1f,.72f,.3f);
    private void Awake() { Instance=this; SetVisible(false); }
    private void OnDestroy() { if (Instance==this) Instance=null; }
    private void Update()
    {
        float now=Time.unscaledTime;
        if (_hitMarker) _hitMarker.SetActive(now<_hitUntil);
        if (_feedbackText && now>_feedbackUntil) _feedbackText.text="";
        _firePulse=Mathf.MoveTowards(_firePulse,0,Time.unscaledDeltaTime*_flashFadeSpeed);
        _damagePulse=Mathf.MoveTowards(_damagePulse,0,Time.unscaledDeltaTime*1.8f);
        if (_fireFlashImage) _fireFlashImage.color=new Color(Cyan.r,Cyan.g,Cyan.b,_firePulse);
        if (_damageEdges!=null) foreach(var edge in _damageEdges) if(edge)edge.color=new Color(1,.22f,.12f,_damagePulse);
        if (_exitUntil>0 && now>_exitUntil) SetVisible(false);
    }
    public void SetVisible(bool visible)
    {
        _exitUntil=0; _hitUntil=0; _firePulse=_damagePulse=0;
        if (_root) _root.SetActive(visible);
        if (_hitMarker) _hitMarker.SetActive(false);
        if (!visible) return;
        _spectating=false; _previousHp=-1;
        if (_flightInstruments) _flightInstruments.SetActive(true);
        if (_modeText) _modeText.text="드론 조종";
        if (_feedbackText) _feedbackText.text="";
        SetTaserReady(true); SetDroneHp(1,1);
        if (_controlGuideText) _controlGuideText.text="SPACE  상승     CTRL  하강     좌클릭  발사     Q  복귀";
    }
    public void ShowPreparation(bool pending)
    {
        SetVisible(true);
        if (_flightInstruments) _flightInstruments.SetActive(false);
        if (_modeText) _modeText.text=pending?"드론 사출 중":"테이저 드론 · 사출 준비";
        if (_controlGuideText) _controlGuideText.text=pending?"연결 대기 중  ·  Q 취소":"좌클릭  사출      우클릭 / Q  취소";
        if (!pending && _audio && _launchSound) _audio.PlayOneShot(_launchSound,.35f);
    }
    public void ShowNotice(string text)
    { if (_feedbackText) { _feedbackText.text=text; _feedbackText.color=Amber; } _feedbackUntil=Time.unscaledTime+1.5f; }
    public void ShowExitNotice(string text)
    {
        SetVisible(true);
        if (_flightInstruments) _flightInstruments.SetActive(false);
        if (_modeText) _modeText.text=text;
        if (_controlGuideText) _controlGuideText.text="";
        _exitUntil=Time.unscaledTime+1.5f;
        if (_audio && _damageSound) _audio.PlayOneShot(_damageSound,.4f);
    }
    public void SetTaserReady(bool ready)
    {
        if (_taserStatusText) { _taserStatusText.text=ready?"1 / 1":"0 / 1"; _taserStatusText.color=ready?Color.white:Color.gray; }
        if (_reticle) _reticle.color=ready?new Color(.57f,1,.75f,.85f):new Color(.6f,.65f,.65f,.4f);
    }
    public void SetDroneHp(int hp,int maxHp)
    {
        float fraction=Mathf.Clamp01((float)hp/Mathf.Max(1,maxHp));
        if (_droneHpText) _droneHpText.text=$"{Mathf.RoundToInt(fraction*100)}";
        if (_hpFill) {
            _hpFill.rectTransform.localScale=new Vector3(fraction,1,1);
            _hpFill.color=fraction>.3f?Cyan:Amber;
        }
        if (_previousHp>=0 && hp<_previousHp) {
            _damagePulse=.7f; ShowNotice(hp>0?"기체 피격":"기체 파괴");
            if (_audio && _damageSound) _audio.PlayOneShot(_damageSound,.5f);
        }
        _previousHp=hp;
    }
    public void SetTelemetry(float altitude,float speed,bool limited)
    {
        if (_spectating) return;
        if (_telemetryText) { _telemetryText.text=float.IsNaN(altitude)?"--m":$"{altitude:0.0}m"; _telemetryText.color=limited?Amber:Color.white; }
        if (limited) ShowNotice("상승 제한");
    }
    public void PlayTaserFireFeedback() { _firePulse=.9f; }
    public void PlayTaserHitConfirm()
    {
        _hitUntil=Time.unscaledTime+_hitMarkerTime;
        if (_feedbackText) { _feedbackText.text="감전 적중"; _feedbackText.color=Cyan; }
        _feedbackUntil=Time.unscaledTime+1.3f;
        if (_audio && _hitSound) _audio.PlayOneShot(_hitSound,.5f);
    }
    public void PlayTaserMiss() { /* Empty charge and projectile feedback communicate a miss. */ }
    public void SetSpectatorDroneMode(bool active,uint ownerPlayerId=0)
    {
        SetVisible(active); _spectating=active;
        if (!active) return;
        if (_modeText) _modeText.text="팀원 드론 관전";
        if (_taserStatusText) _taserStatusText.text="관전 중";
        if (_controlGuideText) _controlGuideText.text="관전 대상의 드론 화면";
        if (_telemetryText) _telemetryText.text="";
    }
}
