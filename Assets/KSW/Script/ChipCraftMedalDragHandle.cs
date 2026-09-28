using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChipCraftMedalDragHandle : MonoBehaviour, IDragHandler
{
    [SerializeField] private int slot;
    public void SetSlot(int value) => slot = value;
    public void OnDrag(PointerEventData eventData)
    {
        GetComponentInParent<MyPageChipCraftController>()?.DragPlacedMedal(slot, eventData);
    }
}
