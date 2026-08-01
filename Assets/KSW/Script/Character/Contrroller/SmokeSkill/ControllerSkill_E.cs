using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControllerSkill_E : CharacterSkill
{
    public static event Action OnMiniMap;

    public static bool IsSelectingSmoke { get; private set; }
    public static bool IsSmokeMapOpen { get; private set; }
    public static bool IsBlockingWeaponInput { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject _pingPrefab;

    [Header("Mini Map")]
    [SerializeField] private GameObject _miniMapOverlay;
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private RectTransform _pingContainer;
    [SerializeField] private Camera _miniMapCamera;

    [Header("Smoke")]
    [SerializeField] private LayerMask _groundMask = ~0;

    private bool _isSelecting;
    private GameObject _currentPing;
    private Coroutine _releaseBlockCoroutine;
    private PlayerIdentity _playerIdentity;
    private FPSCameraLook _cameraLook;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
        _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        AutoAssignMiniMapReferences();
    }

    private void Start()
    {
        AutoAssignMiniMapReferences();
        ForceCloseMiniMapImmediate();
    }

    private void OnDisable()
    {
        ForceCloseMiniMapImmediate();
    }

    private void Update()
    {
        if (!IsMine()) return;
        if (Keyboard.current == null || Mouse.current == null) return;
        if (InGameChatUI.IsChatOpen) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        if (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseSkill();
        }

        if (!_isSelecting) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectAndDropSmoke();
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMiniMap();
        }
    }

    protected override void UseSkill()
    {
        if (_isSelecting)
        {
            CloseMiniMap();
            return;
        }

        if (!CanUsePurchasedSkill(SkillSlot.E))
            return;

        OpenMiniMap();
    }

    private bool IsMine()
    {
        if (_playerIdentity == null) _playerIdentity = GetComponent<PlayerIdentity>();
        return _playerIdentity != null && NetworkManager.Instance != null && _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    private void OpenMiniMap()
    {
        AutoAssignMiniMapReferences();

        _isSelecting = true;
        IsSelectingSmoke = true;
        IsSmokeMapOpen = true;
        IsBlockingWeaponInput = true;

        if (_releaseBlockCoroutine != null)
        {
            StopCoroutine(_releaseBlockCoroutine);
            _releaseBlockCoroutine = null;
        }

        if (_miniMapOverlay != null) _miniMapOverlay.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_cameraLook == null) _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        if (_cameraLook != null) _cameraLook.CanMove = false;

        OnMiniMap?.Invoke();
        Debug.Log("[CONTROLLER SMOKE] minimap open");
    }

    private void CloseMiniMap()
    {
        _isSelecting = false;
        IsSelectingSmoke = false;
        IsSmokeMapOpen = false;

        if (_miniMapOverlay != null) _miniMapOverlay.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_cameraLook == null) _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        if (_cameraLook != null) _cameraLook.CanMove = true;

        ClearPing();

        if (_releaseBlockCoroutine != null) StopCoroutine(_releaseBlockCoroutine);
        _releaseBlockCoroutine = StartCoroutine(ReleaseWeaponBlockAfterMouseUp());

        OnMiniMap?.Invoke();
        Debug.Log("[CONTROLLER SMOKE] minimap close");
    }

    private void ForceCloseMiniMapImmediate()
    {
        _isSelecting = false;
        IsSelectingSmoke = false;
        IsSmokeMapOpen = false;
        IsBlockingWeaponInput = false;

        if (_releaseBlockCoroutine != null)
        {
            StopCoroutine(_releaseBlockCoroutine);
            _releaseBlockCoroutine = null;
        }

        if (_miniMapOverlay != null) _miniMapOverlay.SetActive(false);

        if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_cameraLook == null) _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        if (_cameraLook != null) _cameraLook.CanMove = true;

        ClearPing();
    }

    private IEnumerator ReleaseWeaponBlockAfterMouseUp()
    {
        IsBlockingWeaponInput = true;

        yield return null;

        while (Mouse.current != null && Mouse.current.leftButton.isPressed)
            yield return null;

        yield return null;

        IsBlockingWeaponInput = false;
        _releaseBlockCoroutine = null;
    }

    private void SelectAndDropSmoke()
    {
        AutoAssignMiniMapReferences();

        if (_mapRect == null || _miniMapCamera == null)
        {
            Debug.LogWarning("[CONTROLLER SMOKE] mapRect or miniMapCamera is null");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapRect, mousePos, null, out Vector2 localPoint))
        {
            Debug.LogWarning("[CONTROLLER SMOKE] screen to local failed");
            return;
        }

        if (!_mapRect.rect.Contains(localPoint))
        {
            Debug.LogWarning($"[CONTROLLER SMOKE] clicked outside map localPoint={localPoint}");
            return;
        }

        float normX = localPoint.x / _mapRect.rect.width + 0.5f;
        float normY = localPoint.y / _mapRect.rect.height + 0.5f;

        Ray ray = _miniMapCamera.ViewportPointToRay(new Vector3(normX, normY, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundMask, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("[CONTROLLER SMOKE] minimap raycast miss");
            return;
        }

        SpawnPing(localPoint);

        Debug.Log($"[CONTROLLER SMOKE SELECT] world={hit.point}, local={localPoint}");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendControllerSmokeDrop(hit.point);
            Debug.Log($"[CONTROLLER SMOKE SEND] pos={hit.point}");
        }
        else
        {
            Debug.LogWarning("[CONTROLLER SMOKE] NetworkManager is null");
        }

        CloseMiniMap();
    }

    private void SpawnPing(Vector2 localPoint)
    {
        if (_pingPrefab == null || _pingContainer == null)
        {
            Debug.LogWarning("[CONTROLLER SMOKE] pingPrefab or pingContainer is null");
            return;
        }

        ClearPing();

        _currentPing = Instantiate(_pingPrefab, _pingContainer);
        _currentPing.SetActive(true);

        RectTransform rect = _currentPing.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localPoint;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(24f, 24f);
        }

        Image image = _currentPing.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
            image.color = Color.green;
        }
    }

    private void ClearPing()
    {
        if (_currentPing != null)
        {
            Destroy(_currentPing);
            _currentPing = null;
        }

        if (_pingContainer == null) return;

        for (int i = _pingContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = _pingContainer.GetChild(i);
            if (child != null && (child.name.Contains("Image") || child.name.Contains("Ping")))
                Destroy(child.gameObject);
        }
    }

    private void AutoAssignMiniMapReferences()
    {
        if (_miniMapCamera == null) _miniMapCamera = FindCameraByNameContains("MiniMap");
        if (_miniMapOverlay == null) _miniMapOverlay = FindGameObjectIncludingInactive("MiniMap (Overlay)");

        RectTransform rawImageRect = FindRawImageRectUnderOverlay();

        if (_mapRect == null)
        {
            if (rawImageRect != null) _mapRect = rawImageRect;
            else if (_miniMapOverlay != null) _mapRect = _miniMapOverlay.GetComponent<RectTransform>();
        }

        if (_pingContainer == null)
        {
            GameObject pingObj = FindGameObjectIncludingInactive("PingContainer");
            if (pingObj != null) _pingContainer = pingObj.GetComponent<RectTransform>();
        }

        if (_pingContainer == null && rawImageRect != null)
        {
            GameObject container = new GameObject("PingContainer", typeof(RectTransform));
            container.transform.SetParent(rawImageRect, false);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            _pingContainer = rect;
        }
    }

    private RectTransform FindRawImageRectUnderOverlay()
    {
        if (_miniMapOverlay == null) return null;

        Transform[] children = _miniMapOverlay.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "RawImage") return child.GetComponent<RectTransform>();
        }

        return null;
    }

    private GameObject FindGameObjectIncludingInactive(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == objectName) return child.gameObject;
            }
        }

        return null;
    }

    private Camera FindCameraByNameContains(string keyword)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);

            foreach (Camera cam in cameras)
            {
                if (cam.name.Contains(keyword)) return cam;
            }
        }

        return null;
    }
}