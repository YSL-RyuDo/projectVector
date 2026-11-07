using System.Collections.Generic;
using UnityEngine;

public class PayloadTrainController : MonoBehaviour
{
    public enum PushState
    {
        Idle,       // 정지 (범위 내 플레이어 없음)
        MovingToA,  // PlayerA에 의해 A 거점으로 이동 중
        MovingToB,  // PlayerB에 의해 B 거점으로 이동 중
        Contested,  // 경합 상태 (양 팀 플레이어가 모두 있을 때 정지)
        ReachedA,   // A 거점 종착지에 도달
        ReachedB    // B 거점 종착지에 도달
    }

    [Header("Path")]
    [SerializeField] private TrainPath path;

    [Header("Push Mode Settings")]
    [SerializeField] private Transform goalPointA;
    [SerializeField] private int goalIndexA = 0;

    [SerializeField] private Transform goalPointB;
    [SerializeField] private int goalIndexB = -1;

    [SerializeField] private bool startAtCenter = true;
    [SerializeField] private Transform startPoint;
    [SerializeField] private int startIndex = -1;

    [Header("Player Tags")]
    [SerializeField] private string playerATag = "PlayerA";
    [SerializeField] private string playerBTag = "PlayerB";
    [SerializeField] private string playerTag = "Player"; // 기존 호환용

    [Header("Train Models")]
    [SerializeField] private Transform locomotiveA;
    [SerializeField] private Transform locomotiveB;
    [SerializeField] private Transform locomotive;
    [SerializeField] private Transform[] cargoCars;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float carSpacing = 20f;

    [Header("Player Detection")]
    [SerializeField] private Vector3 detectBoxSize = new Vector3(10f, 6f, 70f);
    [SerializeField] private Vector3 detectBoxOffset = new Vector3(0f, 2f, -20f);

    [Header("Passenger Riding")]
    [SerializeField] private LayerMask trainLayer = ~0;
    [SerializeField] private float groundCheckDistance = 0.5f;

    [Header("Steering")]
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Model Rotation Offsets")]
    [SerializeField] private Vector3 modelRotationOffsetA;
    [SerializeField] private Vector3 modelRotationOffsetB;
    [SerializeField] private Vector3 modelRotationOffset; // 기존 호환용

    private float progress;
    private bool isMoving;
    private PushState currentState = PushState.Idle;

    private float distanceA;
    private float distanceB;

    private Quaternion modelOffsetRotA;
    private Quaternion modelOffsetRotB;

    public float Progress => progress;
    public bool IsMoving => isMoving;
    public PushState State => currentState;
    public float GoalDistanceA => distanceA;
    public float GoalDistanceB => distanceB;

    public Transform LocomotiveA => locomotiveA != null ? locomotiveA : locomotive;
    public Transform LocomotiveB => locomotiveB;

    private struct PassengerInfo
    {
        public Transform player;
        public Transform car;
        public Vector3 prevCarPos;
        public Quaternion prevCarRot;
        public CharacterController cc;
        public Rigidbody rb;
    }

    private void Awake()
    {
        ResolveLocomotives();
    }

    private void OnValidate()
    {
        ResolveLocomotives();
    }

    private void ResolveLocomotives()
    {
        if (locomotiveA == null && locomotive != null)
        {
            locomotiveA = locomotive;
        }
        else if (locomotive == null && locomotiveA != null)
        {
            locomotive = locomotiveA;
        }
    }

    private void Start()
    {
        ResolveLocomotives();

        Transform head = LocomotiveA;
        if (path == null || head == null)
        {
            enabled = false;
            return;
        }

        path.BuildPath();
        InitializeGoalDistances();

        // 머리별 회전 오프셋 설정
        Vector3 rotA = modelRotationOffsetA != Vector3.zero ? modelRotationOffsetA : modelRotationOffset;
        Vector3 rotB = modelRotationOffsetB != Vector3.zero ? modelRotationOffsetB : modelRotationOffset;
        modelOffsetRotA = Quaternion.Euler(rotA);
        modelOffsetRotB = Quaternion.Euler(rotB);

        // 부모 계층 정리
        if (locomotiveA != null && locomotiveA.parent != transform)
            locomotiveA.SetParent(transform, true);

        if (locomotiveB != null && locomotiveB.parent != transform)
            locomotiveB.SetParent(transform, true);

        if (cargoCars != null)
        {
            foreach (Transform car in cargoCars)
            {
                if (car != null && car.parent != transform)
                    car.SetParent(transform, true);
            }
        }

        // 초기 시작 위치
        if (startAtCenter)
        {
            progress = (distanceA + distanceB) * 0.5f;
        }
        else if (startPoint != null)
        {
            progress = path.GetDistanceAtPoint(startPoint);
        }
        else if (startIndex >= 0)
        {
            progress = path.GetDistanceAtPointIndex(startIndex);
        }
        else
        {
            progress = (distanceA + distanceB) * 0.5f;
        }

        progress = Mathf.Clamp(progress, 0f, path.TotalLength);

        UpdateTrainFormation(true);
        isMoving = false;
        currentState = PushState.Idle;
    }

