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

    void Start()
    {
        target = rightPoint;

        if (hpText != null)
            hpText.text = hp.ToString();
    }

    void Update()
    {
        if (!canMove)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            target = target == rightPoint ? leftPoint : rightPoint;
        }
    }

    public void Damage(int damage)
    {
        hp -= damage;

        if (hpText != null)
            hpText.text = hp.ToString();

        if (hp <= 0)
        {
            if (tutorialManager != null)
                tutorialManager.OnEnemyKilled();

            Destroy(gameObject);
        }
    }
}