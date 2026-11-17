using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrainPath : MonoBehaviour
{
    [Header("Path Points")]
    [SerializeField] private Transform[] points;

    [Header("Curve")]
    [SerializeField, Min(2)] private int samplesPerSegment = 20;

    [Header("Path Visualization")]
    [SerializeField] private Color pathColor = Color.cyan;
    [SerializeField, Min(1f)] private float lineThickness = 5f;

    [Header("Point Visualization")]
    [SerializeField] private Color pointColor = Color.yellow;
    [SerializeField, Min(0.01f)] private float pointRadius = 0.3f;

    private Vector3[] sampledPositions;
    private float[] cumulativeDistances;

    public float TotalLength { get; private set; }
    public Transform[] Points => points;
    public int PointCount => points != null ? points.Length : 0;

    private void Awake()
    {
        BuildPath();
    }

    public void BuildPath()
    {
        if (points == null || points.Length < 2)
        {
            Debug.LogError("TrainPath: 포인트가 최소 2개 필요합니다.", this);
            return;
        }

        int segmentCount = points.Length - 1;
        int sampleCount = segmentCount * samplesPerSegment + 1;

        sampledPositions = new Vector3[sampleCount];
        cumulativeDistances = new float[sampleCount];

        int index = 0;

        for (int segment = 0; segment < segmentCount; segment++)
        {
            if (points[segment] == null || points[segment + 1] == null)
            {
                Debug.LogError("TrainPath: 비어있는 포인트가 있습니다.", this);
                sampledPositions = null;
                cumulativeDistances = null;
                TotalLength = 0f;
                return;
            }

            Vector3 p0 = points[segment].position;
            Vector3 p1 = points[segment + 1].position;

            for (int j = 0; j < samplesPerSegment; j++)
            {
                float t = j / (float)samplesPerSegment;
                sampledPositions[index++] = Vector3.Lerp(p0, p1, t);
            }
        }

        sampledPositions[index] = points[points.Length - 1].position;

        cumulativeDistances[0] = 0f;

        for (int i = 1; i < sampledPositions.Length; i++)
        {
            cumulativeDistances[i] =
                cumulativeDistances[i - 1] +
                Vector3.Distance(sampledPositions[i - 1], sampledPositions[i]);
        }

        TotalLength = cumulativeDistances[cumulativeDistances.Length - 1];
    }

    public Vector3 GetPosition(float distance)
    {
        if (sampledPositions == null || sampledPositions.Length == 0)
            BuildPath();

        if (sampledPositions == null || sampledPositions.Length == 0)
            return transform.position;

        distance = Mathf.Clamp(distance, 0f, TotalLength);

        int index = FindSegment(distance);

        if (index >= sampledPositions.Length - 1)
            return sampledPositions[sampledPositions.Length - 1];

        float segmentLength =
            cumulativeDistances[index + 1] - cumulativeDistances[index];

        float t = segmentLength > 0.0001f
            ? (distance - cumulativeDistances[index]) / segmentLength
            : 0f;

        return Vector3.Lerp(
            sampledPositions[index],
            sampledPositions[index + 1],
            t
        );
    }

    public Vector3 GetDirection(float distance)
    {
        if (sampledPositions == null || sampledPositions.Length < 2)
            BuildPath();

        if (sampledPositions == null || sampledPositions.Length < 2 || TotalLength <= 0.0001f)
            return transform.forward;

        const float sampleOffset = 0.2f;
        float clampedDist = Mathf.Clamp(distance, 0f, TotalLength);

        // 스플라인 양 끝 지점에서도 올바른 접선 방향을 반환하도록 샘플 범위 보정
        float distBefore = Mathf.Clamp(clampedDist - sampleOffset, 0f, TotalLength);
        float distAfter = Mathf.Clamp(clampedDist + sampleOffset, 0f, TotalLength);

        if (distAfter - distBefore < sampleOffset)
        {
            if (clampedDist <= sampleOffset)
            {
                distBefore = 0f;
                distAfter = Mathf.Min(TotalLength, sampleOffset * 2f);
            }
            else
            {
                distBefore = Mathf.Max(0f, TotalLength - sampleOffset * 2f);
                distAfter = TotalLength;
            }
        }

        Vector3 before = GetPosition(distBefore);
        Vector3 after = GetPosition(distAfter);
        Vector3 direction = after - before;

        if (direction.sqrMagnitude < 0.000001f)
        {
            if (points != null && points.Length >= 2)
            {
                if (clampedDist <= 0.5f * TotalLength)
                    direction = points[1].position - points[0].position;
                else
                    direction = points[points.Length - 1].position - points[points.Length - 2].position;
            }
        }

        if (direction.sqrMagnitude < 0.000001f)
            return transform.forward;

        return direction.normalized;
    }

    // 포인트 Transform의 인덱스 검색
    public int GetPointIndex(Transform targetPoint)
    {
        if (points == null || targetPoint == null)
            return -1;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == targetPoint)
                return i;
        }
        return -1;
    }

    // 특정 인덱스의 Transform 반환
    public Transform GetPoint(int index)
    {
        if (points == null || index < 0 || index >= points.Length)
            return null;

        return points[index];
    }

    // 특정 포인트 인덱스의 스플라인 누적 거리 반환
    public float GetDistanceAtPointIndex(int pointIndex)
    {
        if (sampledPositions == null || sampledPositions.Length == 0)
            BuildPath();

        if (points == null || points.Length < 2 || cumulativeDistances == null)
            return 0f;

        pointIndex = Mathf.Clamp(pointIndex, 0, points.Length - 1);
        int sampleIndex = pointIndex * samplesPerSegment;

        if (sampleIndex >= cumulativeDistances.Length)
            return TotalLength;

        return cumulativeDistances[sampleIndex];
    }

    // 특정 Transform 포인트의 누적 거리 반환
    public float GetDistanceAtPoint(Transform targetPoint)
    {
        int index = GetPointIndex(targetPoint);
        if (index < 0)
            return 0f;

        return GetDistanceAtPointIndex(index);
    }

    private int FindSegment(float distance)
    {
        int low = 0;
        int high = cumulativeDistances.Length - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (cumulativeDistances[mid] < distance)
                low = mid + 1;
            else
                high = mid;
        }

        return Mathf.Max(0, low - 1);
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length < 2)
            return;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                return;
        }

#if UNITY_EDITOR
        // 경로 선 표시
        int segmentCount = points.Length - 1;
        int resolution = Mathf.Max(10, samplesPerSegment);

        Vector3[] drawPositions =
            new Vector3[segmentCount * resolution + 1];

        int index = 0;

        for (int segment = 0; segment < segmentCount; segment++)
        {
            Vector3 start = points[segment].position;
            Vector3 end = points[segment + 1].position;

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;
                drawPositions[index++] = Vector3.Lerp(start, end, t);
            }
        }

        drawPositions[index] = points[points.Length - 1].position;

        Handles.color = pathColor;
        Handles.DrawAAPolyLine(lineThickness, drawPositions);

        // 각 Point 위치 표시
        Handles.color = pointColor;

        foreach (Transform point in points)
        {
            Handles.SphereHandleCap(
                0,
                point.position,
                Quaternion.identity,
                pointRadius * 2f,
                EventType.Repaint
            );
        }
#else
        Gizmos.color = pathColor;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }

        Gizmos.color = pointColor;

        foreach (Transform point in points)
        {
            Gizmos.DrawSphere(point.position, pointRadius);
        }
#endif
    }
}
