using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MiniMapCamera : MonoBehaviour
{
    public static MiniMapCamera Instance;

    [Header("Settings")]
    [SerializeField] private float cameraHeight = 50f;

    private Transform targetPlayer;
    private Camera miniMapCamera;

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

        // 미니맵 카메라는 MainCamera가 아니다.
        gameObject.tag = "Untagged";

        if (miniMapCamera != null)
        {
            miniMapCamera.orthographic = true;

            if (miniMapCamera.targetTexture == null)
            {
                Debug.LogError(
                    "[MINIMAP CAMERA] Output Texture가 연결되지 않았습니다."
                );
            }
        }

        AudioListener listener =
            GetComponent<AudioListener>();

        if (listener != null)
        {
            listener.enabled = false;
        }
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

    private void LateUpdate()
    {
        if (targetPlayer == null)
            return;

        transform.position = new Vector3(
            targetPlayer.position.x,
            targetPlayer.position.y + cameraHeight,
            targetPlayer.position.z
        );

        transform.rotation = Quaternion.Euler(
            90f,
            targetPlayer.eulerAngles.y,
            0f
        );
    }
}