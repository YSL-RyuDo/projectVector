using UnityEngine;

public class SmokeGrenade : MonoBehaviour
{
    [SerializeField] private GameObject _smokeEffectPrefab;
    [SerializeField] private float _delayBeforeSmoke = 2f;

    private bool _hasCollided = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasCollided || collision.gameObject.CompareTag("Player")) return;

        _hasCollided = true;
        Invoke(nameof(spawnSmoke), _delayBeforeSmoke);
    }

    private void spawnSmoke()
    {
        if (_smokeEffectPrefab != null)
        {
            GameObject smoke = Instantiate(_smokeEffectPrefab, transform.position, Quaternion.identity);
            _smokeEffectPrefab.GetComponentInChildren<ParticleSystem>().Play();
        }
        Destroy(gameObject);
    }
}