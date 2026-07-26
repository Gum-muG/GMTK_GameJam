using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 25f;
    public float lifetime = 5f;

    private string timelineId;
    private string shooterId;
    private ProjectileFaction faction;

    private bool replayVisualOnly;
    private bool initialized;
    private bool ending;

    private Rigidbody projectileBody;

    private void Awake()
    {
        projectileBody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (!initialized)
        {
            TimelineObject timelineObject = GetComponent<TimelineObject>();

            timelineId = timelineObject != null ? timelineObject.TimelineId : gameObject.name;
        }

        if (!replayVisualOnly)
        {
            Invoke(nameof(Expire), lifetime);
        }
    }

    private void FixedUpdate()
    {
        if (replayVisualOnly || ending || CharacterSwapManager.instance == null)
        {
            return;
        }

        Vector3 velocity = projectileBody != null ? projectileBody.linearVelocity : Vector3.zero;

        CharacterSwapManager.instance.RecordProjectileSample(timelineId, transform.position, transform.rotation, velocity);
    }

    public void Initialize(string projectileId, string sourceShooterId, ProjectileFaction projectileFaction, bool isReplayVisualOnly, float projectileLifetime)
    {
        timelineId = projectileId;
        shooterId = sourceShooterId;
        faction = projectileFaction;
        replayVisualOnly = isReplayVisualOnly;
        lifetime = Mathf.Max(0.01f, projectileLifetime);
        initialized = true;
    }

    public void ApplyReplaySample(Vector3 position, Quaternion rotation, Vector3 recordedVelocity)
    {
        if (!replayVisualOnly)
        {
            return;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ending || replayVisualOnly)
        {
            return;
        }

        ending = true;
        RecordFinalStateAndDespawn();

        if (faction == ProjectileFaction.Enemy)
        {
            CharacterDeathManager.instance?.TryKillFromProjectile(collision.gameObject, timelineId);
        }
        else
        {
            EnemyReplayObject enemy = collision.gameObject.GetComponentInParent<EnemyReplayObject>();

            enemy?.TryKill(timelineId);
        }

        Debug.Log("Projectile hit: " + collision.gameObject.name);
        Destroy(gameObject);
    }

    private void Expire()
    {
        if (ending)
        {
            return;
        }

        ending = true;
        RecordFinalStateAndDespawn();
        Destroy(gameObject);
    }

    private void RecordFinalStateAndDespawn()
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager == null)
        {
            return;
        }

        Vector3 velocity = projectileBody != null ? projectileBody.linearVelocity : Vector3.zero;

        manager.RecordProjectileSample(timelineId, transform.position, transform.rotation, velocity);
        manager.RecordProjectileDespawned(timelineId, transform.position, transform.rotation);
    }
}