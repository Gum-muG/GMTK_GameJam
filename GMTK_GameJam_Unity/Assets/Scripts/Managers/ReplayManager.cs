using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

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
        if (currentState != State.Playback)
        {
            replayContainer.Init();
        }
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

    private int snapshotIndex = 0;
    private float nextSnapshotTime;
    private bool hasNextSnapshot = true;

    private void LoadNextSnapshot()
    {
        if(replayContainer.GetSnapshot(snapshotIndex, out SnapshotData currentSnapshot))
        {
            foreach(IReplayObject obj in replayObjects)
            {
                Debug.Log(obj.GetId());
                if(currentSnapshot.GetObjectSnapshot(obj.GetId(), out SnapshotInfo info))
                {
                    
                    obj.LoadSnapshot(info);
                }
            }
        }
        hasNextSnapshot = replayContainer.GetSnapshot(snapshotIndex , out SnapshotData nextSnapshot);
        Debug.Log("Debig");
        if (hasNextSnapshot)
        {
            nextSnapshotTime = nextSnapshot.frameTime;
        }
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
        } else if (currentState == State.Playback)
        {
            Debug.Log(hasNextSnapshot + " " + time + " " + nextSnapshotTime + " " + snapshotIndex);
            snapshotDeltaTotal += Time.fixedDeltaTime;
            if(snapshotDeltaTotal >= snapshotDelta)
            {
                time += Time.fixedDeltaTime;
                if(time  >= nextSnapshotTime && hasNextSnapshot)
                {
                    time += Time.fixedDeltaTime;
                    Debug.Log("Justin Timberlake");
                    LoadNextSnapshot();
                    snapshotIndex++;
                    
                }
                snapshotDeltaTotal -= snapshotDelta;
            }
            
        }
    }

    
    private float time = 0;
    // Creates snapshot data and saves the info to it. Then adds to container
    private void TakeSnapshot()
    {
        Debug.Log("Snapshot");
        time += Time.fixedDeltaTime;
        SnapshotData snapshotData = new SnapshotData(time);

        foreach(IReplayObject replayObject in replayObjects)
        {
            Debug.Log(((PlayerSnapshotInfo)replayObject.SaveSnapshot()).position);
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
    string GetId();
}
[System.Serializable]
// Snapshot data of a frame
public struct SnapshotData
{
    public float frameTime;

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
        
        snapshots.Add(id, data);
        snapshotList.Add(data);
    }

    public bool GetObjectSnapshot(string id, out SnapshotInfo info)
    {
        if (snapshots == null)
        {
            Debug.Log("MOV");
            BuildDictionary();
        }
        return snapshots.TryGetValue(id, out info);
    }
    private void BuildDictionary()
    {
        snapshots = new Dictionary<string, SnapshotInfo>();
        Debug.Log(snapshotList.Count);
        foreach(SnapshotInfo snap in snapshotList)
        {
            
            snapshots.Add(snap.id, snap);
        }
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

