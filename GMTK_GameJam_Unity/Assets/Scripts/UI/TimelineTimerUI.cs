using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text activeCharacterText;
    [SerializeField] private TMP_Text budgetTimerText;
    [SerializeField] private TMP_Text iceTimerText;
    [SerializeField] private TMP_Text fireTimerText;
    [SerializeField] private RectTransform fireTimelineImage;
    [SerializeField] private RectTransform iceTimelineImage;
    [SerializeField] private RectTransform timelinePanel;

    private float originalBudget;

    private CharacterSwapManager timelineManager;

    private void Start()
    {
        timelineManager = CharacterSwapManager.instance;
        originalBudget = timelineManager.TimeBudget;
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
            activeCharacterText.text = timelineManager.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Ice ? "CREATION" : "DESTRUCTION";
            if (timelineManager.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Ice)
                activeCharacterText.color = new Color(0, 1, .35f);
            else
                activeCharacterText.color = new Color(1, 0, .17f);
        }

        fireTimelineImage.sizeDelta = new Vector2(timelineManager.FireTime / (originalBudget / 2) * timelinePanel.sizeDelta.x, fireTimelineImage.sizeDelta.y);
        iceTimelineImage.sizeDelta = new Vector2(timelineManager.IceTime / (originalBudget / 2) * timelinePanel.sizeDelta.x, iceTimelineImage.sizeDelta.y);

        if (budgetTimerText != null)
        {
            budgetTimerText.text = $"{FormatTime(timelineManager.TimeBudget)}";
        }

        if (iceTimerText != null)
        {
            iceTimerText.text = $"{FormatTime(timelineManager.IceTime)}";
            if (timelineManager.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Ice)
            {
                iceTimerText.fontSize = 65;
                iceTimerText.color = new Color(0, 1, .35f);
                iceTimerText.rectTransform.sizeDelta = new Vector2(340, 50);
                fireTimerText.fontSize = 40;
                fireTimerText.color = new Color(.43f, 0, .07f);
                fireTimerText.rectTransform.sizeDelta = new Vector2(230, 50);
            }
        }

        if (fireTimerText != null)
        {
            fireTimerText.text = $"{FormatTime(timelineManager.FireTime)}";
            if (timelineManager.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Fire)
            {
                iceTimerText.fontSize = 40;
                iceTimerText.color = new Color(0, .33f, .11f);
                iceTimerText.rectTransform.sizeDelta = new Vector2(230, 50);
                fireTimerText.fontSize = 65;
                fireTimerText.color = new Color(1, 0, .17f);
                fireTimerText.rectTransform.sizeDelta = new Vector2(340, 50);
            }
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