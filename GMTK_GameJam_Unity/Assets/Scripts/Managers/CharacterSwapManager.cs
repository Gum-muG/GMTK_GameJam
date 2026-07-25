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
    [SerializeField] private CharacterWorldEventTrack iceWorldEventTrack;
    [SerializeField] private MonoBehaviour[] iceControlScripts;
    [SerializeField] private GameObject iceCameraRig;
    [SerializeField] private Rigidbody iceBody;
    [SerializeField] private GameObject iceParticles;

    [Header("Fire")]
    [SerializeField] private CharacterReplayTrack fireReplayTrack;
    [SerializeField] private CharacterBuildEventTrack fireBuildEventTrack;
    [SerializeField] private CharacterWorldEventTrack fireWorldEventTrack;
    [SerializeField] private MonoBehaviour[] fireControlScripts;
    [SerializeField] private GameObject fireCameraRig;
    [SerializeField] private Rigidbody fireBody;
    [SerializeField] private GameObject fireParticles;

    [Header("World events")]
    [SerializeField] private PlatformSpawner platformSpawner;

    [Header("Timeline")]
    [SerializeField] private float timeBudget;
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

    private CharacterWorldEventTrack recordingWorldTrack;
    private CharacterWorldEventTrack playbackWorldTrack;

    private PlayableCharacter activeCharacter;
    private bool timelineRunning;

    private float iceTime;
    private float fireTime;

    public float CurrentTime =>
        GetCharacterTime(activeCharacter);
    public float TimeBudget => timeBudget;
    public float IceTime => iceTime;
    public float FireTime => fireTime;

    public float OtherCharacterTime =>
        GetCharacterTime(
            GetOtherCharacter(activeCharacter));

    public bool IsActiveCharacterAhead =>
        CurrentTime > OtherCharacterTime + 0.001f;

    public bool IsRecording =>
        timelineRunning &&
        recordingTrack != null &&
        recordingTrack.CurrentState ==
            CharacterReplayTrack.TrackState.Record;

    public bool IsTimelineRunning => timelineRunning;

    public PlayableCharacter ActiveCharacter =>
        activeCharacter;

    public Transform ActiveCharacterTransform
    {
        get
        {
            CharacterReplayTrack track =
                GetReplayTrack(activeCharacter);

            return track != null
                ? track.transform
                : null;
        }
    }

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
        timeBudget -= Time.fixedDeltaTime;

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

        // Replay the counterpart world first, then record the resulting
        // shared world into the active character's take.
        playbackTrack?.PlaybackStep(nextTime);
        playbackBuildTrack?.ProcessEventsUpTo(nextTime);
        playbackWorldTrack?.ProcessEventsUpTo(nextTime);

        recordingTrack.RecordStep(
            nextTime,
            elapsedTime);

        recordingWorldTrack?.RecordEnemySnapshots(nextTime);
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
        EnemyReplayObject.ResetAllToStartingState();
        EnemyReplayObject.SetAllPlaybackDriven(false);

        CharacterReplayTrack activeTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack activeBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterWorldEventTrack activeWorldTrack =
            GetWorldTrack(activeCharacter);

        CharacterReplayTrack inactiveTrack =
            GetReplayTrack(GetOtherCharacter(activeCharacter));

        CharacterBuildEventTrack inactiveBuildTrack =
            GetBuildTrack(GetOtherCharacter(activeCharacter));

        CharacterWorldEventTrack inactiveWorldTrack =
            GetWorldTrack(GetOtherCharacter(activeCharacter));

        EnsureCharacterRootActive(activeTrack);

        ResetBodyForRecording(
            GetBody(activeCharacter));

        activeTrack.BeginNewRecording();
        activeBuildTrack.BeginNewRecording(platformSpawner);
        activeWorldTrack.BeginNewRecording(platformSpawner);

        inactiveTrack.Stop();
        inactiveTrack.RestoreStartingPose();
        inactiveBuildTrack.Stop();
        inactiveWorldTrack.Stop();

        recordingTrack = activeTrack;
        recordingBuildTrack = activeBuildTrack;
        recordingWorldTrack = activeWorldTrack;

        playbackTrack = null;
        playbackBuildTrack = null;
        playbackWorldTrack = null;

        ApplyCharacterModes();
        ApplyEnemyMode();
    }

    public void SwapCharacters()
    {
        if (!timelineRunning)
        {
            return;
        }

        recordingTrack?.Stop();
        recordingBuildTrack?.Stop();
        recordingWorldTrack?.Stop();

        PlayableCharacter previousCharacter =
            activeCharacter;

        activeCharacter =
            GetOtherCharacter(activeCharacter);

        CharacterReplayTrack newRecordingTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack newRecordingBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterWorldEventTrack newRecordingWorldTrack =
            GetWorldTrack(activeCharacter);

        CharacterReplayTrack newPlaybackTrack =
            GetReplayTrack(previousCharacter);

        CharacterBuildEventTrack newPlaybackBuildTrack =
            GetBuildTrack(previousCharacter);

        CharacterWorldEventTrack newPlaybackWorldTrack =
            GetWorldTrack(previousCharacter);

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
            newRecordingWorldTrack.BeginNewRecording(
                platformSpawner);
        }
        else
        {
            newRecordingTrack.ResumeRecording(resumeTime);
            newRecordingBuildTrack.ResumeRecording(
                platformSpawner);
            newRecordingWorldTrack.ResumeRecording(
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
            newPlaybackWorldTrack.BeginPlayback(
                platformSpawner,
                resumeTime);

            playbackTrack = newPlaybackTrack;
            playbackBuildTrack = newPlaybackBuildTrack;
            playbackWorldTrack = newPlaybackWorldTrack;
        }
        else
        {
            newPlaybackTrack.Stop();
            newPlaybackBuildTrack.Stop();
            newPlaybackWorldTrack.Stop();

            playbackTrack = null;
            playbackBuildTrack = null;
            playbackWorldTrack = null;
        }

        recordingTrack = newRecordingTrack;
        recordingBuildTrack = newRecordingBuildTrack;
        recordingWorldTrack = newRecordingWorldTrack;

        // Reconstruct construction from both character histories at the newly controlled character's saved absolute timeline position.
        RebuildWorldAt(resumeTime);

        ApplyCharacterModes();
        ApplyEnemyMode();
    }

    public void RestartCurrentTake()
    {
        if (!ValidateReferences())
        {
            return;
        }

        timelineRunning = true;
        SetCharacterTime(activeCharacter, 0f);

        CharacterReplayTrack activeTrack =
            GetReplayTrack(activeCharacter);

        CharacterBuildEventTrack activeBuildTrack =
            GetBuildTrack(activeCharacter);

        CharacterWorldEventTrack activeWorldTrack =
            GetWorldTrack(activeCharacter);

        PlayableCharacter counterpartCharacter =
            GetOtherCharacter(activeCharacter);

        CharacterReplayTrack counterpartTrack =
            GetReplayTrack(counterpartCharacter);

        CharacterBuildEventTrack counterpartBuildTrack =
            GetBuildTrack(counterpartCharacter);

        CharacterWorldEventTrack counterpartWorldTrack =
            GetWorldTrack(counterpartCharacter);

        ResetBodyForRecording(GetBody(activeCharacter));

        activeTrack.BeginNewRecording();
        activeBuildTrack.BeginNewRecording(platformSpawner);
        activeWorldTrack.BeginNewRecording(platformSpawner);

        if (counterpartTrack.HasRecording)
        {
            FreezeBodyForPlayback(GetBody(counterpartCharacter));

            counterpartTrack.BeginPlayback(0f);
            counterpartBuildTrack.BeginPlayback(platformSpawner, 0f);
            counterpartWorldTrack.BeginPlayback(platformSpawner, 0f);

            playbackTrack = counterpartTrack;
            playbackBuildTrack = counterpartBuildTrack;
            playbackWorldTrack = counterpartWorldTrack;
        }
        else
        {
            counterpartTrack.Stop();
            counterpartBuildTrack.Stop();
            counterpartWorldTrack.Stop();

            playbackTrack = null;
            playbackBuildTrack = null;
            playbackWorldTrack = null;
        }

        recordingTrack = activeTrack;
        recordingBuildTrack = activeBuildTrack;
        recordingWorldTrack = activeWorldTrack;

        RebuildWorldAt(0f);
        ApplyCharacterModes();
        ApplyEnemyMode();
    }

    public void StopTimeline()
    {
        timelineRunning = false;

        recordingTrack?.Stop();
        playbackTrack?.Stop();

        recordingBuildTrack?.Stop();
        playbackBuildTrack?.Stop();

        recordingWorldTrack?.Stop();
        playbackWorldTrack?.Stop();

        SetControlScriptsEnabled(
            iceControlScripts,
            false);

        SetControlScriptsEnabled(
            fireControlScripts,
            false);

        EnemyReplayObject.SetAllPlaybackDriven(true);
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

    public bool IsActiveCharacterObject(GameObject target)
    {
        if (target == null || ActiveCharacterTransform == null)
        {
            return false;
        }

        Transform targetTransform = target.transform;

        return targetTransform == ActiveCharacterTransform ||
               targetTransform.IsChildOf(ActiveCharacterTransform);
    }

    public bool RecordProjectileFired(
        string projectileId,
        string shooterId,
        ProjectileFaction faction,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction,
        float speed,
        float lifetime)
    {
        if (!IsRecording || recordingWorldTrack == null)
        {
            return false;
        }

        return recordingWorldTrack.RecordProjectileFired(
            CurrentTime,
            projectileId,
            shooterId,
            faction,
            position,
            rotation,
            direction,
            speed,
            lifetime);
    }

    public bool RecordProjectileSample(
        string projectileId,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity)
    {
        if (!IsRecording || recordingWorldTrack == null)
        {
            return false;
        }

        return recordingWorldTrack.RecordProjectileSample(
            CurrentTime,
            projectileId,
            position,
            rotation,
            velocity);
    }

    public bool RecordProjectileDespawned(
        string projectileId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsRecording || recordingWorldTrack == null)
        {
            return false;
        }

        return recordingWorldTrack.RecordProjectileDespawned(
            CurrentTime,
            projectileId,
            position,
            rotation);
    }

    public bool RecordEnemyDied(
        string enemyId,
        string sourceProjectileId,
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsRecording || recordingWorldTrack == null)
        {
            return false;
        }

        bool recorded = recordingWorldTrack.RecordEnemyDied(
            CurrentTime,
            enemyId,
            sourceProjectileId,
            position,
            rotation);

        if (recorded)
        {
            RefreshCausalState(CurrentTime);
        }

        return recorded;
    }

    public bool RecordCharacterDeath(
        string deathEventId,
        string sourceProjectileId,
        PlayableCharacter character,
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsRecording ||
            recordingWorldTrack == null)
        {
            return false;
        }

        return recordingWorldTrack.RecordCharacterDeath(
            CurrentTime,
            deathEventId,
            sourceProjectileId,
            character,
            position,
            rotation);
    }

    public bool RecordPlatformDespawned(string platformId)
    {
        if (!IsRecording || recordingWorldTrack == null)
        {
            return false;
        }

        return recordingWorldTrack.RecordPlatformDespawned(
            CurrentTime,
            platformId);
    }

    public bool IsEnemyDeadAt(
        string enemyId,
        float timelineTime)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            return false;
        }

        return iceWorldEventTrack.HasEnemyDeathAtOrBefore(
                   enemyId,
                   timelineTime) ||
               fireWorldEventTrack.HasEnemyDeathAtOrBefore(
                   enemyId,
                   timelineTime);
    }

    public bool IsDeathCausePrevented(
        string sourceProjectileId,
        float deathTime)
    {
        if (string.IsNullOrWhiteSpace(sourceProjectileId) ||
            sourceProjectileId == "Environment")
        {
            return false;
        }

        if (!TryGetProjectileFireEvent(
                sourceProjectileId,
                out WorldReplayEventData fireEvent))
        {
            return false;
        }

        return fireEvent.projectileFaction == ProjectileFaction.Enemy &&
               IsEnemyDeadAt(
                   fireEvent.sourceId,
                   fireEvent.time);
    }

    private bool TryGetProjectileFireEvent(
        string projectileId,
        out WorldReplayEventData fireEvent)
    {
        if (iceWorldEventTrack.TryGetProjectileFireEvent(
                projectileId,
                out fireEvent))
        {
            return true;
        }

        return fireWorldEventTrack.TryGetProjectileFireEvent(
            projectileId,
            out fireEvent);
    }

    private void RebuildWorldAt(float timelineTime)
    {
        iceBuildEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime);

        fireBuildEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime);

        EnemyReplayObject.ResetAllToStartingState();

        CharacterWorldEventTrack enemyDriverTrack =
            playbackWorldTrack ?? recordingWorldTrack;

        iceWorldEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime,
            iceWorldEventTrack == enemyDriverTrack);

        fireWorldEventTrack.RebuildWorldUpTo(
            platformSpawner,
            timelineTime,
            fireWorldEventTrack == enemyDriverTrack);

        iceWorldEventTrack.ApplyEnemyDeathsUpTo(timelineTime);
        fireWorldEventTrack.ApplyEnemyDeathsUpTo(timelineTime);

        RefreshCausalState(timelineTime);
    }

    private void RefreshCausalState(float timelineTime)
    {
        iceWorldEventTrack.RefreshDeathMarkers(timelineTime);
        fireWorldEventTrack.RefreshDeathMarkers(timelineTime);
    }

    private void ApplyEnemyMode()
    {
        EnemyReplayObject.SetAllPlaybackDriven(
            playbackWorldTrack != null);
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
            iceVisible,
            iceParticles);

        SetCharacterMode(
            fireReplayTrack,
            fireControlScripts,
            fireCameraRig,
            fireBody,
            fireControlled,
            fireVisible,
            fireParticles);
    }

    private static void SetCharacterMode(
        CharacterReplayTrack track,
        MonoBehaviour[] controlScripts,
        GameObject cameraRig,
        Rigidbody body,
        bool controlled,
        bool visible,
        GameObject particles)
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

        if (particles != null)
        {
            particles.SetActive(visible && !controlled);
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

        if (iceWorldEventTrack == null &&
            iceReplayTrack != null)
        {
            iceWorldEventTrack =
                iceReplayTrack.GetComponent<
                    CharacterWorldEventTrack>();
        }

        if (fireWorldEventTrack == null &&
            fireReplayTrack != null)
        {
            fireWorldEventTrack =
                fireReplayTrack.GetComponent<
                    CharacterWorldEventTrack>();
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

        if (iceWorldEventTrack == null ||
            fireWorldEventTrack == null)
        {
            Debug.LogError(
                "Each character needs a CharacterWorldEventTrack.");

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

    private CharacterWorldEventTrack GetWorldTrack(
        PlayableCharacter character)
    {
        return character == PlayableCharacter.Ice
            ? iceWorldEventTrack
            : fireWorldEventTrack;
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
        iceWorldEventTrack?.ClearSpawnedObjects();
        fireWorldEventTrack?.ClearSpawnedObjects();
    }
}