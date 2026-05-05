using UnityEngine;
using System.Collections.Generic;

public class Sentinel_SlowTrap : MonoBehaviour
{
    [SerializeField] private float _health = 40f;
    [SerializeField] private float _slowRadius = 10f;
    [SerializeField] private float _slowAmount = 0.5f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private GameObject _lineBulletPrefab;

    private Dictionary<GameObject, GameObject> _activeLines = new Dictionary<GameObject, GameObject>();
    private List<GameObject> _slowedEnemies = new List<GameObject>();

    void OnDestroy()
    {
        foreach (var line in _activeLines.Values)
        {
            if (line != null) Destroy(line);
        }
    }

    void Update()
    {
        if (_health <= 0) Destroy(gameObject);

        Collider[] hits = Physics.OverlapSphere(transform.position, _slowRadius, _enemyLayer);
        List<GameObject> currentEnemies = new List<GameObject>();

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                currentEnemies.Add(hit.gameObject);

                if (!_slowedEnemies.Contains(hit.gameObject))
                {
                    applySlow(hit.gameObject);
                    _slowedEnemies.Add(hit.gameObject);

                    GameObject lineObj = Instantiate(_lineBulletPrefab, transform.position, Quaternion.identity);
                    if (lineObj.TryGetComponent(out LineBullet lb))
                    {
                        lb.Setup(this.transform, hit.transform);
                        _activeLines.Add(hit.gameObject, lineObj);
                    }
                }
            }
        }

        for (int i = _slowedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject target = _slowedEnemies[i];
            if (target == null || !currentEnemies.Contains(target))
            {
                if (target != null) removeSlow(target);

                if (_activeLines.ContainsKey(target))
                {
                    Destroy(_activeLines[target]);
                    _activeLines.Remove(target);
                }
                _slowedEnemies.RemoveAt(i);
            }
        }
    }

    private void applySlow(GameObject target)
    {
        if (target.TryGetComponent(out Animator anim)) anim.speed = _slowAmount;
        if (target.TryGetComponent(out ColorController colorCtrl)) colorCtrl.GetColor("purple");
    }

    private void removeSlow(GameObject target)
    {
        if (target.TryGetComponent(out Animator anim)) anim.speed = 1f;
        if (target.TryGetComponent(out ColorController colorCtrl)) colorCtrl.GetColor("original");
    }
}