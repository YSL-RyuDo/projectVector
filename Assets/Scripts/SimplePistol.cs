using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class SimplePistol : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponData weaponData;

    [Header("UI")]
    private TMP_Text ammoText;

    [Header("Camera")]
    private Camera cam;
    private FPSCameraLook cameraLook;
    private WeaponADS weaponADS;

    [Header("Drop")]
    public Transform dropPoint;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isReloading;
    private bool isADS;

    void Awake()
    {
        cam = Camera.main;
        cameraLook = cam.GetComponent<FPSCameraLook>();
        weaponADS = GetComponent<WeaponADS>();

        ammoText = FindFirstObjectByType<TMP_Text>();
    }

    void Start()
    {
        currentAmmo = weaponData.magSize;
        reserveAmmo = weaponData.maxReserveAmmo;

        cam.fieldOfView = weaponData.normalFOV;
        UpdateAmmoUI();
    }

    void Update()
    {
        HandleADS();

        if (isReloading)
            return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropWeapon();
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryFire();
        }
    }

    void HandleADS()
    {
        bool adsInput = Mouse.current.rightButton.isPressed;

        if (adsInput != isADS)
        {
            isADS = adsInput;
            cameraLook?.SetADS(isADS);
            weaponADS?.SetADS(isADS);
        }

        float targetFOV = isADS ? weaponData.adsFOV : weaponData.normalFOV;
        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            weaponData.adsSpeed * Time.deltaTime
        );
    }

    void TryFire()
    {
        if (Time.time < nextFireTime || currentAmmo <= 0)
            return;

        nextFireTime = Time.time + weaponData.fireRate;
        currentAmmo--;
        UpdateAmmoUI();

        cameraLook?.AddRecoil(weaponData.recoil);

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        Debug.DrawRay(ray.origin, ray.direction * weaponData.range, Color.green, 1.0f);

        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            Debug.Log($"Hit: {hit.collider.name}");
        }
    }


    IEnumerator Reload()
    {
        if (currentAmmo == weaponData.magSize || reserveAmmo <= 0)
            yield break;

        isReloading = true;
        ammoText.text = "Reloading...";

        yield return new WaitForSeconds(weaponData.reloadTime);

        int need = weaponData.magSize - currentAmmo;
        int reloadAmount = Mathf.Min(need, reserveAmmo);

        currentAmmo += reloadAmount;
        reserveAmmo -= reloadAmount;

        isReloading = false;
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText == null)
            return;

        ammoText.text = $"{currentAmmo} / {reserveAmmo}";
    }

    public void SetAmmo(int mag, int reserve)
    {
        currentAmmo = mag;
        reserveAmmo = reserve;
        UpdateAmmoUI();
    }

    public void SetDropPoint(Transform point)
    {
        dropPoint = point;
    }

    public void DropWeapon()
    {
        GameObject dropped = Instantiate(
            weaponData.dropPrefab,
            dropPoint.position,
            Quaternion.identity
        );

        DroppedWeapon dw = dropped.GetComponent<DroppedWeapon>();
        dw.weaponData = weaponData;
        dw.magAmmo = currentAmmo;
        dw.reserveAmmo = reserveAmmo;

        Destroy(gameObject);
    }
}
