using UnityEngine;
using UnityEngine.EventSystems;

public class PreviewDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private CharacterPanelController controller;

    public void OnPointerDown(PointerEventData eventData)
    {
        controller.BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller.Drag(eventData);
    }
}