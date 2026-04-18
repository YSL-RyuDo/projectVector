using UnityEngine;

public class MiniMapUI : MonoBehaviour
{
    [SerializeField] private GameObject _miniMapUIPanel;
    [SerializeField] private GameObject _background;
    [SerializeField] private SmokeSkill _smokeSkill;

    private void OnEnable() => SmokeSkill.OnMiniMap += ToggleMiniMap;
    private void OnDisable() => SmokeSkill.OnMiniMap -= ToggleMiniMap;

    private void ToggleMiniMap()
    {
        bool isOpening = !_miniMapUIPanel.activeSelf;

        if (FPSCameraLook.Instance != null)
        {
            FPSCameraLook.Instance.CanMove = !isOpening;
        }

        if (isOpening)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        _smokeSkill.SetSelectingState(isOpening);

        _miniMapUIPanel.SetActive(isOpening);
        _background.SetActive(isOpening);
    }
}