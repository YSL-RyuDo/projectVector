
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
                Debug.LogError("TrainPath: 비어 있는 포인트가 있습니다.", this);
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
        const float sampleOffset = 0.15f;

        Vector3 before = GetPosition(distance - sampleOffset);
        Vector3 after = GetPosition(distance + sampleOffset);

        Vector3 direction = after - before;

        if (direction.sqrMagnitude < 0.000001f)
            return transform.forward;

        return direction.normalized;
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

        // 각 Point를 색상이 있는 원으로 표시
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
