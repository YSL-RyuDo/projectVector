using UnityEngine;

public class Sentinel_SlowTrap : MonoBehaviour
{
    [SerializeField] private float _health = 40f;
    [SerializeField] private float _slowRadius = 10f;
    [SerializeField] private float _slowAmount = 0.5f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _warningDistance = 12f;

    private static AudioClip _generatedWarningClip;

    private uint _trapId;
    private uint _ownerPlayerId;
    private int _ownerTeam = -1;

    private bool _isOwner;
    private bool _isActive;
    private bool _activationRequested;
    private bool _isDestroyed;
    private bool _localSlowApplied;

    private float _zoneRadius;
    private float _zoneDuration;
    private int _detectionMask;

    private AudioSource _warningSource;
    private LineRenderer _zoneLine;
    private Material _zoneMaterial;

    public uint TrapId => _trapId;
    public uint OwnerPlayerId => _ownerPlayerId;
    public int OwnerTeam => _ownerTeam;

    private void Awake()
    {
        _detectionMask = BuildDetectionMask();

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        UnityEngine.UI.Slider slider =
            GetComponentInChildren<UnityEngine.UI.Slider>(true);

        if (slider != null)
            slider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_isDestroyed)
            return;

        if (!_isActive)
        {
            TryRequestActivation();
            return;
        }

