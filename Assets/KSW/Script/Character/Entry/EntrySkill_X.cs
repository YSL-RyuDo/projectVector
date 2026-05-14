using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class EntrySkill_X : CharacterSkill
{
    [Header("Settings")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private int _maxClawCount = 6;
    [SerializeField] private float _burstInterval = 0.05f;
    [SerializeField] private LayerMask _targetLayer;

    private Weapon _currentWeapon;
    private int _currentClawCount = 0;
    private bool _isSkillActive = false;
    private bool _isBurstFiring = false;
    private Camera _mainCam;

    private CinemachineImpulseSource _impulseSource;
    private int _skillShotIndex = 0;

    void Start()
    {
        _mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && !_isSkillActive)
        {
            UseSkill();
        }

        if (_isSkillActive && !_isBurstFiring)
        {
            if (Input.GetMouseButtonDown(0))
            {
                PerformRaycastShot(1);
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (_currentClawCount > 0)
                {
                    StartCoroutine(BurstFireRoutine());
                }
            }
        }
    }

    protected override void UseSkill()
    {
        StartCoroutine(ActivateUltimate());
    }

    private IEnumerator ActivateUltimate()
    {
        _currentWeapon = GetComponentInChildren<Weapon>();

        if (_currentWeapon != null)
        {
            _currentWeapon.CanFire = false;
            _impulseSource = _currentWeapon.GetComponentInParent<CinemachineImpulseSource>();
        }

        _isSkillActive = true;
        _currentClawCount = _maxClawCount;
        _skillShotIndex = 0;

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator BurstFireRoutine()
    {
        _isBurstFiring = true;

        yield return new WaitForSeconds(0.2f);

        int shotsToFire = _currentClawCount;
        for (int i = 0; i < shotsToFire; i++)
        {
            PerformRaycastShot(1);
            yield return new WaitForSeconds(_burstInterval);
        }

        _isBurstFiring = false;
    }

    private void PerformRaycastShot(int count)
    {
        if (_currentClawCount <= 0) return;

        for (int i = 0; i < count; i++)
        {
            ApplyPhysicalFeedback();

            Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _targetLayer))
            {
                EnemyStatus enemy = hit.collider.GetComponent<EnemyStatus>() ?? hit.collider.GetComponentInParent<EnemyStatus>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                }
            }

            _currentClawCount--;
            _skillShotIndex++;

            if (_currentClawCount <= 0)
            {
                EndUltimate();
                break;
            }
        }
    }

    private void ApplyPhysicalFeedback()
    {
        if (_currentWeapon == null || _currentWeapon.WeaponData == null) return;

        WeaponData data = _currentWeapon.WeaponData;
        WeaponRecoil recoil = _currentWeapon.RecoilSystem;

        if (recoil != null && data.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(_skillShotIndex, data.recoilPattern.Length - 1);
            float multiplier = _currentWeapon.IsADS ? data.adsRecoilMultiplier : data.recoilMultiplier;

            recoil.FireRecoil(data.recoilPattern[index], multiplier);

            if (_impulseSource != null)
            {
                _impulseSource.DefaultVelocity.y = _currentWeapon.IsADS ? 0f : 0.1f;
                _impulseSource.GenerateImpulse();
            }
        }
    }

    private void EndUltimate()
    {
        _isSkillActive = false;
        _isBurstFiring = false;
        _skillShotIndex = 0;

        if (_currentWeapon != null)
        {
            _currentWeapon.CanFire = true;
            _currentWeapon.RecoilSystem?.StopRecoil();
        }
    }
}