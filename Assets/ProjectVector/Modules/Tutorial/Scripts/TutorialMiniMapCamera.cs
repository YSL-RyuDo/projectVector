using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TutorialMiniMapCamera : MonoBehaviour
{
    [SerializeField] private float cameraHeight = 50f;
    [SerializeField] private float orthographicSize = 20f;

    private Transform targetPlayer;
    private Camera miniMapCamera;

    public Camera Camera => miniMapCamera;

    private void Awake()
    {
        miniMapCamera = GetComponent<Camera>();

        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = orthographicSize;

        AudioListener listener = GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = false;
    }

    public void SetTarget(Transform target)
    {
        targetPlayer = target;
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