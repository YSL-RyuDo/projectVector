using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponData weaponData;

    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

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

    [Header("Recoil Logic")]
    private int currentShotIndex = 0;
    private int shotsInCurrentSway = 0;

    [Header("Visual Kick States")]
    // 카메라가 일시적으로 돌아갈 목표 위치와 현재 위치
    private Vector2 targetVisualKick;
    private Vector2 currentVisualKick;
    private Vector2 lastKickApplied;

    [Header("Settings")]
    [SerializeField] private float snappiness = 20f;
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private float kickMultiplier = 0.5f;

    [Header("Movement Spread")]
    [SerializeField] private float moveSpreadMultiplier = 2.5f;

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
        cameraLook = cam.GetComponent<FPSCameraLook>();
        weaponADS = GetComponent<WeaponADS>();

        ammoText = FindFirstObjectByType<TMP_Text>();
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

        bool isFiringInput = IsAutomatic() ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
        bool canFire = isFiringInput && currentAmmo > 0 && !isReloading;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (canFire)
        {
            TryFire();
        }
        else
        {
            // 탄이 다 떨어졌거나 마우스를 뗐을 때 회복 모드
            StopFiring();
        }

        ApplyVisualKickOnly();
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

        float targetFOV = isADS ? weaponData.adsFOV : weaponData.normalFOV;
        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            weaponData.adsSpeed * Time.deltaTime
        );
    }

    void StopFiring()
    {
        currentShotIndex = 0;
        shotsInCurrentSway = 0;

        // 사격 중지 시 카메라 목표 오프셋을 0으로 서서히 되돌림
        targetVisualKick = Vector2.Lerp(targetVisualKick, Vector2.zero, returnSpeed * Time.deltaTime);
    }

    void ApplyVisualKickOnly()
    {
        // 1. 현재 킥 값을 목표값으로 부드럽게 이동 (Snappiness)
        currentVisualKick = Vector2.Lerp(currentVisualKick, targetVisualKick, snappiness * Time.deltaTime);

        // 2. 지난 프레임과의 차이(Delta)를 구함
        Vector2 kickDelta = currentVisualKick - lastKickApplied;
        lastKickApplied = currentVisualKick;

        // 3. 카메라에 델타만큼만 회전 적용
        if (cameraLook != null)
        {
            cameraLook.AddYaw(kickDelta.x);
            cameraLook.AddPitch(-kickDelta.y);
        }

        // 사격 중이 아니고 회복이 거의 끝났다면 정밀 초기화
        if (!Mouse.current.leftButton.isPressed && currentVisualKick.magnitude < 0.001f)
        {
            targetVisualKick = Vector2.zero;
            currentVisualKick = Vector2.zero;
            lastKickApplied = Vector2.zero;
        }
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

        Vector2 kickAmount = GetVisualKickAmount();

        float randomSpringForce = Random.Range(0.9f, 1.1f);
        targetVisualKick += kickAmount * randomSpringForce;

        switch (weaponData.weaponType)
        {
            case WeaponData.WeaponType.SR:
            case WeaponData.WeaponType.Pistol:
                FireRaycast();   // 히트스캔
                break;

            case WeaponData.WeaponType.AR:
            case WeaponData.WeaponType.SMG:
            case WeaponData.WeaponType.LMG:
            case WeaponData.WeaponType.DMR:
                FireProjectile();   // 탄 발사
                break;
        }

        currentShotIndex++;
    }

    void FireRaycast()
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
        Quaternion.identity
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

        NetworkManager.Instance.SendHit(
            identity.PlayerId,
            weaponData.damage
        );
    }

    void FireProjectile()
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

    Vector2 GetVisualKickAmount()
    {
        if (weaponData.recoilPattern == null || weaponData.recoilPattern.Length == 0)
            return Vector2.zero;

        int index = Mathf.Min(currentShotIndex, weaponData.recoilPattern.Length - 1);

        Vector2 recoil = weaponData.recoilPattern[index];

        return recoil * weaponData.recoilMultiplier;
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float spread = isADS ? weaponData.adsSpread : weaponData.hipFireSpread;

        if (IsMoving())
        {
            spread *= moveSpreadMultiplier;
        }

        Vector2 randomCircle = Random.insideUnitCircle * spread;

        Vector3 spreadDir =
            baseDirection +
            cam.transform.right * randomCircle.x +
            cam.transform.up * randomCircle.y;

        return spreadDir.normalized;
    }
}