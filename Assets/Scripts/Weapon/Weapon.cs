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

    private WeaponSkinSlot skinSlot;

    public WeaponData Data => weaponData;
    public WeaponSkinData AppliedSkin { get; private set; }

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

        GameObject crosshairObj = GameObject.Find("CrossHair");

        if (crosshairObj != null)
            crosshair = crosshairObj;

        playerItem = GetComponentInParent<PlayerItem>();

        skinSlot = GetComponent<WeaponSkinSlot>();
        if (skinSlot == null)
            skinSlot = GetComponentInChildren<WeaponSkinSlot>(true);
    }

    public void Initialize(
        WeaponData data,
        Transform dp,
        int mag,
        int reserve)
    {
        if (data == null)
        {
            Debug.LogError(
                $"[WEAPON INIT FAIL] WeaponData is null. object={name}",
                this
            );

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
        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
        {
            return;
        }

        if (Mouse.current == null)
            return;

        if (Keyboard.current == null)
            return;

        if (_playerIdentity == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        if (ShouldBlockWeaponInput())
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
        {
            return;
        }

        if (!isPlayer)
            return;

        if (ownerHP != null && ownerHP.IsDead)
            return;

        if (weaponData == null)
            return;

        HandleADS();

        if (playerItem != null && playerItem.IsReloading)
            return;

        bool isFiringInput =
            IsAutomatic()
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;

        if (isFiringInput)
        {
            bool cannotAttack =
                RoundManager.Instance != null &&
                !RoundManager.Instance.CanAttack;

            if (cannotAttack)
                return;

            OnUse();
        }
        else
        {
            currentShotIndex = 0;
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

    private bool ShouldBlockWeaponInput()
    {
        if (StorePanelUI.IsBlockingGameplayInput)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (ControllerSkill_E.IsSelectingSmoke || ControllerSkill_E.IsSmokeMapOpen || ControllerSkill_E.IsBlockingWeaponInput)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (EntrySkill_X.IsLocalBladeMode)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        EntryDashMotor dashMotor = GetComponentInParent<EntryDashMotor>();

        if (dashMotor != null && dashMotor.IsDashing)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        ElectricShock myShock = GetComponentInParent<ElectricShock>();

        if (myShock != null && myShock.IsStunned)
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (InitiatorSkill_Q.IsLocalDroneMode || InitiatorSkill_C.ShouldBlockWeaponInput())
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (SentinelSkill_C.ShouldBlockWeaponInput())
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (SentinelSkill_E.ShouldBlockWeaponInput())
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        if (SentinelSkill_X.ShouldBlockWeaponInput())
        {
            ForceCancelADS();
            currentShotIndex = 0;
            return true;
        }

        return false;
    }

    private void HandleADS()
    {
        if (ShouldBlockWeaponInput())
            return;

        if (weaponData == null)
            return;

        if (cameraController == null)
            return;

        if (cameraController.PlayerCamera == null)
            return;

        bool cannotADS =
            weaponData.weaponType == WeaponData.WeaponType.Pistol ||
            weaponData.weaponType == WeaponData.WeaponType.Shotgun ||
            weaponData.weaponType == WeaponData.WeaponType.Knife;

        bool reloading =
            playerItem != null &&
            playerItem.IsReloading;

        if (cannotADS || reloading)
        {
            if (isADS)
            {
                isADS = false;

                cameraLook?.SetADS(false);
                weaponADS?.SetADS(false);
                playerMovement?.SetADS(false);
            }

            crosshair?.SetActive(true);
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

            crosshair?.SetActive(!isADS);
        }

        float targetFOV =
            isADS
                ? weaponData.adsFOV
                : weaponData.normalFOV;

        float nextFOV =
            Mathf.Lerp(
                cameraController.PlayerCamera.fieldOfView,
                targetFOV,
                weaponData.adsSpeed * Time.deltaTime
            );

        cameraController.SetFOV(nextFOV);
    }

    public override void OnUse()
    {
        base.OnUse();

        if (ShouldBlockWeaponInput())
            return;

        EntrySkill_E beastStealth =
            GetComponentInParent<EntrySkill_E>();

        if (beastStealth != null &&
            beastStealth.IsStealth)
        {
            NetworkManager.Instance?.SendBeastStealthCancel();
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

        nextFireTime =
            Time.time + weaponData.fireRate;

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

        Debug.Log(
            $"[WEAPON USE] weapon={weaponData.name}, ammo={currentAmmo}/{reserveAmmo}"
        );
    }

    public void PlayFireVisualFromServer()
    {
        PlayAttackRecoilVisual();
        PlayRemoteGunSound();

        Debug.Log(
            $"[REMOTE FIRE SOUND] playerId={_playerIdentity?.PlayerId}, weaponId={GetWeaponId()}"
        );
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
        if (weaponData == null ||
            weaponData.gunSound == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = GetComponentInChildren<AudioSource>(true);
        }

        if (audioSource != null &&
            audioSource.isActiveAndEnabled)
        {
            ConfigureWeaponAudioSource(audioSource);
            audioSource.PlayOneShot(weaponData.gunSound);
            return;
        }

        PlayTemporarySpatialSound(
            weaponData.gunSound,
            transform.position,
            $"LocalGunSound_{GetWeaponId()}"
        );
    }

    private void PlayRemoteGunSound()
    {
        if (weaponData == null ||
            weaponData.gunSound == null)
        {
            return;
        }

        Vector3 soundPosition =
            _playerIdentity != null
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

        GameObject soundObject =
            new GameObject(objectName);

        soundObject.transform.position =
            position;

        AudioSource source =
            soundObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = false;

        ConfigureWeaponAudioSource(source);

        source.Play();

        Destroy(
            soundObject,
            clip.length + 0.2f
        );
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
                $"[ATTACK VISUAL] WeaponRecoil not found. weapon={name}"
            );

            return;
        }

        if (weaponData.recoilPattern == null ||
            weaponData.recoilPattern.Length == 0)
        {
            Debug.LogWarning(
                $"[ATTACK VISUAL] recoilPattern is empty. weapon={weaponData.name}"
            );

            return;
        }

        float resetDelay =
            Mathf.Max(
                weaponData.fireRate * 2f,
                0.15f
            );

        if (Time.time - lastAttackVisualTime > resetDelay)
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

        float recoveryDelay =
            Mathf.Max(
                weaponData.fireRate * 1.2f,
                0.1f
            );

        recoilSystem.FireRecoil(
            weaponData.recoilPattern[index],
            recoilMultiplier,
            isADS,
            recoveryDelay
        );

        currentShotIndex++;
        lastAttackVisualTime = Time.time;
    }

    private void FireCurrentWeapon()
    {
        if (ShouldBlockWeaponInput())
            return;

        if (weaponData == null)
            return;

        switch (weaponData.weaponType)
        {
            case WeaponData.WeaponType.SR:
                FireHitscan();
                break;

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
        if (ShouldBlockWeaponInput())
            return;

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

        Vector3 shootDir =
            GetSpreadDirection(cam.transform.forward);

        Ray ray =
            new Ray(cam.transform.position, shootDir);

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
            visualTargetPoint =
                hit.point;

            Debug.Log(
                $"[HITSCAN HIT] object={hit.collider.name}, root={hit.collider.transform.root.name}, point={hit.point}"
            );

            if (TryHandleScannerHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            if (TryHandleDroneHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            if (TryHandleSentinelTrapHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            if (TryHandleSentinelQTrapHit(hit))
            {
                SpawnBulletVisual(
                    shootDir,
                    visualTargetPoint
                );

                return;
            }

            if (TryHandleSentinelETrapHit(hit))
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

    private bool TryGetFirstValidHit(
        Ray ray,
        float range,
        out RaycastHit result)
    {
        result = default;

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                range,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

        if (hits == null ||
            hits.Length == 0)
        {
            return false;
        }

        Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        Transform myRoot =
            transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit =
                hits[i];

            if (hit.collider == null)
                continue;

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

        Drone drone =
            droneHitbox.Drone;

        if (drone != null &&
            drone.OwnerPlayerId == NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[DRONE HIT] ignored own drone. droneId={droneHitbox.DroneId}"
            );

            return true;
        }

        Debug.Log(
            $"[DRONE HIT SEND] droneId={droneHitbox.DroneId}, hitObject={hit.collider.name}"
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
            $"[HITSCAN PLAYER] targetPlayerId={targetIdentity.PlayerId}, userId={targetIdentity.UserId}"
        );

        HitManager.TriggerEnemyHit();

        NetworkManager.Instance.SendHit(
            targetIdentity.PlayerId
        );

        return true;
    }

    private void SpawnBulletVisual(
        Vector3 shootDir,
        Vector3 targetPoint)
    {
        if (bulletPrefab == null ||
            firePoint == null)
        {
            return;
        }

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
        if (ShouldBlockWeaponInput())
            return;

        if (cam == null)
        {
            Debug.LogWarning("[WEAPON] FireProjectile failed. cam is null.");
            return;
        }

        if (weaponData == null)
            return;

        if (bulletPrefab == null ||
            firePoint == null)
        {
            return;
        }

        Vector3 shootDir =
            GetSpreadDirection(cam.transform.forward);

        Ray ray =
            new Ray(cam.transform.position, shootDir);

        bool hasHit =
            TryGetFirstValidHit(
                ray,
                weaponData.range,
                out RaycastHit hit
            );

        if (hasHit)
        {
            Debug.Log(
                $"[PROJECTILE AIM HIT] object={hit.collider.name}, root={hit.collider.transform.root.name}, point={hit.point}"
            );

            if (!TryHandleScannerHit(hit) &&
                !TryHandleDroneHit(hit))
            {
                if (!TryHandleSentinelTrapHit(hit))
                {
                    if (!TryHandleSentinelQTrapHit(hit))
                        TryHandleSentinelETrapHit(hit);
                }
            }
        }

        GameObject bullet =
            Instantiate(
                bulletPrefab,
                cam.transform.position,
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

        Debug.Log(
            $"[PROJECTILE FIRE] weapon={weaponData.name}"
        );
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
                fireStrategy =
                    new ShotGunFire(this, 12);
                break;

            case WeaponData.WeaponType.Knife:
                fireStrategy =
                    new MeleeFire(
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

        switch (weaponData.weaponId)
        {
            case 0: return 12;
            case 1: return 7;
            case 2: return 6;
            case 3: return 20;
            case 4: return 30;
            case 5: return 5;
            case 6: return 5;
            case 7: return 12;
            case 8: return 30;
            case 9: return 25;
            case 10: return 100;
            case 11: return 5;
            case 12: return 5;
            case 13: return 0;

            default:
                return Mathf.Max(
                    0,
                    weaponData.magSize
                );
        }
    }

    public bool CanReloadNow()
    {
        if (weaponData == null)
            return false;

        if (weaponData.weaponType ==
            WeaponData.WeaponType.Knife)
        {
            return false;
        }

        int maxAmmo =
            GetMagazineCapacity();

        if (maxAmmo <= 0)
            return false;

        bool isBuyPhase =
            RoundManager.Instance != null &&
            RoundManager.Instance.currentState ==
            RoundManager.RoundState.Preparation;

        if (isBuyPhase)
        {
            return currentAmmo < maxAmmo ||
                   reserveAmmo < weaponData.maxReserveAmmo;
        }

        if (currentAmmo >= maxAmmo)
            return false;

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
            InGameUIManager.Instance.SetBulletText(
                currentAmmo,
                reserveAmmo
            );
    }

    private bool ShouldUpdateAmmoUI()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponentInParent<PlayerIdentity>();

        if (_playerIdentity == null ||
            NetworkManager.Instance == null)
        {
            return false;
        }

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
        currentAmmo =
            GetMagazineCapacity();

        reserveAmmo =
            weaponData.maxReserveAmmo;

        UpdateAmmoUI();
    }

    public void SetAmmo(
        int mag,
        int reserve)
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
        Debug.LogWarning(
            "[WEAPON] DropWeapon local drop disabled. Use PlayerItem server drop flow."
        );
    }

    bool IsAutomatic()
    {
        return weaponData.weaponType == WeaponData.WeaponType.AR ||
               weaponData.weaponType == WeaponData.WeaponType.SMG ||
               weaponData.weaponType == WeaponData.WeaponType.LMG;
    }

    bool IsMoving()
    {
        EntryDashMotor dashMotor =
            GetComponentInParent<EntryDashMotor>();

        if (dashMotor != null &&
            dashMotor.IsDashing)
        {
            return true;
        }

        if (Keyboard.current == null)
            return false;

        return
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed ||
            playerMovement != null &&
            !playerMovement.GetIsGrounded();
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        if (cam == null)
            return baseDirection.normalized;

        float spread =
            isADS
                ? weaponData.adsSpread
                : weaponData.hipFireSpread;

        if (IsMoving())
        {
            spread *= weaponData.moveSpreadMultiplier;
        }

        Vector2 randomCircle =
            UnityEngine.Random.insideUnitCircle * spread;

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
        if (ShouldBlockWeaponInput())
            return;

        if (bulletPrefab == null ||
            firePoint == null)
        {
            return;
        }

        GameObject grenade =
            Instantiate(
                bulletPrefab,
                firePoint.position,
                firePoint.rotation
            );

        Rigidbody rb =
            grenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                firePoint.forward * weaponData.muzzleVelocity,
                ForceMode.Impulse
            );
        }
    }

    public void FireRaycast()
    {
        if (ShouldBlockWeaponInput())
            return;

        FireHitscan();
    }

    private bool UsesAmmo()
    {
        if (weaponData == null)
            return false;

        return weaponData.weaponType !=
               WeaponData.WeaponType.Knife;
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

    public float GetWeaponReLoadTime()
    {
        if (weaponData == null)
            return 0;

        return weaponData.reloadTime;
    }

    private bool TryHandleScannerHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        ScannerHitbox scannerHitbox =
            hit.collider.GetComponent<ScannerHitbox>();

        if (scannerHitbox == null)
        {
            scannerHitbox =
                hit.collider.GetComponentInParent<ScannerHitbox>();
        }

        if (scannerHitbox == null)
            return false;

        if (scannerHitbox.ScannerId == 0)
        {
            Debug.LogWarning("[SCANNER HIT] ScannerId is 0");
            return true;
        }

        if (NetworkManager.Instance == null)
            return true;

        ScannerProjectile scanner =
            scannerHitbox.Scanner;

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

    private bool TryHandleSentinelTrapHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        Sentinel_Trap trap =
            hit.collider.GetComponent<Sentinel_Trap>();

        if (trap == null)
        {
            trap =
                hit.collider.GetComponentInParent<Sentinel_Trap>();
        }

        if (trap == null)
            return false;

        if (trap.TrapId == 0)
        {
            Debug.LogWarning("[SENTINEL C HIT] TrapId is 0");
            return true;
        }

        if (NetworkManager.Instance == null)
            return true;

        if (trap.OwnerPlayerId == NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[SENTINEL C HIT] ignored own trap. trapId={trap.TrapId}"
            );

            return true;
        }

        if (_playerIdentity != null &&
            _playerIdentity.Team == trap.OwnerTeam)
        {
            Debug.Log(
                $"[SENTINEL C HIT] ignored friendly trap. trapId={trap.TrapId}"
            );

            return true;
        }

        if (SentinelCNetworkManager.Instance != null &&
            SentinelCNetworkManager.Instance.IsSkillProtected(
                trap.OwnerPlayerId
            ))
        {
            Debug.Log(
                $"[SENTINEL C HIT] protected by X. trapId={trap.TrapId}"
            );

            return true;
        }

        if (weaponData == null)
            return true;

        HitManager.TriggerEnemyHit();

        NetworkManager.Instance.SendSentinelCHit(
            trap.TrapId,
            weaponData.damage
        );

        Debug.Log(
            $"[SENTINEL C HIT SEND] trapId={trap.TrapId}, damage={weaponData.damage}"
        );

        return true;
    }

    private bool TryHandleSentinelQTrapHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        Sentinel_SlowTrap trap =
            hit.collider.GetComponent<Sentinel_SlowTrap>();

        if (trap == null)
        {
            trap =
                hit.collider.GetComponentInParent<Sentinel_SlowTrap>();
        }

        if (trap == null)
            return false;

        if (trap.TrapId == 0)
        {
            Debug.LogWarning("[SENTINEL Q HIT] TrapId is 0");
            return true;
        }

        if (NetworkManager.Instance == null)
            return true;

        if (trap.OwnerPlayerId == NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[SENTINEL Q HIT] ignored own trap. trapId={trap.TrapId}"
            );

            return true;
        }

        if (_playerIdentity != null &&
            _playerIdentity.Team == trap.OwnerTeam)
        {
            Debug.Log(
                $"[SENTINEL Q HIT] ignored friendly trap. trapId={trap.TrapId}"
            );

            return true;
        }

        if (SentinelCNetworkManager.Instance != null &&
            SentinelCNetworkManager.Instance.IsSkillProtected(
                trap.OwnerPlayerId
            ))
        {
            Debug.Log(
                $"[SENTINEL Q HIT] protected by X. trapId={trap.TrapId}"
            );

            return true;
        }

        HitManager.TriggerEnemyHit();
        NetworkManager.Instance.SendSentinelQHit(trap.TrapId);

        Debug.Log(
            $"[SENTINEL Q HIT SEND] trapId={trap.TrapId}"
        );

        return true;
    }

    private bool TryHandleSentinelETrapHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        TrapDevice trap =
            hit.collider.GetComponent<TrapDevice>();

        if (trap == null)
        {
            trap =
                hit.collider.GetComponentInParent<TrapDevice>();
        }

        if (trap == null)
            return false;

        if (trap.TrapId == 0)
        {
            Debug.LogWarning("[SENTINEL E HIT] TrapId is 0");
            return true;
        }

        if (NetworkManager.Instance == null)
            return true;

        if (trap.OwnerPlayerId ==
            NetworkManager.Instance.MyPlayerId)
        {
            Debug.Log(
                $"[SENTINEL E HIT] ignored own trap. " +
                $"trapId={trap.TrapId}"
            );

            return true;
        }

        if (_playerIdentity != null &&
            _playerIdentity.Team == trap.OwnerTeam)
        {
            Debug.Log(
                $"[SENTINEL E HIT] ignored friendly trap. " +
                $"trapId={trap.TrapId}"
            );

            return true;
        }

        if (SentinelCNetworkManager.Instance != null &&
            SentinelCNetworkManager.Instance.IsSkillProtected(
                trap.OwnerPlayerId
            ))
        {
            Debug.Log(
                $"[SENTINEL E HIT] protected by X. " +
                $"trapId={trap.TrapId}"
            );

            return true;
        }

        HitManager.TriggerEnemyHit();
        NetworkManager.Instance.SendSentinelEHit(trap.TrapId);

        Debug.Log(
            $"[SENTINEL E HIT SEND] trapId={trap.TrapId}"
        );

        return true;
    }

    public void ApplySkin(WeaponSkinData skin, WeaponData expectedWeapon)
    {
        if (skin != null && skin.baseWeapon != null && skin.baseWeapon != expectedWeapon)
        {
            Debug.LogWarning(
                $"[WEAPON SKIN] mismatch. skin={skin.name} expects {skin.baseWeapon.name}, " +
                $"but current weapon is {expectedWeapon.name}. Skin not applied."
            );
            return;
        }

        AppliedSkin = skin;

        // Awake can be delayed while the first-person holder is inactive.
        // Resolve the slot on demand so the first round matches later rounds.
        if (skinSlot == null)
            skinSlot = GetComponent<WeaponSkinSlot>();

        if (skinSlot == null)
            skinSlot = GetComponentInChildren<WeaponSkinSlot>(true);

        if (skinSlot == null)
        {
            Debug.LogWarning($"[WEAPON SKIN] WeaponSkinSlot missing on {name}. Skin cannot be applied.");
            return;
        }

        skinSlot.ApplySkin(skin);
    }
}