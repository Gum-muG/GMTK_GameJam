using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TimelineObject : MonoBehaviour
{
    [SerializeField] private string timelineId;

    public string TimelineId
    {
        get
        {
            EnsureId();
            return timelineId;
        }
    }

    private void Awake()
    {
        EnsureId();
    }

    public void SetTimelineId(string newId)
    {
        if (!string.IsNullOrWhiteSpace(newId))
        {
            timelineId = newId;
        }
    }

    [ContextMenu("Generate New Timeline ID")]
    private void GenerateNewTimelineId()
    {
        timelineId = Guid.NewGuid().ToString("N");
    }

    private void Update()
    {
        Debug.Log("Name: " + name + ", ID: " + TimelineId);
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(timelineId))
        {
            timelineId = Guid.NewGuid().ToString("N");
        }
    }
}
