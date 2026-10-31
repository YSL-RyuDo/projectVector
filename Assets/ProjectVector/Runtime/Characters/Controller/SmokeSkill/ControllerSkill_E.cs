using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ControllerSkill_E : CharacterSkill
{
    public static event Action OnMiniMap;
    public static bool IsSelectingSmoke { get; private set; }
    public static bool IsSmokeMapOpen { get; private set; }
    public static bool IsBlockingWeaponInput { get; private set; }
    [SerializeField] private GameObject _miniMapOverlay;
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private Camera _miniMapCamera;
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private ControllerSmokeMotion _motion;
    private SmokeTargetingView _view;
    private PlayerIdentity _identity;
    private FPSCameraLook _look;
    private bool _selecting, _selected, _ownsInput, _hidWeapon;
    private Vector3 _selectedWorld;
    private Coroutine _release;
    private void Awake() { _identity = GetComponent<PlayerIdentity>(); _look = GetComponentInChildren<FPSCameraLook>(true); }
#if UNITY_EDITOR
    // Batch-only local integration fixture. Not compiled into game clients.
    // Input, purchase checks, targeting, gesture and teardown still run normally.
    public Action<Vector3> EditorCaptureDrop;
    private bool IsLocalCapture => Application.isBatchMode && PracticePlayerContext.For(this) != null && EditorCaptureDrop != null;
#else
    private bool IsLocalCapture => false;
#endif
    private bool IsMine() => IsLocalCapture || (_identity && NetworkManager.Instance && _identity.PlayerId == NetworkManager.Instance.MyPlayerId);
    private void ResolveView()
    {
        if (!_view) foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
            _view = root.GetComponentInChildren<SmokeTargetingView>(true); if (_view) break;
        }
        if (_view) { _miniMapOverlay = _view.gameObject; _mapRect = _view.Map; }
        if (!_miniMapCamera) foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
            foreach (var cam in root.GetComponentsInChildren<Camera>(true)) if (cam.name.Contains("MiniMap")) { _miniMapCamera = cam; break; }
            if (_miniMapCamera) break;
        }
    }
    private bool Interrupted() => GetComponent<PlayerHP>()?.IsDead == true ||
        GetComponent<PlayerDeathController>()?.IsDead == true || GetComponent<ElectricShock>()?.IsStunned == true ||
        (RoundManager.Instance && (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd || RoundManager.Instance.currentState == RoundManager.RoundState.GameOver)) ||
        (!IsLocalCapture && (!NetworkManager.Instance || !NetworkManager.Instance.IsConnected));
    private void Update()
    {
        if (!IsMine()) return;
        if (_ownsInput && Interrupted()) { CloseImmediate(); return; }
        if (Keyboard.current == null || Mouse.current == null) return;
        if (GameplayActionPolicy.MenuBlocksInput) { if (_selecting) Close(); return; }
        if (Keyboard.current.eKey.wasPressedThisFrame) { UseSkill(); return; }
        if (!_selecting) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (!_selected) PreviewAt(Mouse.current.position.ReadValue());
        if (Mouse.current.leftButton.wasPressedThisFrame) SelectAt(Mouse.current.position.ReadValue());
        if (Mouse.current.rightButton.wasPressedThisFrame && _selected) Confirm();
    }
    private void LateUpdate()
    {
        if (!IsMine()) return;
        if (_ownsInput) { GetComponent<PlayerItem>()?.SetWeaponVisibility(false, this); _hidWeapon = true; }
        else if (_hidWeapon) { GetComponent<PlayerItem>()?.SetWeaponVisibility(true, this); _hidWeapon = false; }
    }
    protected override void UseSkill()
    {
        if (_selecting) { Close(); return; }
        if (_ownsInput || !CanUsePurchasedSkill(SkillSlot.E)) return;
        ResolveView();
        if (!_view || !_mapRect || !_miniMapCamera) { Debug.LogWarning("Smoke targeting references missing in saved scene"); return; }
        _selecting = _ownsInput = true; _selected = false;
        IsSelectingSmoke = IsSmokeMapOpen = IsBlockingWeaponInput = true;
        _miniMapCamera.GetComponent<MiniMapCamera>()?.SetSmokeHeading(_look ? _look.GetYaw() : transform.eulerAngles.y);
        MiniMapManager.Instance?.ShowOverlayMiniMap();
        _miniMapOverlay.SetActive(true); _view.ResetSelection(); _motion?.Begin();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (_look) _look.CanMove = false;
        OnMiniMap?.Invoke();
    }
    public bool SelectAt(Vector2 screenPoint)
    {
        if (!_selecting || !_mapRect || !_miniMapCamera) return false;
        if (!TryMapPoint(screenPoint, out var local)) return false;
        if (!TryGroundPoint(local, out var world)) {
            _selected = false; _view.Invalid(); return false;
        }
        _selectedWorld = world; _selected = true;
        _view.Select(local, FootprintSize(), Vector3.Distance(transform.position, world));
        return true;
    }
    public bool PreviewAt(Vector2 screenPoint)
    {
        if (!_selecting || _selected || !_mapRect || !_miniMapCamera) return false;
        if (!TryMapPoint(screenPoint, out var local)) { _view.ResetSelection(); return false; }
        if (!TryGroundPoint(local, out var world)) { _view.Invalid(); return false; }
        _view.Preview(local, FootprintSize(), Vector3.Distance(transform.position, world));
        return true; // Preview never arms a drop or changes its selected coordinates.
    }
    private bool TryMapPoint(Vector2 screenPoint, out Vector2 local)
    {
        var canvas = _mapRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapRect, screenPoint, uiCamera, out local) && _mapRect.rect.Contains(local);
    }
    private bool TryGroundPoint(Vector2 local, out Vector3 world)
    {
        world = default;
        Vector2 normalized = Rect.PointToNormalized(_mapRect.rect, local);
        Ray ray = _miniMapCamera.ViewportPointToRay(new Vector3(normalized.x, normalized.y));
        // Hidden actors must not move/disable the preview and disclose their position.
        int mask = _groundMask & ~LayerMask.GetMask("LocalPlayer", "Enemy", "Player", "PlayerBody", "FirstPersonArms", "HitBox", "Drone", "Item", "Bullet", "UI");
        var hits = Physics.RaycastAll(ray, 1000, mask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits) {
            if (hit.collider.GetComponentInParent<PlayerIdentity>() || hit.collider.GetComponentInParent<CharacterController>()) continue;
            if (hit.collider.GetComponentInParent<MapUtilityPlacementBlocker>()) return false;
            if (hit.normal.y < .45f) return false;
            world = hit.point; return true;
        }
        return false;
    }
    private Vector2 FootprintSize()
    {
        float height = _miniMapCamera.orthographicSize * 2;
        return new Vector2(9 / (height * _miniMapCamera.aspect) * _mapRect.rect.width, 9 / height * _mapRect.rect.height);
    }
    private void Confirm()
    {
        // Server owns charge consumption/acceptance. Cancellation never sends a drop.
        if (!_selected || Interrupted() || !CanUsePurchasedSkill(SkillSlot.E)) { Close(); return; }
#if UNITY_EDITOR
        if (IsLocalCapture) EditorCaptureDrop(_selectedWorld);
        else
#endif
            NetworkManager.Instance.SendControllerSmokeDrop(_selectedWorld);
        Close();
    }
    private void Close()
    {
        if (!_ownsInput) return;
        _selecting = _selected = false; IsSelectingSmoke = IsSmokeMapOpen = false;
        if (_miniMapCamera) _miniMapCamera.GetComponent<MiniMapCamera>()?.ClearSmokeHeading();
        MiniMapManager.Instance?.ShowHUDMiniMap();
        if (_miniMapOverlay) _miniMapOverlay.SetActive(false);
        if (_look) _look.CanMove = true;
        if (!SettingsMenu.PreserveOpenMenuCursor()) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        _motion?.Release();
        if (_release != null) StopCoroutine(_release);
        _release = StartCoroutine(ReleaseInput()); OnMiniMap?.Invoke();
    }
    private IEnumerator ReleaseInput()
    {
        yield return null;
        while ((_motion && _motion.Active) || (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))) yield return null;
        _ownsInput = false; IsBlockingWeaponInput = false; _release = null;
    }
    public void ForceCloseMiniMapImmediate() => CloseImmediate();
    private void CloseImmediate()
    {
        if (!_ownsInput) return; // Remote disable must not close/reset the local UI.
        if (_release != null) StopCoroutine(_release); _release = null;
        _selecting = _selected = _ownsInput = false;
        IsSelectingSmoke = IsSmokeMapOpen = IsBlockingWeaponInput = false;
        if (_miniMapCamera) _miniMapCamera.GetComponent<MiniMapCamera>()?.ClearSmokeHeading();
        MiniMapManager.Instance?.ShowHUDMiniMap();
        if (_miniMapOverlay) _miniMapOverlay.SetActive(false);
        if (_look) _look.CanMove = true;
        _motion?.ResetPose();
        if (_hidWeapon) { GetComponent<PlayerItem>()?.SetWeaponVisibility(true, this); _hidWeapon = false; }
        if (!SettingsMenu.PreserveOpenMenuCursor()) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        OnMiniMap?.Invoke();
    }
    private void OnDisable() => CloseImmediate();
}
