using System.Collections.Generic;
using UnityEngine;

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