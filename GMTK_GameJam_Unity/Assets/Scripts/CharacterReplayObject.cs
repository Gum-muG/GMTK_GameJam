using UnityEngine;

[DisallowMultipleComponent]
public class CharacterReplayObject : MonoBehaviour, IReplayObject
{
    [SerializeField] private string replayId = "Character";
    [SerializeField] private bool recordScale;

    private CharacterReplayTrack replayTrack;

    private void Awake()
    {
        replayTrack = GetComponentInParent<CharacterReplayTrack>();
    }

    private void OnEnable()
    {
        FindAndRegister();
    }

    private void Start()
    {
        FindAndRegister();
    }

    private void OnDisable()
    {
        replayTrack?.Unregister(this);
    }

    public SnapshotInfo SaveSnapshot()
    {
        return new CharacterTransformSnapshotInfo
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            includesScale = recordScale
        };
    }

    public void LoadSnapshot(SnapshotInfo snapshotInfo)
    {
        CharacterTransformSnapshotInfo transformSnapshot =
            snapshotInfo as CharacterTransformSnapshotInfo;

        if (transformSnapshot == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            transformSnapshot.position,
            transformSnapshot.rotation);

        if (transformSnapshot.includesScale)
        {
            transform.localScale = transformSnapshot.scale;
        }
    }

    public string GetId()
    {
        return replayId;
    }

    private void FindAndRegister()
    {
        if (replayTrack == null)
        {
            replayTrack = GetComponentInParent<CharacterReplayTrack>();
        }

        if (replayTrack == null)
        {
            Debug.LogError(
                $"{name}: CharacterReplayObject needs a " +
                "CharacterReplayTrack on this object or a parent.");
            return;
        }

        replayTrack.Register(this);
    }
}

[System.Serializable]
public class CharacterTransformSnapshotInfo : SnapshotInfo
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public bool includesScale;
}