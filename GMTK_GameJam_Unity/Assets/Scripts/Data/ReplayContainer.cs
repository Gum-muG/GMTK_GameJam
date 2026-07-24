using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReplayContainer", menuName = "Scriptable Objects/ReplayContainer")]

public class ReplayContainer : ScriptableObject
{
    [SerializeField]
    private List<SnapshotData> m_snapshots;

    public void Init()
    {
        m_snapshots = new List<SnapshotData>();
    }

    public void AddSnapshot(SnapshotData snapshot)
    {
        SnapshotData t;
        SnapshotInfo i;
        m_snapshots.Add(snapshot);
        Debug.Log("HERE IS ID" + GetSnapshot(0, out t));
        Debug.Log(t.snapshots.TryGetValue("Player", out i));
        Debug.Log(i.id);
    }

    public bool GetSnapshot(int index, out SnapshotData data)
    {
        if (index >= m_snapshots.Count)
        {
            data = new SnapshotData(-1);
            Debug.Log("Too Big");
            return false;
        }
        if (index < 0)
        {
            Debug.Log("Too Small");
            data = new SnapshotData(-1);
            return false;
        }
        data = m_snapshots[index];
        Debug.Log("Just Right");
        return true;
    }
}
