using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyStatus : MonoBehaviour
{
    [SerializeField] private float _hp = 100f;
    [SerializeField] private bool _isSlowed = false;

    [SerializeField] private Material _normalMat;
    [SerializeField] private Material _slowMat;
    [SerializeField] private Material _concussionMat;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Slider _hpSlider;

    private Coroutine _currentRoutine;
    private Animator _animator;
    private Rigidbody _rb;

    public float Hp => _hp;
    public bool IsSlowed => _isSlowed;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();

        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_normalMat == null && _renderer != null) _normalMat = _renderer.material;
    }

    private void Update()
    {
        _hpSlider.value = _hp;
    }

    public void TakeDamage(float damage)
    {
        _hp -= damage;
        Debug.Log($"적 체력 감소: {_hp}");

        if (_hp <= 0)
        {
            Debug.Log("적 사망");
            //Destroy(gameObject);
        }
    }

    public void ApplySlow()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(slowRoutine(2f));
    }

    public void ApplyConcussion(float duration)
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(concussionRoutine(duration));
    }

    private IEnumerator slowRoutine(float duration)
    {
        _isSlowed = true;
        if (_renderer != null) _renderer.material = _slowMat;
        if (_animator != null) _animator.speed = 0.5f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_rb != null) _rb.linearVelocity *= 0.5f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        resetStatus();
    }

    private IEnumerator concussionRoutine(float duration)
    {
        _isSlowed = false;

        if (_renderer != null) _renderer.material = _concussionMat;
        if (_animator != null) _animator.speed = 0f;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        yield return new WaitForSeconds(duration);

        if (_rb != null) _rb.isKinematic = false;
        resetStatus();
    }

    private void resetStatus()
    {
        if (_animator != null) _animator.speed = 1f;
        _isSlowed = false;
        if (_renderer != null) _renderer.material = _normalMat;
        _currentRoutine = null;
    }
}