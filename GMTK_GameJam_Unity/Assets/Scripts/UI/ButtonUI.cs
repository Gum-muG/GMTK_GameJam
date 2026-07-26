using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonUI : MonoBehaviour, IPointerEnterHandler
{
    public AudioClip hoverSound;
    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.instance.PlaySound(hoverSound, transform, 1f);
    }
}
