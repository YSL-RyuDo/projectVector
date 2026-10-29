using UnityEngine;
using UnityEngine.UI;

// All controls and references are authored in the existing scene loading panel.
public sealed class VectorReconnectView : MonoBehaviour
{
    public GameObject root;
    public MatchLoadingPanelUI loadingPanel;
    public MatchLoadingPresentation presentation;
    public GameObject recoveryActions;
    public Button retry, login;
    public Canvas overlayCanvas;
    public Image backdrop;
    public Color recoveryBackground = new Color(.055f,.065f,.09f,1);
    Color originalBackground;
    bool recovering;
    GameAllReadyData shownRoster;

    void Awake()
    {
        retry.onClick.AddListener(()=>NetworkManager.Instance?.RetryReconnect());
        login.onClick.AddListener(()=>NetworkManager.Instance?.ReturnToLoginAfterDisconnect());
    }
    public static void Show(GameAllReadyData roster,string message,bool failed)
    {
        var view=FindFirstObjectByType<VectorReconnectView>(FindObjectsInactive.Include);
        if(view==null) return;
        if(!view.recovering) {
            view.originalBackground=view.backdrop.color;
            view.recovering=true;
        }
        view.backdrop.color=view.recoveryBackground;
        view.overlayCanvas.overrideSorting=true;
        view.overlayCanvas.sortingOrder=200;
        if(roster!=null && view.shownRoster!=roster)
        {
            var data=new MatchLoadingInfoData { roomId=roster.roomId,attackTeam=0,defenseTeam=1 };
            foreach(var p in roster.members)
            {
                var m=new MatchLoadingMemberData {playerId=p.playerId,nickname=p.nickname,characterId=p.characterId,rankImageId=p.rankImageId,slotIndex=p.slotIndex};
                (p.team==0?data.attackMembers:data.defenseMembers).Add(m);
            }
            view.loadingPanel.Show(data); view.shownRoster=roster;
            view.presentation.mapTitle.text=roster.mapName.ToUpperInvariant()+"  /  경기 재접속";
        }
        view.root.SetActive(true);
        view.presentation.status.text=message;
        view.recoveryActions.SetActive(failed);
    }
    public static void Hide()
    {
        var view=FindFirstObjectByType<VectorReconnectView>(FindObjectsInactive.Include);
        if(view!=null) {
            if(view.recovering) view.backdrop.color=view.originalBackground;
            view.recovering=false; view.overlayCanvas.overrideSorting=false;
            view.recoveryActions.SetActive(false);view.root.SetActive(false);
        }
    }
}
