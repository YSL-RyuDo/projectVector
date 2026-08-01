using UnityEngine;
using UnityEngine.InputSystem;

public class InitiatorSkill_C : CharacterSkill
{
    public static bool IsLocalScannerAiming { get; private set; }

    private static int _blockWeaponInputUntilFrame = -1;

    public static bool ShouldBlockWeaponInput()
    {
        return IsLocalScannerAiming || Time.frameCount <= _blockWeaponInputUntilFrame;
    }

    private static void BlockWeaponInputThisFrame()
    {
        _blockWeaponInputUntilFrame = Time.frameCount + 1;
    }

    [Header("Throw")]
    [SerializeField] private float _baseForce = 12f;
    [SerializeField] private int _lineResolution = 18;
    [SerializeField] private float _lineSpacing = 0.09f;

    [Header("Trajectory Visual")]
    [SerializeField] private float _trajectoryWidth = 0.025f;
    [SerializeField] private float _maxTrajectoryTime = 1.5f;

    [Header("Spawn")]
    [SerializeField] private Transform _throwSpawnPoint;

    private LineRenderer _lineRenderer;
    private bool _isAiming;
    private Camera _mainCam;
    private PlayerIdentity _playerIdentity;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
        _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = _trajectoryWidth;
            _lineRenderer.endWidth = _trajectoryWidth;
            _lineRenderer.positionCount = 0;
        }
    }

    private void Start()
    {
        _mainCam = GetComponentInChildren<Camera>(true);

        if (_mainCam == null)
            _mainCam = Camera.main;
    }

    private void OnDisable()
    {
        CancelAiming();
    }

    private void Update()
    {
        if (!IsMine())
            return;



        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (InGameChatUI.IsChatOpen)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
            return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (_isAiming)
                CancelAiming();
            else
                StartAiming();
        }

        if (!_isAiming)
            return;

        // 조준 중에는 무조건 총기 입력 차단
        IsLocalScannerAiming = true;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BlockWeaponInputThisFrame();

            UseSkill();

            CancelAiming();
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            BlockWeaponInputThisFrame();

            CancelAiming();
            return;
        }
    }

    private void LateUpdate()
    {
        if (!IsMine())
            return;

        if (_isAiming)
            DrawTrajectory();
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    private void StartAiming()
    {
        if (!CanUsePurchasedSkill(SkillSlot.C))
            return;

        _isAiming = true;
        IsLocalScannerAiming = true;

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.startWidth = _trajectoryWidth;
            _lineRenderer.endWidth = _trajectoryWidth;
        }
    }

    private void CancelAiming()
    {
        _isAiming = false;
        IsLocalScannerAiming = false;

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.C))
            return;

        if (!IsMine())
            return;

        if (NetworkManager.Instance == null)
            return;

        Vector3 spawnPos = GetSpawnPosition();
        Vector3 velocity = GetThrowVelocity();

        NetworkManager.Instance.SendScannerThrow(
            spawnPos.x,
            spawnPos.y,
            spawnPos.z,
            velocity.x,
            velocity.y,
            velocity.z
        );

        Debug.Log(
            $"[SCANNER THROW REQUEST] " +
            $"spawn={spawnPos}, velocity={velocity}"
        );
    }

    private void DrawTrajectory()
    {
        if (_lineRenderer == null)
            return;

        Vector3 startPos = GetSpawnPosition();
        Vector3 velocity = GetThrowVelocity();
        Vector3 gravity = Physics.gravity;

        int count = Mathf.Max(2, _lineResolution);
        _lineRenderer.positionCount = count;

        float spacing = _maxTrajectoryTime / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float t = i * spacing;

            Vector3 point =
                startPos +
                velocity * t +
                0.5f * gravity * t * t;

            _lineRenderer.SetPosition(i, point);
        }
    }

    private Vector3 GetThrowVelocity()
    {
        Camera cam = GetActiveCamera();

        Vector3 aimDir;

        if (cam != null)
        {
            aimDir = cam.transform.forward;
        }
        else
        {
            aimDir = transform.forward;
        }

        aimDir.Normalize();

        // 너무 아래로 던지면 바로 바닥에 박히니까 최소 상향 보정
        if (aimDir.y < -0.25f)
            aimDir.y = -0.25f;

        aimDir.Normalize();

        float pitchBonus = Mathf.Clamp01(aimDir.y);
        float finalForce = _baseForce + pitchBonus * _baseForce * 0.35f;

        return aimDir * finalForce;
    }

    private Vector3 GetSpawnPosition()
    {
        if (_throwSpawnPoint != null)
            return _throwSpawnPoint.position;

        Camera cam = GetActiveCamera();

        if (cam != null)
        {
            return cam.transform.position +
                   cam.transform.forward * 0.6f +
                   Vector3.down * 0.15f;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        return transform.position +
               Vector3.up * 1.6f +
               forward * 0.8f;
    }

    private Camera GetActiveCamera()
    {
        if (_mainCam != null && _mainCam.enabled)
            return _mainCam;

        Camera[] cameras = GetComponentsInChildren<Camera>(true);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].enabled)
                return cameras[i];
        }

        if (Camera.main != null)
            return Camera.main;

        return _mainCam;
    }
}