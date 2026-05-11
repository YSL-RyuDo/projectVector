using UnityEngine;
using System.Collections;

public class EntrySkill_E : CharacterSkill
{
    [SerializeField] private float _duration = 5f;
    [SerializeField] private float _speedMultiplier = 1.5f;
    [SerializeField] private float _detectionRange = 3f;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private LayerMask _enemyLayer;

    private PlayerMovement _movement;

    private bool _isStealth = false;
    private float _originalSpeed;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !_isStealth)
        {
            UseSkill();
        }

        if (_isStealth)
        {
            checkDetection();
            checkOtherActions();
        }
    }

    protected override void UseSkill()
    {
        StartCoroutine(activateStealth());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }

    private IEnumerator activateStealth()
    {
        if (_movement == null) yield break;

        _isStealth = true;

        if (_characterModel != null) _characterModel.SetActive(false);

        _originalSpeed = _movement.Speed;
        _movement.Speed = _originalSpeed * _speedMultiplier;

        yield return new WaitForSeconds(_duration);

        if (_isStealth) endStealth();
    }

    private void checkDetection()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, _detectionRange, _enemyLayer);

        if (enemies.Length > 0)
        {
            endStealth();
        }
    }

    private void checkOtherActions()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.C))
        {
            endStealth();
        }
    }

    private void endStealth()
    {
        if (!_isStealth) return;

        StopAllCoroutines();
        _isStealth = false;

        if (_characterModel != null) _characterModel.SetActive(true);
        if (_movement != null) _movement.Speed = _originalSpeed;
    }
}