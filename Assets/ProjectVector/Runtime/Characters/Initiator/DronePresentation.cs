using UnityEngine;

public class DronePresentation : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private Transform[] _fans;
    [SerializeField] private AudioSource _motor;
    private Drone _drone;
    private Vector3 _previousPosition;
    private Quaternion _rest;
    private float _spool;
    private void Awake()
    {
        _drone=GetComponent<Drone>(); _previousPosition=transform.position;
        if (_visual) _rest=_visual.localRotation;
    }
    private void LateUpdate()
    {
        if (_drone==null || _drone.DroneId==0) return;
        _spool=Mathf.MoveTowards(_spool,1,Time.deltaTime*3);
        Vector3 velocity=transform.InverseTransformDirection((transform.position-_previousPosition)/Mathf.Max(.001f,Time.deltaTime));
        _previousPosition=transform.position;
        if (_visual) _visual.localRotation=Quaternion.Slerp(_visual.localRotation,
            _rest*Quaternion.Euler(Mathf.Clamp(velocity.z*1.2f,-9,9),0,Mathf.Clamp(-velocity.x*1.5f,-12,12)),1-Mathf.Exp(-8*Time.deltaTime));
        foreach(var fan in _fans) if(fan)fan.Rotate(Vector3.forward,1500*_spool*Time.deltaTime,Space.Self);
        if (_motor) { _motor.volume=.08f*_spool; _motor.pitch=1+Mathf.Clamp(velocity.magnitude,0,8)*.035f; }
    }
}
