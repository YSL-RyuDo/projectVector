using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFollowPlayer : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int hp = 10;
    public TMP_Text hpText;

    private Transform player;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;
    }

    void Update()
    {
        if (player == null) return;
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        if (Vector3.Distance(agent.destination, player.position) > 0.5f)
        {
            agent.SetDestination(player.position);
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
        {
            Destroy(gameObject);
        }
    }

    void UpdateHP()
    {
        if (hpText != null)
            hpText.text = hp.ToString();
    }
}