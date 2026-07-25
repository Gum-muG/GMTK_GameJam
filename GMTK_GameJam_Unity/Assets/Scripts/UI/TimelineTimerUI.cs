using TMPro;
using UnityEngine;

public class TimelineTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text activeCharacterText;
    [SerializeField] private TMP_Text activeTimerText;
    [SerializeField] private TMP_Text iceTimerText;
    [SerializeField] private TMP_Text fireTimerText;

    private CharacterSwapManager timelineManager;

    private void Start()
    {
        timelineManager = CharacterSwapManager.instance;
    }

    private void Update()
    {
        if (timelineManager == null)
        {
            timelineManager = CharacterSwapManager.instance;

            if (timelineManager == null)
            {
                return;
            }
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (activeCharacterText != null)
        {
            activeCharacterText.text =
                timelineManager.ActiveCharacter.ToString();
        }

        if (activeTimerText != null)
        {
            activeTimerText.text =
                FormatTime(timelineManager.CurrentTime);
        }

        if (iceTimerText != null)
        {
            iceTimerText.text =
                $"Ice: {FormatTime(timelineManager.IceTime)}";
        }

        if (fireTimerText != null)
        {
            fireTimerText.text =
                $"Fire: {FormatTime(timelineManager.FireTime)}";
        }
    }

    private string FormatTime(float time)
    {
        time = Mathf.Max(0f, time);

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int hundredths = Mathf.FloorToInt((time * 100f) % 100f);

        return $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }
}