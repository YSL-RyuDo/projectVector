using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MyPageStatDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string statKey;
    public string StatKey => statKey;
    private RectTransform rect;
    private Canvas canvas;
    private CanvasGroup group;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private bool available = true;
    private bool dropCompleted;

    private void Awake()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    public void Configure(string key) => statKey = key;
    public void SetAvailable(bool value)
    {
        available = value;
        Button button = GetComponent<Button>();
        if (button != null) button.interactable = value;
        Image image = GetComponent<Image>();
        if (image != null) image.color = value ? new Color32(83, 91, 105, 255) : new Color32(52, 57, 66, 170);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!available) return;
        if (rect == null) rect = transform as RectTransform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (rect == null || canvas == null || group == null) return;
        dropCompleted = false;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rect.anchoredPosition;
        group.blocksRaycasts = false;
        Transform dragLayer = MyPagePanelController.Instance != null
            ? MyPagePanelController.Instance.transform
            : canvas.transform;
        transform.SetParent(dragLayer, true);
        transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!available || rect == null || canvas == null) return;
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (available && !dropCompleted && MyPagePanelController.Instance != null)
            dropCompleted = MyPagePanelController.Instance.TryDropStatAtScreenPosition(
                eventData.position, statKey, eventData.pressEventCamera);
        if (group != null) group.blocksRaycasts = true;
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, originalParent.childCount - 1));
        }
        if (rect != null) rect.anchoredPosition = originalPosition;
        originalParent = null;
    }

    public bool CompleteDrop(int slotIndex)
    {
        if (!available || MyPagePanelController.Instance == null) return false;
        dropCompleted = MyPagePanelController.Instance.TryReplaceStat(slotIndex, statKey);
        return dropCompleted;
    }
}
