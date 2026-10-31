using UnityEngine;

public class DroneHitbox : MonoBehaviour
{
    private Drone _drone;

    private void Awake()
    {
        _drone = GetComponentInParent<Drone>();
    }

    public uint DroneId
    {
        get
        {
            if (_drone == null)
                return 0;

            return _drone.DroneId;
        }
    }

    public Drone Drone => _drone;
}