using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only presentation of the existing authoritative balance tables.
public sealed class StoreWeaponDetailsUI : MonoBehaviour
{
    public EquipmentIconCatalog icons;
    public Image artwork;
    public StoreStatBarUI[] stats;
    public Button requestButton;
    public TMP_Text requestLabel,rangeLabel;
    int selectedWeapon=-1;
    void RefreshRequest(){if(requestButton)requestButton.gameObject.SetActive(false);}
    public TMP_Text title,category,handling,damageTitle,damageHeader,damageRows,falloffNote;
    public void Show(StoreItemButton item)
    {
        if(item.IsArmorButton()) {ShowArmor(item.ItemId);return;}
        ShowWeapon(item.ItemId);
    }
    public void ShowWeapon(int id)
    {
        if(id<0 || id>=WeaponStoreStats.All.Length)return;
        selectedWeapon=id;RefreshRequest();
        var data=WeaponStoreStats.All[id];title.text=data.name;
        if(stats!=null && stats.Length==3){stats[0].Present("연사",$"{1f/data.interval:0.##} 발/초",(1f/data.interval)/20f);stats[1].Present("탄창",$"{data.magazine} / {data.reserve} 발",data.magazine/100f);stats[2].Present("재장전",$"{data.reload:0.##} 초",data.reload/5f);}
        category.text=data.category switch {"Pistol"=>"보조 무기","AR"=>"돌격소총","SMG"=>"기관단총","LMG"=>"기관총","Shotgun"=>"산탄총","SR"=>"저격소총","DMR"=>"지정사수소총",_=>"근접 무기"};
        artwork.sprite=icons.Weapon(id);artwork.enabled=artwork.sprite;
        handling.text=$"탄창  {data.magazine}발     예비탄  {data.reserve}발\n발사 간격  {data.interval:0.###}초\n초당 발사  {1f/data.interval:0.##}발\n재장전  {data.reload:0.##}초\n사거리  {data.range:0.##}m";
        if(rangeLabel)rangeLabel.text=$"사거리  {data.range:N0} m";
        damageTitle.text=data.category=="Shotgun"?"산탄 1발당 피해":"명중 피해";
        damageHeader.text="거리<pos=188>머리<pos=275>몸통<pos=363>다리";
        double mid=(data.falloffStart+data.falloffEnd)*.5;
        string Row(string distance,double at)=>$"{distance}<pos=188>{WeaponDamage.Calculate(id,data.damage,at,1)}<pos=275>{WeaponDamage.Calculate(id,data.damage,at,0)}<pos=363>{WeaponDamage.Calculate(id,data.damage,at,2)}";
        damageRows.text=Row($"0–{data.falloffStart:0.#}m",0)+"\n"+Row($"{mid:0.#}m",mid)+"\n"+Row($"{data.falloffEnd:0.#}m 이상",data.falloffEnd);
        falloffNote.text=$"{data.falloffStart:0.#}–{data.falloffEnd:0.#}m에서 피해가 연속 감소\n방어막 적용 전 수치"+(data.category=="Shotgun"?"\n한 번 발사할 때 산탄 12발":"");
    }
    public void ShowArmor(int id)
    {
        selectedWeapon=-1;RefreshRequest();if(rangeLabel)rangeLabel.text="";
        if(stats!=null && stats.Length==3){stats[0].Present("최대 방어막",id==0?"50":"100",id==0?.5f:1f);stats[1].Present("","",0);stats[2].Present("","",0);}
        title.text=id==0?"경량 방어막":"중량 방어막";category.text="방어 장비";artwork.sprite=icons.Armor(id);artwork.enabled=artwork.sprite;
        handling.text="최대 방어막  "+(id==0?50:100);damageTitle.text="";damageHeader.text="";damageRows.text="";falloffNote.text="피해를 받으면 방어막이 차감됩니다.";
    }
}
