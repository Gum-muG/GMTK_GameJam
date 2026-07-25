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
            Debug.LogError("More than one CharacterDeathManager exists in the scene.");
            enabled = false;
            return;
        }

        instance = this;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public bool TryKillFromProjectile(GameObject hitObject, string projectileId)
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

    private void TriggerActiveCharacterDeath(string sourceId)
    {
        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (gameOver || manager == null || !manager.IsRecording)
        {
            return;
        }

        gameOver = true;

        Transform activeCharacter = manager.ActiveCharacterTransform;

        Vector3 deathPosition = activeCharacter != null
            ? activeCharacter.position
            : Vector3.zero;

        Quaternion deathRotation = activeCharacter != null
            ? activeCharacter.rotation
            : Quaternion.identity;

        string deathEventId =
            $"Death_{manager.ActiveCharacter}_{Guid.NewGuid():N}";

        manager.RecordCharacterDeath(
            deathEventId,
            manager.ActiveCharacter,
            deathPosition,
            deathRotation);

        manager.StopTimeline();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (unlockCursorOnGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Debug.Log($"{manager.ActiveCharacter} died. Source: {sourceId}");
    }

    private static void RestoreGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
