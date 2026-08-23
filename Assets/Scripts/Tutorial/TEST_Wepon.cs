using UnityEngine;
using UnityEngine.InputSystem;

public class TESTWepon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera cam;

    private int currentAmmo;
    private float nextFireTime;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (weaponData != null)
            currentAmmo = weaponData.magSize;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (weaponData == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Fire();
        }
    }

    private void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
            return;

        if (bulletPrefab == null || firePoint == null || cam == null)
            return;

        nextFireTime = Time.time + weaponData.fireRate;
        currentAmmo--;

        Vector3 direction = cam.transform.forward;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(
                null,
                weaponData.range,
                weaponData.muzzleVelocity,
                direction,
                false
            );
        }
    }

    public void Initialize(
        WeaponData data,
        int magAmmo)
    {
        weaponData = data;
        currentAmmo = magAmmo;
    }
}