using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StorePanelUI : MonoBehaviour
{
    public static StorePanelUI Instance;

    [Header("Root")]
    [SerializeField] private GameObject storePanelRoot;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Player Info")]
    [SerializeField] private Image playerCharacterImage;
    [SerializeField] private TMP_Text playerNicknameText;
    [SerializeField] private TMP_Text playerCreditText;

    [Header("Character Sprites")]
    [SerializeField] private Sprite[] characterSprites;

    [Header("Store Items")]
    [SerializeField] private StoreItemButton[] itemButtons;

    private int currentCredits = 0;

    private void Awake()
    {
        Instance = this;

        if (storePanelRoot != null)
            storePanelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        ResetOwnedItemsForNewRound();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (IsOpen())
                Close();
            else
                TryOpen();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && IsOpen())
        {
            Close();
        }

        if (IsOpen() && !CanOpenStore())
        {
            Close();
        }
    }

    private bool IsOpen()
    {
        return storePanelRoot != null && storePanelRoot.activeSelf;
    }

    private bool CanOpenStore()
    {
        if (RoundManager.Instance == null)
            return false;

        return RoundManager.Instance.IsBuyPhase;
    }

    public void TryOpen()
    {
        if (!CanOpenStore())
        {
            Debug.Log("[STORE] 구매 페이즈가 아니라 열 수 없습니다.");
            return;
        }

        GameController.Instance.isLoading = true;

        Open();
    }

    public void Open()
    {
        if (storePanelRoot == null)
            return;

        RefreshPlayerInfo();
        RefreshItemButtons();

        storePanelRoot.SetActive(true);
        storePanelRoot.transform.SetAsLastSibling();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        if (storePanelRoot == null)
            return;

        GameController.Instance.isLoading = false;

        storePanelRoot.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[STORE] close");
    }

    public void RefreshPlayerInfo()
    {
        if (NetworkManager.Instance == null)
            return;

        if (!NetworkManager.Instance.TryGetMyScoreboardMember(out ScoreboardMemberData myData))
            return;

        if (playerNicknameText != null)
            playerNicknameText.text = myData.nickname;

        currentCredits = myData.credits;

        if (playerCreditText != null)
            playerCreditText.text = currentCredits.ToString();

        RefreshItemButtons();

        if (playerCharacterImage != null)
        {
            Sprite sprite = GetCharacterSprite(myData.characterId);

            playerCharacterImage.sprite = sprite;
            playerCharacterImage.enabled = sprite != null;
        }
    }

    public void SetCreditDirect(int credits)
    {
        currentCredits = credits;

        if (playerCreditText != null)
            playerCreditText.text = currentCredits.ToString();

        RefreshItemButtons();
    }

    private void RefreshItemButtons()
    {
        if (itemButtons == null)
            return; 

        bool buyPhase = CanOpenStore();

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            itemButton.Refresh(currentCredits, buyPhase);
        }
    }

    public void ResetOwnedItemsForNewRound()
    {
        if (itemButtons == null)
            return;

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            itemButton.SetOwned(false);
        }

        // 기본 권총 P92는 라운드 시작 시 기본 보유
        SetOwnedWeaponFromServer(0);

        RefreshItemButtons();
    }

    public void SetOwnedWeaponFromServer(int weaponId)
    {
        if (itemButtons == null)
            return;

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            if (!itemButton.IsWeaponButton())
                continue;

            // 같은 슬롯 무기는 전부 보유 해제
            // 예: AK 보유 중 AWM 구매하면 AK는 보유 해제, AWM은 보유
            if (itemButton.IsSameSlotWeapon(weaponId))
                itemButton.SetOwned(false);
        }

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            if (!itemButton.IsWeaponButton())
                continue;

            if (itemButton.ItemId == weaponId)
            {
                itemButton.SetOwned(true);
                break;
            }
        }

        RefreshItemButtons();
    }

    public void RemoveOwnedWeaponFromServer(int weaponId)
    {
        if (itemButtons == null)
            return;

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            if (!itemButton.IsWeaponButton())
                continue;

            if (itemButton.ItemId == weaponId)
            {
                itemButton.SetOwned(false);
                break;
            }
        }

        RefreshItemButtons();
    }

    public void SetOwnedArmorFromServer(int maxBarrier)
    {
        if (itemButtons == null)
            return;

        foreach (StoreItemButton itemButton in itemButtons)
        {
            if (itemButton == null)
                continue;

            if (!itemButton.IsArmorButton())
                continue;

            itemButton.SetOwned(false);
            itemButton.SetBlockedByBetterArmor(false);
        }

        // 방어구 없음
        if (maxBarrier <= 0)
        {
            RefreshItemButtons();
            return;
        }

        // 경량 베리어 보유
        if (maxBarrier == 50)
        {
            foreach (StoreItemButton itemButton in itemButtons)
            {
                if (itemButton == null)
                    continue;

                if (!itemButton.IsArmorButton())
                    continue;

                if (itemButton.ItemId == 0)
                {
                    itemButton.SetOwned(true);
                }

                // 중량은 구매 가능해야 하므로 막지 않는다.
                if (itemButton.ItemId == 1)
                {
                    itemButton.SetOwned(false);
                    itemButton.SetBlockedByBetterArmor(false);
                }
            }

            RefreshItemButtons();
            return;
        }

        // 중량 베리어 보유
        if (maxBarrier >= 100)
        {
            foreach (StoreItemButton itemButton in itemButtons)
            {
                if (itemButton == null)
                    continue;

                if (!itemButton.IsArmorButton())
                    continue;

                if (itemButton.ItemId == 0)
                {
                    // 경량은 보유 표시는 하지 않는다.
                    // 가격은 그대로 보이되 클릭만 막는다.
                    itemButton.SetOwned(false);
                    itemButton.SetBlockedByBetterArmor(true);
                }

                if (itemButton.ItemId == 1)
                {
                    itemButton.SetOwned(true);
                    itemButton.SetBlockedByBetterArmor(false);
                }
            }

            RefreshItemButtons();
            return;
        }

        RefreshItemButtons();
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }
}