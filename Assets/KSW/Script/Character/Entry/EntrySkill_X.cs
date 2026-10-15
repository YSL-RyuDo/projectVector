using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EntrySkill_X : CharacterSkill
{
    public static bool IsLocalBladeMode { get; private set; }

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _firePoint;

    [Header("Blade")]
    [SerializeField] private float _range = 35f;
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("Toenail Visual")]
    [SerializeField] private GameObject _toenailPrefab;
    [SerializeField] private float _toenailLifeTime = 0.12f;

    [Header("Visual")]
    [SerializeField] private Material _bladeLineMaterial;
    [SerializeField] private float _lineDuration = 0.08f;
    [SerializeField] private float _lineStartWidth = 0.08f;
    [SerializeField] private float _lineEndWidth = 0.015f;
    [SerializeField] private float _bladeSpreadVisual = 0.05f;

    [Header("Cast")]
    [SerializeField] private float _fireInterval = 0.18f;

    private PlayerIdentity _playerIdentity;
    private ElectricShock _electricShock;

    private bool _isBladeMode;
    private int _bladeCount;
    private float _nextFireTime;
    private Coroutine _durationCoroutine;

    [Header("Toenail Fan")]
    [SerializeField] private Transform _toenailFanAnchor;
    [SerializeField] private Vector3 _fanLocalCenter = new Vector3(0f, -0.15f, 0.65f);
    [SerializeField] private float _fanSpacing = 0.18f;
    [SerializeField] private float _fanArcAngle = 55f;
    [SerializeField] private float _fanScale = 1.0f;

    [Header("Toenail Orbit Visual")]
    [SerializeField] private float _toenailOrbitRadius = 0.75f;
    [SerializeField] private float _toenailOrbitHeight = 1.35f;
    [SerializeField] private float _toenailOrbitRotateSpeed = 120f;

    private readonly System.Collections.Generic.List<Entry_Toenail> _floatingToenails =
        new System.Collections.Generic.List<Entry_Toenail>();

    private readonly System.Collections.Generic.List<GameObject> _equippedToenails =
        new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        _playerIdentity =
            GetComponent<PlayerIdentity>();

        _electricShock =
            GetComponent<ElectricShock>();

        if (_electricShock == null)
            _electricShock =
                GetComponentInChildren<ElectricShock>(true);
    }

    private void Start()
    {
        if (_cameraTransform == null)
        {
            FPSCameraLook camLook =
                GetComponentInChildren<FPSCameraLook>(true);

            if (camLook != null)
                _cameraTransform = camLook.transform;
        }

        if (_firePoint == null)
        {
            Transform foundFirePoint =
                transform.Find("CameraRoot/WeaponHolder/DropPoint");

            if (foundFirePoint != null)
                _firePoint = foundFirePoint;
        }

        if (_firePoint == null)
            _firePoint = _cameraTransform != null ? _cameraTransform : transform;

        if (_toenailFanAnchor == null)
        {
            if (_firePoint != null)
                _toenailFanAnchor = _firePoint;
            else if (_cameraTransform != null)
                _toenailFanAnchor = _cameraTransform;
            else
                _toenailFanAnchor = transform;
        }
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) return;

        if (Keyboard.current == null ||
            Mouse.current == null)
            return;

        if (InGameChatUI.IsChatOpen)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
            return;

        if (InitiatorSkill_Q.IsLocalDroneMode)
            return;

        if (InitiatorSkill_C.ShouldBlockWeaponInput())
            return;

        EntryDashMotor dashMotor =
            GetComponent<EntryDashMotor>();

        if (dashMotor != null &&
            dashMotor.IsDashing)
            return;

        if (_electricShock != null &&
            _electricShock.IsStunned)
            return;

        if (!_isBladeMode)
        {
            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                ActivateBladeUltimate();
            }

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryFireBlade();
        }
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.X))
            return;

        ActivateBladeUltimate();
    }

    private void ActivateBladeUltimate()
    {
        if (NetworkManager.Instance == null)
            return;

        EntrySkill_E stealth =
            GetComponent<EntrySkill_E>();

        if (stealth != null &&
            stealth.IsStealth)
        {
            NetworkManager.Instance.SendBeastStealthCancel();
        }

        NetworkManager.Instance.SendEntryBladeActivate();

        Debug.Log("[ENTRY BLADE ACTIVATE REQUEST]");
    }

    private void TryFireBlade()
    {
        if (!_isBladeMode)
            return;

        if (_bladeCount <= 0)
            return;

        if (Time.time < _nextFireTime)
            return;

        _nextFireTime =
            Time.time + _fireInterval;

        Vector3 rayOrigin =
            _cameraTransform != null
                ? _cameraTransform.position
                : transform.position + Vector3.up * 1.2f;

        Vector3 dir =
            _cameraTransform != null
                ? _cameraTransform.forward
                : transform.forward;

        dir.Normalize();

        Vector3 visualStart =
            _firePoint != null
                ? _firePoint.position
                : rayOrigin;

        uint targetPlayerId = 0;

        Vector3 hitPoint =
            rayOrigin + dir * _range;

        if (Physics.Raycast(
                rayOrigin,
                dir,
                out RaycastHit hit,
                _range,
                _hitMask,
                QueryTriggerInteraction.Ignore))
        {
            hitPoint =
                hit.point;

            PlayerIdentity target =
                hit.collider.GetComponentInParent<PlayerIdentity>();

            if (target != null &&
                NetworkManager.Instance != null &&
                target.PlayerId != NetworkManager.Instance.MyPlayerId)
            {
                targetPlayerId =
                    target.PlayerId;
            }
        }

        NetworkManager.Instance.SendEntryBladeFire(
            dir,
            targetPlayerId,
            hitPoint
        );

        Debug.Log(
            $"[ENTRY BLADE FIRE REQUEST] target={targetPlayerId}, hitPoint={hitPoint}, visualStart={visualStart}"
        );
    }

    public void ApplyBladeStateFromServer(
        bool active,
        int bladeCount,
        float duration)
    {
        if (active && (GetComponent<PlayerHP>()?.IsDead == true || GetComponent<PlayerDeathController>()?.IsDead == true)) active = false;
        _isBladeMode =
            active;

        _bladeCount =
            bladeCount;

        if (IsMine())
            IsLocalBladeMode = active;

        if (_durationCoroutine != null)
        {
            StopCoroutine(_durationCoroutine);
            _durationCoroutine = null;
        }

        if (active)
        {
            CreateFloatingToenails(_bladeCount);

            _durationCoroutine =
                StartCoroutine(
                    BladeDurationRoutine(duration)
                );

            Debug.Log(
                $"[ENTRY BLADE MODE START] blades={_bladeCount}, duration={duration}"
            );
        }
        else
        {
            _bladeCount = 0;

            ClearFloatingToenails();

            if (IsMine())
                IsLocalBladeMode = false;

            Debug.Log("[ENTRY BLADE MODE END]");
        }
    }

    private IEnumerator BladeDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        _isBladeMode = false;
        _bladeCount = 0;

        ClearFloatingToenails();

        if (IsMine())
            IsLocalBladeMode = false;
    }

    public void PlayBladeFireEffect(
    Vector3 start,
    Vector3 end,
    bool hit,
    int remainingBlades)
    {
        Vector3 fireStart =
            ConsumeOneFloatingToenailPosition();

        SpawnToenailVisual(
            fireStart,
            end
        );

        StartCoroutine(
            BladeLineRoutine(
                fireStart,
                end,
                hit
            )
        );

        _bladeCount =
            remainingBlades;

        RebuildFloatingToenails(
            remainingBlades
        );

        if (_bladeCount <= 0)
        {
            ClearFloatingToenails();

            if (IsMine())
                IsLocalBladeMode = false;

            _isBladeMode = false;
        }

        Debug.Log(
            $"[ENTRY BLADE FIRE EFFECT] hit={hit}, remaining={remainingBlades}"
        );
    }

    private IEnumerator BladeLineRoutine(
        Vector3 start,
        Vector3 end,
        bool hit)
    {
        GameObject lineObj =
            new GameObject("EntryBladeLine");

        LineRenderer lr =
            lineObj.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;

        lr.startWidth =
            _lineStartWidth;

        lr.endWidth =
            _lineEndWidth;

        if (_bladeLineMaterial != null)
        {
            lr.material =
                _bladeLineMaterial;
        }
        else
        {
            lr.material =
                new Material(
                    Shader.Find("Sprites/Default")
                );
        }

        lr.startColor =
            hit ? Color.cyan : Color.white;

        lr.endColor =
            hit ? Color.cyan : Color.gray;

        Vector3 jitter =
            Random.insideUnitSphere *
            _bladeSpreadVisual;

        jitter.y = 0f;

        lr.SetPosition(
            0,
            start
        );

        lr.SetPosition(
            1,
            end + jitter
        );

        yield return new WaitForSeconds(
            _lineDuration
        );

        Destroy(lineObj);
    }

    private void SpawnToenailVisual(
    Vector3 startPosition,
    Vector3 endPosition)
    {
        if (_toenailPrefab == null)
        {
            Debug.LogWarning("[ENTRY BLADE] Toenail Prefab is null");
            return;
        }

        GameObject obj =
            Instantiate(
                _toenailPrefab,
                startPosition,
                Quaternion.identity
            );

        Entry_Toenail toenail =
            obj.GetComponent<Entry_Toenail>();

        if (toenail == null)
            toenail =
                obj.GetComponentInChildren<Entry_Toenail>(true);

        if (toenail != null)
        {
            toenail.Init(
                startPosition,
                endPosition,
                _toenailLifeTime
            );
        }
        else
        {
            Vector3 dir =
                endPosition - startPosition;

            if (dir.sqrMagnitude > 0.0001f)
            {
                obj.transform.rotation =
                    Quaternion.LookRotation(
                        dir.normalized,
                        Vector3.up
                    );
            }

            Destroy(
                obj,
                _toenailLifeTime
            );
        }
    }

    private void CreateFloatingToenails(int count)
    {
        ClearFloatingToenails();

        if (_toenailPrefab == null)
        {
            Debug.LogWarning("[ENTRY BLADE] Toenail Prefab is null.");
            return;
        }

        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            GameObject obj =
                Instantiate(
                    _toenailPrefab,
                    transform.position,
                    Quaternion.identity
                );

            Entry_Toenail toenail =
                obj.GetComponent<Entry_Toenail>();

            if (toenail == null)
                toenail =
                    obj.GetComponentInChildren<Entry_Toenail>(true);

            if (toenail == null)
            {
                toenail =
                    obj.AddComponent<Entry_Toenail>();
            }

            toenail.InitFloating(
                transform,
                i,
                count,
                _toenailOrbitRadius,
                _toenailOrbitHeight,
                _toenailOrbitRotateSpeed
            );

            _floatingToenails.Add(toenail);
        }

        Debug.Log($"[ENTRY BLADE FLOATING TOENAIL CREATE] count={count}");
    }

    private void ClearFloatingToenails()
    {
        for (int i = 0; i < _floatingToenails.Count; i++)
        {
            if (_floatingToenails[i] != null)
            {
                Destroy(
                    _floatingToenails[i].gameObject
                );
            }
        }

        _floatingToenails.Clear();
    }

    private Vector3 ConsumeOneFloatingToenailPosition()
    {
        if (_floatingToenails.Count <= 0)
            return _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1.2f;

        Entry_Toenail toenail =
            _floatingToenails[0];

        _floatingToenails.RemoveAt(0);

        if (toenail == null)
            return _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1.2f;

        Vector3 pos =
            toenail.transform.position;

        Destroy(
            toenail.gameObject
        );

        return pos;
    }

    private void RebuildFloatingToenails(int remainingCount)
    {
        ClearFloatingToenails();

        if (_isBladeMode && remainingCount > 0)
        {
            CreateFloatingToenails(remainingCount);
        }
    }

    private void BuildToenailFan(int count)
    {
        ClearToenailFan();

        if (_toenailPrefab == null)
        {
            Debug.LogWarning("[ENTRY BLADE] Toenail Prefab is null");
            return;
        }

        if (_toenailFanAnchor == null)
        {
            if (_firePoint != null)
                _toenailFanAnchor = _firePoint;
            else if (_cameraTransform != null)
                _toenailFanAnchor = _cameraTransform;
            else
                _toenailFanAnchor = transform;
        }

        count =
            Mathf.Clamp(
                count,
                0,
                5
            );

        for (int i = 0; i < count; i++)
        {
            GameObject nail =
                Instantiate(
                    _toenailPrefab,
                    _toenailFanAnchor
                );

            nail.name =
                $"Entry_EquippedToenail_{i}";

            float centerIndex =
                (count - 1) * 0.5f;

            float offsetIndex =
                i - centerIndex;

            float x =
                offsetIndex * _fanSpacing;

            float angle =
                count <= 1
                    ? 0f
                    : Mathf.Lerp(
                        -_fanArcAngle * 0.5f,
                        _fanArcAngle * 0.5f,
                        i / (float)(count - 1)
                    );

            nail.transform.localPosition =
                _fanLocalCenter + new Vector3(x, 0f, 0f);

            nail.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                );

            nail.transform.localScale =
                Vector3.one * _fanScale;

            Entry_Toenail toenail =
                nail.GetComponent<Entry_Toenail>();

            if (toenail == null)
                toenail =
                    nail.GetComponentInChildren<Entry_Toenail>(true);

            if (toenail != null)
                toenail.MakeEquippedVisualOnly();

            _equippedToenails.Add(nail);
        }
    }

    private void RebuildToenailFanToCount(int count)
    {
        count =
            Mathf.Clamp(
                count,
                0,
                5
            );

        if (count <= 0)
        {
            ClearToenailFan();
            return;
        }

        while (_equippedToenails.Count > count)
        {
            int lastIndex =
                _equippedToenails.Count - 1;

            if (_equippedToenails[lastIndex] != null)
                Destroy(_equippedToenails[lastIndex]);

            _equippedToenails.RemoveAt(lastIndex);
        }

        for (int i = 0; i < _equippedToenails.Count; i++)
        {
            GameObject nail =
                _equippedToenails[i];

            if (nail == null)
                continue;

            float centerIndex =
                (count - 1) * 0.5f;

            float offsetIndex =
                i - centerIndex;

            float x =
                offsetIndex * _fanSpacing;

            float angle =
                count <= 1
                    ? 0f
                    : Mathf.Lerp(
                        -_fanArcAngle * 0.5f,
                        _fanArcAngle * 0.5f,
                        i / (float)(count - 1)
                    );

            nail.transform.localPosition =
                _fanLocalCenter + new Vector3(x, 0f, 0f);

            nail.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                );
        }
    }

    private Vector3 GetToenailFireStartPosition(Vector3 fallbackStart)
    {
        if (_equippedToenails.Count > 0)
        {
            GameObject nail =
                _equippedToenails[
                    _equippedToenails.Count - 1
                ];

            if (nail != null)
                return nail.transform.position;
        }

        if (_firePoint != null)
            return _firePoint.position;

        return fallbackStart;
    }

    private void RemoveOneEquippedToenail()
    {
        if (_equippedToenails.Count <= 0)
            return;

        int lastIndex =
            _equippedToenails.Count - 1;

        GameObject nail =
            _equippedToenails[lastIndex];

        _equippedToenails.RemoveAt(lastIndex);

        if (nail != null)
            Destroy(nail);
    }

    private void ClearToenailFan()
    {
        for (int i = 0; i < _equippedToenails.Count; i++)
        {
            if (_equippedToenails[i] != null)
                Destroy(_equippedToenails[i]);
        }

        _equippedToenails.Clear();
    }

    private void OnDestroy()
    {
        ClearFloatingToenails();
        ClearToenailFan();

        if (IsMine())
            IsLocalBladeMode = false;
    }
    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        ApplyBladeStateFromServer(false, 0, 0);
        ClearToenailFan();
    }
}
