using System.Collections.Generic;
using UnityEngine;

public class MiniMapManager : MonoBehaviour
{
    public static MiniMapManager Instance;

    [Header("[HUD 미니맵 설정]")]
    [SerializeField] private RectTransform _miniMapHUD;
    [SerializeField] private float _hudWidth = 180f;
    [SerializeField] private float _hudHeight = 180f;
    [SerializeField] private RectTransform _hudMarkerContainer; // HUD 마스크 내부 부모
    [SerializeField] private RectTransform _hudPlayerMarker;    // HUD의 내 마커

    [Header("[오버레이 미니맵 설정]")]
    [SerializeField] private GameObject _overlayMiniMap;
    [SerializeField] private RectTransform _miniMapOverlay;
    [SerializeField] private float _overlayWidth = 600f;       // 크게 띄울 오버레이 가로 크기
    [SerializeField] private float _overlayHeight = 600f;      // 크게 띄울 오버레이 세로 크기
    [SerializeField] private RectTransform _overlayMarkerContainer; // 오버레이 마스크 내부 부모
    [SerializeField] private RectTransform _overlayPlayerMarker;    // 오버레이의 내 마커

    [Header("공용 마커 설정")]
    [SerializeField] private Camera _miniMapCamera;
    [SerializeField] private GameObject _markerPrefab;

    private Transform _localPlayerTransform;
    private Camera _localPlayerCamera;

    private List<MiniMapEnemy> _activeEnemies = new List<MiniMapEnemy>();

    // HUD 마커와 오버레이 마커를 각각 따로 관리할 딕셔너리
    private Dictionary<MiniMapEnemy, RectTransform> _hudEnemyMarkers = new Dictionary<MiniMapEnemy, RectTransform>();
    private Dictionary<MiniMapEnemy, RectTransform> _overlayEnemyMarkers = new Dictionary<MiniMapEnemy, RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // HUD 미니맵 크기 설정
        if (_miniMapHUD != null) _miniMapHUD.sizeDelta = new Vector2(_hudWidth, _hudHeight);
        if (_hudPlayerMarker != null) _hudPlayerMarker.anchoredPosition = Vector2.zero;

        // 오버레이 미니맵 크기 설정
        if (_miniMapOverlay != null) _miniMapOverlay.sizeDelta = new Vector2(_overlayWidth, _overlayHeight);
        if (_overlayPlayerMarker != null) _overlayPlayerMarker.anchoredPosition = Vector2.zero;

        if (_miniMapCamera == null && MiniMapCamera.Instance != null)
        {
            _miniMapCamera = MiniMapCamera.Instance.GetComponent<Camera>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            var uiAnimator = _overlayMiniMap.GetComponent<UIAnimator>();

            if (_overlayMiniMap.gameObject.activeSelf)
            {
                uiAnimator.Close();
            }
            else
            {
                uiAnimator.Show();
            }
        }
    }

    private void LateUpdate()
    {
        UpdatePlayerMarkerRotation();

        if (_miniMapCamera == null || _localPlayerCamera == null) return;

        // 로컬 플레이어 카메라의 시야 평면 계산
        Plane[] mainCamPlanes = GeometryUtility.CalculateFrustumPlanes(_localPlayerCamera);

        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null || enemy.EnemyCollider == null) continue;

            bool isVisibleInMainCam = GeometryUtility.TestPlanesAABB(mainCamPlanes, enemy.EnemyCollider.bounds);

            if (isVisibleInMainCam)
            {
                // 시야에 보이면 HUD와 오버레이 둘 다 마커 표시/업데이트
                UpdateEnemyMarker(enemy);
            }
            else
            {
                // 시야에서 벗어나면 둘 다 숨기기
                HideEnemyMarker(enemy);
            }
        }
    }

    public void RegisterLocalPlayer(Transform playerTransform)
    {
        _localPlayerTransform = playerTransform;
        _localPlayerCamera = playerTransform.GetComponentInChildren<Camera>(true);

        if (_localPlayerCamera == null)
        {
            Debug.LogError($"[MiniMapManager] {playerTransform.name}의 자식에서 Camera를 찾을 수 없습니다!");
        }

        if (_hudPlayerMarker != null) _hudPlayerMarker.gameObject.SetActive(true);
        if (_overlayPlayerMarker != null) _overlayPlayerMarker.gameObject.SetActive(true);
    }

    private void UpdatePlayerMarkerRotation()
    {
        if (_localPlayerTransform == null) return;

        float playerYaw = _localPlayerTransform.eulerAngles.y;

        // HUD와 오버레이 각각의 내 마커 회전 동기화
        if (_hudPlayerMarker != null) _hudPlayerMarker.localRotation = Quaternion.Euler(0, 0, -playerYaw);
        if (_overlayPlayerMarker != null) _overlayPlayerMarker.localRotation = Quaternion.Euler(0, 0, -playerYaw);
    }

    private void UpdateEnemyMarker(MiniMapEnemy enemy)
    {
        if (_localPlayerTransform == null) return;

        // 1. 공통 월드 거리 차이 및 카메라 배율 계산
        Vector3 positionDelta = enemy.transform.position - _localPlayerTransform.position;
        float cameraScale = 30f;
        if (_miniMapCamera.orthographic)
        {
            cameraScale = _miniMapCamera.orthographicSize;
        }

        // 2. HUD 미니맵 마커 생성 및 위치 업데이트
        if (!_hudEnemyMarkers.ContainsKey(enemy))
        {
            GameObject markerGo = Instantiate(_markerPrefab, _hudMarkerContainer);
            _hudEnemyMarkers[enemy] = markerGo.GetComponent<RectTransform>();
        }
        RectTransform hudMarker = _hudEnemyMarkers[enemy];
        hudMarker.gameObject.SetActive(true);
        float hudX = (positionDelta.x / cameraScale) * (_miniMapHUD.sizeDelta.x * 0.5f);
        float hudY = (positionDelta.z / cameraScale) * (_miniMapHUD.sizeDelta.y * 0.5f);
        hudMarker.anchoredPosition = new Vector2(hudX, hudY);

        // 3. 오버레이 미니맵 마커 생성 및 위치 업데이트
        if (!_overlayEnemyMarkers.ContainsKey(enemy))
        {
            GameObject markerGo = Instantiate(_markerPrefab, _overlayMarkerContainer);
            _overlayEnemyMarkers[enemy] = markerGo.GetComponent<RectTransform>();
        }
        RectTransform overlayMarker = _overlayEnemyMarkers[enemy];
        overlayMarker.gameObject.SetActive(true);
        float overlayX = (positionDelta.x / cameraScale) * (_miniMapOverlay.sizeDelta.x * 0.5f);
        float overlayY = (positionDelta.z / cameraScale) * (_miniMapOverlay.sizeDelta.y * 0.5f);
        overlayMarker.anchoredPosition = new Vector2(overlayX, overlayY);
    }

    private void HideEnemyMarker(MiniMapEnemy enemy)
    {
        if (_hudEnemyMarkers.TryGetValue(enemy, out RectTransform hudMarker))
        {
            hudMarker.gameObject.SetActive(false);
        }
        if (_overlayEnemyMarkers.TryGetValue(enemy, out RectTransform overlayMarker))
        {
            overlayMarker.gameObject.SetActive(false);
        }
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
}