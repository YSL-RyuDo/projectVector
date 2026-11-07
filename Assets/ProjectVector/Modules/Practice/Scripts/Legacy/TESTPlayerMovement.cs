using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TESTPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpPower = 8f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeightOffset = 0.5f;
    [SerializeField] private float crouchSpeed = 8f;

    private CharacterController characterController;
    private FPSCameraLook cameraLook;

    private Vector2 moveInput;
    private Vector3 velocity;

    private bool isCrouching;

    private Vector3 standingPos;
    private Vector3 crouchingPos;

    private float weaponWeight = 1f;
    private float currentMoveSpeed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cameraLook = GetComponentInChildren<FPSCameraLook>();

        currentMoveSpeed = moveSpeed;
    }

    private void Start()
    {
        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(true, transform);

            standingPos = cameraLook.transform.localPosition;
            crouchingPos = standingPos - new Vector3(0, crouchHeightOffset, 0);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Cursor.lockState == CursorLockMode.None)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        HandleInput();
        HandleJump();
        HandleMovement();

        if (Mouse.current != null && cameraLook != null && GetComponent<PracticeWeaponInput>() == null)
        {
            cameraLook.SetADS(Mouse.current.rightButton.isPressed);
        }
    }

    private void HandleInput()
    {
        float x = 0;
        float z = 0;

        if (Keyboard.current.wKey.isPressed) z++;
        if (Keyboard.current.sKey.isPressed) z--;
        if (Keyboard.current.aKey.isPressed) x--;
        if (Keyboard.current.dKey.isPressed) x++;

        moveInput = new Vector2(x, z).normalized;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isCrouching = !isCrouching;
        }
    }

    private void HandleJump()
    {
        if (characterController.isGrounded)
        {
            if (velocity.y < -2f)
                velocity.y = -2f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                velocity.y = jumpPower;
                isCrouching = false;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 targetPos = isCrouching
            ? crouchingPos
            : standingPos;

        cameraLook.transform.localPosition =
            Vector3.Lerp(
                cameraLook.transform.localPosition,
                targetPos,
                Time.deltaTime * crouchSpeed);

        float speed = currentMoveSpeed;

        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            speed /= 1.5f;
        }

        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        Vector3 finalVelocity =
            move * speed +
            Vector3.up * velocity.y;

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    public void SetWeaponWeight(float weight)
    {
        weaponWeight = Mathf.Max(1f, weight);
        currentMoveSpeed = moveSpeed - (1f - (1f / weaponWeight));
    }
}