    // 종착지 거리 계산
    private void InitializeGoalDistances()
    {
        // A지역 종착지
        int finalIndexA = goalIndexA;
        if (goalPointA != null)
        {
            int found = path.GetPointIndex(goalPointA);
            if (found >= 0) finalIndexA = found;
        }
        distanceA = path.GetDistanceAtPointIndex(finalIndexA);

        // B지역 종착지 (-1이면 마지막 포인트)
        int finalIndexB = goalIndexB;
        if (goalPointB != null)
        {
            int found = path.GetPointIndex(goalPointB);
            if (found >= 0) finalIndexB = found;
        }
        else if (finalIndexB < 0)
        {
            finalIndexB = path.PointCount > 0 ? path.PointCount - 1 : 0;
        }
        distanceB = path.GetDistanceAtPointIndex(finalIndexB);
    }

    private void Update()
    {
        Transform head = LocomotiveA;
        if (path == null || head == null)
            return;

        // 탑승자 이동 전 차량 위치 기록
        List<PassengerInfo> passengers = CollectRidingPassengers();

        UpdatePushMovement();
        UpdateTrainFormation(false);

        // 탑승자 위치 동기화
        if (isMoving && passengers.Count > 0)
        {
            SyncPassengers(passengers);
        }
    }

    // 플레이어 감지 및 열차 이동 로직
    private void UpdatePushMovement()
    {
        bool hasPlayerA = HasPlayerInRange(playerATag);
        bool hasPlayerB = HasPlayerInRange(playerBTag);

        // playerATag/BTag로 아무도 없을 때 기존 playerTag 오브젝트 검사
        if (!hasPlayerA && !hasPlayerB && !string.IsNullOrEmpty(playerTag) && playerTag != playerATag && playerTag != playerBTag)
        {
            if (HasPlayerInRange(playerTag))
            {
                hasPlayerA = true;
            }
        }

        if (hasPlayerA && hasPlayerB)
        {
            // 양 팀 모두 범위 내에 있을 때 정지
            isMoving = false;
            currentState = PushState.Contested;
        }
        else if (hasPlayerA)
        {
            // PlayerA만 있음: A 종착지로 이동
            if (Mathf.Abs(progress - distanceA) <= 0.01f)
            {
                progress = distanceA;
                isMoving = false;
                currentState = PushState.ReachedA;
            }
            else
            {
                progress = Mathf.MoveTowards(progress, distanceA, moveSpeed * Time.deltaTime);
                isMoving = true;
                currentState = PushState.MovingToA;
            }
        }
        else if (hasPlayerB)
        {
            // PlayerB만 있음: B 종착지로 이동
            if (Mathf.Abs(progress - distanceB) <= 0.01f)
            {
                progress = distanceB;
                isMoving = false;
                currentState = PushState.ReachedB;
            }
            else
            {
                progress = Mathf.MoveTowards(progress, distanceB, moveSpeed * Time.deltaTime);
                isMoving = true;
                currentState = PushState.MovingToB;
            }
        }
        else
        {
            // 범위 내에 플레이어가 없음: 정지
            isMoving = false;
            currentState = PushState.Idle;
        }

        progress = Mathf.Clamp(progress, 0f, path.TotalLength);
    }

