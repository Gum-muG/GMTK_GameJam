using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterBuildEventTrack : MonoBehaviour
{
    [SerializeField]
    private List<BuildReplayEventData> events =
        new List<BuildReplayEventData>();

    private readonly Dictionary<string, GameObject> spawnedObjects =
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

    public void BeginRecording(PlatformSpawner spawner)
    {
        BeginNewRecording(spawner);
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

    public void BeginPlayback(PlatformSpawner spawner)
    {
        ClearSpawnedObjects();
        RebuildWorldUpTo(spawner, 0f);
        BeginPlayback(spawner, 0f);
    }

    public void Stop()
    {
        isRecording = false;
        isPlayingBack = false;
    }

    public bool RecordBuildEvent(
        float timelineTime,
        string objectId,
        PlatformSpawner.BuildType buildType,
        Vector3 position,
        Quaternion rotation,
        GameObject spawnedObject)
    {
        if (!isRecording)
        {
            return false;
        }

        events.Add(new BuildReplayEventData
        {
            time = timelineTime,
            objectId = objectId,
            buildType = buildType,
            position = position,
            rotation = rotation
        });

        if (spawnedObject != null)
        {
            spawnedObjects[objectId] = spawnedObject;
        }

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
            ReplayBuildEvent(events[nextEventIndex]);
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
            ReplayBuildEvent(events[nextEventIndex]);
            nextEventIndex++;
        }
    }

    public void ClearSpawnedObjects()
    {
        foreach (KeyValuePair<string, GameObject> pair in spawnedObjects)
        {
            if (platformSpawner != null)
            {
                platformSpawner.DespawnRecorded(pair.Key);
            }
            else if (pair.Value != null)
            {
                pair.Value.SetActive(false);
                Destroy(pair.Value);
            }
        }

        spawnedObjects.Clear();
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

    private void ReplayBuildEvent(BuildReplayEventData replayEvent)
    {
        if (platformSpawner == null)
        {
            Debug.LogError(
                $"{name}: CharacterBuildEventTrack needs a PlatformSpawner.");
            return;
        }

        if (spawnedObjects.ContainsKey(replayEvent.objectId))
        {
            return;
        }

        GameObject spawnedObject = platformSpawner.SpawnRecorded(
            replayEvent.buildType,
            replayEvent.position,
            replayEvent.rotation,
            replayEvent.objectId);

        if (spawnedObject != null)
        {
            spawnedObjects[replayEvent.objectId] = spawnedObject;
        }
    }
}

[System.Serializable]
public struct BuildReplayEventData
{
    public float time;
    public string objectId;
    public PlatformSpawner.BuildType buildType;
    public Vector3 position;
    public Quaternion rotation;
}
