using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All panels, buttons and cards are authored and saved in Lobby. Never constructs UI.
public sealed class VectorShopNavigation : MonoBehaviour
{
    public VectorShopPanel shop;
    public GameObject[] pages;
    public Image[] tabs, filters;
    public GameObject legacyMoney;
    public TMP_Text wallet, catalogCount, sortLabel;
    public ScrollRect catalogScroll;
    public VectorShopSlot[] products;
    public int CurrentPage { get; private set; }
    private int filter = -1;
    private int sort;
    private VectorShopState state;
    private static readonly Color Selected = new Color32(194,39,57,255);
    private static readonly Color Normal = new Color32(65,71,81,255);
    private void OnEnable() => SelectPage(0);
    public void SelectPage(int page)
    {
        if (pages == null || shop == null || page < 0 || page >= pages.Length ||
            (shop.sequence != null && shop.sequence.IsPlaying)) return;
        shop.CloseDialogs(); CurrentPage = page;
        for (int i=0;i<pages.Length;++i) pages[i].SetActive(i==page);
        for (int i=0;i<tabs.Length;++i) tabs[i].color=i==page?Selected:Normal;
        if (legacyMoney != null) legacyMoney.SetActive(page==2 || page==3);
    }
    public void Filter(int kind)
    {
        filter=kind; ApplyCatalog();
        for(int i=0;i<filters.Length;++i) filters[i].color=i==kind+1?Selected:Normal;
    }
    public void DefaultOrder() { sort=0; ApplyCatalog(); }
    public void TogglePriceOrder() { sort=sort==1?2:1; ApplyCatalog(); }
    public void OpenProduct(int id) { shop.ShowDetails(); shop.SelectReward(id); }
    public void Refresh(VectorShopState value, Func<VectorShopItem,Sprite> icon)
    {
        state=value; wallet.text="";
        foreach(var slot in products)
        {
            var item=state.items.FirstOrDefault(i=>i.id==slot.itemId);
            if(item==null) continue;
            slot.icon.sprite=icon(item); slot.rarity.color=VectorShopPanel.GradeColors[item.grade];
            slot.label.text=$"{item.name}\n<color=#FFD36A>재화 0  {item.price:N0}</color>"+(state.owned.Contains(item.id)?"  ·  보유":"");
        }
        ApplyCatalog();
    }
    private void ApplyCatalog()
    {
        if(products==null) return;
        var ordered=products.OrderBy(s=>s.itemId).ToArray();
        if(state!=null && sort!=0)
            ordered=(sort==1?products.OrderBy(s=>state.items.FirstOrDefault(i=>i.id==s.itemId)?.price??int.MaxValue):products.OrderByDescending(s=>state.items.FirstOrDefault(i=>i.id==s.itemId)?.price??0)).ThenBy(s=>s.itemId).ToArray();
        int count=0;
        for(int i=0;i<ordered.Length;++i)
        {
            var slot=ordered[i]; int kind=slot.itemId>=1000?1:0;
            bool visible=(filter<0 || kind==filter) && (state==null || state.items.Any(item=>item.id==slot.itemId));
            slot.root.SetActive(visible); slot.root.transform.SetSiblingIndex(i); if(visible) ++count;
        }
        catalogCount.text=$"{count}개 상품";
        sortLabel.text=sort==2?"가격 높은순 ↓":sort==1?"가격 낮은순 ↑":"가격순 ↕";
        Canvas.ForceUpdateCanvases(); catalogScroll.verticalNormalizedPosition=1;
    }
}
