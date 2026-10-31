using UnityEngine;

public class Slow : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color _slowColor = Color.green;

    private PlayerMovement _movement;
    private Animator _animator;
    private Renderer _renderer;

    private float _originalAnimSpeed = 1.0f;
    private Color _originalColor = Color.white;

    private int _slowCount = 0;
    private float _currentMultiplier = 1.0f;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();

        if (_movement == null)
            _movement = GetComponentInChildren<PlayerMovement>(true);

        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        _renderer = GetComponentInChildren<Renderer>(true);

        if (_animator != null)
            _originalAnimSpeed = _animator.speed;

        if (_renderer != null && _renderer.material != null)
        {
            if (_renderer.material.HasProperty("_BaseColor"))
                _originalColor = _renderer.material.GetColor("_BaseColor");
            else if (_renderer.material.HasProperty("_Color"))
                _originalColor = _renderer.material.color;
        }
    }

    public void ApplySlow(float multiplier)
    {
        _slowCount++;

        _currentMultiplier =
            Mathf.Clamp(
                multiplier,
                0.05f,
                1.0f
            );

        ApplySlowState();

        Debug.Log(
            $"[SLOW APPLY] object={gameObject.name}, count={_slowCount}, multiplier={_currentMultiplier}"
        );
    }

    public void RemoveSlow()
    {
        _slowCount--;

        if (_slowCount < 0)
            _slowCount = 0;

        if (_slowCount <= 0)
        {
            RestoreOriginalState();
            return;
        }

        ApplySlowState();

        Debug.Log(
            $"[SLOW REMOVE] object={gameObject.name}, remain={_slowCount}"
        );
    }

    private void ApplySlowState()
    {
        if (_movement != null)
        {
            _movement.SetExternalSpeedMultiplier(
                _currentMultiplier
            );
        }

        if (_animator != null)
        {
            _animator.speed =
                _originalAnimSpeed * _currentMultiplier;
        }

        if (_renderer != null && _renderer.material != null)
        {
            if (_renderer.material.HasProperty("_BaseColor"))
            {
                _renderer.material.SetColor(
                    "_BaseColor",
                    _slowColor
                );
            }
            else if (_renderer.material.HasProperty("_Color"))
            {
                _renderer.material.color =
                    _slowColor;
            }
        }
    }

    private void RestoreOriginalState()
    {
        _currentMultiplier = 1.0f;

        if (_movement != null)
        {
            _movement.SetExternalSpeedMultiplier(
                1.0f
            );
        }

        if (_animator != null)
        {
            _animator.speed =
                _originalAnimSpeed;
        }

        if (_renderer != null && _renderer.material != null)
        {
            if (_renderer.material.HasProperty("_BaseColor"))
            {
                _renderer.material.SetColor(
                    "_BaseColor",
                    _originalColor
                );
            }
            else if (_renderer.material.HasProperty("_Color"))
            {
                _renderer.material.color =
                    _originalColor;
            }
        }

        Debug.Log(
            $"[SLOW RESTORE] object={gameObject.name}"
        );
    }

    private void OnDisable()
    {
        _slowCount = 0;
        RestoreOriginalState();
    }

    private void OnDestroy()
    {
        _slowCount = 0;
        RestoreOriginalState();
    }
}