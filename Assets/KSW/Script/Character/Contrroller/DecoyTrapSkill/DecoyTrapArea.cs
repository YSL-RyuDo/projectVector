using UnityEngine;
using System.Collections;

public class DecoyTrapArea : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _gunfireClip;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private float _duration = 5f;
    private bool _isPlaying = false;

    public void PlayDecoy()
    {
        if (_isPlaying) return;

        if (_audioSource == null || _gunfireClip == null) return;

        StartCoroutine(decoyRoutine());
    }

    private IEnumerator decoyRoutine()
    {
        _isPlaying = true;
        float timer = 0;

        while (timer < _duration)
        {
            int burstCount = Random.Range(2, 6);

            for (int i = 0; i < burstCount; i++)
            {
                _audioSource.PlayOneShot(_gunfireClip);
                if (_muzzleFlash != null) _muzzleFlash.Play();

                float shotInterval = Random.Range(0.12f, 0.18f);
                timer += shotInterval;
                yield return new WaitForSeconds(shotInterval);
            }

            float pause = Random.Range(0.5f, 1.2f);
            timer += pause;
            yield return new WaitForSeconds(pause);
        }

        _isPlaying = false;
        Destroy(gameObject);
    }
}