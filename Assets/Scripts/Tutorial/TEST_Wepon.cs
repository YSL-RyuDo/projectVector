using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TESTWepon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera cam;
    [SerializeField] private TMP_Text ammoText;

    private int currentAmmo;
    private float nextFireTime;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (ammoText == null)
        {
            GameObject ammoObj = GameObject.Find("AmmoText");

            if (ammoObj != null)
                ammoText = ammoObj.GetComponent<TMP_Text>();
        }

        UpdateAmmoUI();
    }

    private void Update()
    {
        if (weaponData == null)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Fire();
        }

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }
    }

    private void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
            return;

        if (bulletPrefab == null ||
            firePoint == null ||
            cam == null)
            return;

        nextFireTime = Time.time + weaponData.fireRate;

        currentAmmo--;
        UpdateAmmoUI();

        Vector3 direction = cam.transform.forward;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        TutorialBullet bulletScript =
            bullet.GetComponent<TutorialBullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(
                weaponData.range,
                weaponData.muzzleVelocity,
                direction
            );
        }
    }

    private void Reload()
    {
        if (weaponData == null)
            return;

        currentAmmo = weaponData.magSize;

        UpdateAmmoUI();
    }

    public void Initialize(
        WeaponData data,
        int magAmmo,
        int reserve)
    {
        weaponData = data;

        currentAmmo = weaponData.magSize;

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null && weaponData != null)
        {
            ammoText.text =
                $"{currentAmmo} / {weaponData.magSize}";
        }
    }
}