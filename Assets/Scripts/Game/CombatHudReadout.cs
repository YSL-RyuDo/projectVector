using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatHudReadout : MonoBehaviour
{
    public TMP_Text health,armor,healthState,ammoState,reserve;
    public TMP_Text[] skillStates = new TMP_Text[4];
    public Graphic[] skillStateBackings = new Graphic[4];
    public HudShapeGraphic ultimateArc;
    public TMP_Text chargeText;
    public Image healthAccent,armorIcon;
    public GameObject reloadRoot;
    public GameObject ammoRoot;
    public TMP_Text reloadText;
    public CanvasGroup skillGroup;
    public EquipmentIconCatalog icons;
    private int ammo;
    private PlayerIdentity subject;
    private float nextFind;
    private static readonly Color Ice=new(.67f,.90f,1), Danger=new(1,.38f,.37f), Gold=new(1,.81f,.43f);

    public void PresentHealth(int hp,int shield,int maxHp,int maxShield)
    {
        hp=Mathf.Clamp(hp,0,Mathf.Max(1,maxHp));shield=Mathf.Clamp(shield,0,Mathf.Max(0,maxShield));
        bool low=hp>0 && hp<=maxHp*.3f;
        health.text=hp.ToString();health.color=low?Danger:Color.white;
        armor.text=shield.ToString();armor.color=shield>0?Ice:new Color(.61f,.66f,.71f);
        healthState.text=hp==0?"사망":low?"체력 위험":"체력";healthState.color=low?Danger:new Color(.78f,.84f,.89f);
        if(healthAccent)healthAccent.color=low?Danger:Ice;
        if(armorIcon && icons){armorIcon.sprite=icons.Armor(maxShield>50?1:0);armorIcon.color=shield>0?Ice:new Color(.47f,.53f,.60f);}
    }
    public void PresentAmmo(int current,int remaining)
    {ammo=current;if(reserve)reserve.text=remaining.ToString();PresentReload(false);}
    public void PresentReload(bool reloading)
    {
        if(reloadRoot)reloadRoot.SetActive(reloading);
        if(reloadText)reloadText.text="재장전 중";
        if(ammoState){ammoState.text=reloading?"재장전":ammo==0?"탄창 비어 있음":"잔탄";ammoState.color=reloading?Gold:ammo==0?Danger:new Color(.78f,.84f,.89f);}
    }
    public void PresentSkills(bool spectating=false,bool blocked=false)
    {
        for(int i=0;i<skillStates.Length;i++)if(skillStates[i])
        {
            var slot=(SkillSlot)i;bool ready=SkillInventoryState.CanUse(slot),used=SkillInventoryState.IsUsed(slot);
            skillStates[i].text=spectating?"관전":used?"사용 완료":blocked?"사용 불가":ready?(i==3?"준비 완료":""):i==3?"충전 중":"미보유";
            if(i<skillStateBackings.Length && skillStateBackings[i])skillStateBackings[i].enabled=skillStates[i].text.Length>0;
            skillStates[i].color=ready && !blocked?Ice:new Color(.68f,.72f,.77f);
        }
        if(ultimateArc){ultimateArc.SetCharge(spectating?0:SkillInventoryState.UltimatePoints,SkillInventoryState.UltimateRequired);ultimateArc.color=SkillInventoryState.CanUse(SkillSlot.X)?Gold:Ice;}
        if(chargeText)chargeText.text=spectating||!SkillInventoryState.HasUltimateState?"—":$"{SkillInventoryState.UltimatePoints} / {SkillInventoryState.UltimateRequired}";
        if(skillGroup)skillGroup.alpha=(spectating||blocked) ? .5f : 1;
    }
    void Update()
    {
        var net=NetworkManager.Instance;if(!net)return;
        bool spectating=SpectatorManager.Instance && SpectatorManager.Instance.IsSpectating;
        uint id=spectating?SpectatorManager.Instance.CurrentTargetPlayerId:net.MyPlayerId;
        if((!subject || subject.PlayerId!=id) && Time.unscaledTime>=nextFind)
        {nextFind=Time.unscaledTime+.5f;subject=null;foreach(var p in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))if(p.PlayerId==id){subject=p;break;}}
        if(!subject){PresentReload(false);return;}
        var item=subject.GetComponent<PlayerItem>();
        var weapon=item?item.GetCurrentWeapon():null;
        if(ammoRoot)ammoRoot.SetActive(weapon && weapon.GetWeaponType()!=WeaponData.WeaponType.Knife);
        PresentReload(!spectating && item && item.IsReloading);
        bool blocked=!spectating && !GameplayActionPolicy.CanAct(subject,GameplayAction.Skill,false);
        PresentSkills(spectating,blocked);
    }
    void OnDisable(){subject=null;PresentReload(false);}
}
