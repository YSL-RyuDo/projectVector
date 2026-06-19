using System.Collections.Generic;
using UnityEngine;

public class MiniMapManager : MonoBehaviour
{
    public static MiniMapManager Instance;

    [Header("[HUD 미니맵 설정]")]
    [SerializeField] private RectTransform _miniMapHUD;
    [SerializeField] private float _hudWidth = 180f;
    [SerializeField] private float _hudHeight = 180f;
    [SerializeField] private RectTransform _hudMarkerContainer;
    [SerializeField] private RectTransform _hudPlayerMarker;
    [SerializeField] private RectTransform _hudTopLayer; // 인스펙터에서 Canvas(MiniMap)을 꼭 할당해줘!

    [Header("[오버레이 미니맵 설정]")]
    [SerializeField] private RectTransform _miniMapOverlay;
    [SerializeField] private float _overlayWidth = 600f;
    [SerializeField] private float _overlayHeight = 600f;
    [SerializeField] private RectTransform _overlayMarkerContainer;
    [SerializeField] private RectTransform _overlayPlayerMarker;

    [Header("공용 마커 프리팹")]
    [SerializeField] private Camera _miniMapCamera;
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField] private GameObject _zoneAMarkerPrefab;
    [SerializeField] private GameObject _zoneBMarkerPrefab;

    private Transform _localPlayerTransform;
    private Camera _localPlayerCamera;

    private List<MiniMapEnemy> _activeEnemies = new List<MiniMapEnemy>();

    private Dictionary<MiniMapEnemy, RectTransform> _hudEnemyMarkers = new Dictionary<MiniMapEnemy, RectTransform>();
    private Dictionary<MiniMapEnemy, RectTransform> _overlayEnemyMarkers = new Dictionary<MiniMapEnemy, RectTransform>();

    [Header("[A / B 구역 표시 마크]")]
    [SerializeField] private RectTransform _zoneAMarker;
    [SerializeField] private RectTransform _zoneBMarker;

    private Transform _zoneATransform;
    private Transform _zoneBTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideAllMiniMaps();
    }

    private void Start()
    {
        if (_miniMapHUD != null)
        {
            _miniMapHUD.sizeDelta =
                new Vector2(
                    _hudWidth,
                    _hudHeight
                );
        }

        if (_hudPlayerMarker != null)
        {
            _hudPlayerMarker.anchoredPosition =
                Vector2.zero;

            _hudPlayerMarker.gameObject.SetActive(false);
        }

        if (_miniMapOverlay != null)
        {
            _miniMapOverlay.sizeDelta =
                new Vector2(
                    _overlayWidth,
                    _overlayHeight
                );
        }

        if (_overlayPlayerMarker != null)
        {
            _overlayPlayerMarker.anchoredPosition =
                Vector2.zero;

            _overlayPlayerMarker.gameObject.SetActive(false);
        }

        if (_miniMapCamera == null &&
            MiniMapCamera.Instance != null)
        {
            _miniMapCamera =
                MiniMapCamera.Instance.GetComponent<Camera>();
        }

        InitZoneMarkers();

        // 맵이 아직 생성되지 않았을 수 있으므로 null 안전하게 검색
        RefreshZoneTargets();

        // GAME_ALL_READY 전까지 미니맵 숨김
        HideAllMiniMaps();
    }

    private void LateUpdate()
    {
        UpdatePlayerMarkerRotation();

        if (_miniMapCamera == null)
        {
            if (MiniMapCamera.Instance != null)
            {
                _miniMapCamera =
                    MiniMapCamera.Instance.GetComponent<Camera>();
            }

            if (_miniMapCamera == null)
                return;
        }

        if (_localPlayerTransform == null)
            return;

        if (_zoneATransform == null ||
            _zoneBTransform == null)
        {
            RefreshZoneTargets();
        }

        UpdateZoneMarkerPositions();

        if (_localPlayerCamera == null)
            return;

        Plane[] mainCamPlanes =
            GeometryUtility.CalculateFrustumPlanes(
                _localPlayerCamera
            );

        foreach (MiniMapEnemy enemy in _activeEnemies)
        {
            if (enemy == null ||
                enemy.EnemyCollider == null)
            {
                continue;
            }

            bool isVisibleInMainCam =
                GeometryUtility.TestPlanesAABB(
                    mainCamPlanes,
                    enemy.EnemyCollider.bounds
                );

            if (isVisibleInMainCam)
                UpdateEnemyMarker(enemy);
            else
                HideEnemyMarker(enemy);
        }
    }

    private void InitZoneMarkers()
    {
        RectTransform parentLayer = _hudTopLayer != null ? _hudTopLayer : _hudMarkerContainer;

        if (_zoneAMarkerPrefab != null && parentLayer != null && _zoneAMarker == null)
        {
            GameObject goA = Instantiate(_zoneAMarkerPrefab, parentLayer);
            _zoneAMarker = goA.GetComponent<RectTransform>();
            _zoneAMarker.localPosition = Vector3.zero;
            _zoneAMarker.localScale = Vector3.one;
            _zoneAMarker.localRotation = Quaternion.identity;
            _zoneAMarker.gameObject.SetActive(true);
        }

        if (_zoneBMarkerPrefab != null && parentLayer != null && _zoneBMarker == null)
        {
            GameObject goB = Instantiate(_zoneBMarkerPrefab, parentLayer);
            _zoneBMarker = goB.GetComponent<RectTransform>();
            _zoneBMarker.localPosition = Vector3.zero;
            _zoneBMarker.localScale = Vector3.one;
            _zoneBMarker.localRotation = Quaternion.identity;
            _zoneBMarker.gameObject.SetActive(true);
        }
    }

    private void UpdateZoneMarkerPositions()
    {
        if (_miniMapCamera == null || _localPlayerTransform == null) return;

        float cameraScale = _miniMapCamera.orthographic ? _miniMapCamera.orthographicSize : 30f;
        Vector2 hudSize = _miniMapHUD != null ? _miniMapHUD.sizeDelta : new Vector2(_hudWidth, _hudHeight);

        float margin = 22f;
        float maxRadius = (hudSize.x * 0.5f) - margin;

        // 카메라가 회전하므로 회전 각도(Y축) 만큼 2D 상에서 역회전 연산 적용
        float cameraYaw = _miniMapCamera.transform.eulerAngles.y;
        Quaternion rotationOffset = Quaternion.Euler(0, 0, cameraYaw);

        // 1. A구역 연산
        if (_zoneATransform != null && _zoneAMarker != null)
        {
            Vector3 deltaA = _zoneATransform.position - _localPlayerTransform.position;

            // 월드 기준 상대 좌표를 미니맵 픽셀 단위로 환산
            Vector3 basePos = new Vector3(
                (deltaA.x / cameraScale) * (hudSize.x * 0.5f),
                (deltaA.z / cameraScale) * (hudSize.y * 0.5f),
                0f
            );

            // 카메라 회전 값을 적용해 텍스트 회전 방향 정렬
            Vector2 targetPos = rotationOffset * basePos;

            if (targetPos.magnitude > maxRadius)
            {
                targetPos = targetPos.normalized * maxRadius;
            }

            _zoneAMarker.anchoredPosition = targetPos;
        }

        // 2. B구역 연산
        if (_zoneBTransform != null && _zoneBMarker != null)
        {
            Vector3 deltaB = _zoneBTransform.position - _localPlayerTransform.position;

            Vector3 basePos = new Vector3(
                (deltaB.x / cameraScale) * (hudSize.x * 0.5f),
                (deltaB.z / cameraScale) * (hudSize.y * 0.5f),
                0f
            );

            Vector2 targetPos = rotationOffset * basePos;

            if (targetPos.magnitude > maxRadius)
            {
                targetPos = targetPos.normalized * maxRadius;
            }

            _zoneBMarker.anchoredPosition = targetPos;
        }
    }

    public void RegisterLocalPlayer(
    Transform playerTransform,
    Camera playerCamera)
    {
        if (playerTransform == null)
        {
            Debug.LogError(
                "[MINIMAP] playerTransform이 null입니다."
            );

            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                $"[MINIMAP] playerCamera가 null입니다. " +
                $"player={playerTransform.name}"
            );

            return;
        }

        _localPlayerTransform =
            playerTransform;

        _localPlayerCamera =
            playerCamera;

        if (_hudPlayerMarker != null)
        {
            _hudPlayerMarker.anchoredPosition =
                Vector2.zero;

            _hudPlayerMarker.gameObject.SetActive(true);
        }

        if (_overlayPlayerMarker != null)
        {
            _overlayPlayerMarker.anchoredPosition =
                Vector2.zero;

            _overlayPlayerMarker.gameObject.SetActive(true);
        }

        if (_zoneAMarker != null)
            _zoneAMarker.gameObject.SetActive(true);

        if (_zoneBMarker != null)
            _zoneBMarker.gameObject.SetActive(true);

        Debug.Log(
            $"[MINIMAP] 로컬 플레이어 등록 완료. " +
            $"player={playerTransform.name}, " +
            $"playerCamera={playerCamera.name}, " +
            $"cameraEnabled={playerCamera.enabled}"
        );
    }

    private void UpdatePlayerMarkerRotation()
    {
        // 미니맵 카메라가 회전형일 때는 플레이어 마커는 언제나 중앙에서 정면(0도)을 바라봐야 해!
        if (_hudPlayerMarker != null) _hudPlayerMarker.localRotation = Quaternion.identity;
        if (_overlayPlayerMarker != null) _overlayPlayerMarker.localRotation = Quaternion.identity;
    }

    private void UpdateEnemyMarker(MiniMapEnemy enemy)
    {
        if (_localPlayerTransform == null) return;

        Vector3 positionDelta = enemy.transform.position - _localPlayerTransform.position;
        float cameraScale = _miniMapCamera.orthographic ? _miniMapCamera.orthographicSize : 30f;

        float cameraYaw = _miniMapCamera.transform.eulerAngles.y;
        Quaternion rotationOffset = Quaternion.Euler(0, 0, cameraYaw);

        if (!_hudEnemyMarkers.ContainsKey(enemy))
        {
            GameObject markerGo = Instantiate(_markerPrefab, _hudMarkerContainer);
            _hudEnemyMarkers[enemy] = markerGo.GetComponent<RectTransform>();
        }
        RectTransform hudMarker = _hudEnemyMarkers[enemy];
        hudMarker.gameObject.SetActive(true);

        Vector3 hudBase = new Vector3(
            (positionDelta.x / cameraScale) * (_miniMapHUD.sizeDelta.x * 0.5f),
            (positionDelta.z / cameraScale) * (_miniMapHUD.sizeDelta.y * 0.5f),
            0f
        );
        hudMarker.anchoredPosition = rotationOffset * hudBase;

        if (!_overlayEnemyMarkers.ContainsKey(enemy))
        {
            GameObject markerGo = Instantiate(_markerPrefab, _overlayMarkerContainer);
            _overlayEnemyMarkers[enemy] = markerGo.GetComponent<RectTransform>();
        }
        RectTransform overlayMarker = _overlayEnemyMarkers[enemy];
        overlayMarker.gameObject.SetActive(true);

        Vector3 overlayBase = new Vector3(
            (positionDelta.x / cameraScale) * (_miniMapOverlay.sizeDelta.x * 0.5f),
            (positionDelta.z / cameraScale) * (_miniMapOverlay.sizeDelta.y * 0.5f),
            0f
        );
        overlayMarker.anchoredPosition = rotationOffset * overlayBase;
    }

    private void HideEnemyMarker(MiniMapEnemy enemy)
    {
        if (_hudEnemyMarkers.TryGetValue(enemy, out RectTransform hudMarker)) hudMarker.gameObject.SetActive(false);
        if (_overlayEnemyMarkers.TryGetValue(enemy, out RectTransform overlayMarker)) overlayMarker.gameObject.SetActive(false);
    }

    public void RegisterEnemy(MiniMapEnemy enemy)
    {
        if (!_activeEnemies.Contains(enemy)) _activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(MiniMapEnemy enemy)
    {
        if (_activeEnemies.Contains(enemy)) _activeEnemies.Remove(enemy);

        if (_hudEnemyMarkers.TryGetValue(enemy, out RectTransform hudMarker))
        {
            if (hudMarker != null) Destroy(hudMarker.gameObject);
            _hudEnemyMarkers.Remove(enemy);
        }
        if (_overlayEnemyMarkers.TryGetValue(enemy, out RectTransform overlayMarker))
        {
            if (overlayMarker != null) Destroy(overlayMarker.gameObject);
            _overlayEnemyMarkers.Remove(enemy);
        }
    }

    public void ShowHUDMiniMap()
    {
        if (_miniMapHUD != null)
            _miniMapHUD.gameObject.SetActive(true);

        if (_miniMapOverlay != null)
            _miniMapOverlay.gameObject.SetActive(false);

        Debug.Log(
            "[MINIMAP] GAME_ALL_READY 이후 HUD 미니맵 활성화"
        );
    }

    public void HideAllMiniMaps()
    {
        if (_miniMapHUD != null)
            _miniMapHUD.gameObject.SetActive(false);

        if (_miniMapOverlay != null)
            _miniMapOverlay.gameObject.SetActive(false);
    }

    public void RefreshZoneTargets()
    {
        GameObject aPosition = null;
        GameObject bPosition = null;

        try
        {
            aPosition =
                GameObject.FindGameObjectWithTag("A Pos");

            bPosition =
                GameObject.FindGameObjectWithTag("B Pos");
        }
        catch (UnityException exception)
        {
            Debug.LogWarning(
                $"[MINIMAP] A Pos 또는 B Pos 태그가 없습니다. {exception.Message}"
            );

            return;
        }

        if (aPosition != null)
            _zoneATransform = aPosition.transform;

        if (bPosition != null)
            _zoneBTransform = bPosition.transform;

        Debug.Log(
            $"[MINIMAP ZONE] " +
            $"A={(_zoneATransform != null ? _zoneATransform.name : "NULL")}, " +
            $"B={(_zoneBTransform != null ? _zoneBTransform.name : "NULL")}"
        );
    }
}