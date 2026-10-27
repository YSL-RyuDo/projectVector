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

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 detectBoxSize = new Vector3(6f, 4f, 12f);
    [SerializeField] private Vector3 detectBoxOffset = new Vector3(0f, 0f, -4f);

    [Header("Passenger Riding")]
    [SerializeField] private LayerMask trainLayer = ~0;
    [SerializeField] private float groundCheckDistance = 0.5f;

    [Header("Steering")]
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Model")]
    [SerializeField] private Vector3 modelRotationOffset;

    private float progress;
    private bool isMoving;

    private Quaternion modelOffsetRotation;
    private Transform playerTransform;
    private CharacterController playerCC;
    private Rigidbody playerRb;

    public float Progress => progress;
    public bool IsMoving => isMoving;

    private void Start()
    {
        if (path == null || locomotive == null)
        {
            enabled = false;
            return;
        }

        FindPlayer();

        modelOffsetRotation = Quaternion.Euler(modelRotationOffset);

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

        progress = Mathf.Clamp(cargoCars != null ? cargoCars.Length * carSpacing : 0f, 0f, path.TotalLength);

        UpdateLocomotive(true);
        UpdateCargoCars(true);
        SetMoving(false);
    }

    private void Update()
    {
        if (path == null || locomotive == null)
            return;

        CheckPlayerDistance();

        Transform currentCar = GetRiddenCar();
        Vector3 prevCarPos = Vector3.zero;
        Quaternion prevCarRot = Quaternion.identity;

        if (currentCar != null)
        {
            prevCarPos = currentCar.position;
            prevCarRot = currentCar.rotation;
        }

        if (isMoving)
        {
            progress = Mathf.Min(progress + moveSpeed * Time.deltaTime,path.TotalLength);

            if (progress >= path.TotalLength) isMoving = false;
        }

        UpdateLocomotive(false);
        UpdateCargoCars(false);

        if (currentCar != null && isMoving)
        {
            SyncPlayerWithCar(currentCar, prevCarPos, prevCarRot);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerCC = playerObj.GetComponent<CharacterController>();
            playerRb = playerObj.GetComponent<Rigidbody>();
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                SetMoving(false);
                return;
            }
        }

        bool isPlayerNear = IsPlayerInRange();
        SetMoving(isPlayerNear);
    }

    private Transform GetRiddenCar()
    {
        if (playerTransform == null)
            return null;

        Vector3 rayOrigin = playerTransform.position + Vector3.up * 0.2f;
        float rayLength = groundCheckDistance + 0.2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, trainLayer))
        {
            if (hit.transform == locomotive || hit.transform.IsChildOf(locomotive))
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

    private void SyncPlayerWithCar(Transform car, Vector3 prevPos, Quaternion prevRot)
    {
        Quaternion deltaRot = car.rotation * Quaternion.Inverse(prevRot);
        Vector3 offsetFromCar = playerTransform.position - prevPos;
        Vector3 targetPlayerPos = car.position + (deltaRot * offsetFromCar);
        Vector3 moveDelta = targetPlayerPos - playerTransform.position;

        if (playerCC != null && playerCC.enabled)
        {
            playerCC.Move(moveDelta);
        }
        else if (playerRb != null && !playerRb.isKinematic)
        {
            playerRb.position += moveDelta;
        }
        else
        {
            playerTransform.position += moveDelta;
        }

        float yawDelta = deltaRot.eulerAngles.y;
        playerTransform.Rotate(0f, yawDelta, 0f, Space.World);
    }

    private bool IsPlayerInRange()
    {
        Vector3 relativePos = Quaternion.Inverse(locomotive.rotation) * (playerTransform.position - locomotive.position);
        Vector3 offsetPos = relativePos - detectBoxOffset;
        Vector3 halfSize = detectBoxSize * 0.5f;
        
        return Mathf.Abs(offsetPos.x) <= halfSize.x && Mathf.Abs(offsetPos.y) <= halfSize.y && Mathf.Abs(offsetPos.z) <= halfSize.z;
    }

    private void UpdateLocomotive(bool instant)
    {
        Vector3 position = path.GetPosition(progress);
        Vector3 direction = path.GetDirection(progress);

        if (direction.sqrMagnitude < 0.000001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up) * modelOffsetRotation;

        if (instant)
        {
            locomotive.rotation = targetRotation;
        }
        else
        {
            locomotive.rotation = Quaternion.RotateTowards(locomotive.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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

            if (car == null) continue;

            float carDistance = Mathf.Max(0f, progress - carSpacing * (i + 1));

            Vector3 carPosition = path.GetPosition(carDistance);
            Vector3 carDirection = path.GetDirection(carDistance);

            if (carDirection.sqrMagnitude < 0.000001f)
                continue;

            Quaternion targetRotation = Quaternion.LookRotation(carDirection, Vector3.up) * modelOffsetRotation;

            if (instant)
            {
                car.rotation = targetRotation;
            }
            else
            {
                car.rotation = Quaternion.RotateTowards(car.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            car.position = carPosition;
        }
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving && path != null && progress < path.TotalLength;
    }

    private void OnDrawGizmosSelected()
    {
        if (locomotive == null)
            return;

        Gizmos.color = Color.yellow;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(locomotive.position, locomotive.rotation, Vector3.one);
        Gizmos.DrawWireCube(detectBoxOffset, detectBoxSize);
        Gizmos.matrix = oldMatrix;
    }
}