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

    private AudioSource audioSource;

    [Header("3D Gun Audio")]
    [SerializeField, Range(0f, 1f)] private float gunVolume = 1f;
    [SerializeField, Min(0.01f)] private float gunMinDistance = 3f;
    [SerializeField, Min(0.01f)] private float gunMaxDistance = 80f;

    private PlayerHP ownerHP;
    private bool isPlayer;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isADS;

    [Header("Recoil Logic")]
    private int currentShotIndex = 0;
    private float lastAttackVisualTime = -999f;

    private IFireStrategy fireStrategy;

    private PlayerIdentity _playerIdentity;

    private PlayerMovement playerMovement;
    private ARMotion arMotion;

    private GameObject crosshair;

    private PlayerItem playerItem;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);

        ConfigureWeaponAudioSource(audioSource);

        arMotion = GetComponent<ARMotion>();

        if (arMotion == null)
            arMotion = GetComponentInChildren<ARMotion>(true);

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

        GameObject crosshairObj = GameObject.Find("CrossHair");
        if (crosshairObj != null)
            crosshair = crosshairObj;

        playerItem = GetComponentInParent<PlayerItem>();
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

        // 스턴 중이면 사격 / ADS / 무기 입력 전부 차단
        ElectricShock myShock =
            GetComponentInParent<ElectricShock>();

        if (myShock != null && myShock.IsStunned)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return;
        }

        if (InitiatorSkill_Q.IsLocalDroneMode ||
            InitiatorSkill_C.ShouldBlockWeaponInput())
        {
            ForceCancelADS();
            return;
        }

        if (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen)
        {
            return;
        }

        if (!isPlayer) return;
        if (ownerHP != null && ownerHP.IsDead) return;

        if (weaponData == null) return;

        HandleADS();

        if (playerItem != null && playerItem.IsReloading)
            return;

        bool isFiringInput = IsAutomatic()
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (isFiringInput)
        {
            bool cannotAttack = (RoundManager.Instance != null && !RoundManager.Instance.CanAttack);
            if (cannotAttack) return;
            OnUse();
        }
        else
        {
            currentShotIndex = 0;
            //recoilSystem?.StopRecoil();
        }
    }
    void OnEnable()
    {
        isADS = false;
        crosshair?.SetActive(true);
        UpdateAmmoUI();
    }

    void OnDisable()
    {
        isADS = false;
        crosshair?.SetActive(true);
        StopAllCoroutines();
    }

    private void ForceCancelADS()
    {
        if (!isADS)
            return;

        isADS = false;

        cameraLook?.SetADS(false);
        weaponADS?.SetADS(false);
        playerMovement?.SetADS(false);

        crosshair?.SetActive(true);

        if (weaponData != null && cameraController != null)
            cameraController.SetFOV(weaponData.normalFOV);
    }

    private void HandleADS()
    {
        if (weaponData == null)
            return;

        if (cameraController == null)
            return;

        if (cameraController.PlayerCamera == null)
            return;

        // 권총과 샷건, 칼은 ADS 금지
        bool cannotADS =
            weaponData.weaponType == WeaponData.WeaponType.Pistol ||
            weaponData.weaponType == WeaponData.WeaponType.Shotgun ||
            weaponData.weaponType == WeaponData.WeaponType.Knife;

        bool reloading = playerItem != null && playerItem.IsReloading;

        if (cannotADS || reloading)
        {
            if (isADS)
            {
                isADS = false;
                cameraLook?.SetADS(false);
                weaponADS?.SetADS(false);

                playerMovement?.SetADS(false);
            }

            // ADS가 불가능하거나 장전 중이면 일반 크로스헤어를 표시한다.
            crosshair?.SetActive(true);

            cameraController.SetFOV(weaponData.normalFOV);
            return;
        }

        /*
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
        */

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

            // 조준 중에는 숨기고, 조준이 풀리면 다시 표시한다.
            crosshair?.SetActive(!isADS);
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

        ElectricShock myShock =
        GetComponentInParent<ElectricShock>();

        if (myShock != null && myShock.IsStunned)
        {
            ForceCancelADS();
            return;
        }

        if (weaponData == null)
            return;

        if (playerItem != null && playerItem.IsReloading)
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

        PlayAttackRecoilVisual();
        FireCurrentWeapon();
        PlayLocalGunSound();

        Debug.Log($"[WEAPON USE] weapon={weaponData.name}, ammo={currentAmmo}/{reserveAmmo}");
    }

    public void PlayFireVisualFromServer()
    {
        PlayAttackRecoilVisual();
        PlayRemoteGunSound();

        Debug.Log($"[REMOTE FIRE SOUND] playerId={_playerIdentity?.PlayerId}, weaponId={GetWeaponId()}");
    }

    private void ConfigureWeaponAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.01f, gunMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, gunMaxDistance);
        source.dopplerLevel = 0f;
        source.volume = gunVolume;
    }

    private void PlayLocalGunSound()
    {
        if (weaponData == null || weaponData.gunSound == null)
            return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = GetComponentInChildren<AudioSource>(true);
        }

        if (audioSource != null && audioSource.isActiveAndEnabled)
        {
            // 로컬 총소리도 같은 AudioSource를 사용하고 반드시 3D로 설정한다.
            ConfigureWeaponAudioSource(audioSource);
            audioSource.PlayOneShot(weaponData.gunSound);
            return;
        }

        // AudioSource가 없는 예외 상황에서도 3D 임시 소스로 재생한다.
        PlayTemporarySpatialSound(
            weaponData.gunSound,
            transform.position,
            $"LocalGunSound_{GetWeaponId()}"
        );
    }

    private void PlayRemoteGunSound()
    {
        if (weaponData == null || weaponData.gunSound == null)
            return;

        Vector3 soundPosition = _playerIdentity != null
            ? _playerIdentity.transform.position
            : transform.position;

        PlayTemporarySpatialSound(
            weaponData.gunSound,
            soundPosition,
            $"RemoteGunSound_{GetWeaponId()}"
        );
    }

    private void PlayTemporarySpatialSound(
        AudioClip clip,
        Vector3 position,
        string objectName)
    {
        if (clip == null)
            return;

        GameObject soundObject = new GameObject(objectName);
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = false;

        ConfigureWeaponAudioSource(source);
        source.Play();

        Destroy(soundObject, clip.length + 0.2f);
    }

    public void PlayKnifeAttackVisualFromServer()
    {
        PlayAttackRecoilVisual();
    }
    private void PlayAttackRecoilVisual()
    {
        if (weaponData == null)
            return;

        if (recoilSystem == null)
        {
            recoilSystem =
                GetComponentInParent<WeaponRecoil>();
        }

        if (recoilSystem == null)
        {
            Debug.LogWarning(
                $"[ATTACK VISUAL] WeaponRecoil not found. " +
                $"weapon={name}"
            );

            return;
        }

        if (weaponData.recoilPattern == null ||
            weaponData.recoilPattern.Length == 0)
        {
            Debug.LogWarning(
                $"[ATTACK VISUAL] recoilPattern is empty. " +
                $"weapon={weaponData.name}"
            );

            return;
        }

        float resetDelay =
            Mathf.Max(
                weaponData.fireRate * 2f,
                0.15f
            );

        if (Time.time - lastAttackVisualTime >
            resetDelay)
        {
            currentShotIndex = 0;
        }

        int index =
            Mathf.Min(
                currentShotIndex,
                weaponData.recoilPattern.Length - 1
            );

        float recoilMultiplier =
            isADS
                ? weaponData.adsRecoilMultiplier
                : weaponData.recoilMultiplier;

        recoilSystem.FireRecoil(
            weaponData.recoilPattern[index],
            recoilMultiplier,
            isADS
        );

        currentShotIndex++;
        lastAttackVisualTime = Time.time;
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
            case WeaponData.WeaponType.DMR:
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

        Vector3 visualTargetPoint =
            ray.origin + shootDir * weaponData.range;

        bool hasHit =
            TryGetFirstValidHit(
                ray,
                weaponData.range,
                out RaycastHit hit
            );

        if (hasHit)
        {
            visualTargetPoint = hit.point;

            Debug.Log(
                $"[HITSCAN HIT] object={hit.collider.name}, " +
                $"root={hit.collider.transform.root.name}, " +
                $"point={hit.point}"
            );

            if (TryHandleScannerHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            // 핵심: 플레이어보다 드론을 먼저 검사해야 함
            if (TryHandleDroneHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            TryHandlePlayerHit(hit);
        }
        else
        {
            Debug.Log("[HITSCAN MISS]");
        }

        SpawnBulletVisual(
            shootDir,
            visualTargetPoint
        );
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

    private bool TryHandleDroneHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        DroneHitbox droneHitbox =
            hit.collider.GetComponent<DroneHitbox>();

        if (droneHitbox == null)
        {
            droneHitbox =
                hit.collider.GetComponentInParent<DroneHitbox>();
        }

        if (droneHitbox == null)
            return false;

        if (droneHitbox.DroneId == 0)
        {
            Debug.LogWarning("[DRONE HIT] DroneId is 0");
            return false;
        }

        if (NetworkManager.Instance == null)
            return true;

        Drone drone = droneHitbox.Drone;

        if (drone != null &&
            drone.OwnerPlayerId == NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[DRONE HIT] ignored own drone. droneId={droneHitbox.DroneId}"
            );

            return true;
        }

        Debug.Log(
            $"[DRONE HIT SEND] droneId={droneHitbox.DroneId}, " +
            $"hitObject={hit.collider.name}"
        );

        HitManager.TriggerEnemyHit();

        NetworkManager.Instance.SendDroneHit(
            droneHitbox.DroneId
        );

        return true;
    }

    private bool TryHandlePlayerHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        PlayerIdentity targetIdentity =
            hit.collider.GetComponentInParent<PlayerIdentity>();

        if (targetIdentity == null)
        {
            Debug.Log("[HITSCAN HIT] hit object is not player.");
            return false;
        }

        if (NetworkManager.Instance == null)
            return true;

        if (targetIdentity.PlayerId ==
            NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log("[HITSCAN HIT] ignored self.");
            return true;
        }

        Debug.Log(
            $"[HITSCAN PLAYER] targetPlayerId={targetIdentity.PlayerId}, " +
            $"userId={targetIdentity.UserId}"
        );

        HitManager.TriggerEnemyHit();

        NetworkManager.Instance.SendHit(
            targetIdentity.PlayerId
        );

        return true;
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
                1000f,
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

        Ray ray =
            cam.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        Vector3 targetPoint;

        bool hasHit =
            TryGetFirstValidHit(
                ray,
                weaponData.range,
                out RaycastHit hit
            );

        if (hasHit)
        {
            targetPoint = hit.point;

            Debug.Log(
                $"[PROJECTILE AIM HIT] object={hit.collider.name}, " +
                $"root={hit.collider.transform.root.name}, " +
                $"point={hit.point}"
            );

            if (hasHit)
            {
                targetPoint = hit.point;

                Debug.Log(
                    $"[PROJECTILE AIM HIT] object={hit.collider.name}, " +
                    $"root={hit.collider.transform.root.name}, " +
                    $"point={hit.point}"
                );

                // 서버 피격 전송만 하고, 총알 비주얼은 아래에서 그대로 생성한다.
                if (TryHandleScannerHit(hit))
                {
                    // return 하지 마라
                }
                else if (TryHandleDroneHit(hit))
                {
                    // return 하지 마라
                }
            }
            else
            {
                targetPoint =
                    ray.origin + ray.direction * weaponData.range;
            }
        }
        else
        {
            targetPoint =
                ray.origin + ray.direction * weaponData.range;
        }

        Vector3 baseDir =
            (targetPoint - firePoint.position).normalized;

        Vector3 shootDir =
            GetSpreadDirection(baseDir);

        GameObject bullet =
            Instantiate(
                bulletPrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDir)
            );

        Bullet bulletScript =
            bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bool isOwnerBullet =
                NetworkManager.Instance != null &&
                _playerIdentity != null &&
                _playerIdentity.PlayerId ==
                NetworkManager.Instance.MyPlayerId;

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

    private int GetMagazineCapacity()
    {
        if (weaponData == null)
            return 0;

        // 서버 ServerWeaponData의 weaponId별 탄창 크기와 동일하게 유지한다.
        switch (weaponData.weaponId)
        {
            case 0: return 12;  // P92
            case 1: return 7;   // P1911
            case 2: return 6;   // R1895
            case 3: return 20;  // UZI
            case 4: return 30;  // UMP
            case 5: return 5;   // S1897
            case 6: return 5;   // AA12
            case 7: return 12;  // Dragunov
            case 8: return 30;  // M416
            case 9: return 25;  // AK47
            case 10: return 100; // M249
            case 11: return 5;   // Kar98
            case 12: return 5;   // AWM
            case 13: return 0;   // Knife
            default:
                return Mathf.Max(0, weaponData.magSize);
        }
    }

    public bool CanReloadNow()
    {
        if (weaponData == null)
            return false;

        if (weaponData.weaponType == WeaponData.WeaponType.Knife)
            return false;

        int maxAmmo = GetMagazineCapacity();

        if (maxAmmo <= 0)
            return false;

        // 탄창이 이미 가득 차 있음
        if (currentAmmo >= maxAmmo)
            return false;

        // 옮겨 담을 예비 탄약이 없음
        if (reserveAmmo <= 0)
            return false;

        return true;
    }

    private void UpdateAmmoUI()
    {
        if (!ShouldUpdateAmmoUI())
            return;

        if (ammoText != null)
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.SetBulletText(currentAmmo, reserveAmmo);
    }

    private bool ShouldUpdateAmmoUI()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponentInParent<PlayerIdentity>();

        if (_playerIdentity == null || NetworkManager.Instance == null)
            return false;

        if (SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating)
        {
            return SpectatorManager.Instance.CurrentTargetPlayerId ==
                   _playerIdentity.PlayerId;
        }

        return _playerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    public void ReloadFull()
    {
        currentAmmo = GetMagazineCapacity();
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
            Keyboard.current.dKey.isPressed ||
            !playerMovement.GetIsGrounded();
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

    // KSW: 무기 재장전 시간 리턴 함수 추가
    public float GetWeaponReLoadTime()
    {
        if (weaponData == null) return 0;
        return weaponData.reloadTime;
    }

    private bool TryHandleScannerHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        ScannerHitbox scannerHitbox =
            hit.collider.GetComponent<ScannerHitbox>();

        if (scannerHitbox == null)
            scannerHitbox =
                hit.collider.GetComponentInParent<ScannerHitbox>();

        if (scannerHitbox == null)
            return false;

        if (scannerHitbox.ScannerId == 0)
        {
            Debug.LogWarning("[SCANNER HIT] ScannerId is 0");
            return true;
        }

        if (NetworkManager.Instance == null)
            return true;

        ScannerProjectile scanner = scannerHitbox.Scanner;

        if (scanner != null &&
            scanner.OwnerPlayerId == NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[SCANNER HIT] ignored own scanner. scannerId={scannerHitbox.ScannerId}"
            );

            return true;
        }

        NetworkManager.Instance.SendScannerHit(
            scannerHitbox.ScannerId
        );

        Debug.Log(
            $"[SCANNER HIT SEND] scannerId={scannerHitbox.ScannerId}"
        );

        return true;
    }
}