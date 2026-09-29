using UnityEngine;
using UnityEngine.AI;

public class TutoEnemyAI : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private Transform player;

    [Header("추적 설정")]
    [SerializeField] private float stopDistance = 2f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (player == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }
}