using System.Linq;
using TMPro;
using UnityEngine;

public sealed class StoreTeamPanelUI : MonoBehaviour
{
    public StoreTeamRowUI[] rows;
    public Sprite[] portraits;
    public TMP_Text notice;
    float refreshAt;
    void OnEnable(){refreshAt=0;}
    void Update()
    {
        if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.15f;
        if (!ClientMatchContext.State.AllowsTeamPurchases)
        {
            // 기존 팀 목록 행만 숨기고 다른 모드에서 복원할 수 있도록 패널은 유지한다.
            foreach (var row in rows) if (row) row.gameObject.SetActive(false);
            if (notice) notice.text = "개인전에서는 팀 구매를 이용할 수 없습니다";
            return;
        }
        var net=NetworkManager.Instance;if(!net || net.ShopScoreboard==null)return;
        var board=net.ShopScoreboard;var mine=board.members.Find(m=>m.playerId==net.MyPlayerId);
        Present(board,net.MyPlayerId,mine?.credits??0,net.TeamPurchaseRequests,RoundManager.Instance && RoundManager.Instance.IsBuyPhase);
        notice.text=net.TeamPurchaseNotice;
    }
    public void Present(ScoreboardData board,uint myId,int budget,System.Collections.Generic.IReadOnlyDictionary<uint,TeamPurchaseRequest> requests,bool buying)
    {
        if (!ClientMatchContext.State.AllowsTeamPurchases)
        {
            foreach (var row in rows) if (row) row.gameObject.SetActive(false);
            return;
        }
        var team=board.members.Where(m=>m.team==board.myTeam).OrderByDescending(m=>m.playerId==myId).ThenBy(m=>m.slotIndex).Take(rows.Length).ToArray();
        for(int i=0;i<rows.Length;i++) {
            var member=i<team.Length?team[i]:null;TeamPurchaseRequest request=null;
            if(member!=null)requests?.TryGetValue(member.playerId,out request);
            var sprite=member!=null && member.characterId>=0 && member.characterId<portraits.Length?portraits[member.characterId]:null;
            rows[i].Present(member,sprite,myId,budget,request,buying,board);
        }
    }
}
