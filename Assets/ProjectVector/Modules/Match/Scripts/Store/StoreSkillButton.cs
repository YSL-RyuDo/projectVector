using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreSkillButton : MonoBehaviour
{
    private const byte SkillStoreItemType = 2;

    [SerializeField] private SkillSlot slot;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text keyLabel, priceLabel, stateLabel;
    private int currentCredits;
    private bool buyPhase;
    private bool listenerRegistered;

    public SkillSlot Slot => slot;

    private void Awake()
    {
        ResolveReferences();
        RegisterListener();
    }

    private void OnEnable()
    {
        SkillInventoryState.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        SkillInventoryState.Changed -= Refresh;
    }

    private void OnDestroy()
    {
        if (button != null && listenerRegistered)
            button.onClick.RemoveListener(OnClickBuy);
    }

    public void Configure(SkillSlot value)
    {
        slot = value;
        ResolveReferences();
        RegisterListener();
        Refresh();
    }

    public void Refresh(int credits, bool isBuyPhase)
    {
        currentCredits = credits;
        buyPhase = isBuyPhase;
        Refresh();
    }

    private void Refresh()
    {
        int price = SkillInventoryState.GetPrice(slot);
        bool purchased = SkillInventoryState.IsPurchased(slot);
        bool used = SkillInventoryState.IsUsed(slot);

        var data = SkillInventoryState.Skill(slot);
        if (keyLabel) keyLabel.text = slot.ToString();
        if (skillIcon) { skillIcon.sprite = data?.skillIcon; skillIcon.enabled = skillIcon.sprite; }
        if (label) label.text = data != null ? data.skillName : slot == SkillSlot.X ? "궁극기" : "스킬 " + slot;
        if (priceLabel) priceLabel.text = used ? "사용 완료" : purchased ? (slot == SkillSlot.X ? "준비 완료" : "보유") :
            slot == SkillSlot.X && !SkillInventoryState.HasUltimateState ? "—" : price.ToString();
        if (stateLabel) stateLabel.text = slot == SkillSlot.X ?
            (SkillInventoryState.HasUltimateState ? SkillInventoryState.UltimatePoints+" / "+SkillInventoryState.UltimateRequired+" 충전" : "충전 확인 중") :
            (used ? "다음 라운드 구매" : purchased ? "사용 가능" : "1회 사용");

        if (button != null)
        {
            button.interactable =
                buyPhase && (slot != SkillSlot.X || SkillInventoryState.HasUltimateState) &&
                !purchased &&
                currentCredits >= price;
        }
    }

    private void OnClickBuy()
    {
        int price = SkillInventoryState.GetPrice(slot);

        if (!buyPhase || (slot == SkillSlot.X && !SkillInventoryState.HasUltimateState) ||
            SkillInventoryState.IsPurchased(slot) ||
            currentCredits < price ||
            NetworkManager.Instance == null)
        {
            return;
        }

        NetworkManager.Instance.SendStoreBuyRequest(
            SkillStoreItemType,
            (int)slot
        );
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);
    }

    private void RegisterListener()
    {
        if (button == null || listenerRegistered)
            return;

        button.onClick.AddListener(OnClickBuy);
        listenerRegistered = true;
    }
}
