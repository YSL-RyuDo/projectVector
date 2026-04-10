using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private Transform _dropPoint;

    [Header("Pickup")]
    [SerializeField] private float _pickupRange = 2.5f;
    [SerializeField] private LayerMask _weaponLayer;

    [Header("Start Weapons")]
    [SerializeField] private WeaponData _startPrimary; // (선택) 주무기
    [SerializeField] private WeaponData _startPistol;  // 피스톨
    [SerializeField] private WeaponData _startKnife;   // 칼

    // 슬롯 시스템
    private GameObject[] _weapons = new GameObject[3]; // 0: 주무기, 1: 피스톨, 2: 근접
    private int _currentSlot = 0;

    private void Start()
    {
        SpawnStartWeapon();
    }

    private void Update()
    {
        // 무기 줍기
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupWeapon();
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropCurrentWeapon();
        }

        // 무기 전환
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SwitchWeapon(0);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SwitchWeapon(1);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SwitchWeapon(2);
    }

    // =========================
    // 시작 무기
    // =========================
    public void SpawnStartWeapon()
    {
        // 1. 피스톨 장착 (슬롯 1)
        if (_startPistol != null)
        {
            EquipToSlot(
                1,
                _startPistol,
                _startPistol.magSize,
                _startPistol.maxReserveAmmo
            );
        }

        // 2. 칼 장착 (슬롯 2)
        if (_startKnife != null)
        {
            EquipToSlot(
                2,
                _startKnife,
                0,
                0
            );
        }

        // 3. (선택) 주무기
        if (_startPrimary != null)
        {
            EquipToSlot(
                0,
                _startPrimary,
                _startPrimary.magSize,
                _startPrimary.maxReserveAmmo
            );
        }

        //  핵심: 시작은 피스톨
        SwitchWeapon(1);
    }

    // =========================
    // 슬롯 판별
    // =========================
    int GetSlotIndex(WeaponData data)
    {
        switch (data.weaponType)
        {
            case WeaponData.WeaponType.Pistol:
                return 1;

            case WeaponData.WeaponType.AR:
            case WeaponData.WeaponType.SMG:
            case WeaponData.WeaponType.LMG:
            case WeaponData.WeaponType.SR:
            case WeaponData.WeaponType.DMR:
                return 0;

            case WeaponData.WeaponType.Shotgun:
                return 2; // 근접 대신 사용 (Knife 만들면 바꾸기)

            default:
                return 0;
        }
    }

    // =========================
    // 무기 장착
    // =========================
    void EquipToSlot(int slot, WeaponData data, int mag, int reserve)
    {
        // 기존 무기 드랍
        if (_weapons[slot] != null)
        {
            Weapon old = _weapons[slot].GetComponent<Weapon>();
            old?.DropWeapon();
            Destroy(_weapons[slot]);
        }

        // 새 무기 생성
        GameObject obj = Instantiate(data.weaponPrefab, _weaponHolder);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Weapon weapon = obj.GetComponent<Weapon>();
        weapon.SetAmmo(mag, reserve);
        weapon.SetDropPoint(_dropPoint);

        _weapons[slot] = obj;

        // 바로 장착
        SwitchWeapon(slot);
    }

    // =========================
    // 무기 전환
    // =========================
    void SwitchWeapon(int slot)
    {
        _currentSlot = slot;

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                _weapons[i].SetActive(i == slot);
            }
        }
    }

    // =========================
    // 무기 줍기
    // =========================
    private void TryPickupWeapon()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _pickupRange,
            _weaponLayer
        );

        DroppedWeapon nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            DroppedWeapon dw = hit.GetComponent<DroppedWeapon>();
            if (dw == null)
                continue;

            float dist = Vector3.Distance(transform.position, dw.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = dw;
            }
        }

        if (nearest != null)
        {
            PickupWeapon(nearest);
        }
    }

    private void PickupWeapon(DroppedWeapon dropped)
    {
        int slot = GetSlotIndex(dropped.weaponData);

        EquipToSlot(
            slot,
            dropped.weaponData,
            dropped.magAmmo,
            dropped.reserveAmmo
        );

        Destroy(dropped.gameObject);
    }

    // =========================
    // 디버그용
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _pickupRange);
    }

    void DropCurrentWeapon()
    {
        if (_currentSlot == 2)
            return;

        GameObject current = _weapons[_currentSlot];

        if (current == null)
            return;

        Weapon weapon = current.GetComponent<Weapon>();

        // 무기 드랍 실행
        weapon?.DropWeapon();

        // 슬롯 비우기
        Destroy(current);
        _weapons[_currentSlot] = null;

        //  다음 무기로 자동 전환 (우선순위)
        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                SwitchWeapon(i);
                return;
            }
        }

        // 모든 무기 없으면 아무것도 안 들기
        _currentSlot = -1;
    }

    public void SetWeaponVisibility(bool visible)
    {
        // 현재 들고 있는 슬롯의 무기만 껐다 켬
        if (_currentSlot != -1 && _weapons[_currentSlot] != null)
        {
            _weapons[_currentSlot].SetActive(visible);
        }
    }

    public int GetCurrentSlotIndex()
    {
        return _currentSlot;
    }
}