using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StorePanelUI : MonoBehaviour
{
    public static StorePanelUI Instance;

    private static int blockGameplayInputThroughFrame = -1;

    public static bool IsBlockingGameplayInput =>
        (Instance != null && Instance.IsOpen()) ||
        Time.frameCount <= blockGameplayInputThroughFrame;

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
    [SerializeField] private StoreSkillButton[] skillButtons;

    private int currentCredits = 0;

    private void Awake()
    {
        Instance = this;
        blockGameplayInputThroughFrame = -1;

        if (storePanelRoot != null)
            storePanelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        ResolveSkillButtons();
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

        blockGameplayInputThroughFrame =
            Mathf.Max(blockGameplayInputThroughFrame, Time.frameCount);

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

        // UI click/release input must not leak into weapon input after closing.
        blockGameplayInputThroughFrame =
            Mathf.Max(blockGameplayInputThroughFrame, Time.frameCount + 1);

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
        bool buyPhase = CanOpenStore();

        if (itemButtons != null)
        {
            foreach (StoreItemButton itemButton in itemButtons)
            {
                if (itemButton != null)
                    itemButton.Refresh(currentCredits, buyPhase);
            }
        }

        if (skillButtons != null)
        {
            foreach (StoreSkillButton skillButton in skillButtons)
            {
                if (skillButton != null)
                    skillButton.Refresh(currentCredits, buyPhase);
            }
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

    private void ResolveSkillButtons()
    {
        if (storePanelRoot == null)
            return;

        StoreSkillButton[] resolved = new StoreSkillButton[4];
        Button[] buttons =
            storePanelRoot.GetComponentsInChildren<Button>(true);

        foreach (Button candidate in buttons)
        {
            if (candidate == null || candidate == closeButton)
                continue;

            if (!TryGetSkillSlot(candidate.gameObject.name, out SkillSlot slot))
                continue;

            StoreSkillButton skillButton =
                candidate.GetComponent<StoreSkillButton>();

            if (skillButton == null)
                skillButton = candidate.gameObject.AddComponent<StoreSkillButton>();

            skillButton.Configure(slot);
            resolved[(int)slot] = skillButton;
        }

        // Freshly created Button (1)~(3) objects also work before they are renamed.
        if (CountResolved(resolved) < 4)
        {
            List<Button> unnamedButtons = new List<Button>();

            foreach (Button candidate in buttons)
            {
                if (candidate == null || candidate == closeButton)
                    continue;

                if (candidate.transform.parent != storePanelRoot.transform)
                    continue;

                if (!candidate.gameObject.name.StartsWith("Button"))
                    continue;

                if (candidate.GetComponent<StoreItemButton>() != null ||
                    candidate.GetComponent<StoreSkillButton>() != null)
                {
                    continue;
                }

                unnamedButtons.Add(candidate);
            }

            unnamedButtons.Sort((left, right) =>
                GetAnchoredX(left).CompareTo(GetAnchoredX(right))
            );

            int candidateIndex = 0;
            for (int slotIndex = 0;
                 slotIndex < resolved.Length && candidateIndex < unnamedButtons.Count;
                 slotIndex++)
            {
                if (resolved[slotIndex] != null)
                    continue;

                Button candidate = unnamedButtons[candidateIndex++];
                StoreSkillButton skillButton =
                    candidate.gameObject.AddComponent<StoreSkillButton>();
                skillButton.Configure((SkillSlot)slotIndex);
                resolved[slotIndex] = skillButton;
            }
        }

        skillButtons = resolved;
    }

    private bool TryGetSkillSlot(string objectName, out SkillSlot slot)
    {
        switch (objectName)
        {
            case "CSkillButton": slot = SkillSlot.C; return true;
            case "QSkillButton": slot = SkillSlot.Q; return true;
            case "ESkillButton": slot = SkillSlot.E; return true;
            case "XSkillButton": slot = SkillSlot.X; return true;
            default:
                slot = SkillSlot.C;
                return false;
        }
    }

    private int CountResolved(StoreSkillButton[] buttons)
    {
        int count = 0;

        foreach (StoreSkillButton button in buttons)
        {
            if (button != null)
                count++;
        }

        return count;
    }

    private float GetAnchoredX(Button button)
    {
        RectTransform rect = button.transform as RectTransform;
        return rect != null ? rect.anchoredPosition.x : 0f;
    }
}