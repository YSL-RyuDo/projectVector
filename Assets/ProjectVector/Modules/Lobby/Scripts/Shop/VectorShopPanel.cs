using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class VectorShopSlot
{
    public int itemId;
    public GameObject root;
    public Image icon, rarity;
    public TMP_Text label;
}

// Scene-authored UI only. No UI creation, destruction or layout in play mode.
public sealed class VectorShopPanel : MonoBehaviour
{
    [Header("Saved Lobby hierarchy")]
    public GameObject rewardDetails, historyPanel, probabilityPanel, resultPanel, purchasePanel;
    public GameObject[] rarityGroups;
    public TMP_Text balance, status, description, historyText, historyPage, probabilityText, purchaseText;
    public VectorShopPreview preview;
    public GachaManager sequence;
    public WeaponSkinDatabase skins;
    public VectorShopSlot[] rewardSlots, resultSlots;
    public Button[] transactionButtons;
    public TMP_Text drawCost, currencyButtonLabel;
    public Button retryButton;
    private static VectorShopPanel instance;
    private VectorShopState state;
    private VectorShopItem selected;
    private int currency, pendingCurrency, pendingCount, pendingItem, purchaseItem;
    private byte pendingOperation;
    private ulong pending;
    private float pendingSince;
    private readonly Dictionary<string, Sprite> icons = new();
    public static readonly Color[] GradeColors = { new Color32(255,203,78,255), new Color32(204,133,255,255), new Color32(104,205,244,255) };

