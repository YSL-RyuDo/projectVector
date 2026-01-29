using UnityEngine;

public class PlayerStateSender : MonoBehaviour
{
    FPSCameraLook cam;

    Vector3 lastSentPos;
    float lastSentYaw;

    float sendInterval = 0.05f; // 20Hz
    float timer;

    void Start()
    {
        cam = GetComponentInChildren<FPSCameraLook>();

        lastSentPos = transform.position;
        lastSentYaw = cam.GetYaw();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < sendInterval)
            return;

        timer = 0f;

        Vector3 pos = transform.position;
        float yaw = cam.GetYaw();

        Vector2 curXZ = new Vector2(pos.x, pos.z);
        Vector2 lastXZ = new Vector2(lastSentPos.x, lastSentPos.z);

        bool movedXZ =
            (curXZ - lastXZ).sqrMagnitude > 0.0004f; // ¾à 0.02m

        bool rotated =
            Mathf.Abs(yaw - lastSentYaw) > 0.5f;

        if (!movedXZ && !rotated)
            return;

        lastSentPos = pos;
        lastSentYaw = yaw;

        NetworkManager.Instance.SendMoveState(
            pos.x,
            pos.y,
            pos.z,
            yaw
        );
    }
}
