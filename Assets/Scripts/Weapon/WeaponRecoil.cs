using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float snappiness = 10f;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Recoil Multipliers")]
    [Tooltip("총기 모델이 튀는 시각적 반동 비율 (정조준 시 이 값을 줄이세요)")]
    public float weaponRecoilMultiplier = 1f;
    [Tooltip("카메라 화면이 튀는 실제 반동 비율 (탄착군을 결정합니다)")]
    public float cameraRecoilMultiplier = 1f;

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
        // 타겟 회전값을 원점으로 서서히 되돌림 (복구)
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);

        // 현재 회전값을 타겟으로 부드럽게 이동 (스냅)
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);

        // 실제 회전 적용 (총기 모델 혹은 컨테이너의 로컬 회전)
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void FireRecoil(Vector2 recoilPattern, float multiplier)
    {
        // 총기 시각적 반동 (총기 모델만 위로 튀어오름)
        // 정조준(ADS) 상태일 때 weaponRecoilMultiplier를 예를 들어 0.2f 정도로 확 줄이면 조준경이 화면을 벗어나지 않습니다.
        targetRotation += new Vector3(recoilPattern.y, recoilPattern.x, Random.Range(-1f, 1f)) * (multiplier * weaponRecoilMultiplier);

        // 카메라 반동 (격발 시 화면 전체를 즉시 튕김)
        // 카메라가 흔들리기 때문에 탄착군(분산도)은 넓게 유지됩니다.
        if (cameraLook != null)
        {
            // 주의: AddPitch/Yaw 내부에 Lerp를 통한 부드러운 회전 로직이 구현되어 있으면 가장 좋습니다.
            cameraLook.AddYaw(recoilPattern.x * multiplier * cameraRecoilMultiplier);
            cameraLook.AddPitch(-recoilPattern.y * multiplier * cameraRecoilMultiplier);
        }
    }

    public void StopRecoil()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
    }
}

// 쏘고 원래 위치로 보내야함, 조준 시 카메라가 총 반동을 못 따라감