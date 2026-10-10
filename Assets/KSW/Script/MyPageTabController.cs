using UnityEngine;
using UnityEngine.UI;

// Routes the shared header to scene-authored sibling content panels. No layout creation.
public sealed class MyPageTabController : MonoBehaviour
{
    public enum Page { Profile, Career, ChipCraft, LevelRewards, Honor }

    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private GameObject[] selectedIndicators;
    [SerializeField] private Button cardEditButton;
    [SerializeField] private MyPageCareerDataController career;
    [SerializeField] private MyPageChipCraftController chipCraft;
    [SerializeField] private MyPageLevelRewardController levelRewards;
    [SerializeField] private MyPageHonorController honor;
    [SerializeField] private RewardReceiptPopup receiptPopup;
    private bool initialized;
    public Page CurrentPage { get; private set; }

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (receiptPopup == null) receiptPopup = GetComponentInChildren<RewardReceiptPopup>(true);
        if (honor == null) honor = GetComponentInChildren<MyPageHonorController>(true);
        for (int i = 0; i < tabButtons.Length; ++i)
        {
            Page page = (Page)i;
            if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => Open(page));
        }
        if (cardEditButton != null) cardEditButton.onClick.AddListener(() => Open(Page.ChipCraft));
    }

    public void Open(Page page)
    {
        Initialize();
        Select(page);
        if (page == Page.Profile) career.ShowProfilePage();
        else if (page == Page.Career) career.ShowCareerPage();
        else if (page == Page.ChipCraft) chipCraft.Open();
        else if (page == Page.LevelRewards) levelRewards.Open();
        else if (page == Page.Honor) honor?.Open();
    }

    public void Select(Page page)
    {
        GetComponent<MyPageBirthdayController>()?.Close();
        CurrentPage = page;
        for (int i = 0; i < pages.Length; ++i)
        {
            if (pages[i] != null) pages[i].SetActive(i == (int)page);
            if (i < selectedIndicators.Length && selectedIndicators[i] != null)
                selectedIndicators[i].SetActive(i == (int)page);
            if (i < tabButtons.Length && tabButtons[i] != null && tabButtons[i].image != null)
                tabButtons[i].image.color = i == (int)page
                    ? new Color32(33, 39, 43, 255)
                    : new Color32(31, 32, 35, 255);
        }
    }

    public bool HandleBack()
    {
        if (receiptPopup != null && receiptPopup.IsOpen)
        {
            receiptPopup.Close();
            return true;
        }
        if (CurrentPage == Page.ChipCraft && chipCraft.HandleEscape()) return true;
        if (CurrentPage == Page.Profile) return false;
        if (CurrentPage == Page.LevelRewards) levelRewards.Close();
        if (CurrentPage == Page.Honor) honor?.Close();
        career.ShowProfilePage();
        return true;
    }
}
