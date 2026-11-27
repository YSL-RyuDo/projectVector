using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BirdController : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Jump Effect")]
    [SerializeField] private Transform birdModel;
    [SerializeField] private float effectDuration = 0.08f;
    [SerializeField] private float stretchAmount = 0.15f;

    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invincibleTime = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private Image[] hearts;

    private Rigidbody rb;
    private Vector3 originalScale;
    private Coroutine jumpEffect;

    private int currentHP;
    private bool isInvincible;
    private bool isDead;
    private Renderer[] renderers;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (birdModel != null)
            originalScale = birdModel.localScale;

        currentHP = maxHP;
        renderers = (birdModel != null ? birdModel : transform).GetComponentsInChildren<Renderer>();
        UpdateHearts();
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
            return;
        }

        CheckHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckHit(collision.gameObject);
    }

    private void CheckHit(GameObject other)
    {
        if (other.CompareTag("Pipe"))
        {
            TakeDamage(1);
            return;
        }

        FlappyEnemy obstacle = other.GetComponentInParent<FlappyEnemy>();
        if (obstacle != null && obstacle.isObstacle)
            TakeDamage(1);
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || isDead) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHearts();

        if (currentHP <= 0)
        {
            isDead = true;
            Debug.Log("플레이어 사망");
            return;
        }

        StartCoroutine(InvincibleRoutine());
    }

    // 무적 동안 깜빡임
    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        float t = 0f;
        bool visible = true;

        while (t < invincibleTime)
        {
            visible = !visible;
            SetVisible(visible);
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        SetVisible(true);
        isInvincible = false;
    }

    private void SetVisible(bool on)
    {
        foreach (var r in renderers)
            r.enabled = on;
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].enabled = i < currentHP;
    }
}