    private void Awake()
    {
        instance = this;
        foreach (var icon in Resources.LoadAll<Sprite>("Shop/ShopRewardAtlas")) icons[icon.name] = icon;
    }
    private void OnEnable() { RequestState(); if (state != null) Refresh(); }
    private void OnDisable() { if (sequence != null) sequence.Dismiss(); CloseDialogs(); }
    private void OnDestroy() { if (instance == this) instance = null; }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (sequence != null && sequence.IsPlaying && sequence.CurrentPhase != GachaManager.Phase.Summary) sequence.Skip();
            else if (resultPanel == null || !resultPanel.activeSelf) CloseDialogs();
        }
        if (pending != 0 && Time.unscaledTime - pendingSince > 10)
        { status.text = "연결이 지연되고 있습니다. 다시 확인해 주세요."; if(retryButton != null) retryButton.gameObject.SetActive(true); }
    }
    private void RequestState(int page = 0) => NetworkManager.Instance?.SendShopRequest(0, 0, 1, 0, 0,
        Mathf.Clamp(page, 0, VectorShopState.HistoryPageLimit - 1));
    public static void Receive(byte[] bytes)
    {
        if (instance == null) return;
        try { instance.Apply(VectorShopState.Parse(bytes)); }
        catch (Exception e) { Debug.LogError("[SHOP] " + e.Message); instance.status.text = "상점 정보를 불러오지 못했습니다."; }
    }
    private void Apply(VectorShopState value)
    {
        state = value;
        if(value.status!=1) MoneyManager.Instance?.SetServerBalances(value.coin0, value.coin1);
        bool transaction = pending != 0 && pending == value.request;
        if (transaction) pending = 0;
        Refresh();
        if (value.status != 0)
        {
            status.text = new[] { "", "상점 데이터 오류", "잘못된 요청", "재화가 부족합니다", "이미 상점에서 보유한 스킨입니다", "저장 실패. 재화는 차감되지 않았습니다" }[Mathf.Clamp(value.status,0,5)];
            return;
        }
        status.text = "";
        if(retryButton != null) retryButton.gameObject.SetActive(false);
        if (transaction && value.rewards.Count > 0)
        {
            CloseDialogs();
            if (isActiveAndEnabled && sequence != null)
                sequence.Play(value.rewards, value.items, ResolveModel, () => ShowResults(value.rewards));
            else ShowResults(value.rewards);
        }
        if (historyPanel.activeSelf) FillHistory();
    }
    private void Refresh()
    {
        if (state == null) return;
        balance.text = ""; // Existing MoneyManager/header is the only balance display.
        RefreshCost();
        var navigation = GetComponent<VectorShopNavigation>();
        if (navigation != null) navigation.Refresh(state, Icon);
        foreach (var button in transactionButtons) if (button != null) button.interactable = pending == 0 && state.status != 1 && state.items.Count > 0;
        foreach (var slot in rewardSlots)
        {
            var item = state.items.FirstOrDefault(i => i.id == slot.itemId);
            slot.root.SetActive(item != null);
            if (item == null) continue;
            slot.icon.sprite = Icon(item);
            slot.label.text = $"{item.name}\n{state.Chance(item):0.###}% · {(state.owned.Contains(item.id) ? "보유" : "미보유")}";
        }
        if (selected != null) UpdateDescription();
    }
    public void DrawOne() => Transact(1,1,0);
    public void DrawTen() => Transact(1,10,0);
    public void ChangeCurrency()
    {
        currency = 1 - currency;
        RefreshCost();
        UpdateDescription();
    }
    private void RefreshCost()
    {
        if(drawCost!=null) drawCost.text=$"1회  { (currency==0?300:1):N0}    ·    10회  {(currency==0?3000:10):N0}";
        if(currencyButtonLabel!=null) currencyButtonLabel.text=$"재화 {currency} · 변경";
    }
    private void Transact(byte operation, int count, int item)
    {
        if (pending != 0 || state == null || state.status == 1 || state.items.Count == 0 || sequence.IsPlaying) return;
        pending = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(),0); if (pending == 0) pending = 1;
        pendingOperation = operation; pendingCount = count; pendingItem = item; pendingCurrency = operation == 2 ? 0 : currency;
        SendPending();
    }
    private void SendPending()
    {
        pendingSince = Time.unscaledTime;
        foreach (var button in transactionButtons) if (button != null) button.interactable = false;
        status.text = "처리 중…";
        NetworkManager.Instance?.SendShopRequest(pendingOperation,pendingCurrency,pendingCount,pendingItem,pending,0);
    }
    public void Retry() { if (pending != 0) SendPending(); else RequestState(); }
    public void CloseDialogs()
    {
        if (sequence != null && sequence.CurrentPhase == GachaManager.Phase.Summary) sequence.Dismiss();
        foreach (var panel in new[] { rewardDetails, historyPanel, probabilityPanel, resultPanel, purchasePanel }) if (panel != null) panel.SetActive(false);
    }
    public void ShowDetails() => ShowGrade(-1);
    public void ShowGrade(int grade)
    {
        CloseDialogs(); rewardDetails.SetActive(true);
        for (int i=0;i<rarityGroups.Length;++i) rarityGroups[i].SetActive(grade < 0 || i == grade);
        if (state == null) { status.text = "상점 데이터를 기다리는 중입니다"; RequestState(); return; }
        var item = state.items.FirstOrDefault(i => grade < 0 || i.grade == grade);
        if (item != null) SelectReward(item.id);
    }
    public void SelectReward(int id)
    {
        selected = state?.items.FirstOrDefault(i => i.id == id);
        if (selected == null) return;
        foreach (var slot in rewardSlots)
        {
            var outline = slot.root.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = slot.itemId == id;
        }
        preview.Show(ResolveModel(selected)); UpdateDescription();
    }
    private void UpdateDescription()
    {
        if (selected == null || state == null) return;
        description.text = $"{selected.name}\n\n{selected.GradeName} · {(selected.kind == 0 ? "캐릭터 외형" : "무기 스킨")}\n\n개별 확률 {state.Chance(selected):0.###}%\n{(state.owned.Contains(selected.id) ? "보유 중" : "미보유")}\n\n재화 0 ×{selected.price:N0}";
    }
    public void ConfirmPurchase()
    {
        if (selected == null) return;
        purchaseItem = selected.id; purchaseText.text = $"{selected.name}\n재화 0 ×{selected.price:N0}\n구매하시겠습니까?";
        purchasePanel.SetActive(true);
    }
    public void Purchase() { purchasePanel.SetActive(false); Transact(2,1,purchaseItem); }
    public void ShowProbabilities()
    {
        CloseDialogs(); probabilityPanel.SetActive(true);
        if (state == null) return;
        probabilityText.text = "신화 5% · 희귀 24% · 고급 71%\n등급 내 균등 확률 / 매 회 독립 추첨 / 천장 없음\n1회: 재화0 300 또는 재화1 1 / 10회: 재화0 3,000 또는 재화1 10\n중복 환급(재화0): 신화150 · 희귀75 · 고급30\n\n" + string.Join("\n", state.items.OrderBy(i => i.grade).Select(i => $"[{i.GradeName}] {i.name}   {state.Chance(i):0.######}%"));
    }
    public void ShowHistory() { CloseDialogs(); historyPanel.SetActive(true); FillHistory(); RequestState(); }
    public void PreviousHistory() { if (state != null && state.page > 0) RequestState(state.page - 1); }
    public void NextHistory() { if (state != null && state.page + 1 < Mathf.Min(state.pages, VectorShopState.HistoryPageLimit)) RequestState(state.page + 1); }
    private void FillHistory()
    {
        if (state == null) return;
        historyPage.text = $"{state.page+1} / {state.pages}";
        historyText.text = state.history.Count == 0 ? "아직 획득 기록이 없습니다." : string.Join("\n\n", state.history.Select(r => $"{DateTimeOffset.FromUnixTimeSeconds(r.time).ToLocalTime():yyyy-MM-dd HH:mm:ss}   {state.items.FirstOrDefault(i=>i.id==r.item)?.name}   {(r.duplicate ? $"중복 · 재화0 +{r.refund}" : "NEW ×1")}"));
    }
    private void ShowResults(IReadOnlyList<VectorShopReward> rewards)
    {
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        for (int i=0;i<resultSlots.Length;++i)
        {
            var slot = resultSlots[i]; slot.root.SetActive(i < rewards.Count);
            if (i >= rewards.Count) continue;
            var reward = rewards[i]; var item = state.items.FirstOrDefault(x=>x.id==reward.item); if (item == null) continue;
            slot.icon.sprite = Icon(item); slot.rarity.color = GradeColors[item.grade];
            slot.label.text = $"{item.name}\n×1 · {(reward.duplicate ? $"중복 / 재화0 +{reward.refund}" : "NEW")}";
        }
    }
    private Sprite Icon(VectorShopItem item)
    {
        icons.TryGetValue(item.kind == 0 ? "character_"+item.model : $"weapon_{item.model}_{item.skin}", out var icon); return icon;
    }
    private GameObject ResolveModel(VectorShopItem item) => item.kind == 0
        ? Resources.Load<GameObject>("LobbyCharacter/" + new[] { "Casual1","PicoChan","unitychan","Controller" }[Mathf.Clamp(item.model,0,3)])
        : skins?.GetById(item.model,item.skin)?.visualModelPrefab;
}
