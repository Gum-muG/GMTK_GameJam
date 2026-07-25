using System.Collections.Generic;
using UnityEngine;

public class TimelineEventRecorder : MonoBehaviour
{
    public static TimelineEventRecorder instance;

    [SerializeField] private PlatformSpawner platformSpawner;
    [SerializeField] private List<TimelineEventData> events = new List<TimelineEventData>();

    private readonly Dictionary<string, GameObject> spawnedObjects =
        new Dictionary<string, GameObject>();

    private int nextEventIndex;
    private bool isRecording;
    private bool isPlayingBack;

    public float CurrentTime => ReplayManager.instance == null
        ? 0f
        : ReplayManager.instance.CurrentTime;

    private void Awake()
    {
        instance = this;
    }

    public void StartRecording()
    {
        ClearSpawnedTimelineObjects();
        events.Clear();
        nextEventIndex = 0;
        isRecording = true;
        isPlayingBack = false;
    }

    public void StartPlayback()
    {
        ClearSpawnedTimelineObjects();
        nextEventIndex = 0;
        isRecording = false;
        isPlayingBack = true;
    }

    public void Stop()
    {
        isRecording = false;
        isPlayingBack = false;
    }

    public void RecordBuildEvent(
        string objectId,
        PlatformSpawner.BuildType buildType,
        Vector3 position,
        Quaternion rotation,
        GameObject spawnedObject)
    {
        if (!isRecording)
        {
            return;
        }

        TimelineEventData newEvent = new TimelineEventData
        {
            time = CurrentTime,
            eventType = TimelineEventType.Build,
            objectId = objectId,
            buildType = buildType,
            position = position,
            rotation = rotation
        };

        events.Add(newEvent);

        if (spawnedObject != null)
        {
            spawnedObjects[objectId] = spawnedObject;
        }
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
            ReplayEvent(events[nextEventIndex]);
            nextEventIndex++;
        }
    }

    private void ReplayEvent(TimelineEventData timelineEvent)
    {
        switch (timelineEvent.eventType)
        {
            case TimelineEventType.Build:
                if (platformSpawner == null)
                {
                    Debug.LogError("TimelineEventRecorder needs a PlatformSpawner reference.");
                    return;
                }

                if (spawnedObjects.ContainsKey(timelineEvent.objectId))
                {
                    return;
                }

                GameObject spawnedObject = platformSpawner.SpawnRecorded(
                    timelineEvent.buildType,
                    timelineEvent.position,
                    timelineEvent.rotation,
                    timelineEvent.objectId);

                if (spawnedObject != null)
                {
                    spawnedObjects[timelineEvent.objectId] = spawnedObject;
                }
                break;
        }
    }

    private void ClearSpawnedTimelineObjects()
    {
        foreach (GameObject spawnedObject in spawnedObjects.Values)
        {
            if (spawnedObject != null)
            {
                Destroy(spawnedObject);
            }
        }

        spawnedObjects.Clear();
    }
}

public enum TimelineEventType
{
    Build
}

[System.Serializable]
public struct TimelineEventData
{
    public float time;
    public TimelineEventType eventType;
    public string objectId;
    public PlatformSpawner.BuildType buildType;
    public Vector3 position;
    public Quaternion rotation;
}