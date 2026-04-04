using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -20f;
    public float jumpPower = 8f;

    [Header("Network")]
    public float sendInterval = 0.05f; // 20hz

    [Header("Animator")]
    [SerializeField] private Animator _mainAnimator;
    [SerializeField] private Animator _shadowAnimator;

    CharacterController cc;
    FPSCameraLook cam;

    float verticalVelocity;

    Vector3 lastSentPos;
    float lastSentYaw;
    float sendTimer;

    public bool CanMove = true;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = GetComponentInChildren<FPSCameraLook>();

        lastSentPos = transform.position;
        lastSentYaw = cam.GetYaw();
    }

    void Update()
    {
        HandleMovement();
        HandleNetworkSend();
    }

    void HandleMovement()
    {
        if (!CanMove)
        {
            if (_mainAnimator != null && _shadowAnimator != null)
            {
                _mainAnimator.SetBool("Walk", false);
                _shadowAnimator.SetBool("Walk", false);
            }
            return;
        }

        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.wKey.isPressed) input.y += 1;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = jumpPower;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        bool isWalking = input.sqrMagnitude > 0;

        Vector3 velocity = move.normalized * speed + Vector3.up * verticalVelocity;
        cc.Move(velocity * Time.deltaTime);

        if (_mainAnimator != null && _shadowAnimator != null)
        {
            _mainAnimator.SetBool("Walk", isWalking && cc.isGrounded);
            _shadowAnimator.SetBool("Walk", isWalking && cc.isGrounded);
        }
    }

    void HandleNetworkSend()
    {
        sendTimer += Time.deltaTime;
        if (sendTimer < sendInterval)
            return;

        sendTimer = 0f;

        Vector3 pos = transform.position;
        float yaw = cam.GetYaw();

        Vector2 curXZ = new Vector2(pos.x, pos.z);
        Vector2 lastXZ = new Vector2(lastSentPos.x, lastSentPos.z);

        bool movedXZ =
            (curXZ - lastXZ).sqrMagnitude > 0.0004f;

        bool movedY =
            Mathf.Abs(pos.y - lastSentPos.y) > 0.01f;

        bool rotated =
            Mathf.Abs(yaw - lastSentYaw) > 0.5f;

        if (!movedXZ && !movedY && !rotated)
            return;

        lastSentPos = pos;
        lastSentYaw = yaw;

        //NetworkManager.Instance.SendMoveState(
        //    pos.x, pos.y, pos.z, yaw
        //);
    }

}
