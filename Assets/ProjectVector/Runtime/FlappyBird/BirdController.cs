using UnityEngine;
using UnityEngine.InputSystem;

public class BirdController : MonoBehaviour
{
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            rb.linearVelocity = new Vector2(0f, jumpForce);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "PipePass")
        {
            FlappyGameManager gameManager =
                FindFirstObjectByType<FlappyGameManager>();

            if (gameManager != null)
            {
                gameManager.AddPipeCount();
            }

            other.enabled = false;
        }
    }
}