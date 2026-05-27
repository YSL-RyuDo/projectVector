using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController characterController;
    private Vector2 moveInput;
    private Vector3 velocity;

    // FPSCameraLook 스크립트 연동용 (테스트 편의성)
    private FPSCameraLook cameraLook;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cameraLook = GetComponentInChildren<FPSCameraLook>();
    }

    private void Start()
    {
        // 시작하자마자 카메라 스크립트에 본인(루트)을 등록하고 활성화
        if (cameraLook != null)
        {
            cameraLook.SetLocalPlayer(true, transform);
        }
    }

    private void Update()
    {
        // 바닥 정렬 및 중력 처리
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 안정적으로 붙어있도록 살짝 음수값 유지
        }

        // 마우스 커서가 풀려있거나 일시정지 상태면 이동 불가
        if (Cursor.lockState == CursorLockMode.None) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        // 1. 신형 Input System으로 키보드 입력 읽기 (W, A, S, D / 방향키 자동 매핑)
        if (Keyboard.current != null)
        {
            float x = 0f;
            float z = 0f;

            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            moveInput = new Vector2(x, z).normalized;
        }

        // 2. 캐릭터가 바라보는 방향을 기준으로 이동 방향 계산 (회전값 반영)
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // 3. 실제 이동 시키기
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 4. 중력 적용
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        // 테스트용 조준(ADS) 임시 토글 (마우스 우클릭 유지 시 조준)
        if (Mouse.current != null && cameraLook != null)
        {
            cameraLook.SetADS(Mouse.current.rightButton.isPressed);
        }
    }
}