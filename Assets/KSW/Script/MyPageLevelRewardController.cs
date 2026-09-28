using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The panel and reward cells are authored in Lobby.unity; this component only updates state.
public sealed class MyPageLevelRewardController : MonoBehaviour
{
    private const int LastStandardLevel = 45;
    private const int MaximumLevel = 100;
    private readonly Button[] rewardButtons = new Button[LastStandardLevel + 1];
    private Transform panel;
    private RightDragLevelRewardScroll scroll;
    private Button bonusButton;
    [SerializeField] private RewardReceiptPopup receiptPopup;
    [SerializeField] private Sprite currency0Icon;
    [SerializeField] private Sprite currency1Icon;
    private bool initialized;
    private int playerLevel = 1;
    private int claimedLevel;
    private int currentExp;
    private int requiredExp;
    private bool waitingForClaim;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        panel = Find("LevelRewardPanel");
        scroll = Find("LevelRewardScroll")?.GetComponent<RightDragLevelRewardScroll>();
        bonusButton = Find("LevelRewardBonusButton")?.GetComponent<Button>();
        if (receiptPopup == null)
            receiptPopup = GetComponentInParent<MyPagePanelController>(true)
                ?.GetComponentInChildren<RewardReceiptPopup>(true);
        for (int level = 1; level <= LastStandardLevel; ++level)
        {
            int target = level;
            rewardButtons[level] = Find($"LevelRewardLevel{level}")?.GetComponent<Button>();
            if (rewardButtons[level] != null)
                rewardButtons[level].onClick.AddListener(() => Claim(target));
        }
        if (bonusButton != null) bonusButton.onClick.AddListener(() => Claim(playerLevel));
    }

    public void Open()
    {
        Initialize();
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        Refresh();
        CenterOnCurrentLevel();
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendLevelRewardRequest();
        else receiptPopup?.ShowMessage("서버 연결이 없어 보상 정보를 불러올 수 없습니다.");
    }

    public void Close()
    {
        Initialize();
        receiptPopup?.Close();
        if (panel != null) panel.gameObject.SetActive(false);
        waitingForClaim = false;
    }

    public bool HandleEscape()
    {
        if (panel == null || !panel.gameObject.activeSelf) return false;
        Close();
        return true;
    }

    public void ApplyState(bool success, int level, int exp, int required, int claimed)
    {
        Initialize();
        if (!success)
        {
            receiptPopup?.ShowMessage("보상 정보를 불러오지 못했습니다.");
            return;
        }
        playerLevel = Mathf.Clamp(level, 1, MaximumLevel);
        currentExp = Mathf.Max(0, exp);
        requiredExp = Mathf.Max(0, required);
        claimedLevel = Mathf.Clamp(claimed, 0, MaximumLevel);
        waitingForClaim = false;
        Refresh();
        CenterOnCurrentLevel();
    }

    public void ApplyClaim(bool success, int newClaimedLevel, int freeAward, int victoryAward)
    {
        Initialize();
        waitingForClaim = false;
        if (!success)
        {
            Refresh();
            receiptPopup?.ShowMessage("수령할 수 없는 보상입니다. 레벨과 연결 상태를 확인해 주세요.");
            NetworkManager.Instance?.SendLevelRewardRequest();
            return;
        }
        claimedLevel = Mathf.Clamp(newClaimedLevel, 0, MaximumLevel);
        Refresh();
        List<RewardReceiptPopup.RewardItem> rewards = new();
        if (freeAward > 0)
            rewards.Add(new RewardReceiptPopup.RewardItem("재화 0", currency0Icon, freeAward));
        if (victoryAward > 0)
            rewards.Add(new RewardReceiptPopup.RewardItem("재화 1", currency1Icon, victoryAward));
        if (rewards.Count > 0 && panel != null && panel.gameObject.activeInHierarchy)
            receiptPopup?.Show(rewards);
    }

    private void Claim(int targetLevel)
    {
        if (waitingForClaim || targetLevel <= claimedLevel || targetLevel > playerLevel) return;
        waitingForClaim = true;
        if (targetLevel <= LastStandardLevel && rewardButtons[targetLevel] != null)
            rewardButtons[targetLevel].interactable = false;
        if (targetLevel > LastStandardLevel && bonusButton != null)
            bonusButton.interactable = false;
        NetworkManager.Instance?.SendLevelRewardClaim(targetLevel);
        if (NetworkManager.Instance == null)
        {
            waitingForClaim = false;
            Refresh();
            receiptPopup?.ShowMessage("서버 연결이 없어 보상을 수령할 수 없습니다.");
        }
    }

    private void Refresh()
    {
        int remaining = Mathf.Max(0, requiredExp - currentExp);
        SetText("LevelRewardExperience", requiredExp > 0
            ? $"현재 Lv.{playerLevel}   EXP {currentExp:N0} / {requiredExp:N0}   /   다음 레벨까지 {remaining:N0}"
            : $"현재 Lv.{playerLevel}   최고 레벨");
        Image progress = Find("LevelRewardExperienceFill")?.GetComponent<Image>();
        if (progress != null)
        {
            RectTransform rect = progress.rectTransform;
            rect.anchorMax = new Vector2(requiredExp > 0 ? Mathf.Clamp01((float)currentExp / requiredExp) : 1f, 1f);
        }
        for (int level = 1; level <= LastStandardLevel; ++level)
        {
            Button button = rewardButtons[level];
            if (button == null) continue;
            bool received = level <= claimedLevel;
            bool unlocked = level <= playerLevel;
            button.interactable = !received && unlocked;
            Image lane = Find($"LevelRewardLevel{level}Lane")?.GetComponent<Image>();
            if (lane != null) lane.color = new Color32(47, 52, 58, 255);
            float fraction = unlocked ? 1 : level == playerLevel + 1 && requiredExp > 0
                ? Mathf.Clamp01((float)currentExp / requiredExp) : 0;
            Image fill = Find($"LevelRewardLevel{level}Progress")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.fillAmount = fraction;
                fill.rectTransform.anchorMax = new Vector2(fraction, 1);
                fill.enabled = fraction > 0;
            }
            SetText($"LevelRewardLevel{level}Experience", level == playerLevel + 1 && requiredExp > 0
                ? $"{currentExp:N0} / {requiredExp:N0}" : string.Empty);
            TMP_Text number = Find($"LevelRewardLevel{level}Number")?.GetComponent<TMP_Text>();
            if (number != null) number.color = unlocked ? new Color32(28, 31, 36, 255) : Color.white;
            SetRewardStateIcons($"LevelRewardLevel{level}", received, unlocked);
            SetRewardStateIcons($"LevelRewardLevel{level}Victory", received, unlocked);
            SetText($"LevelRewardLevel{level}State", string.Empty);
            SetText($"LevelRewardLevel{level}Currency1", level % 5 == 0 ? "x1" : string.Empty);
        }
        if (bonusButton != null)
        {
            bool available = playerLevel > LastStandardLevel && playerLevel > claimedLevel;
            bonusButton.interactable = available;
            SetText("LevelRewardBonusLevel", playerLevel > LastStandardLevel
                ? $"Lv.{LastStandardLevel + 1}~{playerLevel} 추가 보상" : "Lv.45 이후 추가 보상");
            SetText("LevelRewardBonusState", available ? "현재 레벨까지 수령" :
                playerLevel <= LastStandardLevel ? "Lv.46에 해금" : "수령 완료");
        }
    }

    private void SetRewardStateIcons(string prefix, bool received, bool unlocked)
    {
        Image marker = Find(prefix + "Marker")?.GetComponent<Image>();
        if (marker != null)
        {
            marker.enabled = received;
            marker.color = new Color32(57, 215, 75, 255);
        }
        Image locked = Find(prefix + "Lock")?.GetComponent<Image>();
        if (locked != null) locked.enabled = !unlocked;
    }

    private void CenterOnCurrentLevel()
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        Canvas.ForceUpdateCanvases();
        float scrollable = scroll.content.rect.width - scroll.viewport.rect.width;
        if (scrollable <= 0) { scroll.horizontalNormalizedPosition = 0; return; }
        if (playerLevel <= 1) { scroll.horizontalNormalizedPosition = 0; return; }
        if (playerLevel >= 35) { scroll.horizontalNormalizedPosition = 1; return; }
        RectTransform cell = rewardButtons[Mathf.Min(playerLevel, LastStandardLevel)]?.transform as RectTransform;
        if (cell == null) return;
        float center = cell.anchoredPosition.x + scroll.content.rect.width * scroll.content.pivot.x;
        scroll.horizontalNormalizedPosition = Mathf.Clamp01((center - scroll.viewport.rect.width * .5f) / scrollable);
        scroll.StopMovement();
    }

    private Transform Find(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private void SetText(string name, string value)
    {
        TMP_Text text = Find(name)?.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }
}
