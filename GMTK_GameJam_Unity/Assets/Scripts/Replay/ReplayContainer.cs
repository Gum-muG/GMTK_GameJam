using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReplayContainer", menuName = "Scriptable Objects/ReplayContainer")]
public class ReplayContainer : ScriptableObject
{
    [SerializeField] private List<SnapshotData> m_snapshots;

    public void Init()
    {
        m_snapshots = new List<SnapshotData>();
    }

    public void AddSnapshot(SnapshotData snapshot)
    {
        m_snapshots.Add(snapshot);
    }

    public void RemoveSnapshotsAfter(float timelineTime)
    {
        if (m_snapshots == null)
        {
            return;
        }

        m_snapshots.RemoveAll(snapshot => snapshot.frameTime > timelineTime);
    }

    public bool GetSnapshot(int index, out SnapshotData data)
    {
        if (index >= m_snapshots.Count)
        {
            data = new SnapshotData(-1);
            return false;
        }
        if (index < 0)
        {
            data = new SnapshotData(-1);
            return false;
        }
        data = m_snapshots[index];
        return true;
    }
}