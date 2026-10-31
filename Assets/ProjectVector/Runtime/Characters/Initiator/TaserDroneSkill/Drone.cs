using UnityEngine;

public class Drone : MonoBehaviour
{
    public uint DroneId { get; private set; }
    public uint OwnerPlayerId { get; private set; }
    public bool IsMine { get; private set; }

    public DroneMovement Movement { get; private set; }
    public TaserGun TaserGun { get; private set; }

    public Transform ViewTransform
    {
        get
        {
            if (_droneCamera != null)
                return _droneCamera.transform;

            return transform;
        }
    }

    private Camera _droneCamera;
    private AudioListener _audioListener;


    public int CurrentHp { get; private set; } = 1;
    public int MaxHp { get; private set; } = 1;

    public void ApplyHp(int hp, int maxHp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, maxHp);
        MaxHp = Mathf.Max(1, maxHp);

        Debug.Log(
            $"[DRONE HP APPLY] droneId={DroneId}, " +
            $"hp={CurrentHp}/{MaxHp}"
        );
    }

    private void Awake()
    {
        Movement = GetComponent<DroneMovement>();
        TaserGun = GetComponentInChildren<TaserGun>(true);

        _droneCamera = GetComponentInChildren<Camera>(true);
        _audioListener = GetComponentInChildren<AudioListener>(true);
    }

    public void Init(
        uint droneId,
        uint ownerPlayerId,
        bool isMine,
        InitiatorSkill_Q ownerSkill)
    {
        DroneId = droneId;
        OwnerPlayerId = ownerPlayerId;
        IsMine = isMine;

        Debug.Log(
            $"[DRONE INIT] droneId={DroneId}, " +
            $"owner={OwnerPlayerId}, " +
            $"isMine={IsMine}, " +
            $"movement={Movement}, " +
            $"taserGun={TaserGun}, " +
            $"camera={_droneCamera}"
        );

        if (Movement != null)
        {
            Movement.Init(this, isMine);
        }
        else
        {
            Debug.LogError("[DRONE INIT] DroneMovement is null");
        }

        if (TaserGun != null)
        {
            TaserGun.Init(this, ownerSkill, isMine);
        }
        else
        {
            Debug.LogError("[DRONE INIT] TaserGun is null");
        }

        SetLocalCameraActive(isMine);
    }

    public void SetLocalCameraActive(bool active)
    {
        if (!IsMine && active)
            return;

        if (_droneCamera != null)
            _droneCamera.enabled = active;
        else
            Debug.LogWarning("[DRONE CAMERA] Camera is null");

        if (_audioListener != null)
            _audioListener.enabled = active;
    }

    public void ApplyRemoteState(
        Vector3 position,
        float yaw,
        float pitch)
    {
        if (Movement == null)
            return;

        Movement.ApplyRemoteState(position, yaw, pitch);
    }


}
