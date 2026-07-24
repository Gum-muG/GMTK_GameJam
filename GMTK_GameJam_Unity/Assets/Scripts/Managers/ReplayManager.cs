using System.Collections.Generic;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;

    public enum State
    {
        Idle,
        Record,
        Playback
    }

    [Header("Replay data")]
    public ReplayContainer replayContainer;
    public List<IReplayObject> replayObjects = new List<IReplayObject>();

    [Header("Timing")]
    [Min(0.001f)] public float snapshotDelta = 0.02f;

    public State currentState = State.Idle;
    public float CurrentTime { get; private set; }

    // Small compatibility fix only.
    public bool IsRecording => currentState == State.Record;
    public bool IsPlayingBack => currentState == State.Playback;

    private float snapshotAccumulator;
    private int playbackSnapshotIndex;

    private void Awake()
    {
        instance = this;

        if (replayContainer != null)
        {
            replayContainer.Init();
        }
    }

    public void StartRecording()
    {
        if (replayContainer == null)
        {
            Debug.LogError("ReplayManager needs a ReplayContainer.");
            return;
        }

        replayContainer.Init();
        CurrentTime = 0f;
        snapshotAccumulator = 0f;
        playbackSnapshotIndex = 0;
        currentState = State.Record;

        TimelineEventRecorder.instance?.StartRecording();

        // Capture t = 0 so playback has a defined initial state.
        TakeSnapshot();
    }

    public void StartPlayback()
    {
        CurrentTime = 0f;
        snapshotAccumulator = 0f;
        playbackSnapshotIndex = 0;
        currentState = State.Playback;

        TimelineEventRecorder.instance?.StartPlayback();

        // Restore the first snapshot immediately.
        ApplySnapshotsUpTo(CurrentTime);
        TimelineEventRecorder.instance?.ProcessEventsUpTo(CurrentTime);
    }

    public void Stop()
    {
        currentState = State.Idle;
        TimelineEventRecorder.instance?.Stop();
    }

    public void Register(IReplayObject replayObject)
    {
        if (replayObject == null || replayObjects.Contains(replayObject))
        {
            return;
        }

        replayObjects.Add(replayObject);
    }

    public void Unregister(IReplayObject replayObject)
    {
        replayObjects.Remove(replayObject);
    }

    private void FixedUpdate()
    {
        if (currentState == State.Idle)
        {
            return;
        }

        CurrentTime += Time.fixedDeltaTime;

        if (currentState == State.Record)
        {
            snapshotAccumulator += Time.fixedDeltaTime;

            while (snapshotAccumulator >= snapshotDelta)
            {
                TakeSnapshot();
                snapshotAccumulator -= snapshotDelta;
            }
        }
        else if (currentState == State.Playback)
        {
            ApplySnapshotsUpTo(CurrentTime);
            TimelineEventRecorder.instance?.ProcessEventsUpTo(CurrentTime);
        }
    }

    private void TakeSnapshot()
    {
        SnapshotData snapshotData = new SnapshotData(CurrentTime);

        foreach (IReplayObject replayObject in replayObjects)
        {
            if (replayObject == null)
            {
                continue;
            }

            SnapshotInfo info = replayObject.SaveSnapshot();
            snapshotData.AddObjectSnapshot(replayObject.GetId(), info);
        }

        replayContainer.AddSnapshot(snapshotData);
    }

    private void ApplySnapshotsUpTo(float targetTime)
    {
        while (replayContainer.GetSnapshot(playbackSnapshotIndex, out SnapshotData snapshot) &&
               snapshot.frameTime <= targetTime)
        {
            foreach (IReplayObject replayObject in replayObjects)
            {
                if (replayObject == null)
                {
                    continue;
                }

                if (snapshot.GetObjectSnapshot(replayObject.GetId(), out SnapshotInfo info))
                {
                    replayObject.LoadSnapshot(info);
                }
            }

            playbackSnapshotIndex++;
        }
    }
}

public interface IReplayObject
{
    SnapshotInfo SaveSnapshot();
    void LoadSnapshot(SnapshotInfo snapshotInfo);
    string GetId();
}

[System.Serializable]
public struct SnapshotData
{
    public float frameTime;

    [System.NonSerialized]
    public Dictionary<string, SnapshotInfo> snapshots;

    [SerializeReference]
    private List<SnapshotInfo> snapshotList;

    public SnapshotData(float time)
    {
        frameTime = time;
        snapshots = new Dictionary<string, SnapshotInfo>();
        snapshotList = new List<SnapshotInfo>();
    }

    public void AddObjectSnapshot(string id, SnapshotInfo data)
    {
        if (data == null)
        {
            return;
        }

        data.id = id;
        snapshots[id] = data;
        snapshotList.Add(data);
    }

    public bool GetObjectSnapshot(string id, out SnapshotInfo info)
    {
        if (snapshots == null)
        {
            BuildDictionary();
        }

        return snapshots.TryGetValue(id, out info);
    }

    private void BuildDictionary()
    {
        snapshots = new Dictionary<string, SnapshotInfo>();

        if (snapshotList == null)
        {
            return;
        }

        foreach (SnapshotInfo snapshot in snapshotList)
        {
            if (snapshot != null && !string.IsNullOrEmpty(snapshot.id))
            {
                snapshots[snapshot.id] = snapshot;
            }
        }
    }
}

[System.Serializable]
public class SnapshotInfo
{
    public string id;
}

[System.Serializable]
public class PlayerSnapshotInfo : SnapshotInfo
{
    public Vector3 position;
    public Quaternion rotation;
    public PlayerMovement.MovementState state;
}