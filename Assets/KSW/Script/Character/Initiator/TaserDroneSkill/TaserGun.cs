using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TaserGun : MonoBehaviour
{
    [SerializeField] private DroneCamera _droneCamera;

    [Header("Taser Settings")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private bool _singleUse = true;

    private Drone _drone;
    private bool _isMine;
    private bool _used;

    [Header("Feedback")]
    [SerializeField] private LineRenderer _taserBeam;
    [SerializeField] private ParticleSystem _muzzleSpark;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private float _beamShowTime = 0.08f;
    [SerializeField] private ParticleSystem _impactSpark;
    private Vector3 _lastImpact;
    private Coroutine _beamRoutine;
    [SerializeField] private DroneCombatVfx _combatVfx;

    public void Init(
        Drone drone,
        InitiatorSkill_Q skill,
        bool isMine)
    {
        _drone = drone;
        _isMine = isMine;
        _used = false;

        if (_droneCamera == null)
            _droneCamera = GetComponentInChildren<DroneCamera>(true);

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = GetComponentInChildren<AudioSource>(true);

        if (_taserBeam != null)
        {
            _taserBeam.enabled = false;
            _taserBeam.useWorldSpace = true;
            _taserBeam.positionCount = 2;
        }

        Debug.Log(
            $"[TASER INIT] droneId={_drone?.DroneId}, " +
            $"isMine={_isMine}, camera={_droneCamera}, beam={_taserBeam}"
        );
    }

    private void Update()
    {
        if (!_isMine)
            return;
        if (!InitiatorSkill_Q.IsLocalDroneMode || DroneMovement.InputBlocked) return;

        if (Mouse.current == null)
            return;

        if (_singleUse && _used)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            FireTaserRequest();
        }
    }

    private void FireTaserRequest()
    {
        if (!_isMine || !InitiatorSkill_Q.IsLocalDroneMode || InitiatorSkill_Q.IsLocalReturning ||
            DroneMovement.InputBlocked || (_singleUse && _used)) return;
        if (_drone == null)
        {
            Debug.LogWarning("[TASER] drone is null");
            return;
        }

        if (_drone.DroneId == 0)
        {
            Debug.LogWarning("[TASER] invalid droneId=0");
            return;
        }

        if (_droneCamera == null)
        {
            Debug.LogWarning("[TASER] DroneCamera is null");
            return;
        }

        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[TASER] NetworkManager.Instance is null");
            return;
        }

        uint targetPlayerId = 0;

        Transform cam = _droneCamera.transform;
        Ray ray = new Ray(cam.position, cam.forward);

        Vector3 startPoint = cam.position;
        Vector3 endPoint = cam.position + cam.forward * _range;

        PlayerIdentity owner = null;
        foreach (var player in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
            if (player.PlayerId == _drone.OwnerPlayerId) { owner = player; break; }
        bool hasHit = WeaponShotTrace.FirstHit(ray, _range, _hitMask, owner, _drone.transform, out RaycastHit hit);

        if (hasHit)
        {
            endPoint = hit.point;

            Debug.DrawRay(
                cam.position,
                cam.forward * hit.distance,
                Color.yellow,
                1f
            );

            PlayerIdentity target =
                hit.collider.GetComponentInParent<PlayerIdentity>();

            if (target != null &&
                target.PlayerId != NetworkManager.Instance.MyPlayerId)
            {
                targetPlayerId = target.PlayerId;
            }

            Debug.Log(
                $"[TASER REQUEST] hit={hit.collider.name}, " +
                $"root={hit.collider.transform.root.name}, " +
                $"targetPlayerId={targetPlayerId}"
            );
        }
        else
        {
            Debug.DrawRay(
                cam.position,
                cam.forward * _range,
                Color.red,
                1f
            );

            Debug.Log("[TASER REQUEST] miss");
        }

        // 핵심: 여기서 즉시 발사 연출 재생
        PlayLocalTaserFeedback(startPoint, endPoint);
        _lastImpact = endPoint;

        NetworkManager.Instance.SendDroneTaserFire(
            _drone.DroneId,
            targetPlayerId
        );

        _used = true;
    }

    private void PlayLocalTaserFeedback(Vector3 start, Vector3 end)
    {
        if (_combatVfx != null)
            _combatVfx.Fire(start + _droneCamera.transform.right * .12f - _droneCamera.transform.up * .10f + _droneCamera.transform.forward * .3f, end);
        else if (_muzzleSpark != null)
            _muzzleSpark.Play();

        if (_audioSource != null && _fireSound != null)
            _audioSource.PlayOneShot(_fireSound);

        if (DroneHUDUI.Instance != null)
        {
            DroneHUDUI.Instance.PlayTaserFireFeedback();
            DroneHUDUI.Instance.SetTaserReady(false);
        }

        if (_combatVfx != null) return;
        if (_taserBeam != null)
        {
            if (_beamRoutine != null) StopCoroutine(_beamRoutine);
            _beamRoutine = StartCoroutine(BeamRoutine(start, end));
        }
        else
        {
            Debug.LogWarning("[TASER FEEDBACK] LineRenderer is null");
        }
    }

    private IEnumerator BeamRoutine(Vector3 start, Vector3 end)
    {
        _taserBeam.enabled = true;
        _taserBeam.positionCount = 2;
        _taserBeam.SetPosition(0, start);
        _taserBeam.SetPosition(1, end);

        yield return new WaitForSeconds(_beamShowTime);

        _taserBeam.enabled = false;
    }

    public bool HasUsedTaser()
    {
        return _used;
    }

    public void PlayServerResult(bool hit, Transform target)
    {
        if (!_isMine && _droneCamera != null)
        {
            Vector3 origin = _droneCamera.transform.position;
            Vector3 end = target != null ? target.position + Vector3.up : origin + _droneCamera.transform.forward * _range;
            if (_combatVfx) _combatVfx.Fire(origin,end);
            else {
                if (_taserBeam != null) { if (_beamRoutine != null) StopCoroutine(_beamRoutine); _beamRoutine=StartCoroutine(BeamRoutine(origin,end)); }
                if (_muzzleSpark) _muzzleSpark.Play();
            }
            if (_audioSource && _fireSound) _audioSource.PlayOneShot(_fireSound,.45f);
        }
        if (hit && _combatVfx != null)
        {
            Vector3 point = _isMine ? _lastImpact : target != null ? target.position + Vector3.up : transform.position;
            _combatVfx.ConfirmImpact(point, transform.position - point);
        }
        else if (hit && _impactSpark != null)
        {
            _impactSpark.transform.position = _isMine ? _lastImpact : target != null ? target.position + Vector3.up : transform.position;
            _impactSpark.Play();
        }
    }

    public void ResetTaser()
    {
        _used = false;

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetTaserReady(true);
    }
}
