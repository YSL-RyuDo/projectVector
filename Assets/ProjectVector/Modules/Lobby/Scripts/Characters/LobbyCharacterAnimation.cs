using UnityEngine;

public class LobbyCharacterAnimation : MonoBehaviour
{
    private Animator _animator;

    private int _index = 0;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void PlayGreeting()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Greeting");
            _index = 3;
        }
    }
}