    // 양쪽 머리와 화물칸들의 위치 및 회전 갱신
    private void UpdateTrainFormation(bool instant)
    {
        if (path == null) return;

        bool isForwardTowardB = (distanceB >= distanceA);

        // 머리가 2개 모두 지정된 경우
        if (locomotiveA != null && locomotiveB != null)
        {
            int cargoCount = cargoCars != null ? cargoCars.Length : 0;
            float totalSpan = (cargoCount + 1) * carSpacing;

            float distA;
            float distB;

            if (isForwardTowardB)
            {
                // A 거점이 작은 거리 쪽, B 거점이 큰 거리 쪽
                distA = progress - totalSpan * 0.5f;
                distB = progress + totalSpan * 0.5f;

                // locomotiveA: A 거점 방향 바라봄
                UpdateVehicle(locomotiveA, distA, -path.GetDirection(distA), modelOffsetRotA, instant);

                // 화물칸 배치
                if (cargoCars != null)
                {
                    for (int i = 0; i < cargoCars.Length; i++)
                    {
                        if (cargoCars[i] == null) continue;
                        float carDist = distA + carSpacing * (i + 1);
                        UpdateVehicle(cargoCars[i], carDist, path.GetDirection(carDist), modelOffsetRotA, instant);
                    }
                }

                // locomotiveB: B 거점 방향 바라봄
                UpdateVehicle(locomotiveB, distB, path.GetDirection(distB), modelOffsetRotB, instant);
            }
            else
            {
                // A 거점이 큰 거리 쪽, B 거점이 작은 거리 쪽
                distA = progress + totalSpan * 0.5f;
                distB = progress - totalSpan * 0.5f;

                // locomotiveA: A 거점 방향 바라봄
                UpdateVehicle(locomotiveA, distA, path.GetDirection(distA), modelOffsetRotA, instant);

                // 화물칸 배치
                if (cargoCars != null)
                {
                    for (int i = 0; i < cargoCars.Length; i++)
                    {
                        if (cargoCars[i] == null) continue;
                        float carDist = distB + carSpacing * (i + 1);
                        UpdateVehicle(cargoCars[i], carDist, path.GetDirection(carDist), modelOffsetRotA, instant);
                    }
                }

                // locomotiveB: B 거점 방향 바라봄
                UpdateVehicle(locomotiveB, distB, -path.GetDirection(distB), modelOffsetRotB, instant);
            }
        }
        else
        {
            // 단일 머리 모드 fallback
            Transform mainHead = LocomotiveA;
            if (mainLocomotiveIsValid(mainHead))
            {
                Vector3 headDir = path.GetDirection(progress);
                UpdateVehicle(mainHead, progress, headDir, modelOffsetRotA, instant);
            }

            if (cargoCars != null)
            {
                for (int i = 0; i < cargoCars.Length; i++)
                {
                    if (cargoCars[i] == null) continue;
                    float carDist = Mathf.Clamp(progress - carSpacing * (i + 1), 0f, path.TotalLength);
                    Vector3 carDir = path.GetDirection(carDist);
                    UpdateVehicle(cargoCars[i], carDist, carDir, modelOffsetRotA, instant);
                }
            }
        }
    }

    private bool mainLocomotiveIsValid(Transform t) => t != null;

    private void UpdateVehicle(Transform vehicle, float distance, Vector3 forwardDir, Quaternion offsetRot, bool instant)
    {
        if (vehicle == null || path == null) return;

        float clampedDist = Mathf.Clamp(distance, 0f, path.TotalLength);
        Vector3 position = path.GetPosition(clampedDist);

        if (forwardDir.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forwardDir.normalized, Vector3.up) * offsetRot;
            if (instant)
            {
                vehicle.rotation = targetRotation;
            }
            else
            {
                vehicle.rotation = Quaternion.RotateTowards(vehicle.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        vehicle.position = position;
    }

    // 특정 태그의 플레이어가 감지 박스 안에 있는지 확인
    private bool HasPlayerInRange(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        GameObject[] players = GameObject.FindGameObjectsWithTag(tag);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && IsPositionInRange(players[i].transform.position))
            {
                return true;
            }
        }
        return false;
    }

    // 감지 박스 기준 위치 판정
    private bool IsPositionInRange(Vector3 worldPos)
    {
        Transform center = LocomotiveA;
        if (center == null) return false;

        Vector3 relativePos = Quaternion.Inverse(center.rotation) * (worldPos - center.position);
        Vector3 offsetPos = relativePos - detectBoxOffset;
        Vector3 halfSize = detectBoxSize * 0.5f;

        return Mathf.Abs(offsetPos.x) <= halfSize.x &&
               Mathf.Abs(offsetPos.y) <= halfSize.y &&
               Mathf.Abs(offsetPos.z) <= halfSize.z;
    }

