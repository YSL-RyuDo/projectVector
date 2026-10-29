using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    [DefaultExecutionOrder(11000)]
    [RequireComponent(typeof(PingCameraProvider))]
    public sealed class PingManager : MonoBehaviour
    {
        public const float MarkerLifetime=3f;
        [SerializeField] PingOption[] options={
            new(PingType.Location,"여기",new Color(.35f,.83f,1f)),
            new(PingType.Danger,"위험",new Color(1f,.36f,.38f)),
            new(PingType.Attack,"공격",new Color(1f,.72f,.30f)),
            new(PingType.Defend,"방어",new Color(.46f,.86f,.72f))};
        [SerializeField] bool networkMode=true;
        [SerializeField] int localTeamId=-1;
        [Header("Authored UI, created and saved by PingAuthoring before Play")]
        public RectTransform canvasRoot;
        public CanvasGroup presentation;
        public PingWheel wheel;
        public PingMarker[] markerPool;
        public RectTransform hudMap, fullMap;
        public Camera mapCamera;
        public Text statusText;
        public GameObject inputHint;
        public AudioSource audioSource;
        public AudioClip confirmSound, dangerSound;
        public event Action<PingRequest> PingRequested;
        public bool NetworkMode=>networkMode;
        public PingOption[] Options=>options;
        public RectTransform CanvasRoot=>canvasRoot;
        public int ConfirmedCount {get;private set;}
        public uint LastSender {get;private set;}
        public PingType LastType {get;private set;}
        public Vector3 LastPosition {get;private set;}
        public int ActiveMarkerCount {get {int n=0;if(markerPool!=null)foreach(var m in markerPool)if(m&&m.Active)n++;return n;}}
        readonly HashSet<string> received=new();
        readonly Queue<string> receivedOrder=new();
        PingCameraProvider cameras;
        float tokens=3, tokenTime, nextRequest, statusUntil;
        bool visible;

        void Awake(){cameras=GetComponent<PingCameraProvider>();ClearMarkers();}
        public void ConfigureNetwork(bool enabled,int teamId)
        {
            networkMode=enabled;localTeamId=teamId;
            tokens=3;tokenTime=Time.unscaledTime;nextRequest=0;
            ClearMarkers();received.Clear();receivedOrder.Clear();
        }
        public void SetLocalTeam(int team){if(localTeamId!=team)ConfigureNetwork(networkMode,team);}
        public void SetVisibility(bool show,bool allowInput)
        {
            visible=show;
            if(presentation)presentation.alpha=show?1:0;
            if(inputHint)inputHint.SetActive(show&&allowInput);
        }
        public void Notice(string text)
        {if(statusText){statusText.text=text;statusUntil=Time.unscaledTime+1.4f;statusText.gameObject.SetActive(true);}}
        public bool RequestPing(PingType type,Vector3 position)
        {
            if(!isActiveAndEnabled||!PingValidation.IsFinite(position)||FindOption(type)==null)return false;
            if(networkMode&&(localTeamId<0||PingRequested==null))return false;
            var now=Time.unscaledTime;tokens=Mathf.Min(3,tokens+Mathf.Max(0,now-tokenTime));tokenTime=now;
            if(now<nextRequest||tokens<1){Notice("잠시 후 다시 표시할 수 있습니다");return false;}
            tokens--;nextRequest=now+.45f;
            if(networkMode)PingRequested.Invoke(new PingRequest(Guid.NewGuid().ToString("N"),type,position));
            else ShowMarker(0,type,position,MarkerLifetime);
            return true;
        }
        // Only NetworkManager invokes this with an authenticated server packet.
        public bool ReceiveConfirmed(ConfirmedPing ping)
        {
            if(!isActiveAndEnabled||!networkMode||localTeamId<0||ping.TeamId!=localTeamId||
                string.IsNullOrEmpty(ping.PingId)||ping.PingId.Length>128||!PingValidation.IsFinite(ping.Position)||
                FindOption(ping.Type)==null||!received.Add(ping.PingId))return false;
            receivedOrder.Enqueue(ping.PingId);while(receivedOrder.Count>128)received.Remove(receivedOrder.Dequeue());
            ConfirmedCount++;LastSender=ping.SenderId;LastType=ping.Type;LastPosition=ping.Position;
            ShowMarker(ping.SenderId,ping.Type,ping.Position,Mathf.Clamp(ping.Lifetime,.01f,MarkerLifetime));
            return true;
        }
        PingOption FindOption(PingType type)=>Array.Find(options,o=>o.type==type);
        void ShowMarker(uint sender,PingType kind,Vector3 position,float lifetime)
        {
            if(markerPool==null||markerPool.Length==0)return;
            PingMarker selected=null;
            // Each teammate has one current callout; repeating a ping updates it.
            foreach(var m in markerPool)if(m&&m.Active&&m.SenderId==sender){selected=m;break;}
            if(!selected)foreach(var m in markerPool)if(m&&!m.Active){selected=m;break;}
            if(!selected){selected=markerPool[0];foreach(var m in markerPool)if(m.ExpiresAt<selected.ExpiresAt)selected=m;}
            string nickname="아군";
            if(sender==NetworkManager.Instance?.MyPlayerId)nickname="나";
            else foreach(var actor in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
                if(actor.PlayerId==sender){nickname=actor.Nickname;break;}
            selected.Show(sender,position,FindOption(kind),nickname,Time.unscaledTime+lifetime);
            if(audioSource&&visible)audioSource.PlayOneShot(kind==PingType.Danger?dangerSound:confirmSound,.42f);
        }
        void LateUpdate()
        {
            if(statusText&&Time.unscaledTime>=statusUntil)statusText.gameObject.SetActive(false);
            if(markerPool==null||!canvasRoot)return;
            foreach(var marker in markerPool)
            {
                if(!marker||!marker.Active)continue;
                if(Time.unscaledTime>=marker.ExpiresAt){marker.Hide();continue;}
                marker.Refresh(cameras.Current,canvasRoot,mapCamera,hudMap,fullMap);
            }
        }
        public void ClearMarkers()
        {if(markerPool!=null)foreach(var m in markerPool)if(m)m.Hide();if(statusText)statusText.gameObject.SetActive(false);}
        void OnDisable(){ClearMarkers();SetVisibility(false,false);}
    }
}
