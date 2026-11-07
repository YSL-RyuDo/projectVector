using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchLoadingPresentation : MonoBehaviour
{
    public TMP_Text mapTitle, status;
    public MatchIdentityCard[] cards;
    public Image[] portraits;
    public TMP_Text[] roles;
    [Header("2D Character Illustrations (character ID order)")]
    public Sprite[] characterArt;
    [Header("Optional transparent versions; original pick artwork remains unchanged")]
    public Sprite[] portraitOverrides;
    public void Present(MatchLoadingInfoData data)
    {
        var net=NetworkManager.Instance;
        var pick=FindFirstObjectByType<PickMapBackdrop>(FindObjectsInactive.Include);
        mapTitle.text=(pick!=null && pick.SelectedMap==1?"DESERT":"TEST MAP")+"  /  3 대 3";
        status.text="전투 정보 확인 중";
        for(int i=0;i<cards.Length;i++)
        {
            var team=i<3?data.attackMembers:data.defenseMembers; int slot=i%3;
            var member=slot<team.Count?team[slot]:null;
            MatchAppearanceData appearance=null;
            if(member!=null && net!=null) net.ReadyCardAppearances.TryGetValue(member.playerId,out appearance);
            cards[i].Present(member?.nickname??"",appearance,true,member!=null && net!=null && member.playerId==net.MyPlayerId);
            // Custom rooms/old servers may have no cosmetics snapshot. Never invent one.
            cards[i].nickname.text=member?.nickname??"빈 자리";
            cards[i].caption.text=member==null?"":i<3?"선 공격":"선 수비";
            cards[i].concealed.SetActive(false);
            int id=member?.characterId??-1;
            portraits[i].sprite=id>=0&&id<characterArt.Length?characterArt[id]:null;
            if(id>=0 && portraitOverrides!=null && id<portraitOverrides.Length && portraitOverrides[id]!=null)
                portraits[i].sprite=portraitOverrides[id];
            portraits[i].enabled=portraits[i].sprite!=null;
            string[] names={"이니시에이터","컨트롤러","센티널","듀얼리스트"};
            roles[i].text=id>=0&&id<names.Length?names[id]:"";
        }
    }
}
