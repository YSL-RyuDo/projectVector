using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MiniMapCamera : MonoBehaviour
{
    public static MiniMapCamera Instance;

    [Header("HUD 미니맵")]
    [SerializeField] private float cameraHeight = 50f;

    [Tooltip("0 이하이면 Camera의 현재 Orthographic Size를 사용합니다.")]
    [SerializeField] private float hudOrthographicSize = 0f;

    [Header("전체 맵 오버레이")]
    [Tooltip("맵 프리팹 안에 MiniMapFullCenter라는 빈 오브젝트를 만들어도 됩니다.")]
    [SerializeField] private Transform overlayMapCenter;

    [SerializeField]
    private string overlayCenterObjectName =
        "MiniMapFullCenter";

    [Tooltip("중앙 오브젝트를 찾지 못했을 때 사용할 위치")]
    [SerializeField]
    private Vector3 overlayCenterFallback =
        Vector3.zero;

    [SerializeField] private float overlayCameraHeight = 100f;

    [Tooltip("전체 맵이 들어오도록 설정")]
    [SerializeField] private float overlayOrthographicSize = 70f;

    [Tooltip("전체 맵의 북쪽 방향 보정")]
    [SerializeField] private float overlayYaw = 0f;
    [SerializeField] private float attackerOverlayYaw = 90f;
    [SerializeField] private float defenderOverlayYaw = -90f;

    private byte currentSpawnTeam; // 0 = 공격, 1 = 수비

    private Transform targetPlayer;
    private Camera miniMapCamera;

    private bool isOverlayMode;
    private float? smokeHeading;

    public bool IsOverlayMode => isOverlayMode;
    public Camera Camera => miniMapCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        miniMapCamera =
            GetComponent<Camera>();

        gameObject.tag = "Untagged";

        miniMapCamera.orthographic = true;

        if (hudOrthographicSize <= 0f)
        {
            hudOrthographicSize =
                miniMapCamera.orthographicSize;
        }

        if (miniMapCamera.targetTexture == null)
        {
            Debug.LogError(
                "[MINIMAP CAMERA] Output Texture가 연결되지 않았습니다."
            );
        }

        AudioListener listener =
            GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogError(
                "[MINIMAP CAMERA] target이 null입니다."
            );

            return;
        }

        targetPlayer = target;

        Debug.Log(
            $"[MINIMAP CAMERA] 타겟 설정 완료: {target.name}"
        );
    }

    public void SetOverlayMode(bool value)
    {
        isOverlayMode = value;
        if (!value) smokeHeading = null;

        if (isOverlayMode)
            ResolveOverlayCenter();

        ApplyCameraTransform();

        Debug.Log(
            $"[MINIMAP CAMERA] overlayMode={isOverlayMode}"
        );
    }

    // Snapshot the view heading when casting, without changing the ordinary map.
    public void SetSmokeHeading(float yaw)
    {
        smokeHeading = Mathf.Repeat(yaw, 360f);
        ApplyCameraTransform();
    }

    public void ClearSmokeHeading()
    {
        smokeHeading = null;
        ApplyCameraTransform();
    }

    private void ResolveOverlayCenter()
    {
        if (overlayMapCenter != null)
            return;

        if (string.IsNullOrEmpty(overlayCenterObjectName))
            return;

        GameObject centerObject =
            GameObject.Find(overlayCenterObjectName);

        if (centerObject != null)
        {
            overlayMapCenter =
                centerObject.transform;

            Debug.Log(
                $"[MINIMAP CAMERA] 전체 맵 중앙 발견: " +
                $"{overlayMapCenter.name}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[MINIMAP CAMERA] {overlayCenterObjectName}을 찾지 못해 " +
                $"Fallback 위치를 사용합니다. pos={overlayCenterFallback}"
            );
        }
    }

    private void LateUpdate()
    {
        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        if (miniMapCamera == null)
            return;

        if (isOverlayMode)
        {
            Vector3 centerPosition =
                overlayMapCenter != null
                    ? overlayMapCenter.position
                    : overlayCenterFallback;

            transform.position =
                new Vector3(
                    centerPosition.x,
                    centerPosition.y + overlayCameraHeight,
                    centerPosition.z
                );

            float roleYaw =
                currentSpawnTeam == 0
                    ? attackerOverlayYaw
                    : defenderOverlayYaw;

            transform.rotation =
                Quaternion.Euler(
                    90f,
                    smokeHeading ?? (overlayYaw + roleYaw),
                    0f
                );

            miniMapCamera.orthographicSize =
                overlayOrthographicSize;

            if (smokeHeading.HasValue)
            {
                // Fit the original map rectangle after arbitrary rotation. Otherwise
                // diagonal casts can crop corners that were visible in the normal map.
                float delta = (smokeHeading.Value - overlayYaw - roleYaw) * Mathf.Deg2Rad;
                float c = Mathf.Abs(Mathf.Cos(delta)), s = Mathf.Abs(Mathf.Sin(delta));
                float aspect = Mathf.Max(.01f, miniMapCamera.aspect);
                miniMapCamera.orthographicSize *= Mathf.Max(c + s * aspect, c + s / aspect);
            }

            return;
        }

        if (targetPlayer == null)
            return;

        transform.position =
            new Vector3(
                targetPlayer.position.x,
                targetPlayer.position.y + cameraHeight,
                targetPlayer.position.z
            );

        transform.rotation =
            Quaternion.Euler(
                90f,
                targetPlayer.eulerAngles.y,
                0f
            );

        miniMapCamera.orthographicSize =
            hudOrthographicSize;
    }

    public void SetOverlayMapCenter(
    Transform mapCenter)
    {
        if (mapCenter == null)
        {
            Debug.LogError(
                "[MINIMAP CAMERA] 전체 맵 중앙이 null입니다."
            );

            return;
        }

        overlayMapCenter = mapCenter;

        Debug.Log(
            $"[MINIMAP CAMERA] 전체 맵 중앙 등록: " +
            $"{overlayMapCenter.name}, " +
            $"position={overlayMapCenter.position}"
        );
    }

    public void SetLocalRoundRole(byte spawnTeam)
    {
        currentSpawnTeam = spawnTeam;

        ApplyCameraTransform();

        Debug.Log(
            $"[MINIMAP ROLE] spawnTeam={spawnTeam}, " +
            $"overlayYaw={(spawnTeam == 0 ? attackerOverlayYaw : defenderOverlayYaw)}"
        );
    }

    public void SetMapFraming(float hudSize, float fullSize, float hudHeight, float fullHeight)
    {
        hudOrthographicSize = Mathf.Max(1f, hudSize);
        overlayOrthographicSize = Mathf.Max(1f, fullSize);
        cameraHeight = Mathf.Max(1f, hudHeight);
        overlayCameraHeight = Mathf.Max(1f, fullHeight);
        ApplyCameraTransform();
    }
}
