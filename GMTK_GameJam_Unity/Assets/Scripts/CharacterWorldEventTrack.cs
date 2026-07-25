using System.Collections.Generic;
using UnityEngine;

public enum WorldReplayEventType
{
    ProjectileFired,
    ProjectileSample,
    ProjectileDespawned,
    EnemySnapshot,
    EnemyDied,
    CharacterDied,
    PlatformDespawned
}

public enum ProjectileFaction
{
    Player,
    Enemy
}

[System.Serializable]
public struct WorldReplayEventData
{
    public float time;
    public WorldReplayEventType eventType;

    public string objectId;
    public string sourceId;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 direction;
    public Vector3 velocity;

    public float speed;
    public float lifetime;

    public ProjectileFaction projectileFaction;
    public CharacterSwapManager.PlayableCharacter character;
}

[DisallowMultipleComponent]
public class CharacterWorldEventTrack : MonoBehaviour
{
    [SerializeField]
    private List<WorldReplayEventData> events =
        new List<WorldReplayEventData>();

    [Header("Visuals")]
    [SerializeField] private GameObject deathMarkerPrefab;

    private readonly Dictionary<string, GameObject> spawnedProjectiles =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, GameObject> spawnedDeathMarkers =
        new Dictionary<string, GameObject>();

    private PlatformSpawner platformSpawner;
    private int nextEventIndex;
    private bool isRecording;
    private bool isPlayingBack;

    public bool IsRecording => isRecording;
    public bool IsPlayingBack => isPlayingBack;
    public bool HasEvents => events.Count > 0;

    public void BeginNewRecording(PlatformSpawner spawner)
    {
        platformSpawner = spawner;

        ClearSpawnedObjects();
        events.Clear();
        nextEventIndex = 0;

        isRecording = true;
        isPlayingBack = false;
    }

    public void ResumeRecording(PlatformSpawner spawner)
    {
        platformSpawner = spawner;
        isRecording = true;
        isPlayingBack = false;
    }

    public void BeginPlayback(
        PlatformSpawner spawner,
        float startTime)
    {
        platformSpawner = spawner;
        nextEventIndex = FindFirstEventAfter(startTime);

        isRecording = false;
        isPlayingBack = true;
    }

    public void Stop()
    {
        isRecording = false;
        isPlayingBack = false;
    }

