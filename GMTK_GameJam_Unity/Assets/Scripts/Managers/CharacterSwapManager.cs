using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CharacterSwapManager : MonoBehaviour
{
    public static CharacterSwapManager instance;

    public enum PlayableCharacter
    {
        Ice,
        Fire
    }

    [Header("Ice")]
    [SerializeField] private CharacterReplayTrack iceReplayTrack;
    [SerializeField] private CharacterBuildEventTrack iceBuildEventTrack;
    [SerializeField] private MonoBehaviour[] iceControlScripts;
    [SerializeField] private GameObject iceCameraRig;
    [SerializeField] private Rigidbody iceBody;

    [Header("Fire")]
    [SerializeField] private CharacterReplayTrack fireReplayTrack;
    [SerializeField] private CharacterBuildEventTrack fireBuildEventTrack;
    [SerializeField] private MonoBehaviour[] fireControlScripts;
    [SerializeField] private GameObject fireCameraRig;
    [SerializeField] private Rigidbody fireBody;

    [Header("World events")]
    [SerializeField] private PlatformSpawner platformSpawner;

    [Header("Timeline")]
    [SerializeField] private PlayableCharacter startingCharacter =
        PlayableCharacter.Ice;

    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool hideUnrecordedCounterpart = true;

    [Tooltip(
        "Zero or less means no limit. Otherwise each character cursor stops " +
        "at this timeline time.")]
    [SerializeField] private float levelDuration = 60f;

    [Tooltip(
        "Prevents the two character bodies from launching each other when " +
        "their timeline positions overlap.")]
    [SerializeField] private bool ignoreCharacterCollisions = true;

    [Header("Controls")]
    [SerializeField] private KeyCode swapKey = KeyCode.Tab;
    [SerializeField] private KeyCode restartTakeKey = KeyCode.Backspace;

    private CharacterReplayTrack recordingTrack;
    private CharacterReplayTrack playbackTrack;

    private CharacterBuildEventTrack recordingBuildTrack;
    private CharacterBuildEventTrack playbackBuildTrack;

    private PlayableCharacter activeCharacter;
    private bool timelineRunning;

    private float iceTime;
    private float fireTime;

    public float CurrentTime =>
        GetCharacterTime(activeCharacter);

    public float IceTime => iceTime;
    public float FireTime => fireTime;

    public bool IsRecording =>
        timelineRunning &&
        recordingTrack != null &&
        recordingTrack.CurrentState ==
            CharacterReplayTrack.TrackState.Record;

    public PlayableCharacter ActiveCharacter =>
        activeCharacter;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError(
                "More than one CharacterSwapManager exists in the scene.");

            enabled = false;
            return;
        }

        instance = this;

        ResolveEventTracks();

        if (ignoreCharacterCollisions)
        {
            IgnoreCollisionsBetweenCharacters();
        }
    }

    private void Start()
    {
        if (startAutomatically)
        {
            StartTimeline(startingCharacter);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(swapKey))
        {
            SwapCharacters();
        }

        if (Input.GetKeyDown(restartTakeKey))
        {
            RestartCurrentTake();
        }
    }

    private void FixedUpdate()
    {
        if (!timelineRunning || recordingTrack == null)
        {
            return;
        }

        float previousTime = CurrentTime;
        float nextTime =
            previousTime + Time.fixedDeltaTime;

        if (levelDuration > 0f)
        {
            nextTime =
                Mathf.Min(nextTime, levelDuration);
        }

        SetCharacterTime(activeCharacter, nextTime);

        float elapsedTime = nextTime - previousTime;

        if (elapsedTime <= 0f)
        {
            return;
        }

        // Record the active character at its own cursor.
        recordingTrack.RecordStep(
            nextTime,
            elapsedTime);

        // The counterpart is sampled at that same absolute timeline time.
        playbackTrack?.PlaybackStep(nextTime);
        playbackBuildTrack?.ProcessEventsUpTo(nextTime);
    }

    public void StartTimeline(
        PlayableCharacter character)
    {
        if (!ValidateReferences())
        {
            return;
        }

        activeCharacter = character;
        iceTime = 0f;
        fireTime = 0f;
        timelineRunning = true;

        ClearAllSpawnedBuildObjects();

        CharacterReplayTrack activeTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack activeBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterReplayTrack inactiveTrack =
            GetReplayTrack(GetOtherCharacter(activeCharacter));

        CharacterBuildEventTrack inactiveBuildTrack =
            GetBuildTrack(GetOtherCharacter(activeCharacter));

        EnsureCharacterRootActive(activeTrack);

        ResetBodyForRecording(
            GetBody(activeCharacter));

        activeTrack.BeginNewRecording();
        activeBuildTrack.BeginNewRecording(platformSpawner);

        inactiveTrack.Stop();
        inactiveTrack.RestoreStartingPose();
        inactiveBuildTrack.Stop();

        recordingTrack = activeTrack;
        recordingBuildTrack = activeBuildTrack;

        playbackTrack = null;
        playbackBuildTrack = null;

        ApplyCharacterModes();
    }

    public void SwapCharacters()
    {
        if (!timelineRunning)
        {
            StartTimeline(startingCharacter);
            return;
        }

        recordingTrack?.Stop();
        recordingBuildTrack?.Stop();

        PlayableCharacter previousCharacter =
            activeCharacter;

        activeCharacter =
            GetOtherCharacter(activeCharacter);

        CharacterReplayTrack newRecordingTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack newRecordingBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterReplayTrack newPlaybackTrack =
            GetReplayTrack(previousCharacter);

        CharacterBuildEventTrack newPlaybackBuildTrack =
            GetBuildTrack(previousCharacter);

        EnsureCharacterRootActive(newRecordingTrack);
        EnsureCharacterRootActive(newPlaybackTrack);

        float resumeTime =
            GetCharacterTime(activeCharacter);

        // The first time a character becomes active, its cursor is zero and it begins a fresh take. Later swaps resume its existing take.
        bool resumingExistingTake =
            newRecordingTrack.HasRecording;

        if (!resumingExistingTake)
        {
            SetCharacterTime(activeCharacter, 0f);
            resumeTime = 0f;

            newRecordingTrack.BeginNewRecording();
            newRecordingBuildTrack.BeginNewRecording(
                platformSpawner);
        }
        else
        {
            newRecordingTrack.ResumeRecording(resumeTime);
            newRecordingBuildTrack.ResumeRecording(
                platformSpawner);
        }

        ResetBodyForRecording(
            GetBody(activeCharacter));

        if (newPlaybackTrack.HasRecording)
        {
            FreezeBodyForPlayback(
                GetBody(previousCharacter));

            newPlaybackTrack.BeginPlayback(resumeTime);
            newPlaybackBuildTrack.BeginPlayback(
                platformSpawner,
                resumeTime);

            playbackTrack = newPlaybackTrack;
            playbackBuildTrack = newPlaybackBuildTrack;
        }
        else
        {
            newPlaybackTrack.Stop();
            newPlaybackBuildTrack.Stop();

            playbackTrack = null;
            playbackBuildTrack = null;
        }

        recordingTrack = newRecordingTrack;
        recordingBuildTrack = newRecordingBuildTrack;

        // Reconstruct construction from both character histories at the newly controlled character's saved absolute timeline position.
        RebuildWorldAt(resumeTime);

        ApplyCharacterModes();
    }

    public void RestartCurrentTake()
    {
        if (!timelineRunning)
        {
            return;
        }

        SetCharacterTime(activeCharacter, 0f);

        CharacterReplayTrack activeTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack activeBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterReplayTrack counterpartTrack =
            GetReplayTrack(
                GetOtherCharacter(activeCharacter));

        CharacterBuildEventTrack counterpartBuildTrack =
            GetBuildTrack(
                GetOtherCharacter(activeCharacter));

        ResetBodyForRecording(
            GetBody(activeCharacter));

        activeTrack.BeginNewRecording();
        activeBuildTrack.BeginNewRecording(platformSpawner);

        if (counterpartTrack.HasRecording)
        {
            FreezeBodyForPlayback(
                GetBody(
                    GetOtherCharacter(activeCharacter)));

            counterpartTrack.BeginPlayback(0f);
            counterpartBuildTrack.BeginPlayback(
                platformSpawner,
                0f);

            playbackTrack = counterpartTrack;
            playbackBuildTrack = counterpartBuildTrack;
        }
        else
        {
            counterpartTrack.Stop();
            counterpartBuildTrack.Stop();

            playbackTrack = null;
            playbackBuildTrack = null;
        }

        recordingTrack = activeTrack;
        recordingBuildTrack = activeBuildTrack;

        RebuildWorldAt(0f);
        ApplyCharacterModes();
    }

    public void StopTimeline()
    {
        timelineRunning = false;

        recordingTrack?.Stop();
        playbackTrack?.Stop();

        recordingBuildTrack?.Stop();
        playbackBuildTrack?.Stop();

        SetControlScriptsEnabled(
            iceControlScripts,
            false);

        SetControlScriptsEnabled(
            fireControlScripts,
            false);
    }

    public bool RecordBuildEvent(
        string objectId,
        PlatformSpawner.BuildType buildType,
        Vector3 position,
        Quaternion rotation,
        GameObject spawnedObject)
    {
        if (!IsRecording ||
            recordingBuildTrack == null)
        {
            return false;
        }

        return recordingBuildTrack.RecordBuildEvent(
            CurrentTime,
            objectId,
            buildType,
            position,
            rotation,
            spawnedObject);
    }

    private void RebuildWorldAt(float timelineTime)
    {
        iceBuildEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime);

        fireBuildEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime);
    }

    private void ApplyCharacterModes()
    {
        bool iceControlled =
            activeCharacter == PlayableCharacter.Ice;

        bool fireControlled = !iceControlled;

        bool iceVisible =
            iceControlled ||
            (playbackTrack == iceReplayTrack &&
             iceReplayTrack.HasRecording);

        bool fireVisible =
            fireControlled ||
            (playbackTrack == fireReplayTrack &&
             fireReplayTrack.HasRecording);

        if (!hideUnrecordedCounterpart)
        {
            iceVisible = true;
            fireVisible = true;
        }

        SetCharacterMode(
            iceReplayTrack,
            iceControlScripts,
            iceCameraRig,
            iceBody,
            iceControlled,
            iceVisible);

        SetCharacterMode(
            fireReplayTrack,
            fireControlScripts,
            fireCameraRig,
            fireBody,
            fireControlled,
            fireVisible);
    }

    private static void SetCharacterMode(
        CharacterReplayTrack track,
        MonoBehaviour[] controlScripts,
        GameObject cameraRig,
        Rigidbody body,
        bool controlled,
        bool visible)
    {
        if (track != null &&
            track.gameObject.activeSelf != visible)
        {
            track.gameObject.SetActive(visible);
        }

        SetControlScriptsEnabled(
            controlScripts,
            controlled);

        if (cameraRig != null)
        {
            cameraRig.SetActive(controlled);
        }

        if (body != null)
        {
            if (controlled)
            {
                ResetBodyForRecording(body);
            }
            else
            {
                FreezeBodyForPlayback(body);
            }
        }
    }

    private static void SetControlScriptsEnabled(
        MonoBehaviour[] scripts,
        bool enabledState)
    {
        if (scripts == null)
        {
            return;
        }

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
            {
                script.enabled = enabledState;
            }
        }
    }

    private static void ResetBodyForRecording(
        Rigidbody body)
    {
        if (body == null)
        {
            return;
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
    }

    private static void FreezeBodyForPlayback(
        Rigidbody body)
    {
        if (body == null)
        {
            return;
        }

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.useGravity = false;
        body.isKinematic = true;
    }

    private void IgnoreCollisionsBetweenCharacters()
    {
        if (iceReplayTrack == null ||
            fireReplayTrack == null)
        {
            return;
        }

        Collider[] iceColliders =
            iceReplayTrack.GetComponentsInChildren<Collider>(
                true);

        Collider[] fireColliders =
            fireReplayTrack.GetComponentsInChildren<Collider>(
                true);

        foreach (Collider iceCollider in iceColliders)
        {
            if (iceCollider == null)
            {
                continue;
            }

            foreach (Collider fireCollider in fireColliders)
            {
                if (fireCollider != null)
                {
                    Physics.IgnoreCollision(
                        iceCollider,
                        fireCollider,
                        true);
                }
            }
        }
    }

    private void ResolveEventTracks()
    {
        if (iceBuildEventTrack == null &&
            iceReplayTrack != null)
        {
            iceBuildEventTrack =
                iceReplayTrack.GetComponent<
                    CharacterBuildEventTrack>();
        }

        if (fireBuildEventTrack == null &&
            fireReplayTrack != null)
        {
            fireBuildEventTrack =
                fireReplayTrack.GetComponent<
                    CharacterBuildEventTrack>();
        }
    }

    private bool ValidateReferences()
    {
        ResolveEventTracks();

        bool valid = true;

        if (iceReplayTrack == null ||
            fireReplayTrack == null)
        {
            Debug.LogError(
                "Assign both character replay tracks.");

            valid = false;
        }

        if (iceBuildEventTrack == null ||
            fireBuildEventTrack == null)
        {
            Debug.LogError(
                "Each character needs a CharacterBuildEventTrack.");

            valid = false;
        }

        if (platformSpawner == null)
        {
            Debug.LogError(
                "Assign the PlatformSpawner.");

            valid = false;
        }

        if (iceReplayTrack != null &&
            fireReplayTrack != null &&
            iceReplayTrack == fireReplayTrack)
        {
            Debug.LogError(
                "Ice and Fire cannot use the same replay track.");

            valid = false;
        }

        if (iceCameraRig != null &&
            fireCameraRig != null &&
            iceCameraRig == fireCameraRig)
        {
            Debug.LogError(
                "Ice Camera Rig and Fire Camera Rig reference the same object.");

            valid = false;
        }

        return valid;
    }

    private float GetCharacterTime(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? iceTime
            : fireTime;
    }

    private void SetCharacterTime(
        PlayableCharacter character,
        float time)
    {
        time = Mathf.Max(0f, time);

        if (character == PlayableCharacter.Ice)
        {
            iceTime = time;
        }
        else
        {
            fireTime = time;
        }
    }

    private CharacterReplayTrack GetReplayTrack(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? iceReplayTrack
            : fireReplayTrack;
    }

    private CharacterBuildEventTrack GetBuildTrack(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? iceBuildEventTrack
            : fireBuildEventTrack;
    }

    private Rigidbody GetBody(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? iceBody
            : fireBody;
    }

    private static PlayableCharacter GetOtherCharacter(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? PlayableCharacter.Fire
            : PlayableCharacter.Ice;
    }

    private static void EnsureCharacterRootActive(
        CharacterReplayTrack track)
    {
        if (track != null &&
            !track.gameObject.activeSelf)
        {
            track.gameObject.SetActive(true);
        }
    }

    private void ClearAllSpawnedBuildObjects()
    {
        iceBuildEventTrack?.ClearSpawnedObjects();
        fireBuildEventTrack?.ClearSpawnedObjects();
    }
}