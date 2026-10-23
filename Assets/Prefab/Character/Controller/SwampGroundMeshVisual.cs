using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SwampGroundMeshVisual : MonoBehaviour
{
    [SerializeField] private float _radius = 3f;
    [SerializeField] private int _segments = 48;
    [SerializeField] private int _rings = 8;
    [SerializeField] private float _rayStartHeight = 3f;
    [SerializeField] private float _rayDistance = 8f;
    [SerializeField] private float _surfaceOffset = 0.025f;
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private bool _randomEdge = true;
    [SerializeField] private float _edgeNoise = 0.18f;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Init(float radius, Material material, LayerMask groundMask)
    {
        _radius = radius;
        _groundMask = groundMask;

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        if (material != null)
            _meshRenderer.sharedMaterial = material;

        BuildMesh();
    }

    private void BuildMesh()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        int vertexCount = 1 + (_rings * _segments);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[_segments * 3 + (_rings - 1) * _segments * 6];

        Vector3 centerWorld = ProjectToGround(transform.position);
        vertices[0] = transform.InverseTransformPoint(centerWorld);
        uvs[0] = new Vector2(0.5f, 0.5f);

        // A star-shaped footprint: stops at cover and never bends around a corner.
        float[] reach=new float[_segments];
        for(int s=0;s<_segments;s++){
            float angle=s/(float)_segments*Mathf.PI*2;
            Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var end=ProjectToGround(transform.position+direction*_radius);
            reach[s]=_radius;
            if(SwampSurfaceQuery.Blocked(centerWorld,end,out var hit)){
                var offset=hit.point-centerWorld;offset.y=0;
                reach[s]=Mathf.Clamp(offset.magnitude-.045f,0,_radius);
            }
        }

        int v = 1;

        for (int r = 1; r <= _rings; r++)
        {
            float ringT = r / (float)_rings;
            float ringRadius = _radius * ringT;

            for (int s = 0; s < _segments; s++)
            {
                float angle = (s / (float)_segments) * Mathf.PI * 2f;
                float edgeMul = 1f;

                if (_randomEdge && r == _rings)
                {
                    float n = Mathf.PerlinNoise(Mathf.Cos(angle) * 3.1f + transform.position.x, Mathf.Sin(angle) * 3.1f + transform.position.z);
                    edgeMul = 1f + (n - 0.5f) * _edgeNoise;
                }

                float actualRadius=Mathf.Min(_radius,reach[s])*ringT;
                Vector3 flatOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * actualRadius;
                Vector3 sampleWorld = transform.position + flatOffset;
                Vector3 groundWorld = ProjectToGround(sampleWorld);

                vertices[v] = transform.InverseTransformPoint(groundWorld);
                uvs[v] = new Vector2(0.5f + flatOffset.x/(_radius*2), 0.5f + flatOffset.z/(_radius*2));
                v++;
            }
        }

        int t = 0;

        for (int s = 0; s < _segments; s++)
        {
            int a = 0;
            int b = 1 + s;
            int c = 1 + ((s + 1) % _segments);

            triangles[t++] = a;
            triangles[t++] = c;
            triangles[t++] = b;
        }

        for (int r = 1; r < _rings; r++)
        {
            int innerStart = 1 + (r - 1) * _segments;
            int outerStart = 1 + r * _segments;

            for (int s = 0; s < _segments; s++)
            {
                int innerA = innerStart + s;
                int innerB = innerStart + ((s + 1) % _segments);
                int outerA = outerStart + s;
                int outerB = outerStart + ((s + 1) % _segments);

                triangles[t++] = innerA;
                triangles[t++] = innerB;
                triangles[t++] = outerB;

                triangles[t++] = innerA;
                triangles[t++] = outerB;
                triangles[t++] = outerA;
            }
        }

        if (_mesh != null)
            Destroy(_mesh);

        _mesh = new Mesh();
        _mesh.name = "SwampGroundMesh";
        _mesh.vertices = vertices;
        _mesh.uv = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }

    private Vector3 ProjectToGround(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * _rayStartHeight;

        var hits=Physics.RaycastAll(origin,Vector3.down,_rayDistance,_groundMask,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.collider.GetComponentInParent<CharacterController>() || hit.collider.GetComponentInParent<PlayerIdentity>() || hit.collider.GetComponentInParent<SwampTrap>())continue;
            if (hit.normal.y >= 0.45f)
                return hit.point + hit.normal * _surfaceOffset;
        }

        return worldPos + Vector3.up * _surfaceOffset;
    }

    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }
}
