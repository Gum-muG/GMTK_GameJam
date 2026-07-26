using UnityEngine;

public class PlatformDespawnWarning : MonoBehaviour
{
    [Min(0.1f)] public float warningDuration = 1.5f;
    [Min(1)] public int flashCount = 3;

    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SetRemainingTime(float remainingTime)
    {
        if (remainingTime > warningDuration)
        {
            SetVisible(true);
            return;
        }

        float progress = Mathf.Clamp01(1f - remainingTime / warningDuration);
        int flashStep = Mathf.FloorToInt(progress * flashCount * 2f);
        SetVisible(flashStep % 2 == 0);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = visible;
            }
        }
    }

    private void OnDisable()
    {
        SetVisible(true);
    }
}