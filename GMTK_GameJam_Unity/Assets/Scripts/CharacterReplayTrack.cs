using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterReplayTrack : MonoBehaviour
{
    public enum TrackState
    {
        Idle,
        Record,
        Playback
    }

    [Header("Replay data")]
    [SerializeField] private ReplayContainer replayContainer;

    [Header("Timing")]
    [SerializeField, Min(0.001f)] private float snapshotDelta = 0.02f;

    [Header("Registration")]
    [SerializeField]
    private bool discoverReplayObjectsInChildren = true;

    private readonly List<IReplayObject> replayObjects =
        new List<IReplayObject>();

    private float snapshotAccumulator;
    private int playbackSnapshotIndex;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Vector3 startingScale;

    public TrackState CurrentState { get; private set; } = TrackState.Idle;
    public bool HasRecording { get; private set; }
    public float RecordedDuration { get; private set; }

    private void Awake()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        startingScale = transform.localScale;

        EnsureContainer();

        if (discoverReplayObjectsInChildren)
        {
            DiscoverReplayObjects();
        }
    }

    private void Start()
    {
        if (discoverReplayObjectsInChildren)
        {
            DiscoverReplayObjects();
        }
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

    // Clears this character's previous take and starts again at time zero.
    public void BeginNewRecording()
    {
        EnsureContainer();
        replayContainer.Init();

        RestoreStartingPose();

        snapshotAccumulator = 0f;
        playbackSnapshotIndex = 0;
        RecordedDuration = 0f;
        HasRecording = false;
        CurrentState = TrackState.Record;

        TakeSnapshot(0f);
        HasRecording = true;
    }

    // Resumes this character at its already-recorded timeline cursor.
    // Existing snapshots are preserved.
    public void ResumeRecording(float timelineTime)
    {
        if (!HasRecording)
        {
            BeginNewRecording();
            return;
        }

        SeekTo(timelineTime);

        snapshotAccumulator = 0f;
        RecordedDuration = Mathf.Max(RecordedDuration, timelineTime);
        CurrentState = TrackState.Record;
    }

    public void BeginRecording()
    {
        BeginNewRecording();
    }

    public void RecordStep(float timelineTime, float deltaTime)
    {
        if (CurrentState != TrackState.Record)
        {
            return;
        }

        snapshotAccumulator += deltaTime;
        RecordedDuration = Mathf.Max(RecordedDuration, timelineTime);

        while (snapshotAccumulator >= snapshotDelta)
        {
            TakeSnapshot(timelineTime);
            snapshotAccumulator -= snapshotDelta;
        }
    }

    // Starts sequential playback from a shared-timeline time.
    public void BeginPlayback(float startTime)
    {
        if (!HasRecording)
        {
            CurrentState = TrackState.Idle;
            return;
        }

        playbackSnapshotIndex = 0;
        CurrentState = TrackState.Playback;

        ApplySnapshotsUpTo(startTime);
    }

    public void BeginPlayback()
    {
        BeginPlayback(0f);
    }

    public void PlaybackStep(float timelineTime)
    {
        if (CurrentState != TrackState.Playback)
        {
            return;
        }

        ApplySnapshotsUpTo(timelineTime);
    }

  
    // Restores this track to its most recent snapshot at or before targetTime.
    public void SeekTo(float targetTime)
    {
        if (!HasRecording)
        {
            RestoreStartingPose();
            return;
        }

        playbackSnapshotIndex = 0;
        ApplySnapshotsUpTo(targetTime);
    }

    public void Stop()
    {
        CurrentState = TrackState.Idle;
    }

    public void RestoreStartingPose()
    {
        transform.SetPositionAndRotation(
            startingPosition,
            startingRotation);

        transform.localScale = startingScale;
    }

    private void EnsureContainer()
    {
        if (replayContainer != null)
        {
            return;
        }

        replayContainer =
            ScriptableObject.CreateInstance<ReplayContainer>();

        replayContainer.name =
            $"{name}_RuntimeReplayContainer";

        replayContainer.Init();
    }

    private void DiscoverReplayObjects()
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IReplayObject replayObject)
            {
                Register(replayObject);
            }
        }
    }

    private void TakeSnapshot(float timelineTime)
    {
        SnapshotData snapshotData =
            new SnapshotData(timelineTime);

        foreach (IReplayObject replayObject in replayObjects)
        {
            if (replayObject == null)
            {
                continue;
            }

            SnapshotInfo info =
                replayObject.SaveSnapshot();

            snapshotData.AddObjectSnapshot(
                replayObject.GetId(),
                info);
        }

        replayContainer.AddSnapshot(snapshotData);
    }

    private void ApplySnapshotsUpTo(float targetTime)
    {
        while (replayContainer.GetSnapshot(
                   playbackSnapshotIndex,
                   out SnapshotData snapshot) &&
               snapshot.frameTime <= targetTime)
        {
            foreach (IReplayObject replayObject in replayObjects)
            {
                if (replayObject == null)
                {
                    continue;
                }

                if (snapshot.GetObjectSnapshot(
                        replayObject.GetId(),
                        out SnapshotInfo info))
                {
                    replayObject.LoadSnapshot(info);
                }
            }

            playbackSnapshotIndex++;
        }
    }
}