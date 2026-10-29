using System;
using UnityEngine;

namespace Game.Pings
{
    /*
     * 로컬 클라이언트마다 하나. Z 짧게 떼기 = 일반 핑, 0.2초 유지 = 휠 표시.
     * 마우스 이동 = 종류 선택, Z 떼기 = 해당 프레임 최종 조준 위치에 핑 요청.
     * 휠이 열린 뒤 중앙 X 원에서 Z 떼기 = 취소. ESC는 사용하지 않습니다.
     * 생성한 핑은 월드 좌표에 고정되고 3초 후 제거됩니다.
     * 지형을 못 맞히면 전송하지 않고 안내합니다. 적 추적/Follow Mask 없음.
     * 적 콜라이더도 무시하려면 Hit Mask에서 적 레이어를 제외하세요.
     *
     * projectVector에서는 PingBridge가 아래 연결/해제를 자동 처리합니다.
     * 수동 카메라 연결 예시(로컬 소유권 확인 후 호출):
     *     pingSystem.BindCamera(localPlayerRenderCamera);
     * 리스폰/관전 시 동일 API에 새 Camera 전달. 해제 시 null 전달.
     * FOV/위치/회전 변경 또는 같은 출력 Camera의 Cinemachine 전환은 재연결 불필요.
     *
     * 마우스 룩의 입력 처리 전에 다음 조건을 추가하세요:
     *     if (Game.Pings.PingSystem.WheelOpen) return;
     * 또는 인스턴스 IsWheelOpen / WheelToggled 이벤트를 사용합니다.
     * 입력만 차단하고 카메라 추적/네트워크 갱신은 계속하세요.
     * Cinemachine 입력 어댑터도 휠이 열렸을 때 look 값을 0으로 반환하도록 연결.
     * 바인딩을 유지한 채 같은 Camera의 모드만 전환할 때 필요하면 CancelWheel() 호출.
     * 채팅/메뉴/리스폰 대기 중에는 SetInputEnabled(false), 복귀 시 true.
     *
     * Unity 6 + uGUI(com.unity.ugui) 필요. 일반 키보드 키 Z 기본.
     * 새 입력 시스템 특수키/패드/키 리바인딩은 이 폴링 입력을 프로젝트 InputAction으로 교체.
     * 카메라 구성 참고: https://docs.unity3d.com/ScriptReference/Camera.ViewportPointToRay.html
     */
    // PingBridge(9000)가 게임 상태를 읽은 다음 입력 처리.
    // LateUpdate에서 최종 카메라 자세를 읽고 핑을 확정합니다.
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PingManager), typeof(PingCameraProvider))]
    public sealed class PingSystem : MonoBehaviour
    {
        [Header("Local input")]
        [SerializeField] private KeyCode pingKey = KeyCode.Z;
        [SerializeField] private bool inputEnabled = true;
        [Tooltip("이 시간 미만의 탭은 일반 핑, 계속 누르면 도넛 휠이 열립니다.")]
        [SerializeField, Min(0.05f)] private float wheelHoldDelay = 0.2f;
        [Header("Aim position")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField, Min(1)] private float maxDistance = 150f;
        [SerializeField] private Vector2 aimViewport = new Vector2(0.5f, 0.5f);
        [Tooltip("PlayerIdentity/Drone 아래 콜라이더를 건너뛰고 지형/월드 좌표를 선택합니다.")]
        [SerializeField] private bool ignoreCharactersAndDrones = true;
        [Header("Wheel settings (pixels)")]
        [SerializeField, Min(60)] private float wheelRadius = 130;
        [SerializeField, Min(1)] private float deadZone = 30;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 1f;
        [SerializeField] private bool unlockCursorWhileOpen;
        [SerializeField] private bool showSystemCursor;

        // 정적 속성은 기존 README의 룩 차단 코드와 호환. 로컬 UI가 하나인 구성용.
        public static bool WheelOpen => openWheelCount > 0;
        public bool IsWheelOpen { get; private set; }
        public event Action<bool> WheelToggled;
        private static int openWheelCount;
        private PingManager manager;
        private PingCameraProvider cameras;
        private PingWheel wheel;
        private Camera openedCamera;
        private Vector3 capturedPosition;
        private bool previousHeld;
        private bool ignoreUntilReleased;
        private bool hasFocus = true;
        private CursorLockMode savedLock;
        private bool savedVisible;
        private bool commitPending;
        private int pendingSelection = -1;
        private bool pressActive;
        private float pressedAt;
        private Transform ignoredPlayer;
        private Transform ignoredDrone;
        private Func<bool> externalInputGate;
        public bool InputEnabled => inputEnabled;

        // 외부 UI/카메라 관리자가 우선권을 가지면 커서 상태를 덮어쓰지 않습니다.
        public void SetInputGate(Func<bool> gate) => externalInputGate = gate;
        public void SetIgnoredRoots(Transform player, Transform drone)
        { ignoredPlayer = player; ignoredDrone = drone; }
        public void ExcludeHitLayers(int layers) => hitMask = hitMask.value & ~layers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => openWheelCount = 0;

        private void Awake()
        {
            manager = GetComponent<PingManager>(); cameras = GetComponent<PingCameraProvider>();
            wheel = manager.wheel;
            if (!wheel || !manager.CanvasRoot) { Debug.LogError("Ping UI must be authored before Play", this); enabled=false; return; }
            wheel.Initialize(manager.Options, wheelRadius, deadZone);
        }

        private void OnEnable()
        {
            ignoreUntilReleased = true;
            cameras.CameraChanged += OnCameraChanged;
        }

        public void BindCamera(Camera camera) => cameras.BindCamera(camera);

        public void SetInputEnabled(bool enabled)
        {
            if (inputEnabled == enabled) return;
            inputEnabled = enabled;
            if (!enabled) CancelWheel();
            ignoreUntilReleased = true;
        }

        private void Update()
        {
            bool held = PingInputCompat.Held(pingKey);
            if (!inputEnabled || !hasFocus || !manager.isActiveAndEnabled ||
                (externalInputGate != null && !externalInputGate()))
            { CancelWheel(); previousHeld = held; ignoreUntilReleased = true; return; }
            if (ignoreUntilReleased)
            { previousHeld = held; if (!held) ignoreUntilReleased = false; return; }
            if (pressActive && cameras.Current != openedCamera) CancelWheel();
            if (held && !previousHeld && !pressActive)
            {
                openedCamera = cameras.Current;
                if (openedCamera != null)
                { pressActive = true; pressedAt = Time.unscaledTime; }
            }
            if (pressActive && held)
            {
                if (!IsWheelOpen && Time.unscaledTime - pressedAt >= Mathf.Max(0.05f, wheelHoldDelay))
                    OpenWheel();
                else if (IsWheelOpen) wheel.Move(PingInputCompat.MouseDelta() * mouseSensitivity);
            }
            if (!held && previousHeld && pressActive)
            {
                // Z 해제 프레임에는 포인터를 더 움직이지 않고 직전 선택을 확정합니다.
                pendingSelection = IsWheelOpen ? wheel.SelectedIndex :
                    Time.unscaledTime - pressedAt < Mathf.Max(0.05f, wheelHoldDelay)
                        ? Array.FindIndex(manager.Options, option => option.type == PingType.Location) : -1;
                wheel.Close(); // 월드 핑 확정 전에도 휠은 즉시 숨깁니다.
                commitPending = true;
            }
            previousHeld = held;
        }

        private void LateUpdate()
        {
            if (!pressActive) return;
            if (cameras.Current != openedCamera || (externalInputGate != null && !externalInputGate()))
            { CancelWheel(false); return; }
            if (commitPending) CommitWheel();
        }

        private bool CaptureAim()
        {
            Ray ray = openedCamera.ViewportPointToRay(new Vector3(
                Mathf.Clamp01(aimViewport.x), Mathf.Clamp01(aimViewport.y), 0));
            float closest = Mathf.Max(1, maxDistance);
            bool found = false;
            // 한 번의 요청에만 할당. NonAlloc 고정 버퍼가 가득 차서 가까운 벽을 놓치는 경우 방지.
            foreach (RaycastHit hit in Physics.RaycastAll(ray, closest, hitMask, QueryTriggerInteraction.Ignore))
            {
                Transform t = hit.collider.transform;
                if ((ignoredPlayer != null && t.IsChildOf(ignoredPlayer)) ||
                    (ignoredDrone != null && t.IsChildOf(ignoredDrone))) continue;
                if (ignoreCharactersAndDrones &&
                    (hit.collider.GetComponentInParent<global::PlayerIdentity>() != null ||
                     hit.collider.GetComponentInParent<global::Drone>() != null)) continue;
                if (hit.distance >= closest) continue;
                closest = hit.distance; capturedPosition = hit.point; found = true;
            }
            if (!found) manager.Notice("표시할 지형을 가리켜 주세요");
            return found;
        }

        private void OpenWheel()
        {
            openedCamera = cameras.Current;
            if (openedCamera == null) return;
            commitPending = false;
            pendingSelection = -1;
            savedLock = Cursor.lockState; savedVisible = Cursor.visible;
            Cursor.lockState = unlockCursorWhileOpen ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = unlockCursorWhileOpen && showSystemCursor;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.CanvasRoot,
                openedCamera.pixelRect.center, null, out Vector2 local);
            ((RectTransform)wheel.transform).anchoredPosition = local;
            wheel.transform.SetAsLastSibling(); wheel.Open();
            IsWheelOpen = true; openWheelCount++; WheelToggled?.Invoke(true);
        }

        private void CommitWheel()
        {
            int index = pendingSelection;
            if (index >= 0 && !CaptureAim()) index = -1;
            Vector3 position = capturedPosition;
            CloseWheel();
            if (index >= 0) manager.RequestPing(manager.Options[index].type, position);
        }

        public void CancelWheel() => CloseWheel();
        public void CancelWheel(bool restoreCursor) => CloseWheel(restoreCursor);

        private void CloseWheel(bool restoreCursor = true)
        {
            commitPending = false; pendingSelection = -1; pressActive = false;
            openedCamera = null;
            if (!IsWheelOpen) return;
            IsWheelOpen = false; openWheelCount = Mathf.Max(0, openWheelCount - 1);
            if (wheel != null) wheel.Close();
            CursorLockMode ownedLock = unlockCursorWhileOpen ? CursorLockMode.Confined : CursorLockMode.Locked;
            bool ownedVisibility = unlockCursorWhileOpen && showSystemCursor;
            if (restoreCursor && Cursor.lockState == ownedLock && Cursor.visible == ownedVisibility)
            { Cursor.lockState = savedLock; Cursor.visible = savedVisible; }
            openedCamera = null; WheelToggled?.Invoke(false);
        }

        private void OnCameraChanged(Camera camera)
        { if (pressActive && camera != openedCamera) CancelWheel(); }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) CancelWheel();
            ignoreUntilReleased = true;
        }

        private void OnApplicationPause(bool paused)
        { if (paused) { CancelWheel(); ignoreUntilReleased = true; } }

        private void OnDisable()
        {
            if (cameras != null) cameras.CameraChanged -= OnCameraChanged;
            CancelWheel();
        }

        private void OnDestroy()
        { if (wheel != null) Destroy(wheel.gameObject); }
    }
}
