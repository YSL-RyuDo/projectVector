using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float snappiness = 10f;
    [SerializeField] private float returnSpeed = 5f;

    // 상태 변수
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private FPSCameraLook cameraLook;

    void Awake()
    {
        cameraLook = GetComponentInParent<FPSCameraLook>();
    }

    void Update()
    {
        // 1. 타겟 회전값을 원점으로 서서히 되돌림 (복구)
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);

        // 2. 현재 회전값을 타겟으로 부드럽게 이동 (스냅)
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);

        // 3. 실제 회전 적용 (총기 모델 혹은 컨테이너의 로컬 회전)
        transform.localRotation = Quaternion.Euler(currentRotation);

        // 4. 카메라에 반동의 일부를 전달
        if (cameraLook != null)
        {
            // Y축(Yaw)과 X축(Pitch) 일부를 카메라 조준선에 반영
            cameraLook.AddYaw(currentRotation.y * Time.deltaTime);
            cameraLook.AddPitch(-currentRotation.x * Time.deltaTime);
        }
    }

    public void FireRecoil(Vector2 recoilPattern, float multiplier)
    {
        // 사격 시 호출: 패턴 값에 멀티플라이어를 곱해 타겟에 더함
        targetRotation += new Vector3(recoilPattern.y, recoilPattern.x, Random.Range(-1f, 1f)) * multiplier;
    }

    public void StopRecoil()
    {
        targetRotation = Vector2.Lerp(targetRotation, Vector2.zero, returnSpeed * Time.deltaTime);
    }
}