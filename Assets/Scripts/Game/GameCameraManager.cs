using UnityEngine;

public class GameCameraManager : MonoBehaviour
{
    public static GameCameraManager Instance;

    [Header("Scene Camera")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private AudioListener sceneAudioListener;

    private void Awake()
    {
        Instance = this;

        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneAudioListener == null && sceneCamera != null)
            sceneAudioListener = sceneCamera.GetComponent<AudioListener>();
    }

    public void EnterGameplayCamera(PlayerCameraController localPlayerCamera)
    {
        if (sceneCamera != null)
        {
            sceneCamera.enabled = false;
            sceneCamera.tag = "Untagged";
        }

        if (sceneAudioListener != null)
            sceneAudioListener.enabled = false;

        if (localPlayerCamera != null)
            localPlayerCamera.SetLocalPlayer(true);
    }
}