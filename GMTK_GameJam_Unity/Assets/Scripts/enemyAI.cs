using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask Ground;
    public LayerMask Player;

    private ProjectileShooter projectileShooterScript;

    public Vector3 walkPoint;
    public float walkPointRange;

    private bool walkPointSet;

    public float timeBetweenAttacks = 1f;
    private bool alreadyAttacked;

    public float sightRange = 15f;
    public float attackRange = 10f;

    public bool playerInSightRange;
    public bool playerInAttackRange;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        projectileShooterScript = GetComponentInParent<ProjectileShooter>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component was not found.");
        }

        if (projectileShooterScript == null)
        {
            Debug.LogError("ProjectileShooter component was not found.");
        }
    }

    private void Update()
    {
        ResolveActivePlayer();

        if (player == null || agent == null || !agent.enabled)
        {
            return;
        }

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position);

        playerInSightRange =
            distanceToPlayer <= sightRange;

        playerInAttackRange =
            distanceToPlayer <= attackRange;

        if (playerInAttackRange && playerInSightRange)
        {
            AttackPlayer();
        }
        else if (playerInSightRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrolling();
        }
    }

    private void ResolveActivePlayer()
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager != null && manager.ActiveCharacterTransform != null)
        {
            player = manager.ActiveCharacterTransform;
        }
    }

    private void Patrolling()
    {
        if (!walkPointSet)
        {
            SearchWalkPoint();
        }

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        Vector3 randomPosition = transform.position +
                                 new Vector3(randomX, 0f, randomZ);

        if (NavMesh.SamplePosition(
                randomPosition,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        agent.ResetPath();

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        if (!alreadyAttacked && projectileShooterScript != null)
        {
            projectileShooterScript.FireProjectileAt(player);

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
