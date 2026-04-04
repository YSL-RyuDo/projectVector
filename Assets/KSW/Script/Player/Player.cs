using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    public PlayerState State { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerWeapon Weapon { get; private set; }
    
    public LocalPlayerController localPlayerController { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        State = GetComponent<PlayerState>();
        //Movement = GetComponent<PlayerMovement>();
        Weapon = GetComponent<PlayerWeapon>();
        localPlayerController = GetComponent<LocalPlayerController>();
    }
}