        UpdateLocalSlow();
    }

    public void InitNetwork(
        uint trapId,
        uint ownerPlayerId,
        int ownerTeam,
        bool isOwner,
        Vector3 position,
        float health,
        float detectionRadius,
        float zoneRadius)
    {
        _trapId = trapId;
        _ownerPlayerId = ownerPlayerId;
        _ownerTeam = ownerTeam;
        _isOwner = isOwner;

        _health = health;
        _slowRadius = detectionRadius;
        _zoneRadius = zoneRadius;

        _isActive = false;
        _activationRequested = false;
        _isDestroyed = false;

        transform.position = position;

        SetupEnemyWarningSound();

        Debug.Log(
            $"[SENTINEL Q INIT] trapId={trapId}, owner={ownerPlayerId}, " +
            $"team={ownerTeam}, isOwner={isOwner}, radius={detectionRadius}"
        );
    }

    public void ActivateFromServer(
        float duration,
        float slowMultiplier)
    {
        if (_isDestroyed || _isActive)
            return;

        _isActive = true;
        _zoneDuration = duration;
        _slowAmount = Mathf.Clamp(slowMultiplier, 0.05f, 1f);

        StopWarningSound();
        CreateZoneVisual();
        UpdateLocalSlow();

        Debug.Log(
            $"[SENTINEL Q ACTIVATE] trapId={_trapId}, " +
            $"duration={duration}, slow={_slowAmount}"
        );
    }

    public void DestroyFromServer()
    {
        DestroyLocal();
    }

    private void TryRequestActivation()
    {
        if (!_isOwner ||
            _activationRequested ||
            _trapId == 0 ||
            NetworkManager.Instance == null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _slowRadius,
            _detectionMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            PlayerIdentity identity =
                hit.GetComponentInParent<PlayerIdentity>();

            if (identity == null)
                continue;

            if (identity.PlayerId == _ownerPlayerId ||
                identity.Team == _ownerTeam)
            {
                continue;
            }

            PlayerHP hp = identity.GetComponent<PlayerHP>();

            if (hp != null && hp.IsDead)
                continue;

            _activationRequested = true;
            NetworkManager.Instance.SendSentinelQActivate(_trapId);

            Debug.Log(
                $"[SENTINEL Q DETECT] trapId={_trapId}, enemy={identity.PlayerId}"
            );

            return;
        }
    }

    private void UpdateLocalSlow()
    {
        if (NetworkManager.Instance == null)
            return;

        PlayerIdentity localPlayer = NetworkManager.Instance.MyPlayer;

        if (localPlayer == null)
            return;

        PlayerMovement movement =
            localPlayer.GetComponent<PlayerMovement>();

        if (movement == null)
            movement = localPlayer.GetComponentInChildren<PlayerMovement>(true);

        if (movement == null)
            return;

        bool isInside =
            Vector3.Distance(
                transform.position,
                localPlayer.transform.position
            ) <= _zoneRadius;

        if (isInside && !_localSlowApplied)
        {
            movement.ApplySwampSlow(_slowAmount);
            _localSlowApplied = true;

            Debug.Log(
                $"[SENTINEL Q SLOW APPLY] trapId={_trapId}, player={localPlayer.PlayerId}"
            );
        }
        else if (!isInside && _localSlowApplied)
        {
            movement.RemoveSwampSlow();
            _localSlowApplied = false;

            Debug.Log(
                $"[SENTINEL Q SLOW REMOVE] trapId={_trapId}, player={localPlayer.PlayerId}"
            );
        }
    }

    private void RemoveLocalSlow()
    {
        if (!_localSlowApplied ||
            NetworkManager.Instance == null)
        {
            _localSlowApplied = false;
            return;
        }

        PlayerIdentity localPlayer = NetworkManager.Instance.MyPlayer;

        if (localPlayer != null)
        {
            PlayerMovement movement =
                localPlayer.GetComponent<PlayerMovement>();

            if (movement == null)
            {
                movement =
                    localPlayer.GetComponentInChildren<PlayerMovement>(true);
            }

            if (movement != null)
                movement.RemoveSwampSlow();
        }

        _localSlowApplied = false;
    }

    private void SetupEnemyWarningSound()
    {
        if (NetworkManager.Instance == null)
            return;

        PlayerIdentity localPlayer = NetworkManager.Instance.MyPlayer;

        if (localPlayer == null ||
            localPlayer.Team == _ownerTeam)
        {
            return;
        }

        _warningSource = gameObject.AddComponent<AudioSource>();
        _warningSource.playOnAwake = false;
        _warningSource.loop = true;
        _warningSource.spatialBlend = 1f;
        _warningSource.rolloffMode = AudioRolloffMode.Linear;
        _warningSource.minDistance = 1f;
        _warningSource.maxDistance = _warningDistance;
        _warningSource.volume = 0.35f;
        _warningSource.dopplerLevel = 0f;
        _warningSource.clip = GetWarningClip();
        _warningSource.Play();
    }

    private static AudioClip GetWarningClip()
    {
        if (_generatedWarningClip != null)
            return _generatedWarningClip;

        const int sampleRate = 22050;
        const int sampleCount = sampleRate;
        const float toneDuration = 0.16f;
        const float frequency = 620f;

        float[] samples = new float[sampleCount];
        int toneSamples = Mathf.RoundToInt(sampleRate * toneDuration);

        for (int i = 0; i < toneSamples; i++)
        {
            float time = (float)i / sampleRate;
            float envelope = 1f - ((float)i / toneSamples);

            samples[i] =
                Mathf.Sin(2f * Mathf.PI * frequency * time) *
                envelope *
                0.35f;
        }

        _generatedWarningClip = AudioClip.Create(
            "SentinelQWarning",
            sampleCount,
            1,
            sampleRate,
            false
        );
        _generatedWarningClip.SetData(samples, 0);

        return _generatedWarningClip;
    }

    private void StopWarningSound()
    {
        if (_warningSource == null)
            return;

        _warningSource.Stop();
        Destroy(_warningSource);
        _warningSource = null;
    }

    private void CreateZoneVisual()
    {
        GameObject lineObject = new GameObject("SentinelQSlowZone");
        lineObject.transform.SetParent(transform, false);
        lineObject.transform.localPosition = Vector3.up * 0.04f;

        _zoneLine = lineObject.AddComponent<LineRenderer>();
        _zoneLine.useWorldSpace = false;
        _zoneLine.loop = true;
        _zoneLine.positionCount = 64;
        _zoneLine.startWidth = 0.08f;
        _zoneLine.endWidth = 0.08f;
        _zoneLine.numCornerVertices = 3;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            _zoneMaterial = new Material(shader);
            _zoneMaterial.color = new Color(0.2f, 0.75f, 1f, 0.8f);
            _zoneLine.material = _zoneMaterial;
        }

        for (int i = 0; i < _zoneLine.positionCount; i++)
        {
            float angle =
                (Mathf.PI * 2f * i) /
                _zoneLine.positionCount;

            _zoneLine.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * _zoneRadius,
                    0f,
                    Mathf.Sin(angle) * _zoneRadius
                )
            );
        }
    }

    private int BuildDetectionMask()
    {
        int mask = _enemyLayer.value;

        IncludeLayer(ref mask, "Default");
        IncludeLayer(ref mask, "LocalPlayer");
        IncludeLayer(ref mask, "Enemy");
        IncludeLayer(ref mask, "Player");

        return mask;
    }

    private static void IncludeLayer(ref int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
            mask |= 1 << layer;
    }

    private void DestroyLocal()
    {
        if (_isDestroyed)
            return;

        _isDestroyed = true;

        RemoveLocalSlow();
        StopWarningSound();

        if (_zoneMaterial != null)
        {
            Destroy(_zoneMaterial);
            _zoneMaterial = null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        RemoveLocalSlow();
        StopWarningSound();

        if (_zoneMaterial != null)
            Destroy(_zoneMaterial);
    }
}
