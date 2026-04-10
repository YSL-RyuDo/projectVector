using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using Unity.Cinemachine;

public class Weapon : Item
{
    [Header("References")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private WeaponRecoil recoilSystem;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private TMP_Text ammoText;

    [Header("Camera")]
    private CinemachineCamera vCam;
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

    [Header("Recoil Logic")]
    private int currentShotIndex = 0;
    private CinemachineImpulseSource impulseSource;

    private IFireStrategy fireStrategy;

    void Awake()
    {
        ownerHP = GetComponentInParent<PlayerHP>();
        isLocalPlayer = GetComponentInParent<LocalPlayerController>() != null;

        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        vCam = FindFirstObjectByType<CinemachineCamera>();
        cameraLook = cam.GetComponentInParent<FPSCameraLook>();
        weaponADS = GetComponent<WeaponADS>();

        ammoText = FindFirstObjectByType<TMP_Text>();

        if (recoilSystem == null) recoilSystem = GetComponentInParent<WeaponRecoil>();

        impulseSource = GetComponentInParent<CinemachineImpulseSource>();

        SetupStrategy();
    }

    public void Initialize(WeaponData data, Transform dp, int mag, int reserve)
    {
        weaponData = data;
        dropPoint = dp;
        currentAmmo = mag;
        reserveAmmo = reserve;

        UpdateAmmoUI();

        if (cam != null)
            cam.fieldOfView = weaponData.normalFOV;

        if (vCam != null)
            vCam.Lens.FieldOfView = weaponData.normalFOV;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (ownerHP != null && ownerHP.IsDead) return;

        HandleADS();

        if (isReloading) return;

        bool isFiringInput = IsAutomatic()
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (isFiringInput)
        {
            OnUse();
        }
        else
        {
            // 사격 중지 시 인덱스 초기화
            currentShotIndex = 0;
            recoilSystem?.StopRecoil();
        }

    }

    void OnEnable()
    {
        UpdateAmmoUI();
    }

    void OnDisable()
    {
        isReloading = false;
        StopAllCoroutines();
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

        if (vCam != null)
        {
            // [핵심 수정] cam.fieldOfView 대신 vCam.Lens.FieldOfView를 수정합니다.
            float targetFOV = isADS ? weaponData.adsFOV : weaponData.normalFOV;

            vCam.Lens.FieldOfView = Mathf.Lerp(
                vCam.Lens.FieldOfView,
                targetFOV,
                weaponData.adsSpeed * Time.deltaTime
            );
        }
    }

    public override void OnUse()
    {
        base.OnUse();

        if (Time.time < nextFireTime || currentAmmo <= 0)
            return;

        if (ownerHP != null && ownerHP.IsDead)
            return;

        nextFireTime = Time.time + weaponData.fireRate;

        currentAmmo--;
        UpdateAmmoUI();

        // [반동 호출] 분리된 스크립트로 패턴 전달
        if (recoilSystem != null && weaponData.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(currentShotIndex, weaponData.recoilPattern.Length - 1);
            recoilSystem.FireRecoil(weaponData.recoilPattern[index], weaponData.recoilMultiplier);
            impulseSource.GenerateImpulse();
        }

        fireStrategy?.Fire();

        currentShotIndex++;
    }

    public override void OnDrop()
    {
        DropWeapon();
    }

    public void FireRaycast()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        Vector3 dir = GetSpreadDirection(ray.direction);

        ray = new Ray(ray.origin, dir);

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

        Instantiate(
        weaponData.bulletHolePrefab,
        hit.point + hit.normal * 0.01f,
        Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180f, 0)
        );

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
        //    weaponData.damage
        //);
    }

    public void FireProjectile()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        // Raycast와 동일한 방향 계산
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 1000f;
        }

        Vector3 baseDir = (targetPoint - firePoint.position).normalized;

        // 탄퍼짐 적용
        Vector3 shootDir = GetSpreadDirection(baseDir);

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDir)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Init(
                ownerHP,
                weaponData.damage,
                weaponData.range,
                weaponData.muzzleVelocity,
                shootDir      // 방향 전달
            );
        }
    }

    void SetupStrategy()
    {
        switch (weaponData.weaponType)
        {
            case WeaponData.WeaponType.Pistol:
            case WeaponData.WeaponType.SR:
                fireStrategy = new RaycastFire(this);
                break;

            case WeaponData.WeaponType.AR:
            case WeaponData.WeaponType.SMG:
            case WeaponData.WeaponType.LMG:
                fireStrategy = new ProjectileFire(this);
                break;
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
            Debug.LogWarning("[Weapon] Drop failed: missing data");
            return;
        }

        GameObject dropped = Instantiate(
            weaponData.dropPrefab,
            dropPoint.position,
            Quaternion.identity
        );

        DroppedItem dw = dropped.GetComponent<DroppedItem>();
        if (dw != null)
        {
            dw.weaponData = weaponData;
            dw.magAmmo = currentAmmo;
            dw.reserveAmmo = reserveAmmo;
        }

        dropped.GetComponent<Rigidbody>()?.AddForce(transform.forward * 1f, ForceMode.Impulse);

        Destroy(gameObject);
    }

    bool IsAutomatic()
    {
        return weaponData.weaponType == WeaponData.WeaponType.AR ||
               weaponData.weaponType == WeaponData.WeaponType.SMG ||
               weaponData.weaponType == WeaponData.WeaponType.LMG;
    }

    bool IsMoving()
    {
        if (Keyboard.current == null)
            return false;

        return
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed;
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float spread = isADS ? weaponData.adsSpread : weaponData.hipFireSpread;

        if (IsMoving())
        {
            spread *= weaponData.moveSpreadMultiplier;
        }

        Vector2 randomCircle = Random.insideUnitCircle * spread;

        Vector3 spreadDir =
            baseDirection +
            cam.transform.right * randomCircle.x +
            cam.transform.up * randomCircle.y;

        return spreadDir.normalized;
    }

    public WeaponData.WeaponType GetWeaponType()
    {
        return weaponData.weaponType;
    }
}