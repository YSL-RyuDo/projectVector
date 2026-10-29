using UnityEngine;

public class RemotePlayerMovement : MonoBehaviour
{
    public float positionLerpSpeed = 12f;
    public float rotationLerpSpeed = 15f;

    Vector3 targetPos;
    float targetYaw;

    void Awake()
    {
        targetPos = transform.position;
        targetYaw = transform.eulerAngles.y;
    }

    public void ApplyNetworkState(Vector3 pos, float yaw)
    {
        targetPos = pos;
        targetYaw = yaw;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            positionLerpSpeed * Time.deltaTime
        );

        Quaternion targetRot =
            Quaternion.Euler(0f, targetYaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationLerpSpeed * Time.deltaTime
        );
    }
}
