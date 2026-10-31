using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    [Header("오디오 소스 및 리소스")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _footstepClips;

    [Header("발소리 간격")]
    [SerializeField] private float _stepInterval = 0.3f;

    private float _nextStepTime;

    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayFootstep()
    {
        if (Time.time < _nextStepTime) return;

        if (_footstepClips == null || _footstepClips.Length == 0 || _audioSource == null) return;

        int randomIndex = Random.Range(0, _footstepClips.Length);
        _audioSource.PlayOneShot(_footstepClips[randomIndex]);

        _nextStepTime = Time.time + _stepInterval;
    }
}