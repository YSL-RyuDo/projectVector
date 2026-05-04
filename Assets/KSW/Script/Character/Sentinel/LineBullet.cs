using UnityEngine;

public class LineBullet : MonoBehaviour
{
    private LineRenderer lr;
    private Transform _target;
    private Transform _trap;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (_target == null || _trap == null)
        {
            Destroy(gameObject);
            return;
        }

        lr.SetPosition(0, _trap.position);
        lr.SetPosition(1, _target.position);
    }

    public void Setup(Transform trap, Transform target)
    {
        _trap = trap;
        _target = target;
    }
}