using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterDeathManager : MonoBehaviour
{
    public static CharacterDeathManager instance;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private bool unlockCursorOnGameOver = true;

    private bool gameOver;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError(
                "More than one CharacterDeathManager exists in the scene.");
            enabled = false;
            return;
        }

        instance = this;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public bool TryKillFromProjectile(
        GameObject hitObject,
        string projectileId)
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (gameOver ||
            manager == null ||
            !manager.IsRecording ||
            !manager.IsActiveCharacterObject(hitObject))
        {
            return false;
        }

        TriggerActiveCharacterDeath(projectileId);
        return true;
    }

    public void KillActiveCharacter()
    {
        TriggerActiveCharacterDeath("Environment");
    }

    public void RetryCurrentTake()
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (manager == null)
        {
            return;
        }

        gameOver = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        manager.RestartCurrentTake();
        RestoreGameplayCursor();
    }

    public void RestartLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void TriggerActiveCharacterDeath(
        string sourceProjectileId)
    {
        CharacterSwapManager manager =
            CharacterSwapManager.instance;

        if (gameOver ||
            manager == null ||
            !manager.IsRecording)
        {
            return;
        }

        CharacterSwapManager.PlayableCharacter
            hitCharacter =
                manager.ActiveCharacter;

        Transform activeCharacter =
            manager.ActiveCharacterTransform;

        Vector3 deathPosition =
            activeCharacter != null
                ? activeCharacter.position
                : Vector3.zero;

        Quaternion deathRotation =
            activeCharacter != null
                ? activeCharacter.rotation
                : Quaternion.identity;

        string deathEventId =
            $"Death_{hitCharacter}_{Guid.NewGuid():N}";

        bool recorded =
            manager.RecordCharacterDeath(
                deathEventId,
                sourceProjectileId,
                hitCharacter,
                deathPosition,
                deathRotation);

        if (!recorded)
        {
            return;
        }

        // The other character is still in the past.
        // Leave a marker and allow this recording to continue.
        if (manager.IsActiveCharacterAhead)
        {
            Debug.Log(
                $"{hitCharacter} death marker recorded at " +
                $"{manager.CurrentTime:F2}. Recording continues.");

            return;
        }

        // Both characters have reached this point in time,
        // so the death is now final.
        EnterGameOver(
            hitCharacter,
            sourceProjectileId);
    }

    private void EnterGameOver(
        CharacterSwapManager.PlayableCharacter deadCharacter,
        string sourceId)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;

        CharacterSwapManager.instance?.
            StopTimeline();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (unlockCursorOnGameOver)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }

        Debug.Log(
            $"{deadCharacter} died. Source: {sourceId}");
    }

    public void ResolveRecordedDeath(
        WorldReplayEventData deathEvent)
    {
        if (gameOver)
        {
            return;
        }

        CharacterSwapManager manager =
            CharacterSwapManager.instance;

        if (manager == null)
        {
            return;
        }

        // Changed history prevented the projectile.
        if (manager.IsDeathCausePrevented(
                deathEvent.sourceId,
                deathEvent.time))
        {
            return;
        }

        EnterGameOver(
            deathEvent.character,
            deathEvent.sourceId);
    }

    private static void RestoreGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
