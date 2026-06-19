using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : Item
{
    [Header("References")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private WeaponRecoil recoilSystem;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private TMP_Text ammoText;

    [Header("Hit Scan")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Camera")]
    private Camera cam;
    private FPSCameraLook cameraLook;
    private WeaponADS weaponADS;
    private PlayerCameraController cameraController;
    private Transform adsTarget;

    [Header("Drop")]
    [SerializeField] private Transform dropPoint;

    private PlayerHP ownerHP;
    private bool isPlayer;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isReloading;
    private bool isADS;

    [Header("Recoil Logic")]
    private int currentShotIndex = 0;

    private IFireStrategy fireStrategy;

    private PlayerIdentity _playerIdentity;

    private PlayerMovement playerMovement;

    void Awake()
    {
        ownerHP = GetComponentInParent<PlayerHP>();
        isPlayer = GetComponentInParent<PlayerMovement>() != null;
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (!isPlayer)
        {
            enabled = false;
            return;
        }

        cameraController = GetComponentInParent<PlayerCameraController>();

        if (cameraController != null)
        {
            cam = cameraController.PlayerCamera;
            cameraLook = cameraController.CameraLook;
        }
        else
        {
            //Debug.LogWarning($"[WEAPON] PlayerCameraController not found. object={name}", this);
        }

        weaponADS = GetComponent<WeaponADS>();
        _playerIdentity = GetComponentInParent<PlayerIdentity>();

        if (recoilSystem == null)
            recoilSystem = GetComponentInParent<WeaponRecoil>();

        if (cameraController != null && cameraController.PlayerCamera != null)
        {
            Transform target = cameraController.PlayerCamera.transform.Find("ADSTarget");
            if (target != null)
            {
                adsTarget = target;
                weaponADS?.SetADSTarget(adsTarget);
            }
        }

        GameObject ammoObj = GameObject.Find("AmmoText");
        if (ammoObj != null)
            ammoText = ammoObj.GetComponent<TMP_Text>();
        //else
            //Debug.LogWarning($"[WEAPON] AmmoText object not found. object={name}", this);
    }

    public void Initialize(WeaponData data, Transform dp, int mag, int reserve)
    {
        if (data == null)
        {
            Debug.LogError($"[WEAPON INIT FAIL] WeaponData is null. object={name}", this);
            enabled = false;
            return;
        }

        weaponData = data;
        dropPoint = dp;
        currentAmmo = mag;
        reserveAmmo = reserve;

        SetupStrategy();

        UpdateAmmoUI();

        if (cameraController != null)
        {
            cameraController.SetFOV(weaponData.normalFOV);
        }
    }
    void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        if (Mouse.current == null) return;
        if (Keyboard.current == null) return;

        if (_playerIdentity == null) return;
        if (NetworkManager.Instance == null) return;
        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId) return;

        if (!isPlayer) return;
        if (ownerHP != null && ownerHP.IsDead) return;

        if (weaponData == null) return;

        HandleADS();

        if (isReloading) return;

        bool isFiringInput = IsAutomatic()
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            currentShotIndex = 0;
            recoilSystem?.StopRecoil();

            StartCoroutine(Reload());
            return;
        }

        if (isFiringInput)
        {
            bool cannotAttack = (RoundManager.Instance != null && !RoundManager.Instance.CanAttack);
            if (cannotAttack) return;
            OnUse();
        }
        else
        {
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

    private void HandleADS()
    {
        if (weaponData == null)
            return;

        if (cameraController == null)
                    return;

        if (cameraController.PlayerCamera == null)
            return;

        // 권총과 칼은 ADS 금지
        bool cannotADS =
            weaponData.weaponType == WeaponData.WeaponType.Pistol ||
            weaponData.weaponType == WeaponData.WeaponType.Knife;

        if (cannotADS)
        {
            // 이전 무기에서 ADS 중이었다면 강제로 해제
            if (isADS)
            {
                isADS = false;

                cameraLook?.SetADS(false);
                weaponADS?.SetADS(false);
            }

            // FOV도 일반 상태로 고정
            cameraController.SetFOV(weaponData.normalFOV);

            return;
        }

        if (Mouse.current == null)
            return;

        bool adsInput =
            Mouse.current.rightButton.isPressed;

        if (adsInput != isADS)
        {
            isADS = adsInput;

            cameraLook?.SetADS(isADS);
            weaponADS?.SetADS(isADS);

            playerMovement?.SetADS(isADS);
        }

        float targetFOV = isADS
            ? weaponData.adsFOV
            : weaponData.normalFOV;

        float nextFOV = Mathf.Lerp(
            cameraController.PlayerCamera.fieldOfView,
            targetFOV,
            weaponData.adsSpeed * Time.deltaTime
        );

        cameraController.SetFOV(nextFOV);
}

    public override void OnUse()
    {
        base.OnUse();

        if (weaponData == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (UsesAmmo() && currentAmmo <= 0)
            return;

        if (ownerHP != null && ownerHP.IsDead)
            return;

        nextFireTime = Time.time + weaponData.fireRate;

        if (UsesAmmo())
        {
            currentAmmo--;
            UpdateAmmoUI();

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.SendFire();
        }

        if (recoilSystem != null && weaponData.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(currentShotIndex, weaponData.recoilPattern.Length - 1);

            float recoilMultiplier = isADS
                ? weaponData.adsRecoilMultiplier
                : weaponData.recoilMultiplier;

            recoilSystem.FireRecoil(
                weaponData.recoilPattern[index],
                recoilMultiplier,
                isADS
            );
        }

        FireCurrentWeapon();
        currentShotIndex++;

        Debug.Log($"[WEAPON USE] weapon={weaponData.name}, ammo={currentAmmo}/{reserveAmmo}");
    }

    private void FireCurrentWeapon()
    {
        if (weaponData == null)
            return;

        switch (weaponData.weaponType)
        {
            // 즉시 판정 무기
            case WeaponData.WeaponType.SR:
                FireHitscan();
                break;

            // 실제 총알 오브젝트로 판정할 무기
            case WeaponData.WeaponType.AR:
            case WeaponData.WeaponType.SMG:
            case WeaponData.WeaponType.LMG:
            case WeaponData.WeaponType.Pistol:
                FireProjectile();
                break;

            case WeaponData.WeaponType.Shotgun:
                fireStrategy?.Fire();
                break;

            case WeaponData.WeaponType.Knife:
                fireStrategy?.Fire();
                break;
        }
    }

    public override void OnDrop()
    {
        DropWeapon();
    }

    private void FireHitscan()
    {
        if (cam == null)
        {
            Debug.LogWarning("[WEAPON] FireHitscan failed. cam is null.");
            return;
        }

        if (weaponData == null)
        {
            Debug.LogWarning("[WEAPON] FireHitscan failed. weaponData is null.");
            return;
        }

        Vector3 shootDir = GetSpreadDirection(cam.transform.forward);
        Ray ray = new Ray(cam.transform.position, shootDir);

        Vector3 visualTargetPoint = ray.origin + shootDir * weaponData.range;

        bool hasHit = TryGetFirstValidHit(ray, weaponData.range, out RaycastHit hit);

        if (hasHit)
        {
            visualTargetPoint = hit.point;

            Debug.Log(
                $"[HITSCAN HIT] object={hit.collider.name}, " +
                $"root={hit.collider.transform.root.name}, " +
                $"point={hit.point}"
            );

            PlayerIdentity targetIdentity =
                hit.collider.GetComponentInParent<PlayerIdentity>();

            if (targetIdentity != null)
            {
                if (NetworkManager.Instance == null)
                    return;

                if (targetIdentity.PlayerId == NetworkManager.Instance.MyPlayerId)
                {
                    Debug.Log("[HITSCAN HIT] ignored self.");
                }
                else
                {
                    Debug.Log(
                        $"[HITSCAN PLAYER] targetPlayerId={targetIdentity.PlayerId}, " +
                        $"userId={targetIdentity.UserId}"
                    );

                    NetworkManager.Instance.SendHit(targetIdentity.PlayerId);
                }
            }
            else
            {
                Debug.Log("[HITSCAN HIT] hit object is not player.");
            }
        }
        else
        {
            Debug.Log("[HITSCAN MISS]");
        }

        SpawnBulletVisual(shootDir, visualTargetPoint);
    }

    private bool TryGetFirstValidHit(Ray ray, float range, out RaycastHit result)
    {
        result = default;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRoot = transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            // 내 캐릭터, 내 무기, 내 자식 콜라이더는 무시
            if (hit.collider.transform.root == myRoot)
                continue;

            result = hit;
            return true;
        }

        return false;
    }

    private void SpawnBulletVisual(Vector3 shootDir, Vector3 targetPoint)
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDir)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            // Raycast 무기용 시각 효과 총알은 Hit 전송 금지
            bulletScript.Init(
                _playerIdentity,
                weaponData.range,
                weaponData.muzzleVelocity,
                shootDir,
                false
            );
        }
    }

    public void FireProjectile()
    {
        if (cam == null)
        {
            Debug.LogWarning("[WEAPON] FireProjectile failed. cam is null.");
            return;
        }

        if (weaponData == null)
            return;

        if (bulletPrefab == null || firePoint == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range, hitMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * weaponData.range;
        }

        Vector3 baseDir = (targetPoint - firePoint.position).normalized;
        Vector3 shootDir = GetSpreadDirection(baseDir);

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDir)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bool isOwnerBullet =
                NetworkManager.Instance != null &&
                _playerIdentity != null &&
                _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

            bulletScript.Init(
                _playerIdentity,
                weaponData.range,
                weaponData.muzzleVelocity,
                shootDir,
                isOwnerBullet
            );
        }

        Debug.Log($"[PROJECTILE FIRE] weapon={weaponData.name}");
    }

    void SetupStrategy()
    {
        switch (weaponData.weaponType)
        {
            case WeaponData.WeaponType.Pistol:
            case WeaponData.WeaponType.AR:
            case WeaponData.WeaponType.SMG:
            case WeaponData.WeaponType.LMG:
            case WeaponData.WeaponType.SR:
                fireStrategy = null;
                break;

            case WeaponData.WeaponType.Shotgun:
                fireStrategy = new ShotGunFire(this, 12);
                break;

            case WeaponData.WeaponType.Knife:
                fireStrategy = new MeleeFire(
                    this.transform,
                    this.weaponData.range,
                    this.weaponData.damage
                );
                break;
        }
    }

    IEnumerator Reload()
    {
        if (weaponData == null)
            yield break;

        if (isReloading)
            yield break;

        if (weaponData.weaponType == WeaponData.WeaponType.Knife)
            yield break;

        if (currentAmmo >= weaponData.magSize || reserveAmmo <= 0)
            yield break;

        isReloading = true;

        if (ammoText != null)
            ammoText.text = "Reloading...";

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.SetBulletText(0, reserveAmmo);

        yield return new WaitForSeconds(weaponData.reloadTime);

        // 여기서 클라가 직접 탄약을 계산하지 않는다.
        // 서버에 장전 요청을 보내고, 서버의 SC_AMMO_UPDATE를 받아서 갱신한다.
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendReload();

        isReloading = false;
    }
    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";
        }

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetBulletText(currentAmmo, reserveAmmo);
        }
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
        Debug.LogWarning("[WEAPON] DropWeapon local drop disabled. Use PlayerItem server drop flow.");
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
        if (cam == null)
            return baseDirection.normalized;

        float spread = isADS ? weaponData.adsSpread : weaponData.hipFireSpread;

        if (IsMoving())
        {
            spread *= weaponData.moveSpreadMultiplier;
        }

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spread;

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

    public void ThrowGrenade()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        GameObject grenade = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody rb = grenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(firePoint.forward * weaponData.muzzleVelocity, ForceMode.Impulse);
        }
    }

    public void FireRaycast()
    {
        FireHitscan();
    }

    private bool UsesAmmo()
    {
        if (weaponData == null)
            return false;

        return weaponData.weaponType != WeaponData.WeaponType.Knife;
    }

    public int GetWeaponId()
    {
        if (weaponData == null)
            return 0;

        return weaponData.weaponId;
    }

    public float GetWeight()
    {
        if (weaponData == null)
            return 1f;

        return weaponData.weight;
    }
}