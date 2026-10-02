using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A saved decorative accent only: does not replace button callbacks or layout.
public sealed class AuthoredHoverAccent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public CanvasGroup accent;
    private bool hovered, selected;
    private Selectable control;
    private void Awake() => control = GetComponent<Selectable>();
    private void Update()
    {
        if (accent == null) return;
        float target = (hovered || selected) && (control == null || control.IsInteractable()) ? 1f : 0f;
        accent.alpha = Mathf.MoveTowards(accent.alpha, target, Time.unscaledDeltaTime * 8f);
    }
    private void OnDisable() { hovered = selected = false; if (accent != null) accent.alpha = 0; }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;
}
