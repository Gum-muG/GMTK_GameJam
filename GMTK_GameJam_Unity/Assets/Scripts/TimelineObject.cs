using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TimelineObject : MonoBehaviour
{
    [SerializeField] private string timelineId;

    public string TimelineId => timelineId;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(timelineId))
        {
            timelineId = Guid.NewGuid().ToString("N");
        }
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
}
