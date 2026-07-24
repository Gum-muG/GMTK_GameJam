using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A self-contained snapshot track for one character.
///
/// It deliberately uses the same IReplayObject, SnapshotData, SnapshotInfo,
/// and ReplayContainer types as ReplayManager, while keeping its own state.
/// This allows one character track to record while another plays back.
/// </summary>
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
        // Covers script execution orders where child replay components register
        // after this component's Awake.
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

    public void BeginRecording()
    {
        EnsureContainer();
        replayContainer.Init();

        RestoreStartingPose();

        snapshotAccumulator = 0f;
        playbackSnapshotIndex = 0;
        RecordedDuration = 0f;
        HasRecording = false;
        CurrentState = TrackState.Record;

        // Capture t = 0, matching the existing ReplayManager behavior.
        TakeSnapshot(0f);
        HasRecording = true;
    }

    public void RecordStep(float timelineTime, float deltaTime)
    {
        if (CurrentState != TrackState.Record)
        {
            return;
        }

        snapshotAccumulator += deltaTime;
        RecordedDuration = timelineTime;

        // This intentionally mirrors ReplayManager's snapshot loop.
        while (snapshotAccumulator >= snapshotDelta)
        {
            TakeSnapshot(timelineTime);
            snapshotAccumulator -= snapshotDelta;
        }
    }

    public void BeginPlayback()
    {
        if (!HasRecording)
        {
            CurrentState = TrackState.Idle;
            return;
        }

        playbackSnapshotIndex = 0;
        CurrentState = TrackState.Playback;

        ApplySnapshotsUpTo(0f);
    }

    public void PlaybackStep(float timelineTime)
    {
        if (CurrentState != TrackState.Playback)
        {
            return;
        }

        ApplySnapshotsUpTo(timelineTime);
    }

    public void Stop()
    {
        CurrentState = TrackState.Idle;
    }

    public void RestoreStartingPose()
    {
        transform.SetPositionAndRotation(startingPosition, startingRotation);
        transform.localScale = startingScale;
    }

    private void EnsureContainer()
    {
        if (replayContainer != null)
        {
            return;
        }

        replayContainer = ScriptableObject.CreateInstance<ReplayContainer>();
        replayContainer.name = $"{name}_RuntimeReplayContainer";
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
        SnapshotData snapshotData = new SnapshotData(timelineTime);

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