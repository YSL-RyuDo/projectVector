using System.Collections.Generic;
using UnityEngine;

public class DroneModeUIController : MonoBehaviour
{
    public static DroneModeUIController Instance { get; private set; }

    [Header("UI Roots")]
    [SerializeField] private Transform _gameUIsRoot;
    [SerializeField] private GameObject _droneHudRoot;

    [Header("드론 모드에서도 유지할 UI가 있으면 넣기")]
    [SerializeField] private GameObject[] _keepActiveInDroneMode;

    private readonly Dictionary<GameObject, bool> _previousActiveStates = new();
    private bool _isDroneMode;

    private void Awake()
    {
        Instance = this;

        if (_droneHudRoot != null)
            _droneHudRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetDroneModeUI(bool active)
    {
        if (_gameUIsRoot == null)
        {
            Debug.LogError("[DRONE UI] GameUIs Root is null");
            return;
        }

        if (_droneHudRoot == null)
        {
            Debug.LogError("[DRONE UI] DroneHUD Root is null");
            return;
        }

        if (active)
        {
            EnterDroneUI();
        }
        else
        {
            ExitDroneUI();
        }
    }

    private void EnterDroneUI()
    {
        if (_isDroneMode)
            return;

        _isDroneMode = true;
        _previousActiveStates.Clear();

        for (int i = 0; i < _gameUIsRoot.childCount; i++)
        {
            GameObject child = _gameUIsRoot.GetChild(i).gameObject;

            if (child == null)
                continue;

            _previousActiveStates[child] = child.activeSelf;

            if (child == _droneHudRoot)
                continue;

            if (ShouldKeepActive(child))
                continue;

            child.SetActive(false);
        }

        _droneHudRoot.SetActive(true);

        Debug.Log("[DRONE UI] enter drone mode");
    }

    private void ExitDroneUI()
    {
        if (!_isDroneMode)
            return;

        _isDroneMode = false;

        foreach (var pair in _previousActiveStates)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SetActive(pair.Value);
        }

        _droneHudRoot.SetActive(false);
        _previousActiveStates.Clear();

        Debug.Log("[DRONE UI] exit drone mode");
    }

    private bool ShouldKeepActive(GameObject obj)
    {
        if (_keepActiveInDroneMode == null)
            return false;

        for (int i = 0; i < _keepActiveInDroneMode.Length; i++)
        {
            if (_keepActiveInDroneMode[i] == obj)
                return true;
        }

        return false;
    }

    public void SetSpectatorDroneViewUI(bool active)
    {
        SetDroneModeUI(active);

        if (DroneHUDUI.Instance != null)
        {
            if (active)
                DroneHUDUI.Instance.SetSpectatorDroneMode(true);
            else
                DroneHUDUI.Instance.SetVisible(false);
        }
    }
}