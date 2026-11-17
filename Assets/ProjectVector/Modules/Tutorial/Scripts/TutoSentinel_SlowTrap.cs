
using System.Collections.Generic;
using UnityEngine;

public class TutoSentinel_SlowTrap : MonoBehaviour
{
    [SerializeField] private float _health = 40f;
    [SerializeField] private float _slowRadius = 10f;
    [SerializeField] private float _slowAmount = 0.5f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _warningDistance = 12f;

    [SerializeField] private float _zoneRadius = 5f;
    [SerializeField] private float _zoneDuration = 5f;

    private static AudioClip _generatedWarningClip;

    private bool _isActive;
    private bool _activationRequested;
    private bool _isDestroyed;

    private int _detectionMask;
    private float _remainingDuration;

    private AudioSource _warningSource;
    private LineRenderer _zoneLine;
    private Material _zoneMaterial;

    private readonly HashSet<TutoEnemyMove> _slowedEnemies =
        new HashSet<TutoEnemyMove>();

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

        SetupEnemyWarningSound();
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

        _remainingDuration -= Time.deltaTime;

        if (_remainingDuration <= 0f)
            DestroyLocal();
    }

    private void ActivateLocal(
        float duration,
        float slowMultiplier)
    {
        if (_isDestroyed || _isActive)
            return;

        _isActive = true;
        _remainingDuration = duration;
        _slowAmount = Mathf.Clamp(slowMultiplier, 0.05f, 1f);

        StopWarningSound();
        CreateZoneVisual();
        UpdateLocalSlow();
    }

    private void TryRequestActivation()
    {
        if (_activationRequested)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _slowRadius,
            _detectionMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            TutoEnemyMove enemy =
                hit.GetComponentInParent<TutoEnemyMove>();

            if (enemy == null || enemy.IsDead)
                continue;

            _activationRequested = true;

            ActivateLocal(_zoneDuration, _slowAmount);

            return;
        }
    }

    private void UpdateLocalSlow()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _zoneRadius,
            _detectionMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<TutoEnemyMove> insideEnemies =
            new HashSet<TutoEnemyMove>();

        foreach (Collider hit in hits)
        {
            TutoEnemyMove enemy =
                hit.GetComponentInParent<TutoEnemyMove>();

            if (enemy == null || enemy.IsDead)
                continue;

            insideEnemies.Add(enemy);

            if (!_slowedEnemies.Contains(enemy))
            {
                enemy.ApplyTrapSlow(GetInstanceID(), _slowAmount);
                _slowedEnemies.Add(enemy);
            }
        }

        foreach (TutoEnemyMove enemy in
                 new List<TutoEnemyMove>(_slowedEnemies))
        {
            if (!insideEnemies.Contains(enemy))
            {
                if (enemy != null)
                    enemy.RemoveTrapSlow(GetInstanceID());

                _slowedEnemies.Remove(enemy);
            }
        }
    }

    private void RemoveLocalSlow()
    {
        foreach (TutoEnemyMove enemy in _slowedEnemies)
        {
            if (enemy != null)
                enemy.RemoveTrapSlow(GetInstanceID());
        }

        _slowedEnemies.Clear();
    }

    private void SetupEnemyWarningSound()
    {
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

    public void Damage(float damage)
    {
        if (_isDestroyed)
            return;

        _health -= damage;

        if (_health <= 0f)
            DestroyLocal();
    }
}
