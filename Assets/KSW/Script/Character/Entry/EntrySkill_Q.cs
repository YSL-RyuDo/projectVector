using UnityEngine;

public class EntrySkill_Q : CharacterSkill
{
    [SerializeField] private GameObject _smokeGrenadePrefab;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _throwForce = 25f;

    void Start()
    {
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseSkill();
        }
    }

    protected override void UseSkill()
    {
        if (_smokeGrenadePrefab == null || _throwPoint == null || _playerCamera == null) return;

        GameObject grenade = Instantiate(_smokeGrenadePrefab, _throwPoint.position, _playerCamera.transform.rotation);

        Rigidbody rb = grenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 throwDirection = _playerCamera.transform.forward;
            rb.AddForce(throwDirection * _throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }
}