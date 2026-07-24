using System.Collections.Generic;
using UnityEngine;

public class TimelineEventRecorder : MonoBehaviour
{
    public static TimelineEventRecorder instance;

    [SerializeField]
    private List<TimelineEventData> events = new List<TimelineEventData>();

    private float timelineTime;
    private int nextEventIndex;

    private bool isRecording;
    private bool isPlayingBack;

    public float CurrentTime => timelineTime;

    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if (isRecording)
        {
            timelineTime += Time.fixedDeltaTime;
        }
        else if (isPlayingBack)
        {
            timelineTime += Time.fixedDeltaTime;
            ProcessEvents();
        }
    }

    public void StartRecording()
    {
        timelineTime = 0f;
        nextEventIndex = 0;

        events.Clear();

        isRecording = true;
        isPlayingBack = false;
    }

    public void StartPlayback()
    {
        timelineTime = 0f;
        nextEventIndex = 0;

        isRecording = false;
        isPlayingBack = true;
    }

    public void Stop()
    {
        isRecording = false;
        isPlayingBack = false;
    }

    public void RecordSpawnEvent(
        string objectId,
        string prefabId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!isRecording)
        {
            return;
        }

        TimelineEventData newEvent = new TimelineEventData
        {
            time = timelineTime,
            eventType = TimelineEventType.Spawn,
            objectId = objectId,
            prefabId = prefabId,
            position = position,
            rotation = rotation
        };

        events.Add(newEvent);
    }

    private void ProcessEvents()
    {
        while (
            nextEventIndex < events.Count &&
            timelineTime >= events[nextEventIndex].time)
        {
            ReplayEvent(events[nextEventIndex]);
            nextEventIndex++;
        }
    }

    private void ReplayEvent(TimelineEventData timelineEvent)
    {
        switch (timelineEvent.eventType)
        {
            case TimelineEventType.Spawn:
                Debug.Log(
                    $"Spawn {timelineEvent.prefabId} " +
                    $"at {timelineEvent.time} seconds");
                break;
        }
    }
}

public enum TimelineEventType
{
    Spawn
}

[System.Serializable]
public struct TimelineEventData
{
    public float time;
    public TimelineEventType eventType;

    public string objectId;
    public string prefabId;

    public Vector3 position;
    public Quaternion rotation;
}