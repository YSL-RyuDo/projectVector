using System.Collections;
using UnityEngine;

public class ScannerProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float _zapDuration = 0.15f;
    [SerializeField] private float _lifeTimeAfterAnchor = 3.5f;
    [SerializeField] private int _zapSegmentCount = 8;
    [SerializeField] private float _zapJitterAmount = 0.25f;
    [SerializeField] private float _zapStartWidth = 0.025f;
    [SerializeField] private float _zapEndWidth = 0.008f;

    private uint _scannerId;
    private uint _ownerPlayerId;
    private bool _isMine;
    private bool _isAnchored;

    private Rigidbody _rb;
    private Collider _collider;
    private LineRenderer _lineRenderer;

    public uint ScannerId => _scannerId;
    public uint OwnerPlayerId => _ownerPlayerId;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer == null)
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
        _lineRenderer.useWorldSpace = true;

        _lineRenderer.startWidth = _zapStartWidth;
        _lineRenderer.endWidth = _zapEndWidth;
    }

    public void Init(
        uint scannerId,
        uint ownerPlayerId,
        bool isMine,
        Vector3 velocity)
    {
        _scannerId = scannerId;
        _ownerPlayerId = ownerPlayerId;
        _isMine = isMine;
        _isAnchored = false;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = velocity;
        }

        Debug.Log(
            $"[SCANNER INIT] " +
            $"scannerId={_scannerId}, owner={_ownerPlayerId}, " +
            $"isMine={_isMine}, velocity={velocity}"
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isAnchored)
            return;

        if (collision.gameObject.CompareTag("Player"))
            return;

        Vector3 anchorPos = transform.position;

        ApplyAnchored(anchorPos);

        // 핵심:
        // 소유자 클라만 서버에 안착 보고한다.
        if (_isMine && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendScannerAnchor(
                _scannerId,
                anchorPos.x,
                anchorPos.y,
                anchorPos.z
            );

            Debug.Log(
                $"[SCANNER ANCHOR REQUEST] " +
                $"scannerId={_scannerId}, pos={anchorPos}"
            );
        }
    }

    public void ApplyAnchored(Vector3 position)
    {
        if (_isAnchored)
            return;

        _isAnchored = true;

        transform.position = position;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        Destroy(gameObject, _lifeTimeAfterAnchor);

        Debug.Log(
            $"[SCANNER ANCHORED APPLY] " +
            $"scannerId={_scannerId}, pos={position}"
        );
    }

    public void PlayZap(Vector3 targetPosition)
    {
        StopCoroutine(nameof(ZapEffectRoutine));
        StartCoroutine(ZapEffectRoutine(targetPosition));
    }

    private IEnumerator ZapEffectRoutine(Vector3 targetPosition)
    {
        if (_lineRenderer == null)
            yield break;

        int segmentCount = Mathf.Max(2, _zapSegmentCount);

        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = segmentCount;

        Vector3 start = transform.position;
        Vector3 end = targetPosition;

        float timer = 0f;

        while (timer < _zapDuration)
        {
            timer += Time.deltaTime;

            float normalized = Mathf.Clamp01(timer / _zapDuration);

            float width = Mathf.Lerp(
                _zapStartWidth,
                _zapEndWidth,
                normalized
            );

            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);

                Vector3 point = Vector3.Lerp(
                    start,
                    end,
                    t
                );

                if (i != 0 && i != segmentCount - 1)
                {
                    float fade = Mathf.Sin(t * Mathf.PI);

                    point += Random.insideUnitSphere *
                             _zapJitterAmount *
                             fade;
                }

                _lineRenderer.SetPosition(i, point);
            }

            yield return null;
        }

        _lineRenderer.enabled = false;
    }
}