using UnityEngine;

namespace Game.Pings
{
    [DefaultExecutionOrder(9000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PingSystem))]
    public sealed class PingBridge : MonoBehaviour
    {
        [Tooltip("서버 없는 씬 테스트에서만 명시적으로 지정하는 로컬 PlayerIdentity.")]
        [SerializeField] private PlayerIdentity offlineLocalPlayer;

        private PingSystem pings;
        private PingManager manager;
        private PlayerIdentity localPlayer;
        private PlayerCameraController playerView;
        private PlayerDeathController death;
        private PlayerHP hp;
        private Camera currentCamera;
        private Drone currentDrone;
        private bool canRequest;
        private bool previousDead;
        private bool previousSpectating;
        private uint previousTargetId;
        private NetworkManager previousNetwork;
        private bool previousConnected;
        private uint previousPlayerId;
        private float nextLookup;

        private void Awake()
        {
            pings = GetComponent<PingSystem>(); manager = GetComponent<PingManager>();
            // 무기/팔/미니맵/선택용 모델이 에임 레이에 끼어들지 않도록 프로젝트 레이어 제외.
            pings.ExcludeHitLayers(LayerMask.GetMask("Ignore Raycast", "UI", "MiniMap",
                "LocalPlayer", "FirstPersonArms", "SelectModel", "Bullet",
                "AK47 01", "M416 01", "UMP 01", "AWM 01", "P92 01",
                "AK47 01 (Rotate)", "M416 01 (Rotate)", "UMP 01 (Rotate)",
                "AWM 01 (Rotate)", "P92 01 (Rotate)"));
        }

        private void OnEnable() => pings.SetInputGate(CanRequest);
        private bool CanRequest() => isActiveAndEnabled && canRequest;
        private void Update() => RefreshContext();
        private void LateUpdate() => RefreshContext();

        private void RefreshContext()
        {
            NetworkManager network = NetworkManager.Instance;
            bool connected = network != null && network.IsConnected;
            uint playerId = connected ? network.MyPlayerId : 0;
            if (network != previousNetwork || connected != previousConnected || playerId != previousPlayerId)
            {
                SetLocalPlayer(null);
                manager.ConfigureNetwork(manager.NetworkMode, -1);
                previousNetwork = network; previousConnected = connected; previousPlayerId = playerId;
                nextLookup = 0;
            }

            if (connected)
            {
                if (localPlayer != null && (!localPlayer.gameObject.activeInHierarchy || localPlayer.PlayerId != playerId))
                    SetLocalPlayer(null);
                // NetworkManager.MyPlayer 내부는 씬 전체 검색이므로 유효한 참조를 캐시합니다.
                if (localPlayer == null && Time.unscaledTime >= nextLookup)
                {
                    nextLookup = Time.unscaledTime + 0.25f;
                    PlayerIdentity candidate = network.MyPlayer;
                    if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.PlayerId == playerId)
                        SetLocalPlayer(candidate);
                }
            }
            else if (localPlayer != offlineLocalPlayer) SetLocalPlayer(offlineLocalPlayer);

            bool spectating = SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating;
            uint targetId = spectating ? SpectatorManager.Instance.CurrentTargetPlayerId : 0;
            bool dead = (death != null && death.IsDead) || (hp != null && hp.IsDead);
            if (spectating != previousSpectating || targetId != previousTargetId || dead != previousDead)
            {
                pings.CancelWheel(false);
                if (previousDead && !dead) manager.ClearMarkers();
                previousDead = dead; previousSpectating = spectating; previousTargetId = targetId;
            }

            Camera selected = null;
            Drone drone = null;
            bool localAvailable = localPlayer != null && localPlayer.gameObject.activeInHierarchy;
            if (localAvailable)
            {
                // 관전은 자신의 출력 Camera를 계속 사용. 상대 드론의 카메라는 선택하지 않음.
                if (!spectating && DroneNetworkManager.Instance != null &&
                    DroneNetworkManager.Instance.TryGetActiveDroneByOwner(localPlayer.PlayerId, out Drone candidate) &&
                    candidate != null && candidate.IsMine && candidate.ViewTransform != null)
                {
                    Camera droneCamera = candidate.ViewTransform.GetComponent<Camera>();
                    if (IsOutputCamera(droneCamera)) { selected = droneCamera; drone = candidate; }
                }
                if (selected == null && playerView != null && IsOutputCamera(playerView.PlayerCamera))
                    selected = playerView.PlayerCamera;
            }
            if (selected != currentCamera || drone != currentDrone)
            {
                pings.CancelWheel(false);
                currentCamera = selected; currentDrone = drone;
            }
            pings.BindCamera(selected);
            pings.SetIgnoredRoots(localAvailable ? localPlayer.transform : null, drone != null ? drone.transform : null);
            int team = localAvailable && localPlayer.Team <= 1 ? localPlayer.Team : -1;
            manager.SetLocalTeam(team);

            GameController game = GameController.Instance;
            bool uiBlocked = InGameChatUI.BlocksGameplayInput ||
                (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen) ||
                (game != null && (game.IsPaused || game.isLoading));
            // 설정/상점 등에서 이미 커서를 풀었으면 핑이 커서를 다시 가두지 않습니다.
            bool freeCursor = Cursor.lockState == CursorLockMode.None;
            canRequest = localAvailable && selected != null && !dead && !spectating &&
                !uiBlocked && !freeCursor && (!manager.NetworkMode || connected);
            if (!canRequest) pings.CancelWheel(false);
        }

        private static bool IsOutputCamera(Camera camera) => camera != null &&
            camera.isActiveAndEnabled && camera.targetTexture == null && camera.targetDisplay == 0;

        private void SetLocalPlayer(PlayerIdentity player)
        {
            pings.CancelWheel(false); manager.ClearMarkers();
            localPlayer = player;
            playerView = player != null ? player.GetComponentInChildren<PlayerCameraController>(true) : null;
            death = player != null ? player.GetComponentInChildren<PlayerDeathController>(true) : null;
            hp = player != null ? player.GetComponentInChildren<PlayerHP>(true) : null;
        }

        private void OnDisable()
        {
            canRequest = false;
            if (pings != null)
            {
                pings.CancelWheel();
                // 비활성 어댑터에서 입력이 일반 Camera 모드로 되돌아가는 것을 방지.
                pings.SetInputGate(CanRequest);
                pings.BindCamera(null);
            }
        }
    }
}
