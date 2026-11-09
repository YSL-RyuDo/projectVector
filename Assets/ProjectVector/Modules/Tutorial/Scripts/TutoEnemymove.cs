
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutoEnemyMove : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Move")]
    public bool canMove = false;

    public Transform leftPoint;
    public Transform rightPoint;
    public float speed = 3f;

    [Header("HP")]
    public int hp = 100;
    public TMP_Text hpText;

    private Transform target;
    private bool isDead = false;

    private Dictionary<int, float> _slowEffects =
        new Dictionary<int, float>();

    public bool IsDead => isDead;

    void Start()
    {
        target = rightPoint;

        if (hpText != null)
            hpText.text = hp.ToString();
    }

    void Update()
    {
        if (!canMove || isDead)
            return;

        if (target == null)
            return;

        float currentSpeed = speed;

        foreach (float slowAmount in _slowEffects.Values)
            currentSpeed = Mathf.Min(currentSpeed, speed * slowAmount);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            target = target == rightPoint ? leftPoint : rightPoint;
        }
    }

    public void ApplyTrapSlow(int trapId, float slowAmount)
    {
        _slowEffects[trapId] = Mathf.Clamp(slowAmount, 0.05f, 1f);
    }

    public void RemoveTrapSlow(int trapId)
    {
        _slowEffects.Remove(trapId);
    }

    public void Damage(int damage)
    {
        if (isDead)
            return;

        hp -= damage;

        if (hpText != null)
            hpText.text = Mathf.Max(0, hp).ToString();

        if (hp <= 0)
        {
            isDead = true;

            if (tutorialManager != null)
                tutorialManager.OnEnemyKilled();

            Destroy(gameObject);
        }
    }
}
