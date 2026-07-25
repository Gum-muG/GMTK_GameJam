using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 25f;
    public float lifetime = 5f;

    private string timelineId;
    private bool replayVisualOnly;
    private bool initialized;
    private bool ending;

    private void Start()
    {
        if (!initialized)
        {
            TimelineObject timelineObject = GetComponent<TimelineObject>();

            timelineId = timelineObject != null
                ? timelineObject.TimelineId
                : gameObject.name;
        }

        Invoke(nameof(Expire), lifetime);
    }

    public void Initialize(
        string projectileId,
        bool isReplayVisualOnly,
        float remainingLifetime)
    {
        timelineId = projectileId;
        replayVisualOnly = isReplayVisualOnly;
        lifetime = Mathf.Max(0.01f, remainingLifetime);
        initialized = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ending)
        {
            return;
        }

        ending = true;

        if (!replayVisualOnly)
        {
            CharacterSwapManager.instance?
                .RecordProjectileDespawned(timelineId);

            CharacterDeathManager.instance?
                .TryKillFromProjectile(collision.gameObject, timelineId);
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

        if (!replayVisualOnly)
        {
            CharacterSwapManager.instance?
                .RecordProjectileDespawned(timelineId);
        }

        Destroy(gameObject);
    }
}
