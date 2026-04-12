using UnityEngine;

public class Slow : MonoBehaviour
{
    private Animator _animator;
    private Renderer _renderer;

    private float _originalAnimSpeed = 1.0f;
    private Color _originalColor;

    private int _swampLayerCount = 0;

    [SerializeField] private Color _slowColor = Color.green;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();

        if (_animator != null) _originalAnimSpeed = _animator.speed;
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    public void ApplySlow(float multiplier)
    {
        _swampLayerCount++;

        if (_swampLayerCount == 1)
        {
            if (_animator != null) _animator.speed = multiplier;
            if (_renderer != null) _renderer.material.color = _slowColor;
            Debug.Log($"{gameObject.name} 슬로우 적용");
        }
    }

    public void RemoveSlow()
    {
        _swampLayerCount--;

        if (_swampLayerCount <= 0)
        {
            _swampLayerCount = 0;
            RestoreOriginalState();
        }
    }

    private void RestoreOriginalState()
    {
        if (_animator != null) _animator.speed = 1.0f;
        if (_renderer != null) _renderer.material.color = _originalColor;
        Debug.Log($"{gameObject.name} 원래 상태로 복구!");
    }

    private void OnDisable()
    {
        _swampLayerCount = 0;
        RestoreOriginalState();
    }
}