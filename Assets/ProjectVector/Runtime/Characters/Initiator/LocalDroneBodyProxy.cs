using UnityEngine;

// Keep the existing prefab API, but reuse the actual body instead of cloning it.
public class LocalDroneBodyProxy : MonoBehaviour
{
    private bool _visible;
    public void SetVisible(bool visible)
    {
        _visible = visible;
        GetComponent<PlayerCameraController>()?.SetDroneBodyView(visible);
    }
    private void OnDisable()
    {
        if (_visible) SetVisible(false);
    }
}
