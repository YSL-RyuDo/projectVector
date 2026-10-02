using UnityEngine;

public sealed class ProfileRevealSequence : MonoBehaviour
{
    public AuthoredPanelMotion[] sections;
    public void Show(bool visible)
    {
        foreach (var section in sections)
        {
            if (section == null) continue;
            if (visible) section.Play();
            else { section.ResetMotion(); section.GetComponent<CanvasGroup>().alpha = 1; }
        }
    }
}
