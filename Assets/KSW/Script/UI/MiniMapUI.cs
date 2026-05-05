using UnityEngine;

public class MiniMapUI : MonoBehaviour
{
    private FPSCameraLook _camera;

    [SerializeField] private GameObject _miniMapUIPanel;
    [SerializeField] private GameObject _background;
    [SerializeField] private SmokeSkill _smokeSkill;

    [SerializeField] private Camera _targetMiniMapCamera;
    [SerializeField] private RectTransform _mapRect;

    private void OnEnable() => SmokeSkill.OnMiniMap += ToggleMiniMap;
    private void OnDisable() => SmokeSkill.OnMiniMap -= ToggleMiniMap;

    public void RegisterLocalPlayerCamera(FPSCameraLook cam)
    {
        _camera = cam;
    }

    public void RegisterPlayer(GameObject playerObj)
    {
        // 1. 플레이어 자식에 있는 SmokeSkill을 찾음
        var smoke = playerObj.GetComponentInChildren<SmokeSkill>();

        if (smoke != null)
        {
            // 2. 하이라키에 있는 카메라와 UI 정보를 꽂아줌
            smoke.SetupMiniMapReferences(_targetMiniMapCamera, _mapRect);
        }
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