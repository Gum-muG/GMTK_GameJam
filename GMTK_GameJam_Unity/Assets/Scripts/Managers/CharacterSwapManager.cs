using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Records the controlled character while replaying the counterpart.
/// </summary>
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
        "Prevents the two character bodies from launching each other when " +
        "they occupy the same recorded position.")]
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

    public float CurrentTime { get; private set; }

    public bool IsRecording =>
        timelineRunning &&
        recordingTrack != null &&
        recordingTrack.CurrentState ==
            CharacterReplayTrack.TrackState.Record;

    public PlayableCharacter ActiveCharacter => activeCharacter;

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

        CurrentTime += Time.fixedDeltaTime;

        recordingTrack.RecordStep(
            CurrentTime,
            Time.fixedDeltaTime);

        playbackTrack?.PlaybackStep(CurrentTime);
        playbackBuildTrack?.ProcessEventsUpTo(CurrentTime);
    }

    public void StartTimeline(PlayableCharacter character)
    {
        if (!ValidateReferences())
        {
            return;
        }

        activeCharacter = character;
        CurrentTime = 0f;
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

        ResetBodyForRecording(GetBody(activeCharacter));
        activeTrack.BeginRecording();
        activeBuildTrack.BeginRecording(platformSpawner);

        inactiveTrack.Stop();
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

        PlayableCharacter previousCharacter = activeCharacter;
        activeCharacter = GetOtherCharacter(activeCharacter);

        ClearAllSpawnedBuildObjects();
        CurrentTime = 0f;

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

        // Clear old physics before moving either character to t = 0.
        ResetBodyForRecording(GetBody(activeCharacter));
        FreezeBodyForPlayback(GetBody(previousCharacter));

        // The newly controlled character starts a replacement take.
        newRecordingTrack.BeginRecording();
        newRecordingBuildTrack.BeginRecording(platformSpawner);

        if (newPlaybackTrack.HasRecording)
        {
            newPlaybackTrack.BeginPlayback();
            newPlaybackBuildTrack.BeginPlayback(platformSpawner);
            newPlaybackBuildTrack.ProcessEventsUpTo(0f);

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

        ApplyCharacterModes();
    }

    public void RestartCurrentTake()
    {
        if (!timelineRunning ||
            recordingTrack == null ||
            recordingBuildTrack == null)
        {
            return;
        }

        ClearAllSpawnedBuildObjects();
        CurrentTime = 0f;

        ResetBodyForRecording(GetBody(activeCharacter));

        recordingTrack.BeginRecording();
        recordingBuildTrack.BeginRecording(platformSpawner);

        if (playbackTrack != null && playbackTrack.HasRecording)
        {
            FreezeBodyForPlayback(
                GetBody(GetOtherCharacter(activeCharacter)));

            playbackTrack.BeginPlayback();
            playbackBuildTrack?.BeginPlayback(platformSpawner);
            playbackBuildTrack?.ProcessEventsUpTo(0f);
        }

        ApplyCharacterModes();
    }

    public void StopTimeline()
    {
        timelineRunning = false;

        recordingTrack?.Stop();
        playbackTrack?.Stop();

        recordingBuildTrack?.Stop();
        playbackBuildTrack?.Stop();

        SetControlScriptsEnabled(iceControlScripts, false);
        SetControlScriptsEnabled(fireControlScripts, false);
    }

    public bool RecordBuildEvent(
        string objectId,
        PlatformSpawner.BuildType buildType,
        Vector3 position,
        Quaternion rotation,
        GameObject spawnedObject)
    {
        if (!IsRecording || recordingBuildTrack == null)
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
        if (track != null && track.gameObject.activeSelf != visible)
        {
            track.gameObject.SetActive(visible);
        }

        SetControlScriptsEnabled(controlScripts, controlled);

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

    private static void ResetBodyForRecording(Rigidbody body)
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

    private static void FreezeBodyForPlayback(Rigidbody body)
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
        if (iceReplayTrack == null || fireReplayTrack == null)
        {
            return;
        }

        Collider[] iceColliders =
            iceReplayTrack.GetComponentsInChildren<Collider>(true);

        Collider[] fireColliders =
            fireReplayTrack.GetComponentsInChildren<Collider>(true);

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
        if (iceBuildEventTrack == null && iceReplayTrack != null)
        {
            iceBuildEventTrack =
                iceReplayTrack.GetComponent<CharacterBuildEventTrack>();
        }

        if (fireBuildEventTrack == null && fireReplayTrack != null)
        {
            fireBuildEventTrack =
                fireReplayTrack.GetComponent<CharacterBuildEventTrack>();
        }
    }

    private bool ValidateReferences()
    {
        ResolveEventTracks();

        bool valid = true;

        if (iceReplayTrack == null || fireReplayTrack == null)
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

        ValidateControlScriptOwnership(
            iceControlScripts,
            iceReplayTrack,
            "Ice");

        ValidateControlScriptOwnership(
            fireControlScripts,
            fireReplayTrack,
            "Fire");

        if (iceCameraRig != null &&
            fireCameraRig != null &&
            iceCameraRig == fireCameraRig)
        {
            Debug.LogError(
                "Ice Camera Rig and Fire Camera Rig reference the same " +
                "GameObject. Use separate rigs or a shared-camera controller.");

            valid = false;
        }

        return valid;
    }

    private static void ValidateControlScriptOwnership(
        MonoBehaviour[] scripts,
        CharacterReplayTrack expectedTrack,
        string characterName)
    {
        if (scripts == null || expectedTrack == null)
        {
            return;
        }

        Transform expectedRoot = expectedTrack.transform;

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            if (!script.transform.IsChildOf(expectedRoot) &&
                script.transform != expectedRoot)
            {
                Debug.LogWarning(
                    $"{characterName} Control Scripts contains " +
                    $"{script.name}/{script.GetType().Name}, but that " +
                    "component is not under the expected character root.");
            }
        }
    }

    private void ClearAllSpawnedBuildObjects()
    {
        iceBuildEventTrack?.ClearSpawnedObjects();
        fireBuildEventTrack?.ClearSpawnedObjects();
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

    private Rigidbody GetBody(PlayableCharacter character)
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
        if (track != null && !track.gameObject.activeSelf)
        {
            track.gameObject.SetActive(true);
        }
    }
}