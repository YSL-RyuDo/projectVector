using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Saved scene objects only. The existing HUD continues displaying the followed ally's vitals/ammo.
public sealed class SpectatorHUD : MonoBehaviour
{
    public GameObject root;
    public CanvasGroup presentation;
    public TMP_Text playerName,state,weaponName,controls,ultimate;
    public Image portrait,weapon;
    public EquipmentIconCatalog icons;
    public Sprite[] portraits;
    void Awake() { if(root)root.SetActive(false); }
    void LateUpdate()
    {
        var spectate=SpectatorManager.Instance;
        if(!spectate || !spectate.IsSpectating) { if(root)root.SetActive(false); return; }
        if(root)root.SetActive(true);
        var net=NetworkManager.Instance;uint id=spectate.CurrentTargetPlayerId;
        var member=net?.CurrentScoreboard?.members.Find(m=>m.playerId==id && m.team==net.CurrentScoreboard.myTeam);
        PlayerCombatSnapshot snapshot=null;
        if(net && id!=0)net.TryGetCombatSnapshot(id,out snapshot);
        Present(member,snapshot,spectate.TargetCount,spectate.IsDroneView);
        if(presentation){presentation.alpha=ReportUI.OverlayBlocked?0:1;presentation.blocksRaycasts=false;}
    }
    public void Present(ScoreboardMemberData member,PlayerCombatSnapshot snapshot,int count,bool drone)
    {
        if(root)root.SetActive(true);
        bool available=member!=null && count>0;
        playerName.richText=false;playerName.text=available?member.nickname:"생존한 아군 없음";
        state.text=available?(drone?"드론 시점":"아군 관전"):"라운드 종료 대기";
        controls.text=available && count>1?"우클릭  이전    |    좌클릭  다음":"";
        portrait.enabled=available && member.characterId>=0 && member.characterId<portraits.Length;
        if(portrait.enabled)portrait.sprite=portraits[member.characterId];
        int id=snapshot?.weaponId??-1;
        weapon.enabled=available && id>=0 && icons && icons.Weapon(id);
        if(weapon.enabled)weapon.sprite=icons.Weapon(id);
        weaponName.text=available && id>=0?ReportUI.GetSourceDisplayName(0,id,0):"";
        ultimate.text=available && member.hasLoadout?(member.ultimatePoints>=member.ultimateRequired?"궁극기 준비 완료":$"궁극기 {member.ultimatePoints} / {member.ultimateRequired}"):"";
    }
}
