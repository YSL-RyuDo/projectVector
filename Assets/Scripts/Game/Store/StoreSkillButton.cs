using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreSkillButton : MonoBehaviour
{
    private const byte SkillStoreItemType = 2;

    [SerializeField] private SkillSlot slot;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

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

        if (label != null)
        {
            if (used)
                label.text = $"{slot}  사용 완료";
            else if (purchased)
                label.text = $"{slot}  보유";
            else
                label.text = $"{slot} 스킬  {price}";
        }

        if (button != null)
        {
            button.interactable =
                buyPhase &&
                !purchased &&
                currentCredits >= price;
        }
    }

    private void OnClickBuy()
    {
        int price = SkillInventoryState.GetPrice(slot);

        if (!buyPhase ||
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
