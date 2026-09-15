using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Pings
{
    /*
     * 설치: 빈 로컬 UI 오브젝트에 PingSystem 추가(필수 컴포넌트 자동 추가).
     * PingCameraProvider의 Aim Camera 연결, PingSystem의 Hit Mask에서 플레이어 레이어 제외.
     * 네트워크 플레이어 프리팹마다 생성하지 말고 로컬 클라이언트마다 하나만 둡니다.
     *
     * 권장 서버 흐름(이 파일은 SDK에 독립적인 클라이언트 표시/요청 계층):
     * 1. ConfigureNetwork(true, localTeamId) 후 PingRequested를 네트워크 어댑터에서 구독.
     * 2. PingRequested -> NetworkManager의 새 요청 패킷으로 RequestId/Type/Position 전달.
     * 3. 서버는 접속 세션에서 발신자와 팀을 조회. 클라이언트가 보낸 팀/발신자는 신뢰 금지.
     *    생존/관전 권한, 핑 종류, 유한 좌표, 월드 경계, 거리, 쿨다운, RequestId 중복 검증.
     *    벽 너머 제한이 필요하면 서버에서 시야 검사. 허공 핑 허용 정책도 동일하게 적용.
     * 4. 서버가 고유 PingId를 생성하고 해당 팀 연결에만 ConfirmedPing 전송(발신자 포함).
     * 5. 클라이언트 알림 패킷 수신 -> Unity 메인 스레드에서 ReceiveConfirmed 호출.
     *    서버 핑 오브젝트 스폰/Transform 동기화는 필요 없음: 고정 좌표 이벤트만 전달.
     * 6. 표시 수명은 각 클라이언트 수신 후 실시간 3초. 일시정지(timeScale=0)에도 만료.
     *    모두 같은 서버 시각에 만료해야 한다면 메시지에 서버 만료 시각을 추가하고
     *    SDK의 동기화된 서버 시계로 남은 시간을 계산하도록 변경.
     *
     * 네트워크 연결/재접속/팀 변경/로그아웃 때 ConfigureNetwork를 호출하면
     * 이전 세션 마커와 중복 캐시가 초기화됩니다. 구독은 어댑터 OnDisable에서 해제.
     * 아래 로컬 필터/쿨다운은 보안 검증을 대체하지 않습니다. 실제 패킷 처리는 별도 구현 필요.
     */
    // SpectatorManager/드론의 LateUpdate와 핑 입력 확정 이후에 투영합니다.
    [DefaultExecutionOrder(11000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PingCameraProvider))]
    public sealed class PingManager : MonoBehaviour
    {
        public const float MarkerLifetime = 3f;
        [SerializeField] private PingOption[] options =
        {
            new PingOption(PingType.Location, "HERE", new Color(0.25f, 0.8f, 1f)),
            new PingOption(PingType.Danger, "DANGER", new Color(1f, 0.3f, 0.25f)),
            new PingOption(PingType.Attack, "ATTACK", new Color(1f, 0.7f, 0.2f)),
            new PingOption(PingType.Defend, "DEFEND", new Color(0.35f, 1f, 0.55f))
        };
        [Header("Marker settings")]
        [SerializeField, Range(1, 64)] private int maxMarkers = 16;
        [SerializeField, Min(40)] private float edgePadding = 80;
        [SerializeField, Min(0)] private float requestCooldown = 0.35f;
        [Header("Network (enable before accepting multiplayer input)")]
        [SerializeField] private bool networkMode;
        [SerializeField] private int localTeamId = -1;

        public event Action<PingRequest> PingRequested;
        public bool NetworkMode => networkMode;
        public PingOption[] Options => options;
        public RectTransform CanvasRoot { get { EnsureInitialized(); return canvasRoot; } }
        private readonly List<PingMarker> markers = new List<PingMarker>();
        private readonly HashSet<string> receivedIds = new HashSet<string>();
        private readonly Queue<string> receivedOrder = new Queue<string>();
        private PingCameraProvider cameras;
        private RectTransform canvasRoot;
        private float nextRequestAt;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (canvasRoot != null) return;
            cameras = GetComponent<PingCameraProvider>();
            // 잘못된 Inspector 배열은 기본 항목으로 복구해 휠의 0 나눗셈을 방지합니다.
            if (options == null || options.Length == 0 || Array.Exists(options, option =>
                option == null || !Enum.IsDefined(typeof(PingType), option.type)))
                options = new[] { new PingOption(PingType.Location, "HERE", Color.cyan) };
            var go = new GameObject("PingCanvas", typeof(RectTransform), typeof(Canvas));
            // 기존 Canvas 아래에 PingManager를 배치해도 픽셀 좌표/정렬이 독립적입니다.
            // OnDisable/OnEnable/OnDestroy에서 표시와 수명을 직접 관리합니다.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            canvasRoot = (RectTransform)go.transform;
            // 기본 Overlay 픽셀 좌표 사용. EventSystem/TMP/프리팹/스프라이트 불필요.
        }

        public void ConfigureNetwork(bool enabled, int teamId)
        {
            networkMode = enabled; localTeamId = teamId;
            nextRequestAt = 0; ClearMarkers();
            receivedIds.Clear(); receivedOrder.Clear();
        }

        public void SetLocalTeam(int teamId)
        {
            if (localTeamId != teamId) ConfigureNetwork(networkMode, teamId);
        }

        // true는 로컬 표시 또는 요청 전송을 의미하며 서버 승인 여부가 아닙니다.
        public bool RequestPing(PingType type, Vector3 position)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || !PingValidation.IsFinite(position) || FindOption(type) == null ||
                Time.unscaledTime < nextRequestAt) return false;
            if (networkMode && (localTeamId < 0 || PingRequested == null)) return false;
            nextRequestAt = Time.unscaledTime + requestCooldown;
            if (!networkMode) ShowMarker(type, position);
            else PingRequested.Invoke(new PingRequest(Guid.NewGuid().ToString("N"), type, position));
            return true;
        }

        // 검증된 서버 수신부에서만 호출. 일반 클라이언트 입력은 RequestPing으로 보냅니다.
        public bool ReceiveConfirmed(ConfirmedPing ping)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || !networkMode || localTeamId < 0 || ping.TeamId != localTeamId ||
                string.IsNullOrEmpty(ping.PingId) || ping.PingId.Length > 128 ||
                !PingValidation.IsFinite(ping.Position) || FindOption(ping.Type) == null ||
                !receivedIds.Add(ping.PingId)) return false;
            receivedOrder.Enqueue(ping.PingId);
            // 최근 이벤트 중복만 제거. 오래된 요청/재전송 방지는 서버/전송 계층에서도 처리.
            while (receivedOrder.Count > 512) receivedIds.Remove(receivedOrder.Dequeue());
            ShowMarker(ping.Type, ping.Position);
            return true;
        }

        private PingOption FindOption(PingType type) => Array.Find(options, option => option.type == type);

        private void ShowMarker(PingType type, Vector3 position)
        {
            while (markers.Count >= Mathf.Max(1, maxMarkers)) RemoveMarker(0);
            var marker = PingUI.Rect("PingMarker", canvasRoot, Vector2.one * 30)
                .gameObject.AddComponent<PingMarker>();
            marker.Initialize(position, FindOption(type), Time.unscaledTime + MarkerLifetime);
            markers.Add(marker);
            marker.Refresh(cameras.Current, canvasRoot, edgePadding);
        }

        private void LateUpdate()
        {
            Camera current = cameras.Current;
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                if (markers[i] == null || Time.unscaledTime >= markers[i].ExpiresAt) RemoveMarker(i);
                else markers[i].Refresh(current, canvasRoot, edgePadding);
            }
        }

        private void RemoveMarker(int index)
        {
            if (markers[index] != null)
            { markers[index].gameObject.SetActive(false); Destroy(markers[index].gameObject); }
            markers.RemoveAt(index);
        }

        public void ClearMarkers()
        { for (int i = markers.Count - 1; i >= 0; i--) RemoveMarker(i); }

        private void OnDisable()
        {
            ClearMarkers();
            if (canvasRoot != null) canvasRoot.gameObject.SetActive(false);
        }

        private void OnEnable()
        { if (canvasRoot != null) canvasRoot.gameObject.SetActive(true); }

        private void OnDestroy()
        { if (canvasRoot != null) Destroy(canvasRoot.gameObject); }
    }
}
