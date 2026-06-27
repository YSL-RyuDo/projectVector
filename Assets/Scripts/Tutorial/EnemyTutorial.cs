using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTutorial : MonoBehaviour, IDamageable
{
    public int hp = 10;
    public TMP_Text hpText;

    [Header("Patrol")]
    public Transform leftPoint;
    public Transform rightPoint;

    NavMeshAgent agent;
    Transform targetPoint;

    void Start()
    {
        UpdateHP();

        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        agent.updateRotation = false; 

        targetPoint = rightPoint;

        if (agent.isOnNavMesh)
            agent.SetDestination(targetPoint.position);
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!agent.pathPending && agent.remainingDistance <= 0.3f)
        {
            targetPoint = targetPoint == rightPoint ? leftPoint : rightPoint;
            agent.SetDestination(targetPoint.position);
        }
    }

    void LateUpdate()
    {
        if (hpText != null && Camera.main != null)
        {
            hpText.transform.rotation = Quaternion.LookRotation(
                hpText.transform.position - Camera.main.transform.position
            );
        }
    }

    public void TakeDamage()
    {
      

        hp--;
        UpdateHP();

        if (hp <= 0)
            Destroy(gameObject);
    }

    void UpdateHP()
    {
        if (hpText != null)
            hpText.text = hp.ToString();
    }
}