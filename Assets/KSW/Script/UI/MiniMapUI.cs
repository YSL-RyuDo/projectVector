using UnityEngine;

public class MiniMapUI : MonoBehaviour
{
    private FPSCameraLook _camera;

    [SerializeField] private GameObject _miniMapUIPanel;
    [SerializeField] private GameObject _background;

    private ControllerSkill_E _smokeSkill;

    private void OnEnable() => ControllerSkill_E.OnMiniMap += ToggleMiniMap;
    private void OnDisable() => ControllerSkill_E.OnMiniMap -= ToggleMiniMap;

    void Start()
    {
        _smokeSkill = GetComponentInParent<ControllerSkill_E>();
    }

    private void ToggleMiniMap()
    {
        bool isOpening = !_miniMapUIPanel.activeSelf;

        if (_camera != null)
        {
            _camera.CanMove = !isOpening;
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