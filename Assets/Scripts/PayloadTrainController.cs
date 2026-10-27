using UnityEngine;

public class PayloadTrainController : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private TrainPath path;

    [Header("Train")]
    [SerializeField] private Transform locomotive;
    [SerializeField] private Transform[] cargoCars;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float carSpacing = 4f;

    [Header("Steering")]
    [Tooltip("차체의 최대 회전 속도 (도/초)")]
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Model")]
    [Tooltip("모델 앞쪽이 로컬 Z+ 방향이 아니면 조정")]
    [SerializeField] private Vector3 modelRotationOffset;

    private float progress;
    private bool isMoving;

    private Quaternion modelOffsetRotation;

    public float Progress => progress;
    public bool IsMoving => isMoving;

    private void Start()
    {
        if (path == null || locomotive == null)
        {
            Debug.LogError(
                "PayloadTrainController: Path와 Locomotive를 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        modelOffsetRotation = Quaternion.Euler(modelRotationOffset);

        // 기관차와 화물칸을 컨트롤러의 자식으로 유지한다.
        if (locomotive.parent != transform)
            locomotive.SetParent(transform, true);

        if (cargoCars != null)
        {
            foreach (Transform car in cargoCars)
            {
                if (car != null && car.parent != transform)
                    car.SetParent(transform, true);
            }
        }

        // 기존 시작 진행도 유지
        progress = Mathf.Clamp(
            cargoCars != null ? cargoCars.Length * carSpacing : 0f,
            0f,
            path.TotalLength
        );

        UpdateLocomotive(true);
        UpdateCargoCars(true);

        // 기존 자동 이동 동작 유지
        SetMoving(true);
    }

    private void Update()
    {
        if (path == null || locomotive == null)
            return;

        if (isMoving)
        {
            progress = Mathf.Min(
                progress + moveSpeed * Time.deltaTime,
                path.TotalLength
            );

            if (progress >= path.TotalLength)
                isMoving = false;
        }

        UpdateLocomotive(false);
        UpdateCargoCars(false);
    }

    private void UpdateLocomotive(bool instant)
    {
        // 기관차도 화물칸과 동일하게 경로에서 위치와 방향을 가져온다.
        Vector3 position = path.GetPosition(progress);
        Vector3 direction = path.GetDirection(progress);

        if (direction.sqrMagnitude < 0.000001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction, Vector3.up)
            * modelOffsetRotation;

        if (instant)
        {
            locomotive.rotation = targetRotation;
        }
        else
        {
            locomotive.rotation = Quaternion.RotateTowards(
                locomotive.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        locomotive.position = position;
    }

    private void UpdateCargoCars(bool instant)
    {
        if (cargoCars == null)
            return;

        for (int i = 0; i < cargoCars.Length; i++)
        {
            Transform car = cargoCars[i];

            if (car == null)
                continue;

            float carDistance = Mathf.Max(
                0f,
                progress - carSpacing * (i + 1)
            );

            Vector3 carPosition = path.GetPosition(carDistance);
            Vector3 carDirection = path.GetDirection(carDistance);

            if (carDirection.sqrMagnitude < 0.000001f)
                continue;

            Quaternion targetRotation =
                Quaternion.LookRotation(carDirection, Vector3.up)
                * modelOffsetRotation;

            if (instant)
            {
                car.rotation = targetRotation;
            }
            else
            {
                car.rotation = Quaternion.RotateTowards(
                    car.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            car.position = carPosition;
        }
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving && path != null && progress < path.TotalLength;
    }
}