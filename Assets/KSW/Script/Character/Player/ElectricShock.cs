using UnityEngine;
using System.Collections;
using System.Net.NetworkInformation;
using static UnityEngine.Rendering.DebugUI;

public class ElectricShock : MonoBehaviour
{
    private Coroutine _shockCoroutine;

    [SerializeField] private Material _black;
    [SerializeField] private Material _yellow;

    [SerializeField] private Renderer _renderer;

    private Animator _animator;

    private float _damage = 0.01f;
    private float _hp = 100f;
    private int _shockCount = 0;

    // 외부 읽기 전용
    public float Hp => _hp;
    public int ShockCount => _shockCount;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void Shock(float duration)
    {
        if (_shockCoroutine != null) StopCoroutine(_shockCoroutine);

        _shockCoroutine = StartCoroutine(shockRoutine(duration));
    }

    private IEnumerator shockRoutine(float duration)
    {
        Debug.Log($"{gameObject.name} : 감전 시작");

        _shockCount++;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            Debug.Log($"{gameObject.name} Hp : {this._hp -= _damage}");
            _renderer.material = _yellow;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _renderer.material = _black;
        Debug.Log($"{gameObject.name} : 감전 종료");
        Debug.Log($"{gameObject.name} : 스택 {_shockCount}");
    }

    public void ApplyStun(float duration)
    {
        Debug.Log($"<color=red>[STUN]</color> {gameObject.name} : {duration}초 동안 기절");

        if (_shockCoroutine != null) StopCoroutine(_shockCoroutine);

        _shockCount = 0;

        _shockCoroutine = StartCoroutine(stunRoutine(duration));
    }

    private IEnumerator stunRoutine(float duration)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (_animator != null) _animator.speed = 0f;

        _renderer.material = _yellow;

        yield return new WaitForSeconds(duration);

        if (_animator != null) _animator.speed = 1f;

        Debug.Log($"<color=cyan>[STUN END]</color> {gameObject.name} : 기절 풀림");
        Debug.Log($"{gameObject.name} : 스택 {_shockCount}");

        _renderer.material = _black;
    }
}