using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MyPageStatDropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex;
    public void Configure(int value) => slotIndex = value;
    public void OnDrop(PointerEventData eventData)
    {
        MyPageStatDragItem item = eventData.pointerDrag?.GetComponent<MyPageStatDragItem>();
        if (item != null) item.CompleteDrop(slotIndex);
    }
}
