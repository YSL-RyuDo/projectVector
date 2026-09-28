using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RightDragLevelRewardScroll : ScrollRect
{
    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        WithRightButton(eventData, base.OnInitializePotentialDrag);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        WithRightButton(eventData, base.OnBeginDrag);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        WithRightButton(eventData, base.OnDrag);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        WithRightButton(eventData, base.OnEndDrag);
    }

    public override void OnScroll(PointerEventData eventData) { }

    private static void WithRightButton(PointerEventData eventData, Action<PointerEventData> handler)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        // ScrollRect only accepts Left internally. Restore the original event for other handlers.
        eventData.button = PointerEventData.InputButton.Left;
        try { handler(eventData); }
        finally { eventData.button = PointerEventData.InputButton.Right; }
    }
}
