using UnityEngine;

public class MiniMapUI : MonoBehaviour
{
    [SerializeField] private GameObject _miniMapUIPanel;
    [SerializeField] private GameObject _background;

    private void OnEnable()
    {
        ControllerSkill_E.OnMiniMap += RefreshMiniMap;
    }

    private void OnDisable()
    {
        ControllerSkill_E.OnMiniMap -= RefreshMiniMap;
    }

    private void RefreshMiniMap()
    {
        bool active = ControllerSkill_E.IsSelectingSmoke;

        if (_miniMapUIPanel != null)
            _miniMapUIPanel.SetActive(active);

        if (_background != null)
            _background.SetActive(active);
    }
}