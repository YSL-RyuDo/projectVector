using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Displays rewards using the item template authored in Lobby.unity.
public sealed class RewardReceiptPopup : MonoBehaviour
{
    public readonly struct RewardItem
    {
        public readonly string name;
        public readonly Sprite icon;
        public readonly int quantity;

        public RewardItem(string name, Sprite icon, int quantity)
        {
            this.name = name;
            this.icon = icon;
            this.quantity = quantity;
        }
    }

    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;

    private readonly List<GameObject> itemViews = new();
    private bool initialized;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (confirmButton != null) confirmButton.onClick.AddListener(Close);
        if (itemTemplate != null) itemTemplate.SetActive(false);
    }

    public void Show(IReadOnlyList<RewardItem> items)
    {
        Initialize();
        if (items == null || items.Count == 0) return;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (titleText != null) titleText.text = "아이템 획득을 축하합니다";
        if (messageText != null) messageText.gameObject.SetActive(false);

        for (int i = 0; i < items.Count; ++i)
        {
            GameObject view = GetItemView(i);
            view.SetActive(true);
            Image icon = view.transform.Find("RewardIcon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = items[i].icon;
                icon.enabled = items[i].icon != null;
                icon.preserveAspect = true;
            }
            TMP_Text name = view.transform.Find("RewardName")?.GetComponent<TMP_Text>();
            if (name != null) name.text = items[i].name;
            TMP_Text quantity = view.transform.Find("RewardQuantity")?.GetComponent<TMP_Text>();
            if (quantity != null) quantity.text = $"×{items[i].quantity:N0}";
        }
        for (int i = items.Count; i < itemViews.Count; ++i) itemViews[i].SetActive(false);
        ResizeItems(items.Count);
    }

    public void ShowMessage(string message)
    {
        Initialize();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (titleText != null) titleText.text = "안내";
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = message;
        }
        foreach (GameObject view in itemViews) view.SetActive(false);
    }

    public void Close() => gameObject.SetActive(false);

    private GameObject GetItemView(int index)
    {
        while (itemViews.Count <= index)
        {
            GameObject view = Instantiate(itemTemplate, content, false);
            view.name = "RewardItem" + itemViews.Count;
            itemViews.Add(view);
        }
        return itemViews[index];
    }

    private void ResizeItems(int count)
    {
        if (grid == null || content == null || viewport == null) return;
        Canvas.ForceUpdateCanvases();
        int columns = count <= 4 ? count : count <= 8 ? 4 : 6;
        float availableWidth = viewport.rect.width;
        float size = count <= 3 ? 160f : count <= 6 ? 132f : count <= 12 ? 96f : 82f;
        size = Mathf.Min(size, (availableWidth - (columns - 1) * grid.spacing.x) / columns);
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(size, size + 40f);
        int rows = Mathf.CeilToInt((float)count / columns);
        float requiredHeight = rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
        content.sizeDelta = new Vector2(0, Mathf.Max(viewport.rect.height, requiredHeight));
        content.anchoredPosition = Vector2.zero;
    }
}
