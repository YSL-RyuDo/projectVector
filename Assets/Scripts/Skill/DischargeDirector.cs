using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// A saved HUD component routes authoritative events; only temporary world VFX are instantiated.
public sealed class DischargeDirector : MonoBehaviour
{
    public static DischargeDirector Instance { get; private set; }
    [SerializeField] private DischargeVfx _effect;
    [SerializeField] private CanvasGroup _ownerNotice, _victimNotice, _victimPulse;
    [SerializeField] private TMP_Text _ownerText;
    [SerializeField] private AudioSource _ownerAudio, _victimAudio;
    private sealed class Cast { public uint owner; public DischargeVfx effect; public float expires; public int count; public bool ended; public HashSet<uint> targets = new(); public HashSet<byte> slices = new(); }
    private readonly Dictionary<uint, Cast> _casts = new();
    private float _ownerLeft, _victimLeft, _pulseLeft;
    private uint _noticeCast;
    private void Awake() { Instance = this; ResetFeedback(); }
    static uint U(byte[] b, int p) => BitConverter.ToUInt32(b, p);
    static float F(byte[] b, int p) => BitConverter.ToSingle(b, p);
    static Vector3 V(byte[] b, int p) => new(F(b,p), F(b,p+4), F(b,p+8));
    static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    static InitiatorSkill_X Actor(uint id)
    {
        foreach (var actor in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
            if (actor.PlayerId == id) return actor.GetComponent<InitiatorSkill_X>();
        return null;
    }
    public void Receive(ushort type, byte[] b)
    {
        if (b == null) return;
        if (type == (ushort)PacketType.PKT_SC_DISCHARGE_GESTURE)
        { if (b.Length == 5 && b[4] <= 1) Actor(U(b,0))?.ReceiveGesture(b[4] != 0); return; }
        if (type == (ushort)PacketType.PKT_SC_DISCHARGE_BEGIN)
        {
            if (b.Length != 45 || b[44] != 7 || !Finite(V(b,8)) || !Finite(V(b,20)) ||
                !float.IsFinite(F(b,32)) || F(b,32) <= 0 || F(b,32) > 50 ||
                !float.IsFinite(F(b,36)) || F(b,36) < 0 || F(b,36) > 5 ||
                !float.IsFinite(F(b,40)) || F(b,40) <= 0 || F(b,40) > 1) return;
            uint id=U(b,0), owner=U(b,4); if (_casts.ContainsKey(id)) return;
            var cast = new Cast { owner=owner, expires=Time.unscaledTime+F(b,36)+7*F(b,40)+2 };
            _casts.Add(id,cast); Actor(owner)?.ConfirmBegin();
            if (_effect) { cast.effect=Instantiate(_effect); cast.effect.Begin(V(b,8),V(b,20),F(b,32),F(b,36),F(b,40)); }
            return;
        }
        if (type == (ushort)PacketType.PKT_SC_DISCHARGE_END)
        {
            if (b.Length != 9 || b[8] > 1 || !_casts.TryGetValue(U(b,0),out var ended) || ended.owner != U(b,4)) return;
            ended.ended=true; ended.effect?.Finish(b[8]!=0); ended.expires=Time.unscaledTime+.7f; return;
        }
        if (type != (ushort)PacketType.PKT_SC_DISCHARGE_STRIKE || b.Length != 130 || b[8]>=7 || b[9]>6 ||
            !_casts.TryGetValue(U(b,0),out var current) || current.owner!=U(b,4) || current.ended) return;
        for(int i=0;i<b[9];i++) if(!Finite(V(b,14+i*20))) return;
        if (!current.slices.Add(b[8])) return;
        current.effect?.Strike(b[8]);
        uint local = NetworkManager.Instance ? NetworkManager.Instance.MyPlayerId : 0;
        for(int i=0;i<b[9];i++)
        {
            int offset=10+i*20; uint target=U(b,offset);
            if (target==0 || !current.targets.Add(target)) continue;
            current.effect?.Hit(V(b,offset+4)); current.count++;
            ConfirmHit(local,current.owner,U(b,0),target,current.count);
        }
    }
    public void ConfirmHit(uint local,uint owner,uint cast,uint victim,int count)
    {
        if(local==0)return;
        if(local==owner)ShowOwner(cast,count);
        if(local==victim)ShowVictim();
    }
    public void ShowOwner(uint cast, int count)
    {
        if (_noticeCast!=cast || _ownerLeft<=0) _noticeCast=cast;
        if (_ownerText) _ownerText.text=$"방전 적중  {count}명";
        if(_ownerLeft<.95f && _ownerAudio) _ownerAudio.Play();
        _ownerLeft=1.15f; Advance(0);
    }
    public void ShowVictim()
    {
        _victimLeft=.95f; _pulseLeft=.22f; if(_victimAudio) _victimAudio.Play(); Advance(0);
    }
    public void Advance(float delta)
    {
        _ownerLeft=Mathf.Max(0,_ownerLeft-delta); _victimLeft=Mathf.Max(0,_victimLeft-delta); _pulseLeft=Mathf.Max(0,_pulseLeft-delta);
        Show(_ownerNotice,_ownerLeft,.25f); Show(_victimNotice,_victimLeft,.25f); Show(_victimPulse,_pulseLeft,.22f);
    }
    static void Show(CanvasGroup group,float time,float fade)
    {
        if(!group)return;group.alpha=Mathf.Clamp01(time/fade);group.blocksRaycasts=group.interactable=false;group.gameObject.SetActive(time>0);
    }
    private void Update()
    {
        var net=NetworkManager.Instance;
        if(!net || !net.IsConnected || !net.IsAuthenticated){ClearAll();return;}
        Advance(Time.unscaledDeltaTime);
        var expired=new List<uint>(); foreach(var c in _casts)if(c.Value.expires<=Time.unscaledTime)expired.Add(c.Key);
        foreach(uint id in expired){if(_casts[id].effect)Destroy(_casts[id].effect.gameObject);_casts.Remove(id);}
    }
    public void ResetFeedback()
    {
        _ownerLeft=_victimLeft=_pulseLeft=0;_noticeCast=0;
        if(_ownerAudio)_ownerAudio.Stop();if(_victimAudio)_victimAudio.Stop();Advance(0);
    }
    public void ClearAll()
    {
        foreach(var c in _casts.Values)if(c.effect){if(Application.isPlaying)Destroy(c.effect.gameObject);else DestroyImmediate(c.effect.gameObject);}_casts.Clear();ResetFeedback();
    }
    private void OnDisable()=>ClearAll();
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
