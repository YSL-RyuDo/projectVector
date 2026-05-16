using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_E : CharacterSkill
{
    [Header("프리팹")]
    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private GameObject _pingPrefab;

    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private RectTransform _pingContainer;

    private Camera _miniMapCamera;

    private List<Vector3> _targetLocations = new List<Vector3>();
    private bool _isSelecting = false;

    public static event Action OnMiniMap;

    private void Start()
    {
        GameObject miniMapCamera = GameObject.FindWithTag("MiniMapCamera");
        _miniMapCamera = miniMapCamera.GetComponent<Camera>();
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseSkill();
        }

        if (_isSelecting)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                selectLocation();
            }
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                dropSmokes();
            }
        }
    }

    protected override void UseSkill()
    {
        OnMiniMap?.Invoke();
    }

    public void SetSelectingState(bool state)
    {
        _isSelecting = state;
        if (!state) clearPings();
    }

    private void selectLocation()
    {
        if (_mapRect == null || _miniMapCamera == null)
        {
            Debug.LogWarning("미니맵 MapRect 또는 MiniMapCamera가 인스펙터에 할당되지 않았습니다.");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapRect, mousePos, null, out Vector2 localPoint))
        {
            float normX = (localPoint.x / _mapRect.rect.width) + 0.5f;
            float normY = (localPoint.y / _mapRect.rect.height) + 0.5f;

            Ray ray = _miniMapCamera.ViewportPointToRay(new Vector3(normX, normY, 0));

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject ping = Instantiate(_pingPrefab, _pingContainer);
                ping.GetComponent<RectTransform>().anchoredPosition = localPoint;

                _targetLocations.Add(hit.point);
                Debug.Log($"좌표 예약: {hit.point}");
            }
        }
    }

    private void dropSmokes()
    {
        foreach (Vector3 pos in _targetLocations)
        {
            Instantiate(_smokePrefab, pos, Quaternion.identity);
        }

        _targetLocations.Clear();
        OnMiniMap?.Invoke();
    }

    private void clearPings()
    {
        if (_pingContainer == null) return;

        _targetLocations.Clear();

        int childs = _pingContainer.childCount;
        for (int i = childs - 1; i >= 0; i--)
        {
            GameObject target = _pingContainer.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}