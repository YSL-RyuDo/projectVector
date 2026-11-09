using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler
{
    public enum StoreItemType : byte
    {
        Weapon = 0,
        Armor = 1
    }

    [Header("Store Data")]
    [SerializeField] private StoreItemType itemType;
    [SerializeField] private int itemId;
    [SerializeField] private int price;
    [SerializeField] private bool purchasable = true;

    [Header("Display")]
    [SerializeField] private string itemName;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;

    [Header("Button")]
    [SerializeField] private Button button;

    [SerializeField] private Image equipmentImage;
    [SerializeField] private EquipmentIconCatalog equipmentIcons;
    [SerializeField] private StoreWeaponDetailsUI details;
    public void OnPointerEnter(PointerEventData eventData) {if(details)details.Show(this);}
    public void OnSelect(BaseEventData eventData) {if(details)details.Show(this);}
    [SerializeField] private GameObject requestBadge;
    private float nextRequestRefresh,nextRequestClick;
    public void SetRequestState(bool pending){if(requestBadge)requestBadge.SetActive(pending);}
    public bool TryGetRequestAction(TeamPurchaseRequest current,bool buying,out byte action,out ulong token)
    {
        action=0;token=0;
        if(!buying || !IsWeaponButton() || !purchasable || price<=0)return false;
        if(current!=null && current.weaponId==itemId && current.status==1){action=1;token=current.requestId;return true;}
        return !owned;
    }
    private void Update()
    {
        if(Time.unscaledTime<nextRequestRefresh)return;nextRequestRefresh=Time.unscaledTime+.15f;
        var net=NetworkManager.Instance;TeamPurchaseRequest current=null;if(net)net.TeamPurchaseRequests?.TryGetValue(net.MyPlayerId,out current);
        SetRequestState(current!=null && current.status==1 && current.weaponId==itemId && IsWeaponButton());
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData==null || eventData.button!=PointerEventData.InputButton.Right || Time.unscaledTime<nextRequestClick)return;
        var net=NetworkManager.Instance;if(!net)return;TeamPurchaseRequest current=null;net.TeamPurchaseRequests?.TryGetValue(net.MyPlayerId,out current);
        if(!TryGetRequestAction(current,RoundManager.Instance && RoundManager.Instance.IsBuyPhase,out byte action,out ulong token))return;
        nextRequestClick=Time.unscaledTime+.4f;net.SendTeamPurchase(action,itemId,token);
    }
    private int lastCredits = 0;
    private bool lastBuyPhase = false;
    private bool owned = false;
    private bool blockedByBetterArmor = false;

    public StoreItemType ItemType => itemType;
    public int ItemId => itemId;
    public int Price => price;
    public bool IsOwned => owned;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClickBuy);

        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickBuy);
    }

    public void SetOwned(bool value)
    {
        owned = value;
        Refresh(lastCredits, lastBuyPhase);
    }

    public void SetBlockedByBetterArmor(bool value)
    {
        blockedByBetterArmor = value;
        Refresh(lastCredits, lastBuyPhase);
    }

    public bool IsWeaponButton()
    {
        return itemType == StoreItemType.Weapon;
    }

    public bool IsArmorButton()
    {
        return itemType == StoreItemType.Armor;
    }

    public bool IsSameSlotWeapon(int weaponId)
    {
        if (!IsWeaponButton())
            return false;

        if (IsPistolWeapon(itemId) && IsPistolWeapon(weaponId))
            return true;

        if (IsPrimaryWeapon(itemId) && IsPrimaryWeapon(weaponId))
            return true;

        return itemId == weaponId;
    }

    private bool IsPistolWeapon(int weaponId)
    {
        return weaponId >= 0 && weaponId <= 2;
    }

    private bool IsPrimaryWeapon(int weaponId)
    {
        return weaponId >= 3 && weaponId <= 12;
    }

    private void RefreshDisplay()
    {
        if (equipmentImage && equipmentIcons)
        {
            equipmentImage.sprite = IsArmorButton() ? equipmentIcons.Armor(itemId) : equipmentIcons.Weapon(itemId);
            equipmentImage.enabled = equipmentImage.sprite != null;
            equipmentImage.preserveAspect = true;
        }
        if (itemNameText != null)
            itemNameText.text = itemName;

        if (priceText == null)
            return;

        if (owned)
        {
            priceText.text = "보유";
            return;
        }

        if (price <= 0)
        {
            priceText.text = "무료";
            return;
        }

        priceText.text = price.ToString();
    }

    public void Refresh(int currentCredits, bool buyPhase)
    {
        lastCredits = currentCredits;
        lastBuyPhase = buyPhase;

        RefreshDisplay();

        if (button == null)
            return;

        bool canBuy =
            buyPhase &&
            purchasable &&
            !owned &&
            !blockedByBetterArmor &&
            price >= 0 &&
            currentCredits >= price;

        button.interactable = canBuy;
    }

    private void OnClickBuy()
    {
        if (owned)
        {
            Debug.Log("[STORE ITEM] 이미 보유 중인 아이템입니다.");
            return;
        }

        if (blockedByBetterArmor)
        {
            Debug.Log("[STORE ITEM] 이미 더 좋은 방어구를 보유 중입니다.");
            return;
        }

        if (!lastBuyPhase)
        {
            Debug.Log("[STORE ITEM] 구매 페이즈가 아닙니다.");
            return;
        }

        if (!purchasable)
        {
            Debug.Log("[STORE ITEM] 구매 불가능한 아이템입니다.");
            return;
        }

        if (lastCredits < price)
        {
            Debug.Log("[STORE ITEM] 크레딧이 부족합니다.");
            return;
        }

        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[STORE ITEM] NetworkManager is null");
            return;
        }

        Debug.Log(
            $"[STORE BUY CLICK] name={itemName}, type={(byte)itemType}, itemId={itemId}, price={price}"
        );

        NetworkManager.Instance.SendStoreBuyRequest(
            (byte)itemType,
            itemId
        );
    }
}