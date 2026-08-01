using UnityEngine;

public class SmokeGrenade : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private float _delayBeforeSmoke = 0.0f;
    [SerializeField] private float _stateSendInterval = 0.05f;

    [Header("Fallback")]
    [SerializeField] private float _autoAttachTime = 2.0f;

    [Header("Remote Sync")]
    [SerializeField] private float _remoteMoveLerpSpeed = 25f;
    [SerializeField] private float _remoteRotateLerpSpeed = 25f;

    private uint _smokeId;
    private uint _ownerPlayerId;
    private bool _isMine;

    private bool _hasAttached;
    private Rigidbody _rb;
    private Collider[] _colliders;

    private Vector3 _attachPoint;
    private Vector3 _attachNormal;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    private float _stateSendTimer;
    private float _lifeTimer;

    public uint SmokeId => _smokeId;
    public uint OwnerPlayerId => _ownerPlayerId;
    public bool IsMine => _isMine;

    private void Awake()
    {
        _rb =
            GetComponent<Rigidbody>();

        _colliders =
            GetComponentsInChildren<Collider>(true);
    }

    private void Update()
    {
        if (_isMine)
        {
            UpdateOwner();
        }
        else
        {
            UpdateRemoteInterpolation();
        }
    }

    public void InitFromServer(
        uint smokeId,
        uint ownerPlayerId,
        bool isMine,
        Vector3 velocity)
    {
        _smokeId = smokeId;
        _ownerPlayerId = ownerPlayerId;
        _isMine = isMine;

        _hasAttached = false;
        _stateSendTimer = 0f;
        _lifeTimer = 0f;

        _targetPosition = transform.position;
        _targetRotation = transform.rotation;

        if (_isMine)
        {
            InitOwnerPhysics(velocity);

            // 스폰 직후 1회 즉시 위치 전송
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendEntrySmokeState(
                    _smokeId,
                    transform.position,
                    transform.rotation
                );
            }
        }
        else
        {
            InitRemotePhysics();
        }

        Debug.Log(
            $"[SMOKE GRENADE INIT] smokeId={_smokeId}, " +
            $"owner={_ownerPlayerId}, isMine={_isMine}, velocity={velocity}"
        );
    }

    private void InitOwnerPhysics(Vector3 velocity)
    {
        SetCollidersEnabled(true);

        if (_rb == null)
        {
            Debug.LogError("[SMOKE GRENADE] Rigidbody missing");
            return;
        }

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        _rb.linearVelocity = velocity;
        _rb.angularVelocity =
            Random.insideUnitSphere * 10f;
    }

    private void InitRemotePhysics()
    {
        SetCollidersEnabled(false);

        if (_rb == null)
            return;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.useGravity = false;
        _rb.isKinematic = true;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == null)
                continue;

            _colliders[i].enabled = enabled;
        }
    }

    private void UpdateOwner()
    {
        if (_hasAttached)
            return;

        _lifeTimer += Time.deltaTime;

        UpdateOwnerStateSend();

        if (_lifeTimer >= _autoAttachTime)
        {
            Debug.LogWarning(
                $"[SMOKE AUTO ATTACH] smokeId={_smokeId}, pos={transform.position}"
            );

            AttachAtCurrentPosition(Vector3.up);
        }
    }

    private void UpdateOwnerStateSend()
    {
        if (NetworkManager.Instance == null)
            return;

        _stateSendTimer += Time.deltaTime;

        if (_stateSendTimer < _stateSendInterval)
            return;

        _stateSendTimer = 0f;

        NetworkManager.Instance.SendEntrySmokeState(
            _smokeId,
            transform.position,
            transform.rotation
        );
    }

    private void UpdateRemoteInterpolation()
    {
        transform.position =
            Vector3.Lerp(
                transform.position,
                _targetPosition,
                Time.deltaTime * _remoteMoveLerpSpeed
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                _targetRotation,
                Time.deltaTime * _remoteRotateLerpSpeed
            );
    }

    public void ApplyRemoteState(
        Vector3 position,
        Quaternion rotation)
    {
        if (_isMine)
            return;

        _targetPosition = position;
        _targetRotation = rotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasAttached)
            return;

        if (!_isMine)
            return;

        if (collision.gameObject.CompareTag("Player"))
            return;

        if (collision.contactCount <= 0)
            return;

        ContactPoint contact =
            collision.GetContact(0);

        Debug.Log(
            $"[SMOKE COLLISION] smokeId={_smokeId}, " +
            $"hit={collision.gameObject.name}, point={contact.point}, normal={contact.normal}"
        );

        AttachAtPosition(
            contact.point,
            contact.normal
        );
    }

    private void AttachAtCurrentPosition(Vector3 normal)
    {
        AttachAtPosition(
            transform.position,
            normal
        );
    }

    private void AttachAtPosition(
        Vector3 point,
        Vector3 normal)
    {
        if (_hasAttached)
            return;

        _hasAttached = true;

        _attachPoint = point;
        _attachNormal = normal;

        StopOwnerPhysics();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendEntrySmokeState(
                _smokeId,
                transform.position,
                transform.rotation
            );
        }

        if (_delayBeforeSmoke > 0f)
        {
            Invoke(
                nameof(SendAttachToServer),
                _delayBeforeSmoke
            );
        }
        else
        {
            SendAttachToServer();
        }
    }

    private void StopOwnerPhysics()
    {
        if (_rb == null)
            return;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    private void SendAttachToServer()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.SendEntrySmokeAttach(
            _smokeId,
            _attachPoint,
            _attachNormal
        );

        Debug.Log(
            $"[SMOKE ATTACH REQUEST] smokeId={_smokeId}, " +
            $"point={_attachPoint}, normal={_attachNormal}"
        );
    }
}