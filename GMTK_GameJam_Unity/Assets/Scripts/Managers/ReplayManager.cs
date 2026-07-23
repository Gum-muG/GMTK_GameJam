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
    // Keeps data of replay in a scriptable object
    public ReplayContainer replayContainer;
    // List of objects being recorded
    public List<IReplayObject> replayObjects;
    public State currentState;
    // Time between snapshots
    public float snapshotDelta;

    private float snapshotDeltaTotal;
    private void Awake()
    {
        instance = this;
        replayContainer.Init();
    }

    public void StartRecording()
    {
        currentState = State.Record;
    }
    public void StartPlayback()
    {
        currentState = State.Playback;
    }
    public void Stop()
    {
        currentState = State.Idle;
    }
    // Adds object to list of recorded objects
    public void Register(IReplayObject replayObject)
    {
        if(replayObjects == null)
        {
            replayObjects = new List<IReplayObject>();
        }
        replayObjects.Add(replayObject);
    }

    public void FixedUpdate()
    {
        // If recording, take snapshot everty snapshotDelta seconds
        if(currentState == State.Record)
        {
            snapshotDeltaTotal += Time.fixedDeltaTime;
            if(snapshotDeltaTotal >= snapshotDelta)
            {
                TakeSnapshot();
                snapshotDeltaTotal -= snapshotDelta;
            }
        }
    }

    private int snapshotIndex = 0;
    private float time = 0;
    // Creates snapshot data and saves the info to it. Then adds to container
    private void TakeSnapshot()
    {
        Debug.Log("Snapshot");
        time += Time.fixedDeltaTime;
        SnapshotData snapshotData = new SnapshotData(time);

        foreach(IReplayObject replayObject in replayObjects)
        {
            snapshotData.AddObjectSnapshot(((UnityEngine.Object)replayObject).name, replayObject.SaveSnapshot());
        }
        replayContainer.AddSnapshot(snapshotData);
        snapshotIndex++;
    }
}

// Interface to allow different classes to communicate
public interface IReplayObject
{
    SnapshotInfo SaveSnapshot();
    void LoadSnapshot(SnapshotInfo snapshotInfo);
}
// Snapshot data of a frame
public struct SnapshotData
{
    public float frameTime;

    public Dictionary<string, SnapshotInfo> snapshots;

    public SnapshotData(float time)
    {
        frameTime = time;
        snapshots = new Dictionary<string, SnapshotInfo>();
    }
    public void AddObjectSnapshot(string id, SnapshotInfo data)
    {
        snapshots.Add(id, data);
    }
    
}
// Base snapshot info
[System.Serializable]
public class SnapshotInfo
{
    public string id;
}
// Player snapshot info
[System.Serializable]
public class PlayerSnapshotInfo : SnapshotInfo
{
    public Vector3 position;
    public Quaternion rotation;
    public PlayerMovement.MovementState state;
}

