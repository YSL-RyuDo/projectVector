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

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            _range,
            _hitMask,
            QueryTriggerInteraction.Ignore
        );

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

        NetworkManager.Instance.SendDroneTaserFire(
            _drone.DroneId,
            targetPlayerId
        );

        _used = true;
    }

    private void PlayLocalTaserFeedback(Vector3 start, Vector3 end)
    {
        if (_muzzleSpark != null)
            _muzzleSpark.Play();

        if (_audioSource != null && _fireSound != null)
            _audioSource.PlayOneShot(_fireSound);

        if (DroneHUDUI.Instance != null)
        {
            DroneHUDUI.Instance.PlayTaserFireFeedback();
            DroneHUDUI.Instance.SetTaserReady(false);
        }

        if (_taserBeam != null)
        {
            StopCoroutine(nameof(BeamRoutine));
            StartCoroutine(BeamRoutine(start, end));
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

    public void ResetTaser()
    {
        _used = false;

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetTaserReady(true);
    }
}