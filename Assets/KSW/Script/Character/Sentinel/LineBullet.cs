using UnityEngine;

public class LineBullet : MonoBehaviour
{
    private LineRenderer _lr;
    private Transform _target;
    private Transform _trap;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (_target == null || _trap == null)
        {
            Destroy(gameObject);
            return;
        }

        _lr.SetPosition(0, _trap.position);
        _lr.SetPosition(1, _target.position);
    }

    public void Setup(Transform trap, Transform target)
    {
        _trap = trap;
        _target = target;
    }
}