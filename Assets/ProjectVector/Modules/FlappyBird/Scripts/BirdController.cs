using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BirdController : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Jump Effect")]
    [SerializeField] private Transform birdModel;
    [SerializeField] private float effectDuration = 0.08f;
    [SerializeField] private float stretchAmount = 0.15f;

    private Rigidbody rb;
    private Vector3 originalScale;
    private Coroutine jumpEffect;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (birdModel != null)
            originalScale = birdModel.localScale;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
            PlayJumpEffect();
        }
    }

    // 실제 점프
    private void Jump()
    {
        rb.linearVelocity = new Vector3(0f, jumpForce, 0f);
    }

    // 외형 연출
    private void PlayJumpEffect()
    {
        if (birdModel == null)
            return;

        if (jumpEffect != null)
            StopCoroutine(jumpEffect);

        jumpEffect = StartCoroutine(JumpAnimation());
    }

    private IEnumerator JumpAnimation()
    {
        Vector3 squash = new Vector3(
            originalScale.x * (1f + stretchAmount),
            originalScale.y * (1f - stretchAmount),
            originalScale.z
        );

        Vector3 stretch = new Vector3(
            originalScale.x * (1f - stretchAmount),
            originalScale.y * (1f + stretchAmount),
            originalScale.z
        );

        yield return ChangeScale(birdModel.localScale, squash);
        yield return ChangeScale(squash, stretch);
        yield return ChangeScale(stretch, originalScale);

        jumpEffect = null;
    }

    private IEnumerator ChangeScale(Vector3 start, Vector3 end)
    {
        float time = 0f;

        while (time < effectDuration)
        {
            time += Time.deltaTime;

            birdModel.localScale = Vector3.Lerp(
                start,
                end,
                Mathf.Clamp01(time / effectDuration)
            );

            yield return null;
        }

        birdModel.localScale = end;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "PipePass")
        {
            FlappyGameManager gameManager =
                FindFirstObjectByType<FlappyGameManager>();

            if (gameManager != null)
                gameManager.AddPipeCount();

            other.enabled = false;
        }
    }
}