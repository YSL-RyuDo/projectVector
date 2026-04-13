using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    public PlayerState State { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerItem Item { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        State = GetComponent<PlayerState>();
        Movement = GetComponent<PlayerMovement>();
        Item = GetComponent<PlayerItem>();
    }
}