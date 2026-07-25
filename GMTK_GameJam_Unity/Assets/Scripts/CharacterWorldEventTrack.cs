using System.Collections.Generic;
using UnityEngine;

public enum WorldReplayEventType
{
    ProjectileFired,
    ProjectileDespawned,
    CharacterDied,
    PlatformDespawned
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

    public float speed;
    public float lifetime;

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
            position = position,
            rotation = rotation,
            direction = direction.normalized,
            speed = speed,
            lifetime = lifetime
        });

        return true;
    }

    public bool RecordProjectileDespawned(
        float timelineTime,
        string projectileId)
    {
        if (!isRecording || string.IsNullOrWhiteSpace(projectileId))
        {
            return false;
        }

        events.Add(new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.ProjectileDespawned,
            objectId = projectileId
        });

        return true;
    }

    public bool RecordCharacterDeath(
        float timelineTime,
        string deathEventId,
        CharacterSwapManager.PlayableCharacter character,
        Vector3 position,
        Quaternion rotation)
    {
        if (!isRecording)
        {
            return false;
        }

        WorldReplayEventData deathEvent = new WorldReplayEventData
        {
            time = timelineTime,
            eventType = WorldReplayEventType.CharacterDied,
            objectId = deathEventId,
            character = character,
            position = position,
            rotation = rotation
        };

        events.Add(deathEvent);
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
            ReplayEvent(events[nextEventIndex], timelineTime, false);
            nextEventIndex++;
        }
    }

    public void RebuildWorldUpTo(
        PlatformSpawner spawner,
        float timelineTime)
    {
        platformSpawner = spawner;

        ClearSpawnedObjects();
        nextEventIndex = 0;

        while (nextEventIndex < events.Count &&
               events[nextEventIndex].time <= timelineTime)
        {
            ReplayEvent(events[nextEventIndex], timelineTime, true);
            nextEventIndex++;
        }
    }

    public void ClearSpawnedObjects()
    {
        foreach (GameObject projectile in spawnedProjectiles.Values)
        {
            DisableAndDestroy(projectile);
        }

        foreach (GameObject marker in spawnedDeathMarkers.Values)
        {
            DisableAndDestroy(marker);
        }

        spawnedProjectiles.Clear();
        spawnedDeathMarkers.Clear();
    }

    private void ReplayEvent(
        WorldReplayEventData replayEvent,
        float requestedTime,
        bool rebuilding)
    {
        switch (replayEvent.eventType)
        {
            case WorldReplayEventType.ProjectileFired:
                ReplayProjectileFired(replayEvent, requestedTime, rebuilding);
                break;

            case WorldReplayEventType.ProjectileDespawned:
                DespawnReplayProjectile(replayEvent.objectId);
                break;

            case WorldReplayEventType.CharacterDied:
                SpawnDeathMarker(replayEvent);
                break;

            case WorldReplayEventType.PlatformDespawned:
                platformSpawner?.DespawnRecorded(replayEvent.objectId);
                break;
        }
    }

    private void ReplayProjectileFired(
        WorldReplayEventData replayEvent,
        float requestedTime,
        bool rebuilding)
    {
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

        float elapsedTime = rebuilding
            ? Mathf.Max(0f, requestedTime - replayEvent.time)
            : 0f;

        GameObject projectile = shooter.SpawnReplayProjectile(
            replayEvent.objectId,
            replayEvent.position,
            replayEvent.direction,
            replayEvent.speed,
            replayEvent.lifetime,
            elapsedTime);

        if (projectile != null)
        {
            spawnedProjectiles[replayEvent.objectId] = projectile;
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

    private int FindFirstEventAfter(float timelineTime)
    {
        int index = 0;

        while (index < events.Count &&
               events[index].time <= timelineTime)
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
