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
    [SerializeField] private Transform dropPoint;

    private PlayerHP ownerHP;
    private bool isLocalPlayer;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isReloading;
    private bool isADS;

    void Awake()
    {
        ownerHP = GetComponentInParent<PlayerHP>();
        isLocalPlayer = GetComponentInParent<PlayerMovement>() != null;

        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        cameraLook = cam.GetComponent<FPSCameraLook>();
        weaponADS = GetComponent<WeaponADS>();

        ammoText = FindFirstObjectByType<TMP_Text>();
    }

    void Start()
    {
        if (!isLocalPlayer) return;

        currentAmmo = weaponData.magSize;
        reserveAmmo = weaponData.maxReserveAmmo;

        cam.fieldOfView = weaponData.normalFOV;
        UpdateAmmoUI();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (ownerHP != null && ownerHP.IsDead)
            return;

        HandleADS();

        if (isReloading)
            return;

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

        if (ownerHP != null && ownerHP.IsDead)
            return;

        nextFireTime = Time.time + weaponData.fireRate;
        currentAmmo--;
        UpdateAmmoUI();

        cameraLook?.AddRecoil(weaponData.recoil);

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        Debug.DrawRay(
            ray.origin,
            ray.direction * weaponData.range,
            Color.red,
            1.0f
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            Debug.Log("[HIT] Raycast hit nothing");
            return;
        }

        Debug.Log($"[HIT] Collider = {hit.collider.name}");

        var hp = hit.collider.GetComponentInParent<PlayerHP>();
        if (hp == null)
        {
            Debug.Log("[HIT] PlayerHP 없음 (플레이어 아님)");
            return;
        }

        if (hp == ownerHP)
        {
            Debug.Log("[HIT] Self hit ignored");
            return;
        }

        if (hp.IsDead)
        {
            Debug.Log("[HIT] Target already dead");
            return;
        }

        var identity = hp.GetComponent<PlayerIdentity>();
        if (identity == null)
        {
            Debug.Log("[HIT] PlayerIdentity 없음");
            return;
        }

        Debug.Log($"[HIT] SEND → targetId={identity.PlayerId}");

        //NetworkManager.Instance.SendHit(
        //    identity.PlayerId,
        //    20
        //);
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

    public void ReloadFull()
    {
        currentAmmo = weaponData.magSize;
        reserveAmmo = weaponData.maxReserveAmmo;
        UpdateAmmoUI();
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
        if (dropPoint == null || weaponData == null)
        {
            Debug.LogWarning("[SimplePistol] Drop failed: missing data");
            return;
        }

        GameObject dropped = Instantiate(
            weaponData.dropPrefab,
            dropPoint.position,
            Quaternion.identity
        );

        DroppedWeapon dw = dropped.GetComponent<DroppedWeapon>();
        if (dw != null)
        {
            dw.weaponData = weaponData;
            dw.magAmmo = currentAmmo;
            dw.reserveAmmo = reserveAmmo;
        }

        Destroy(gameObject);
    }

}
