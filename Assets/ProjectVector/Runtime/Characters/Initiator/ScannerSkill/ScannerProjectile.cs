using UnityEngine;
public class ScannerProjectile : MonoBehaviour
{
    [SerializeField] private ScannerPresentation _presentation;
    [SerializeField] private float _airDeployDelay=.6f;
    private Rigidbody _rb;
    private bool _isMine,_anchored,_requested,_ending;
    private float _age;
    public uint ScannerId {get;private set;}
    public uint OwnerPlayerId {get;private set;}
    public bool IsAnchored=>_anchored;
    private void Awake()=>_rb=GetComponent<Rigidbody>();
    public void Init(uint scannerId,uint ownerPlayerId,bool isMine,Vector3 velocity)
    {
        ScannerId=scannerId;OwnerPlayerId=ownerPlayerId;_isMine=isMine;_age=0;
        if(_rb){_rb.isKinematic=false;_rb.linearVelocity=velocity;_rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
        foreach(var player in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
            foreach(var collider in player.GetComponentsInChildren<Collider>())
                foreach(var own in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(own,collider);
    }
    private void Update()
    {
        if(_anchored || _ending)return;_age+=Time.deltaTime;
        if(_age>=_airDeployDelay)RequestAnchor();
        if(_age>12)Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.GetComponentInParent<PlayerIdentity>())return;
        RequestAnchor();
    }
    private void RequestAnchor()
    {
        if(_requested || _anchored)return;_requested=true;
        FreezeBody();
        if(_isMine && NetworkManager.Instance){var p=transform.position;NetworkManager.Instance.SendScannerAnchor(ScannerId,p.x,p.y,p.z);}
    }
    public void ApplyAnchored(Vector3 position)
    {
        if(_ending)return;transform.position=position;
        FreezeBody();
        if(_anchored)return;_anchored=true;_presentation?.Anchor();
    }
    public void PlayZap(Vector3 targetPosition, bool playSound=true){if(!_ending)_presentation?.Zap(targetPosition,playSound);}
    private void FreezeBody(){if(_rb && !_rb.isKinematic){_rb.linearVelocity=Vector3.zero;_rb.angularVelocity=Vector3.zero;_rb.isKinematic=true;}}
    public void End(bool expired)
    {
        if(_ending)return;_ending=true;
        foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
        _presentation?.End(expired);Destroy(gameObject,.8f);
    }
}
