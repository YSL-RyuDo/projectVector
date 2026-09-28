using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Right-drag content, or left-drag the scrollbar handle. No wheel or track click.
public sealed class HonorGuideScrollRect : ScrollRect
{
    public override void OnScroll(PointerEventData eventData) { }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
        => Forward(eventData, base.OnInitializePotentialDrag);
    public override void OnBeginDrag(PointerEventData eventData)
        => Forward(eventData, base.OnBeginDrag);
    public override void OnDrag(PointerEventData eventData)
        => Forward(eventData, base.OnDrag);
    public override void OnEndDrag(PointerEventData eventData)
        => Forward(eventData, base.OnEndDrag);

    private static void Forward(PointerEventData data, System.Action<PointerEventData> action)
    {
        if (data.button != PointerEventData.InputButton.Right)
            return;
        data.button = PointerEventData.InputButton.Left;
        try { action(data); }
        finally { data.button = PointerEventData.InputButton.Right; }
    }
}
