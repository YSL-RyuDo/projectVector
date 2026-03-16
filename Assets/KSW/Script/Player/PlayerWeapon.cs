using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [Header("References")]
    // 무기가 장착될 위치
    [SerializeField] private Transform _weaponHolder;
    // 무기 드롭 시 생성될 위치
    [SerializeField] private Transform _dropPoint;

    [Header("Pickup")]
    // 무기 줍기 가능한 반경
    [SerializeField] private float _pickupRange = 2.5f;
    // 무기 오브젝트만 감지하기 위한 레이어
    [SerializeField] private LayerMask _weaponLayer;

    [Header("Start Weapon")]
    // 게임 시작 시 지급할 무기 데이터
    [SerializeField] private WeaponData _startWeaponData;

    // 현재 장착 중인 무기 오브젝트
    private GameObject _currentWeapon;

    private void Start()
    {
        SpawnStartWeapon();
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupWeapon();
        }
    }

    public void SpawnStartWeapon()
    {
        if (_startWeaponData == null)
            return;

        // 기존 무기 제거
        for (int i = _weaponHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(_weaponHolder.GetChild(i).gameObject);
        }

        // 시작 무기 생성
        _currentWeapon = Instantiate(_startWeaponData.weaponPrefab, _weaponHolder);

        // 위치/회전 초기화
        _currentWeapon.transform.localPosition = Vector3.zero;
        _currentWeapon.transform.localRotation = Quaternion.identity;

        // 탄약 세팅
        var pistol = _currentWeapon.GetComponent<Weapon>();
        pistol.SetAmmo(_startWeaponData.magSize, _startWeaponData.maxReserveAmmo);
        // 드롭 위치 설정
        pistol.SetDropPoint(_dropPoint);
    }

    private void TryPickupWeapon()
    {
        // 주변에서 무기 레이어에 해당하는 콜라이더 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRange, _weaponLayer);

        DroppedWeapon nearest = null;
        float minDist = float.MaxValue;

        // 가장 가까운 무기 찾기
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

        // 가까운 무기가 있으면 줍기
        if (nearest != null)
        {
            PickupWeapon(nearest);
        }
    }

    private void PickupWeapon(DroppedWeapon dropped)
    {
        // 이미 무기를 들고 있다면 기존 무기 드롭
        if (_currentWeapon != null)
        {
            Weapon old = _currentWeapon.GetComponent<Weapon>();
            old?.DropWeapon();
        }

        // 새로운 무기 생성
        _currentWeapon = Instantiate(dropped.weaponData.weaponPrefab, _weaponHolder);

        // 탄약 정보 복원
        _currentWeapon.transform.localPosition = Vector3.zero;
        _currentWeapon.transform.localRotation = Quaternion.identity;

        Weapon pistol = _currentWeapon.GetComponent<Weapon>();
        pistol.SetAmmo(dropped.magAmmo, dropped.reserveAmmo);
        pistol.SetDropPoint(_dropPoint);

        // 바닥에 있던 무기 제거
        Destroy(dropped.gameObject);
    }

    // 씬에서 선택했을 때 픽업 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _pickupRange);
    }
}