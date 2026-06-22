using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MiniMapManager : MonoBehaviour
{
    public static MiniMapManager Instance;

    [Header("[HUD 미니맵 설정]")]
    [SerializeField] private RectTransform _miniMapHUD;
    [SerializeField] private float _hudWidth = 180f;
    [SerializeField] private float _hudHeight = 180f;

    [SerializeField] private RectTransform _hudMarkerContainer;
    [SerializeField] private RectTransform _hudPlayerMarker;
    [SerializeField] private RectTransform _hudTopLayer;

    [Header("[오버레이 미니맵 설정]")]
    [SerializeField] private RectTransform _miniMapOverlay;
    [SerializeField] private float _overlayWidth = 600f;
    [SerializeField] private float _overlayHeight = 600f;

    [SerializeField] private RectTransform _overlayMarkerContainer;
    [SerializeField] private RectTransform _overlayPlayerMarker;
    [SerializeField] private RectTransform _overlayTopLayer;

    [Header("플레이어 마커")]
    [Tooltip("Image 컴포넌트가 있는 마커 프리팹")]
    [SerializeField] private GameObject _markerPrefab;

    [Tooltip("CharacterId 순서대로 캐릭터 이미지를 넣습니다.")]
    [SerializeField] private Sprite[] _characterSprites;

    [SerializeField]
    private Color _allyOutlineColor =
        new Color(0.2f, 0.7f, 1f, 1f);

    [SerializeField]
    private Color _enemyOutlineColor =
        new Color(1f, 0.25f, 0.25f, 1f);

    [SerializeField] private float _hudMarkerEdgePadding = 18f;

    [Header("적 시야 판정")]
    [Tooltip("플레이어와 벽이 포함된 레이어를 모두 선택합니다.")]
    [SerializeField] private LayerMask _enemyVisibilityMask = ~0;

    [Header("미니맵 카메라")]
    [SerializeField] private Camera _miniMapCamera;

    [Header("A / B 구역")]
    [SerializeField] private GameObject _zoneAMarkerPrefab;
    [SerializeField] private GameObject _zoneBMarkerPrefab;

    [SerializeField] private RectTransform _zoneAMarker;
    [SerializeField] private RectTransform _zoneBMarker;

    [SerializeField] private RectTransform _overlayZoneAMarker;
    [SerializeField] private RectTransform _overlayZoneBMarker;

    private Transform _zoneATransform;
    private Transform _zoneBTransform;

    private Transform _localPlayerTransform;
    private PlayerIdentity _localPlayerIdentity;
    private Camera _localPlayerCamera;

    private bool _isOverlayOpen;

    private readonly List<MiniMapEnemy> _registeredPlayers =
        new List<MiniMapEnemy>();

    private readonly Dictionary<MiniMapEnemy, RectTransform>
        _hudPlayerMarkers =
            new Dictionary<MiniMapEnemy, RectTransform>();

    private readonly Dictionary<MiniMapEnemy, RectTransform>
        _overlayPlayerMarkers =
            new Dictionary<MiniMapEnemy, RectTransform>();

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideAllMiniMaps();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        if (_miniMapOverlay != null)
        {
            _miniMapOverlay.sizeDelta =
                new Vector2(
                    _overlayWidth,
                    _overlayHeight
                );
        }

        if (_hudPlayerMarker != null)
        {
            _hudPlayerMarker.anchoredPosition =
                Vector2.zero;

            _hudPlayerMarker.gameObject.SetActive(false);
        }

        if (_overlayPlayerMarker != null)
        {
            _overlayPlayerMarker.gameObject.SetActive(false);
        }

        RefreshMiniMapCamera();
        InitZoneMarkers();
        RefreshZoneTargets();

        HideAllMiniMaps();
    }

    private void Update()
    {
        if (_localPlayerTransform == null)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMiniMap();
        }
    }

    private void LateUpdate()
    {
        RefreshMiniMapCamera();

        if (_miniMapCamera == null)
            return;

        if (_localPlayerTransform == null ||
            _localPlayerIdentity == null)
        {
            return;
        }

        if (_zoneATransform == null ||
            _zoneBTransform == null)
        {
            RefreshZoneTargets();
        }

        UpdateLocalPlayerMarkers();
        UpdateZoneMarkerPositions();
        UpdateRegisteredPlayerMarkers();
    }

    private void RefreshMiniMapCamera()
    {
        if (_miniMapCamera != null)
            return;

        if (MiniMapCamera.Instance == null)
            return;

        _miniMapCamera =
            MiniMapCamera.Instance.Camera;

        if (_miniMapCamera == null)
        {
            _miniMapCamera =
                MiniMapCamera.Instance.GetComponent<Camera>();
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
                "[MINIMAP] playerCamera가 null입니다."
            );

            return;
        }

        _localPlayerTransform =
            playerTransform;

        _localPlayerCamera =
            playerCamera;

        _localPlayerIdentity =
            playerTransform.GetComponent<PlayerIdentity>();

        if (_localPlayerIdentity == null)
        {
            _localPlayerIdentity =
                playerTransform.GetComponentInChildren<PlayerIdentity>(true);
        }

        if (_localPlayerIdentity == null)
        {
            Debug.LogError(
                "[MINIMAP] 로컬 PlayerIdentity가 없습니다."
            );

            return;
        }

        ApplyCharacterVisual(
            _hudPlayerMarker,
            _localPlayerIdentity.CharacterId,
            true
        );

        ApplyCharacterVisual(
            _overlayPlayerMarker,
            _localPlayerIdentity.CharacterId,
            true
        );

        if (_hudPlayerMarker != null)
        {
            _hudPlayerMarker.anchoredPosition =
                Vector2.zero;

            _hudPlayerMarker.gameObject.SetActive(true);
        }

        if (_overlayPlayerMarker != null)
        {
            _overlayPlayerMarker.gameObject.SetActive(true);
        }

        SetZoneMarkerActive(true);

        Debug.Log(
            $"[MINIMAP] 로컬 등록 완료. " +
            $"playerId={_localPlayerIdentity.PlayerId}, " +
            $"team={_localPlayerIdentity.Team}, " +
            $"characterId={_localPlayerIdentity.CharacterId}"
        );
    }

    public void RegisterPlayer(MiniMapEnemy player)
    {
        if (player == null)
            return;

        if (_registeredPlayers.Contains(player))
            return;

        _registeredPlayers.Add(player);

        Debug.Log(
            $"[MINIMAP] 플레이어 등록. object={player.name}"
        );
    }

    public void UnregisterPlayer(MiniMapEnemy player)
    {
        if (player == null)
            return;

        _registeredPlayers.Remove(player);

        DestroyMarker(
            _hudPlayerMarkers,
            player
        );

        DestroyMarker(
            _overlayPlayerMarkers,
            player
        );
    }

    // 기존 코드와의 호환용
    public void RegisterEnemy(MiniMapEnemy enemy)
    {
        RegisterPlayer(enemy);
    }

    public void UnregisterEnemy(MiniMapEnemy enemy)
    {
        UnregisterPlayer(enemy);
    }

    private void UpdateLocalPlayerMarkers()
    {
        if (_hudPlayerMarker != null)
        {
            _hudPlayerMarker.gameObject.SetActive(true);
            _hudPlayerMarker.anchoredPosition = Vector2.zero;

            // HUD 맵이 플레이어 방향에 맞춰 회전하므로
            // 로컬 마커는 항상 위쪽을 바라본다.
            _hudPlayerMarker.localRotation =
                Quaternion.identity;
        }

        if (_overlayPlayerMarker != null)
        {
            SetMarkerWorldPosition(
                _overlayPlayerMarker,
                _localPlayerTransform.position,
                false
            );

            float mapYaw =
                _miniMapCamera.transform.eulerAngles.y;

            float playerYaw =
                _localPlayerTransform.eulerAngles.y;

            _overlayPlayerMarker.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    mapYaw - playerYaw
                );
        }
    }

    private void UpdateRegisteredPlayerMarkers()
    {
        Plane[] cameraPlanes = null;

        if (_localPlayerCamera != null)
        {
            cameraPlanes =
                GeometryUtility.CalculateFrustumPlanes(
                    _localPlayerCamera
                );
        }

        for (int i = _registeredPlayers.Count - 1;
             i >= 0;
             i--)
        {
            MiniMapEnemy player =
                _registeredPlayers[i];

            if (player == null)
            {
                _registeredPlayers.RemoveAt(i);
                continue;
            }

            PlayerIdentity identity =
                player.Identity;

            if (identity == null)
            {
                HidePlayerMarkers(player);
                continue;
            }

            if (identity.PlayerId ==
                _localPlayerIdentity.PlayerId)
            {
                HidePlayerMarkers(player);
                continue;
            }

            if (player.IsDead)
            {
                HidePlayerMarkers(player);
                continue;
            }

            bool isAlly =
                identity.Team ==
                _localPlayerIdentity.Team;

            if (!isAlly)
            {
                bool enemyVisible =
                    IsEnemyVisible(
                        player,
                        cameraPlanes
                    );

                if (!enemyVisible)
                {
                    HidePlayerMarkers(player);
                    continue;
                }
            }

            UpdatePlayerMarker(
                player,
                isAlly
            );
        }
    }

    private void UpdatePlayerMarker(
        MiniMapEnemy player,
        bool isAlly)
    {
        RectTransform hudMarker =
            GetOrCreateMarker(
                _hudPlayerMarkers,
                player,
                _hudMarkerContainer,
                isAlly
            );

        RectTransform overlayMarker =
            GetOrCreateMarker(
                _overlayPlayerMarkers,
                player,
                _overlayMarkerContainer,
                isAlly
            );

        int characterId =
            player.Identity != null
                ? player.Identity.CharacterId
                : -1;

        ApplyCharacterVisual(
            hudMarker,
            characterId,
            isAlly
        );

        ApplyCharacterVisual(
            overlayMarker,
            characterId,
            isAlly
        );

        /*
         * HUD:
         * 기존대로 로컬 플레이어 중심이다.
         * 아군이 HUD 범위 밖이면 가장자리로 고정한다.
         */
        if (hudMarker != null)
        {
            SetMarkerWorldPosition(
                hudMarker,
                player.MarkerWorldPosition,
                true
            );
        }

        /*
         * 오버레이:
         * 전체 맵 카메라 기준 실제 위치에 표시한다.
         */
        if (overlayMarker != null)
        {
            SetMarkerWorldPosition(
                overlayMarker,
                player.MarkerWorldPosition,
                false
            );
        }
    }

    private RectTransform GetOrCreateMarker(
        Dictionary<MiniMapEnemy, RectTransform> dictionary,
        MiniMapEnemy player,
        RectTransform parent,
        bool isAlly)
    {
        if (dictionary.TryGetValue(
            player,
            out RectTransform existing))
        {
            if (existing != null)
                return existing;

            dictionary.Remove(player);
        }

        if (_markerPrefab == null ||
            parent == null)
        {
            return null;
        }

        GameObject markerObject =
            Instantiate(
                _markerPrefab,
                parent
            );

        RectTransform marker =
            markerObject.GetComponent<RectTransform>();

        if (marker == null)
        {
            Debug.LogError(
                "[MINIMAP] MarkerPrefab에 RectTransform이 없습니다."
            );

            Destroy(markerObject);
            return null;
        }

        marker.localScale =
            Vector3.one;

        marker.localRotation =
            Quaternion.identity;

        dictionary[player] =
            marker;

        return marker;
    }

    private void ApplyCharacterVisual(
        RectTransform marker,
        int characterId,
        bool isAlly)
    {
        if (marker == null)
            return;

        Image image =
            marker.GetComponent<Image>();

        if (image == null)
        {
            image =
                marker.GetComponentInChildren<Image>(true);
        }

        if (image == null)
        {
            Debug.LogWarning(
                $"[MINIMAP MARKER] Image가 없습니다. marker={marker.name}"
            );

            return;
        }

        if (_characterSprites != null &&
            characterId >= 0 &&
            characterId < _characterSprites.Length &&
            _characterSprites[characterId] != null)
        {
            image.sprite =
                _characterSprites[characterId];

            image.preserveAspect = true;
            image.color = Color.white;
        }

        UnityEngine.UI.Outline outline =
            image.GetComponent<UnityEngine.UI.Outline>();

        if (outline == null)
        {
            outline =
                image.gameObject.AddComponent<UnityEngine.UI.Outline>();
        }

        outline.effectColor =
            isAlly
                ? _allyOutlineColor
                : _enemyOutlineColor;

        outline.effectDistance =
            new Vector2(2f, -2f);
    }

    private void SetMarkerWorldPosition(
        RectTransform marker,
        Vector3 worldPosition,
        bool clampToHudEdge)
    {
        if (marker == null ||
            _miniMapCamera == null)
        {
            return;
        }

        RectTransform parent =
            marker.parent as RectTransform;

        if (parent == null)
            return;

        Vector3 viewportPosition =
            _miniMapCamera.WorldToViewportPoint(
                worldPosition
            );

        if (viewportPosition.z <= 0f)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        Vector2 markerPosition =
            new Vector2(
                (viewportPosition.x - 0.5f) *
                parent.rect.width,

                (viewportPosition.y - 0.5f) *
                parent.rect.height
            );

        bool insideViewport =
            viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f;

        if (clampToHudEdge)
        {
            float maxRadius =
                Mathf.Min(
                    parent.rect.width,
                    parent.rect.height
                ) * 0.5f -
                _hudMarkerEdgePadding;

            if (markerPosition.magnitude >
                maxRadius)
            {
                markerPosition =
                    markerPosition.normalized *
                    maxRadius;
            }

            marker.gameObject.SetActive(true);
        }
        else
        {
            marker.gameObject.SetActive(
                insideViewport
            );

            if (!insideViewport)
                return;
        }

        marker.anchoredPosition =
            markerPosition;
    }

    private bool IsEnemyVisible(
        MiniMapEnemy enemy,
        Plane[] cameraPlanes)
    {
        if (_localPlayerCamera == null)
            return false;

        if (enemy == null ||
            enemy.EnemyCollider == null)
        {
            return false;
        }

        if (cameraPlanes == null)
            return false;

        bool insideCamera =
            GeometryUtility.TestPlanesAABB(
                cameraPlanes,
                enemy.EnemyCollider.bounds
            );

        if (!insideCamera)
            return false;

        Vector3 targetPosition =
            enemy.MarkerWorldPosition;

        Vector3 viewport =
            _localPlayerCamera.WorldToViewportPoint(
                targetPosition
            );

        if (viewport.z <= 0f ||
            viewport.x < 0f ||
            viewport.x > 1f ||
            viewport.y < 0f ||
            viewport.y > 1f)
        {
            return false;
        }

        Vector3 origin =
            _localPlayerCamera.transform.position;

        Vector3 direction =
            targetPosition - origin;

        float distance =
            direction.magnitude;

        if (distance <= 0.01f)
            return true;

        direction.Normalize();

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                direction,
                distance + 0.1f,
                _enemyVisibilityMask,
                QueryTriggerInteraction.Ignore
            );

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        Transform enemyRoot =
            enemy.Identity != null
                ? enemy.Identity.transform
                : enemy.transform;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Transform hitTransform =
                hits[i].collider.transform;

            // 자기 자신의 Collider는 무시
            if (IsSameHierarchy(
                hitTransform,
                _localPlayerTransform))
            {
                continue;
            }

            // 가장 먼저 맞은 것이 적이면 실제로 보이는 상태
            if (IsSameHierarchy(
                hitTransform,
                enemyRoot))
            {
                return true;
            }

            // 벽이나 다른 오브젝트가 먼저 맞으면 가려진 상태
            return false;
        }

        // Raycast에 아무것도 안 맞았으면 중간 장애물이 없는 상태
        return true;
    }

    private bool IsSameHierarchy(
        Transform target,
        Transform root)
    {
        if (target == null ||
            root == null)
        {
            return false;
        }

        return
            target == root ||
            target.IsChildOf(root) ||
            root.IsChildOf(target);
    }

    private void HidePlayerMarkers(
        MiniMapEnemy player)
    {
        if (_hudPlayerMarkers.TryGetValue(
            player,
            out RectTransform hudMarker))
        {
            if (hudMarker != null)
                hudMarker.gameObject.SetActive(false);
        }

        if (_overlayPlayerMarkers.TryGetValue(
            player,
            out RectTransform overlayMarker))
        {
            if (overlayMarker != null)
                overlayMarker.gameObject.SetActive(false);
        }
    }

    private void DestroyMarker(
        Dictionary<MiniMapEnemy, RectTransform> dictionary,
        MiniMapEnemy player)
    {
        if (!dictionary.TryGetValue(
            player,
            out RectTransform marker))
        {
            return;
        }

        if (marker != null)
            Destroy(marker.gameObject);

        dictionary.Remove(player);
    }

    private void InitZoneMarkers()
    {
        RectTransform hudParent =
            _hudTopLayer != null
                ? _hudTopLayer
                : _hudMarkerContainer;

        RectTransform overlayParent =
            _overlayTopLayer != null
                ? _overlayTopLayer
                : _overlayMarkerContainer;

        if (_zoneAMarker == null)
        {
            _zoneAMarker =
                CreateZoneMarker(
                    _zoneAMarkerPrefab,
                    hudParent
                );
        }

        if (_zoneBMarker == null)
        {
            _zoneBMarker =
                CreateZoneMarker(
                    _zoneBMarkerPrefab,
                    hudParent
                );
        }

        if (_overlayZoneAMarker == null)
        {
            _overlayZoneAMarker =
                CreateZoneMarker(
                    _zoneAMarkerPrefab,
                    overlayParent
                );
        }

        if (_overlayZoneBMarker == null)
        {
            _overlayZoneBMarker =
                CreateZoneMarker(
                    _zoneBMarkerPrefab,
                    overlayParent
                );
        }
    }

    private RectTransform CreateZoneMarker(
        GameObject prefab,
        RectTransform parent)
    {
        if (prefab == null ||
            parent == null)
        {
            return null;
        }

        GameObject marker =
            Instantiate(
                prefab,
                parent
            );

        RectTransform rect =
            marker.GetComponent<RectTransform>();

        if (rect == null)
        {
            Destroy(marker);
            return null;
        }

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.anchoredPosition =
            Vector2.zero;

        return rect;
    }

    private void UpdateZoneMarkerPositions()
    {
        if (_zoneATransform != null)
        {
            SetMarkerWorldPosition(
                _zoneAMarker,
                _zoneATransform.position,
                true
            );

            SetMarkerWorldPosition(
                _overlayZoneAMarker,
                _zoneATransform.position,
                false
            );
        }

        if (_zoneBTransform != null)
        {
            SetMarkerWorldPosition(
                _zoneBMarker,
                _zoneBTransform.position,
                true
            );

            SetMarkerWorldPosition(
                _overlayZoneBMarker,
                _zoneBTransform.position,
                false
            );
        }
    }

    private void SetZoneMarkerActive(bool value)
    {
        if (_zoneAMarker != null)
            _zoneAMarker.gameObject.SetActive(value);

        if (_zoneBMarker != null)
            _zoneBMarker.gameObject.SetActive(value);

        if (_overlayZoneAMarker != null)
            _overlayZoneAMarker.gameObject.SetActive(value);

        if (_overlayZoneBMarker != null)
            _overlayZoneBMarker.gameObject.SetActive(value);
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
                $"[MINIMAP] A Pos 또는 B Pos 태그가 없습니다. " +
                $"{exception.Message}"
            );

            return;
        }

        if (aPosition != null)
            _zoneATransform = aPosition.transform;

        if (bPosition != null)
            _zoneBTransform = bPosition.transform;
    }

    public void ToggleMiniMap()
    {
        if (_miniMapHUD == null ||
            _miniMapOverlay == null)
        {
            Debug.LogError(
                "[MINIMAP] HUD 또는 Overlay가 연결되지 않았습니다."
            );

            return;
        }

        if (_isOverlayOpen)
            ShowHUDMiniMap();
        else
            ShowOverlayMiniMap();
    }

    public void ShowHUDMiniMap()
    {
        _isOverlayOpen = false;

        if (MiniMapCamera.Instance != null)
        {
            MiniMapCamera.Instance.SetOverlayMode(false);
        }

        if (_miniMapHUD != null)
            _miniMapHUD.gameObject.SetActive(true);

        if (_miniMapOverlay != null)
            _miniMapOverlay.gameObject.SetActive(false);

        Debug.Log(
            "[MINIMAP] HUD 미니맵 활성화"
        );
    }

    public void ShowOverlayMiniMap()
    {
        _isOverlayOpen = true;

        if (MiniMapCamera.Instance != null)
        {
            MiniMapCamera.Instance.SetOverlayMode(true);
        }

        if (_miniMapHUD != null)
            _miniMapHUD.gameObject.SetActive(false);

        if (_miniMapOverlay != null)
        {
            _miniMapOverlay.gameObject.SetActive(true);
            _miniMapOverlay.SetAsLastSibling();
        }

        Debug.Log(
            "[MINIMAP] 전체 맵 오버레이 활성화"
        );
    }

    public void HideAllMiniMaps()
    {
        _isOverlayOpen = false;

        if (MiniMapCamera.Instance != null)
        {
            MiniMapCamera.Instance.SetOverlayMode(false);
        }

        if (_miniMapHUD != null)
            _miniMapHUD.gameObject.SetActive(false);

        if (_miniMapOverlay != null)
            _miniMapOverlay.gameObject.SetActive(false);
    }

    public void SetMapWorldReferences(
    Transform zoneA,
    Transform zoneB)
    {
        _zoneATransform = zoneA;
        _zoneBTransform = zoneB;

        if (_zoneAMarker != null)
            _zoneAMarker.gameObject.SetActive(zoneA != null);

        if (_zoneBMarker != null)
            _zoneBMarker.gameObject.SetActive(zoneB != null);

        if (_overlayZoneAMarker != null)
            _overlayZoneAMarker.gameObject.SetActive(zoneA != null);

        if (_overlayZoneBMarker != null)
            _overlayZoneBMarker.gameObject.SetActive(zoneB != null);

        Debug.Log(
            $"[MINIMAP WORLD REFERENCES] " +
            $"A={(_zoneATransform != null ? _zoneATransform.name : "NULL")}, " +
            $"B={(_zoneBTransform != null ? _zoneBTransform.name : "NULL")}"
        );
    }
}