    public bool RecordProjectileFired(
        float timelineTime,
        string projectileId,
        string shooterId,
        ProjectileFaction faction,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction,
        float speed,
        float lifetime)
    {
        if (!isRecording)
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.ProjectileFired,
            objectId = projectileId,
            sourceId = shooterId,
            projectileFaction = faction,
            position = position,
            rotation = rotation,
            direction = direction.normalized,
            speed = speed,
            lifetime = lifetime
        });

        return true;
    }

    public bool RecordProjectileSample(
        float timelineTime,
        string projectileId,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity)
    {
        if (!isRecording || string.IsNullOrWhiteSpace(projectileId))
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.ProjectileSample,
            objectId = projectileId,
            position = position,
            rotation = rotation,
            velocity = velocity
        });

        return true;
    }

    public bool RecordProjectileDespawned(
        float timelineTime,
        string projectileId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!isRecording || string.IsNullOrWhiteSpace(projectileId))
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.ProjectileDespawned,
            objectId = projectileId,
            position = position,
            rotation = rotation
        });

        return true;
    }

    public void RecordEnemySnapshots(float timelineTime)
    {
        if (!isRecording)
        {
            return;
        }

        foreach (EnemyReplayObject enemy in EnemyReplayObject.AllEnemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            events.Add(new WorldReplayEventData
            {
                time = timelineTime,
                eventType = WorldReplayEventType.EnemySnapshot,
                objectId = enemy.EnemyId,
                position = enemy.transform.position,
                rotation = enemy.transform.rotation,
                velocity = enemy.GetVelocity()
            });
        }
    }

    public bool RecordEnemyDied(
        float timelineTime,
        string enemyId,
        string sourceProjectileId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!isRecording || string.IsNullOrWhiteSpace(enemyId))
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.EnemyDied,
            objectId = enemyId,
            sourceId = sourceProjectileId,
            position = position,
            rotation = rotation
        });

        return true;
    }

    public bool RecordCharacterDeath(
        float timelineTime,
        string deathEventId,
        string sourceProjectileId,
        CharacterSwapManager.PlayableCharacter character,
        Vector3 position,
        Quaternion rotation)
    {
        if (!isRecording)
        {
            return false;
        }

        WorldReplayEventData deathEvent =
            new WorldReplayEventData
            {
                time = timelineTime,
                eventType =
                    WorldReplayEventType.CharacterDied,

                objectId = deathEventId,
                sourceId = sourceProjectileId,

                character = character,
                position = position,
                rotation = rotation
            };

        events.Add(deathEvent);

        // Marker appears immediately, but the player remains alive.
        SpawnDeathMarker(deathEvent);

        return true;
    }

    public bool RecordPlatformDespawned(
        float timelineTime,
        string platformId)
    {
        if (!isRecording || string.IsNullOrWhiteSpace(platformId))
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.PlatformDespawned,
            objectId = platformId
        });

        return true;
    }

    public void ProcessEventsUpTo(float timelineTime)
    {
        if (!isPlayingBack)
        {
            return;
        }

        while (nextEventIndex < events.Count &&
            events[nextEventIndex].time <= timelineTime)
        {
            WorldReplayEventData replayEvent =
                events[nextEventIndex];

            ReplayEvent(
                replayEvent,
                true);

            // This only happens during actual playback,
            // not while rebuilding during a swap or seek.
            if (replayEvent.eventType ==
                WorldReplayEventType.CharacterDied)
            {
                CharacterDeathManager.instance?.
                    ResolveRecordedDeath(replayEvent);
            }

            nextEventIndex++;
        }
    }

    public void RebuildWorldUpTo(
        PlatformSpawner spawner,
        float timelineTime,
        bool applyEnemySnapshots)
    {
        platformSpawner = spawner;

        ClearSpawnedObjects();
        nextEventIndex = 0;

        while (nextEventIndex < events.Count &&
               events[nextEventIndex].time <= timelineTime)
        {
            ReplayEvent(events[nextEventIndex], applyEnemySnapshots);
            nextEventIndex++;
        }
    }

    public void ApplyEnemyDeathsUpTo(float timelineTime)
    {
        foreach (WorldReplayEventData replayEvent in events)
        {
            if (replayEvent.time > timelineTime)
            {
                break;
            }

            if (replayEvent.eventType != WorldReplayEventType.EnemyDied)
            {
                continue;
            }

            if (EnemyReplayObject.TryGetEnemy(
                    replayEvent.objectId,
                    out EnemyReplayObject enemy))
            {
                enemy.SetDead(true);
            }
        }
    }

    public bool HasEnemyDeathAtOrBefore(
        string enemyId,
        float timelineTime)
    {
        foreach (WorldReplayEventData replayEvent in events)
        {
            if (replayEvent.time > timelineTime)
            {
                break;
            }

            if (replayEvent.eventType == WorldReplayEventType.EnemyDied &&
                replayEvent.objectId == enemyId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetProjectileFireEvent(
        string projectileId,
        out WorldReplayEventData fireEvent)
    {
        foreach (WorldReplayEventData replayEvent in events)
        {
            if (replayEvent.eventType == WorldReplayEventType.ProjectileFired &&
                replayEvent.objectId == projectileId)
            {
                fireEvent = replayEvent;
                return true;
            }
        }

        fireEvent = default;
        return false;
    }

    public void RefreshDeathMarkers(float timelineTime)
    {
        ClearDeathMarkers();

        foreach (WorldReplayEventData replayEvent in events)
        {
            if (replayEvent.time > timelineTime)
            {
                break;
            }

            if (replayEvent.eventType == WorldReplayEventType.CharacterDied)
            {
                SpawnDeathMarker(replayEvent);
            }
        }
    }

    public void ClearSpawnedObjects()
    {
        foreach (GameObject projectile in spawnedProjectiles.Values)
        {
            DisableAndDestroy(projectile);
        }

        spawnedProjectiles.Clear();
        ClearDeathMarkers();
    }

    private void ReplayEvent(
        WorldReplayEventData replayEvent,
        bool applyEnemySnapshots)
    {
        switch (replayEvent.eventType)
        {
            case WorldReplayEventType.ProjectileFired:
                ReplayProjectileFired(replayEvent);
                break;

            case WorldReplayEventType.ProjectileSample:
                ReplayProjectileSample(replayEvent);
                break;

            case WorldReplayEventType.ProjectileDespawned:
                DespawnReplayProjectile(replayEvent.objectId);
                break;

            case WorldReplayEventType.EnemySnapshot:
                if (applyEnemySnapshots)
                {
                    ReplayEnemySnapshot(replayEvent);
                }
                break;

            case WorldReplayEventType.EnemyDied:
                if (applyEnemySnapshots)
                {
                    ReplayEnemyDied(replayEvent);
                }
                break;

            case WorldReplayEventType.CharacterDied:
                SpawnDeathMarker(replayEvent);
                break;

            case WorldReplayEventType.PlatformDespawned:
                platformSpawner?.DespawnRecorded(replayEvent.objectId);
                break;
        }
    }

    private void ReplayProjectileFired(WorldReplayEventData replayEvent)
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager != null &&
            manager.IsEnemyDeadAt(replayEvent.sourceId, replayEvent.time))
        {
            return;
        }

        if (spawnedProjectiles.ContainsKey(replayEvent.objectId))
        {
            return;
        }

        if (!ProjectileShooter.TryGetShooter(
                replayEvent.sourceId,
                out ProjectileShooter shooter))
        {
            Debug.LogWarning(
                $"Could not find projectile shooter '{replayEvent.sourceId}'.");
            return;
        }

        GameObject projectile = shooter.SpawnReplayProjectile(
            replayEvent.objectId,
            replayEvent.projectileFaction,
            replayEvent.position,
            replayEvent.direction);

        if (projectile != null)
        {
            spawnedProjectiles[replayEvent.objectId] = projectile;
        }
    }

    private void ReplayProjectileSample(WorldReplayEventData replayEvent)
    {
        if (!spawnedProjectiles.TryGetValue(
                replayEvent.objectId,
                out GameObject projectile) ||
            projectile == null)
        {
            return;
        }

        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.ApplyReplaySample(
                replayEvent.position,
                replayEvent.rotation,
                replayEvent.velocity);
        }
        else
        {
            projectile.transform.SetPositionAndRotation(
                replayEvent.position,
                replayEvent.rotation);
        }
    }

    private void ReplayEnemySnapshot(WorldReplayEventData replayEvent)
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager != null &&
            manager.IsEnemyDeadAt(replayEvent.objectId, replayEvent.time))
        {
            return;
        }

        if (EnemyReplayObject.TryGetEnemy(
                replayEvent.objectId,
                out EnemyReplayObject enemy))
        {
            enemy.ApplyRecordedPose(
                replayEvent.position,
                replayEvent.rotation);
        }
    }

    private static void ReplayEnemyDied(WorldReplayEventData replayEvent)
    {
        if (EnemyReplayObject.TryGetEnemy(
                replayEvent.objectId,
                out EnemyReplayObject enemy))
        {
            enemy.SetDead(true);
        }
    }

    private void DespawnReplayProjectile(string projectileId)
    {
        if (!spawnedProjectiles.TryGetValue(
                projectileId,
                out GameObject projectile))
        {
            return;
        }

        DisableAndDestroy(projectile);
        spawnedProjectiles.Remove(projectileId);
    }

    private void SpawnDeathMarker(WorldReplayEventData replayEvent)
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager != null &&
            manager.IsDeathCausePrevented(
                replayEvent.sourceId,
                replayEvent.time))
        {
            RemoveDeathMarker(replayEvent.objectId);
            return;
        }

        if (deathMarkerPrefab == null ||
            spawnedDeathMarkers.ContainsKey(replayEvent.objectId))
        {
            return;
        }

        GameObject marker = Instantiate(
            deathMarkerPrefab,
            replayEvent.position,
            replayEvent.rotation);

        marker.name =
            $"DeathMarker_{replayEvent.character}_{replayEvent.objectId}";

        spawnedDeathMarkers[replayEvent.objectId] = marker;
    }

    private void RemoveDeathMarker(string deathEventId)
    {
        if (!spawnedDeathMarkers.TryGetValue(
                deathEventId,
                out GameObject marker))
        {
            return;
        }

        DisableAndDestroy(marker);
        spawnedDeathMarkers.Remove(deathEventId);
    }

    private void ClearDeathMarkers()
    {
        foreach (GameObject marker in spawnedDeathMarkers.Values)
        {
            DisableAndDestroy(marker);
        }

        spawnedDeathMarkers.Clear();
    }

    private int FindFirstEventAfter(float timelineTime)
    {
        int index = 0;

        while (index < events.Count && events[index].time <= timelineTime)
        {
            index++;
        }

        return index;
    }

    private static void DisableAndDestroy(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(false);
        Destroy(target);
    }
}
