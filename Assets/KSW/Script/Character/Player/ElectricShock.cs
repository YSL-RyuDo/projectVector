using System.Collections;
using UnityEngine;

public class ElectricShock : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _shockMaterial;

    [Header("Renderer")]
    [SerializeField] private Renderer _renderer;

    [Header("Visual")]
    [SerializeField] private float _defaultShockVisualDuration = 0.35f;
    [SerializeField] private ParticleSystem _shockEffect;
    private float _lastEffectTime = -1f;

    private Animator _animator;
    private Rigidbody _rb;

    private Coroutine _shockVisualCoroutine;
    private Coroutine _stunCoroutine;

    private int _shockCount;

    public int ShockCount => _shockCount;
    public bool IsStunned { get; private set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();

        if (_renderer == null)
            _renderer = GetComponentInChildren<Renderer>(true);

        if (_renderer != null && _normalMaterial == null)
            _normalMaterial = _renderer.material;
    }

    /*
     * 주의:
     * 이제 스택 판정은 서버가 한다.
     * 이 함수는 클라이언트 시각 효과용으로만 쓴다.
     * 여기서 _shockCount++ 하면 안 된다.
     */
    public void Shock(float duration)
    {
        PlayShockVisual(duration);
    }

    public void PlayShockVisual(float duration = -1f)
    {
        if (duration <= 0f)
            duration = _defaultShockVisualDuration;

        if (_shockVisualCoroutine != null)
            StopCoroutine(_shockVisualCoroutine);

        _shockVisualCoroutine = StartCoroutine(ShockVisualRoutine(duration));
    }

    private IEnumerator ShockVisualRoutine(float duration)
    {
        if (_shockEffect != null && Time.unscaledTime-_lastEffectTime>.08f)
        {
            _lastEffectTime=Time.unscaledTime;
            _shockEffect.Play();
        }
        Debug.Log($"[SHOCK VISUAL START] {gameObject.name}");

        if (_rb != null)
            _rb.linearVelocity = Vector3.zero;

        if (_renderer != null && _shockMaterial != null)
            _renderer.material = _shockMaterial;

        yield return new WaitForSeconds(duration);

        if (!IsStunned)
        {
            if (_renderer != null && _normalMaterial != null)
                _renderer.material = _normalMaterial;
        }

        Debug.Log($"[SHOCK VISUAL END] {gameObject.name}, stack={_shockCount}");
    }

    public void ApplyServerStatus(
        int shockStack,
        bool stunned,
        float stunSeconds)
    {
        _shockCount = Mathf.Clamp(shockStack, 0, 3);

        Debug.Log(
            $"[ELECTRIC STATUS] {gameObject.name}, " +
            $"stack={_shockCount}, stunned={stunned}, stunSeconds={stunSeconds}"
        );

        if (stunned)
        {
            ApplyStun(stunSeconds);
            return;
        }

        // 서버가 스택만 갱신한 경우
        if (_shockCount > 0)
        {
            PlayShockVisual(_defaultShockVisualDuration);
        }
    }

    public void ApplyStun(float duration)
    {
        if (duration <= 0f)
            return;

        Debug.Log($"<color=red>[STUN START]</color> {gameObject.name}, duration={duration}");

        if (_stunCoroutine != null)
            StopCoroutine(_stunCoroutine);

        _stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        IsStunned = true;

        // E 스킬이 스택을 소모했으므로 클라 표시도 0으로 맞춘다.
        _shockCount = 0;

        if (_shockVisualCoroutine != null)
        {
            StopCoroutine(_shockVisualCoroutine);
            _shockVisualCoroutine = null;
        }

        if (_rb != null)
            _rb.linearVelocity = Vector3.zero;

        if (_animator != null)
            _animator.speed = 0f;

        if (_renderer != null && _shockMaterial != null)
            _renderer.material = _shockMaterial;

        yield return new WaitForSeconds(duration);

        IsStunned = false;

        if (_animator != null)
            _animator.speed = 1f;

        if (_renderer != null && _normalMaterial != null)
            _renderer.material = _normalMaterial;

        Debug.Log($"<color=cyan>[STUN END]</color> {gameObject.name}");

        _stunCoroutine = null;
    }

    public void ClearElectricState()
    {
        if (_shockEffect != null) _shockEffect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        _lastEffectTime=-1f;
        _shockCount = 0;
        IsStunned = false;

        if (_shockVisualCoroutine != null)
        {
            StopCoroutine(_shockVisualCoroutine);
            _shockVisualCoroutine = null;
        }

        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
            _stunCoroutine = null;
        }

        if (_animator != null)
            _animator.speed = 1f;

        if (_renderer != null && _normalMaterial != null)
            _renderer.material = _normalMaterial;
    }

    private void OnDisable() { ClearElectricState(); }
}
