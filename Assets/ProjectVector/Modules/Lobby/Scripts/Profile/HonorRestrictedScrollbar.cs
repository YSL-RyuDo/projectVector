using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HonorRestrictedScrollbar : Scrollbar
{
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (handleRect == null || !RectTransformUtility.RectangleContainsScreenPoint(
            handleRect, eventData.position, eventData.pressEventCamera)) return;
        base.OnPointerDown(eventData);
    }
}