    // 탑승자 수집
    private List<PassengerInfo> CollectRidingPassengers()
    {
        List<PassengerInfo> list = new List<PassengerInfo>();

        CollectPassengersForTag(playerATag, list);
        CollectPassengersForTag(playerBTag, list);
        if (!string.IsNullOrEmpty(playerTag) && playerTag != playerATag && playerTag != playerBTag)
        {
            CollectPassengersForTag(playerTag, list);
        }

        return list;
    }

    private void CollectPassengersForTag(string tag, List<PassengerInfo> list)
    {
        if (string.IsNullOrEmpty(tag)) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag(tag);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            Transform t = players[i].transform;
            Transform riddenCar = GetRiddenCar(t);
            if (riddenCar != null)
            {
                list.Add(new PassengerInfo
                {
                    player = t,
                    car = riddenCar,
                    prevCarPos = riddenCar.position,
                    prevCarRot = riddenCar.rotation,
                    cc = players[i].GetComponent<CharacterController>(),
                    rb = players[i].GetComponent<Rigidbody>()
                });
            }
        }
    }

    // 해당 오브젝트가 탑승한 차량(머리 또는 화물칸) 반환
    private Transform GetRiddenCar(Transform target)
    {
        if (target == null) return null;

        Vector3 rayOrigin = target.position + Vector3.up * 0.2f;
        float rayLength = groundCheckDistance + 0.2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, trainLayer))
        {
            if (locomotiveA != null && (hit.transform == locomotiveA || hit.transform.IsChildOf(locomotiveA)))
                return locomotiveA;

            if (locomotiveB != null && (hit.transform == locomotiveB || hit.transform.IsChildOf(locomotiveB)))
                return locomotiveB;

            if (locomotive != null && (hit.transform == locomotive || hit.transform.IsChildOf(locomotive)))
                return locomotive;

            if (cargoCars != null)
            {
                foreach (Transform car in cargoCars)
                {
                    if (car != null && (hit.transform == car || hit.transform.IsChildOf(car)))
                        return car;
                }
            }
        }
        return null;
    }

    // 탑승자 위치/회전 동기화
    private void SyncPassengers(List<PassengerInfo> passengers)
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerInfo info = passengers[i];
            if (info.player == null || info.car == null) continue;

            Quaternion deltaRot = info.car.rotation * Quaternion.Inverse(info.prevCarRot);
            Vector3 offsetFromCar = info.player.position - info.prevCarPos;
            Vector3 targetPlayerPos = info.car.position + (deltaRot * offsetFromCar);
            Vector3 moveDelta = targetPlayerPos - info.player.position;

            if (info.cc != null && info.cc.enabled)
            {
                info.cc.Move(moveDelta);
            }
            else if (info.rb != null && !info.rb.isKinematic)
            {
                info.rb.position += moveDelta;
            }
            else
            {
                info.player.position += moveDelta;
            }

            float yawDelta = deltaRot.eulerAngles.y;
            info.player.Rotate(0f, yawDelta, 0f, Space.World);
        }
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = LocomotiveA;
        if (center != null)
        {
            Gizmos.color = currentState == PushState.Contested ? Color.magenta :
                           currentState == PushState.MovingToA ? Color.cyan :
                           currentState == PushState.MovingToB ? Color.red : Color.yellow;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);
            Gizmos.DrawWireCube(detectBoxOffset, detectBoxSize);
            Gizmos.matrix = oldMatrix;
        }

        // A/B 머리 방향 및 종착지 시각화
        if (locomotiveA != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(locomotiveA.position, locomotiveA.forward * 3f);
        }

        if (locomotiveB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(locomotiveB.position, locomotiveB.forward * 3f);
        }

        if (path != null)
        {
            // A 종착지
            int idxA = goalIndexA;
            if (goalPointA != null)
            {
                int f = path.GetPointIndex(goalPointA);
                if (f >= 0) idxA = f;
            }
            Vector3 posA = path.GetPosition(path.GetDistanceAtPointIndex(idxA));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(posA, 1.5f);

            // B 종착지
            int idxB = goalIndexB;
            if (goalPointB != null)
            {
                int f = path.GetPointIndex(goalPointB);
                if (f >= 0) idxB = f;
            }
            else if (idxB < 0)
            {
                idxB = path.PointCount > 0 ? path.PointCount - 1 : 0;
            }
            Vector3 posB = path.GetPosition(path.GetDistanceAtPointIndex(idxB));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(posB, 1.5f);
        }
    }
}
