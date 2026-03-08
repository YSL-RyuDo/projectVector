using UnityEngine;
using System.Collections;
using System.Net.NetworkInformation;

public class ElectricShock : MonoBehaviour
{
    private Coroutine _shockCoroutine;

    [SerializeField] private Material _black;
    [SerializeField] private Material _yellow;

    [SerializeField] private Renderer _renderer;

    private int _shockCount = 0;
    private float _damage = 0.01f;
    private float _hp = 100f;

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
}