using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputSender : MonoBehaviour
{
    FPSCameraLook cam;
    float lastYaw;

    void Start()
    {
        cam = GetComponentInChildren<FPSCameraLook>();
        lastYaw = cam.GetYaw();
    }

    void Update()
    {
        float yaw = cam.GetYaw();

        if (Mathf.Abs(yaw - lastYaw) > 0.1f)
        {
            NetworkManager.Instance.SendRotate(yaw);
            lastYaw = yaw;
        }

        float x = 0f;
        float z = 0f;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.sKey.isPressed) z -= 1f;
        if (kb.wKey.isPressed) z += 1f;

        if (x != 0 || z != 0)
        {
            NetworkManager.Instance.SendMove(x, z, yaw);
        }
    }
}
