using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All layout is saved in the scene. No runtime UI creation or enemy-state search.
public sealed class ElectricStatusUI : MonoBehaviour
{
    public static ElectricStatusUI Instance {get;private set;}
    [SerializeField] private GameObject _statusRoot, _confirmationRoot;
    [SerializeField] private TMP_Text _statusText, _confirmationText;
    [SerializeField] private Image[] _stacks = new Image[0];
    [SerializeField] private Image _remaining;
    private ElectricShock _local;
    private float _confirmUntil,_searchAt;
    private uint _playerId;
    private void Awake(){Instance=this;Hide();}
    public void ConfirmCast(int count){if(_confirmationText)_confirmationText.text=count>0?$"방전  {count}명":"적중 대상 없음";_confirmUntil=Time.unscaledTime+.95f;}
    private void Update()
    {
        var net=NetworkManager.Instance;
        if(!net || !net.IsConnected || !net.IsAuthenticated){Hide();return;}
        if(_playerId!=net.MyPlayerId){_local=null;_playerId=net.MyPlayerId;}
        if(!_local && Time.unscaledTime>=_searchAt){
            _searchAt=Time.unscaledTime+1;
            foreach(var identity in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
                if(identity.PlayerId==_playerId){_local=identity.GetComponent<ElectricShock>();break;}
        }
        bool alive=_local && _local.gameObject.activeInHierarchy && _local.GetComponent<PlayerHP>()?.IsDead!=true;
        if(!alive){Hide();return;}
        Present(_local.ShockCount,_local.IsStunned?_local.StunRemaining:0,_local.StunDuration);
        if(_confirmationRoot)_confirmationRoot.SetActive(Time.unscaledTime<_confirmUntil);
    }
    // Also used for deterministic layout checks; does not affect gameplay status.
    public void Present(int stacks,float remaining,float duration)
    {
        bool stun=remaining>0;
        if(_statusRoot)_statusRoot.SetActive(stun||stacks>0);
        if(_statusText)_statusText.text=stun?$"기절  {remaining:0.0}초":$"감전  {Mathf.Clamp(stacks,0,3)}";
        for(int i=0;i<_stacks.Length;i++)if(_stacks[i])_stacks[i].color=i<stacks?new Color(1,.68f,.22f):new Color(.22f,.28f,.32f);
        if(_remaining){_remaining.gameObject.SetActive(stun);_remaining.fillAmount=duration>0?Mathf.Clamp01(remaining/duration):0;}
    }
    public void Hide(){if(_statusRoot)_statusRoot.SetActive(false);if(_confirmationRoot)_confirmationRoot.SetActive(false);_confirmUntil=0;}
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
