using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TimelineObject))]
public class EnemyReplayObject : MonoBehaviour
{
    private static readonly Dictionary<string, EnemyReplayObject> enemiesById =
        new Dictionary<string, EnemyReplayObject>();

    [Header("Components")]
    [SerializeField] private enemyAI enemyAi;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Collider[] gameplayColliders;
    [SerializeField] private Renderer[] renderers;

    [SerializeField] private Rigidbody enemyBody;

    private bool startingUseGravity;
    private bool startingIsKinematic;
    private TimelineObject timelineObject;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Vector3 startingScale;

    public string EnemyId =>
        timelineObject != null
            ? timelineObject.TimelineId
            : gameObject.name;

    public bool IsDead { get; private set; }
    public bool IsPlaybackDriven { get; private set; }

    public static IEnumerable<EnemyReplayObject> AllEnemies =>
        enemiesById.Values;

    private void Awake()
    {
        timelineObject = GetComponent<TimelineObject>();

        if (enemyAi == null)
        {
            enemyAi = GetComponent<enemyAI>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (gameplayColliders == null || gameplayColliders.Length == 0)
        {
            gameplayColliders = GetComponentsInChildren<Collider>(true);
        }

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (enemyBody == null)
        {
            enemyBody = GetComponent<Rigidbody>();
        }

        if (enemyBody != null)
        {
            startingUseGravity = enemyBody.useGravity;
            startingIsKinematic = enemyBody.isKinematic;
        }

        startingPosition = transform.position;
        startingRotation = transform.rotation;
        startingScale = transform.localScale;
    }

    private void OnEnable()
    {
        RegisterEnemy();
    }

    private void OnDisable()
    {
        if (enemiesById.TryGetValue(
                EnemyId,
                out EnemyReplayObject registeredEnemy) &&
            registeredEnemy == this)
        {
            enemiesById.Remove(EnemyId);
        }
    }

    public static bool TryGetEnemy(
        string enemyId,
        out EnemyReplayObject enemy)
    {
        return enemiesById.TryGetValue(enemyId, out enemy);
    }

    public static void SetAllPlaybackDriven(bool playbackDriven)
    {
        foreach (EnemyReplayObject enemy in enemiesById.Values)
        {
            if (enemy != null)
            {
                enemy.SetPlaybackDriven(playbackDriven);
            }
        }
    }

    public static void ResetAllToStartingState()
    {
        foreach (EnemyReplayObject enemy in enemiesById.Values)
        {
            if (enemy != null)
            {
                enemy.ResetToStartingState();
            }
        }
    }

    public void SetPlaybackDriven(bool playbackDriven)
    {
        IsPlaybackDriven = playbackDriven;
        RefreshComponents();
    }

    public void SetDead(bool dead)
    {
        IsDead = dead;
        RefreshComponents();
    }

    public bool TryKill(string sourceProjectileId)
    {
        if (IsDead)
        {
            return false;
        }

        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager == null || !manager.IsRecording)
        {
            return false;
        }

        bool recorded = manager.RecordEnemyDied(
            EnemyId,
            sourceProjectileId,
            transform.position,
            transform.rotation);

        if (recorded)
        {
            SetDead(true);
        }

        return recorded;
    }

        public void ApplyRecordedPose(
            Vector3 position,
            Quaternion rotation)
        {
            if (IsDead)
            {
                return;
            }

            transform.SetPositionAndRotation(
                position,
                rotation);

            if (agent != null && agent.enabled)
            {
                agent.nextPosition = position;
            }
        }

    public Vector3 GetVelocity()
    {
        if (agent != null && agent.enabled)
        {
            return agent.velocity;
        }

        return Vector3.zero;
    }

    public void ResetToStartingState()
    {
        IsDead = false;

        transform.SetPositionAndRotation(startingPosition, startingRotation);
        transform.localScale = startingScale;

        RefreshComponents();

        if (agent != null && agent.enabled)
        {
            agent.Warp(startingPosition);
            agent.ResetPath();
        }
    }

    private void RefreshComponents()
    {
        bool visibleAndCollidable = !IsDead;
        bool aiEnabled = !IsDead && !IsPlaybackDriven;

        if (enemyAi != null)
        {
            enemyAi.enabled = aiEnabled;
        }

        if (agent != null)
        {
            if (agent.enabled != aiEnabled)
            {
                agent.enabled = aiEnabled;
            }

            if (agent.enabled)
            {
                agent.Warp(transform.position);
            }
        }

        if (gameplayColliders != null)
        {
            foreach (Collider enemyCollider in gameplayColliders)
            {
                if (enemyCollider != null)
                {
                    enemyCollider.enabled = visibleAndCollidable;
                }
            }
        }

        if (renderers != null)
        {
            foreach (Renderer enemyRenderer in renderers)
            {
                if (enemyRenderer != null)
                {
                    enemyRenderer.enabled = visibleAndCollidable;
                }
            }
        }

        if (enemyBody != null)
        {
            if (IsPlaybackDriven || IsDead)
            {
                if (!enemyBody.isKinematic)
                {
                    enemyBody.linearVelocity = Vector3.zero;
                    enemyBody.angularVelocity = Vector3.zero;
                }

                enemyBody.useGravity = false;
                enemyBody.isKinematic = true;
            }
            else
            {
                enemyBody.isKinematic = startingIsKinematic;
                enemyBody.useGravity = startingUseGravity;
                enemyBody.WakeUp();
            }
        }
    }

    private void RegisterEnemy()
    {
        if (enemiesById.TryGetValue(
                EnemyId,
                out EnemyReplayObject existingEnemy) &&
            existingEnemy != this)
        {
            Debug.LogWarning(
                $"Duplicate enemy timeline ID '{EnemyId}'. " +
                "Generate a different ID on one enemy.");
        }

        enemiesById[EnemyId] = this;
    }
}
