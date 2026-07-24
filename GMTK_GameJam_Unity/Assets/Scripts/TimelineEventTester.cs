using UnityEngine;

public class TimelineEventTester : MonoBehaviour
{
    private int platformId;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TimelineEventRecorder.instance.StartRecording();
            Debug.Log("Started event recording");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TimelineEventRecorder.instance.RecordSpawnEvent(
                "Platform_" + platformId,
                "Platform",
                transform.position,
                transform.rotation
            );

            Debug.Log("Recorded platform event: Platform_" + platformId);
            platformId++;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TimelineEventRecorder.instance.Stop();
            Debug.Log("Stopped event recording");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TimelineEventRecorder.instance.StartPlayback();
            Debug.Log("Started event playback");
        }
    }
}