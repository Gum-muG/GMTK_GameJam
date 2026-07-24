using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask Ground;
    public LayerMask Player;

    private ProjectileShooter projectileShooterScript;

    //patrolling
    public Vector3 walkPoint;
    public float walkPointRange;

    private bool walkPointSet;

    //attacking
    public float timeBetweenAttacks = 1f;

    private bool alreadyAttacked;

    //ranges
    public float sightRange = 15f;
    public float attackRange = 10f;

    public bool playerInSightRange;
    public bool playerInAttackRange;

    private void Awake()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Could not find a GameObject named Player.");
        }

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
        if (player == null || agent == null)
        {
            return;
        }

        playerInSightRange = Physics.CheckSphere(
            transform.position,
            sightRange,
            Player
        );

        playerInAttackRange = Physics.CheckSphere(
            transform.position,
            attackRange,
            Player
        );

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
        // Stop moving while attacking.
        agent.isStopped = true;
        agent.ResetPath();

        // Face the player without tilting vertically.
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