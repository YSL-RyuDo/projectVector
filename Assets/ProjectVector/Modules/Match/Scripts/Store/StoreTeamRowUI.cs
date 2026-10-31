using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All elements are authored in the scene. Only values and visibility change here.
public sealed class StoreTeamRowUI : MonoBehaviour
{
    public Image portrait,accent;
    public TMP_Text nickname,credits,requestText,actionText;
    public LoadoutSummaryUI loadout;
    public Button action;
    TeamPurchaseRequest current;
    bool own;
    float nextClick;
    void Awake(){action.onClick.AddListener(Act);}
    void OnDestroy(){if(action)action.onClick.RemoveListener(Act);}
    void Act()
    {
        if(current==null || Time.unscaledTime<nextClick)return;
        nextClick=Time.unscaledTime+.5f;
        NetworkManager.Instance?.SendTeamPurchase(own?(byte)1:(byte)2,current.weaponId,current.requestId);
    }
    public void Present(ScoreboardMemberData member,Sprite sprite,uint myId,int budget,TeamPurchaseRequest request,bool buying,ScoreboardData board)
    {
        gameObject.SetActive(member!=null);if(member==null)return;
        own=member.playerId==myId;current=request;
        portrait.sprite=sprite;portrait.enabled=sprite;nickname.text=member.nickname+(own?"  나":"");credits.text=member.credits.ToString("N0");
        accent.color=own?new Color(.48f,.84f,.94f):new Color(.23f,.33f,.42f);
        loadout.SetData(member);requestText.text="";action.gameObject.SetActive(false);
        if(request==null || !buying)return;
        var weapon=WeaponStoreStats.All[request.weaponId].name;
        if(request.status==2){var payer=board?.members.Find(m=>m.playerId==request.payerId);requestText.text=weapon+", "+(payer?.nickname??"아군")+" 구매 완료";return;}
        requestText.text=weapon+" 요청";
        action.gameObject.SetActive(true);actionText.text=own?"요청 취소":budget>=request.price?$"사주기  {request.price:N0}":$"부족  {request.price-budget:N0}";
        action.interactable=buying && (own || budget>=request.price) && (!(member.hasLifeState || member.hasLoadout) || member.equipmentAlive);
    }
}
