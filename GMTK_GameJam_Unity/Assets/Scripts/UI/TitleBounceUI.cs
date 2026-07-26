using UnityEngine;
using UnityEngine.UI;

public class TitleBounceUI : MonoBehaviour
{
    public RectTransform titleImage;
    public float strength;
    public float speed;
    private float delta;
    private bool increment = true;
    private float startWidth, startHeight;

    void Start()
    {
        delta = 1-strength;
        startWidth = titleImage.sizeDelta.x;
        startHeight = titleImage.sizeDelta.y;
    }

    void Update()
    {
        if(increment)
        {
            delta += speed;
            if(delta >= 1+strength)
            {
                increment = false;
            }
        } else
        {
            delta -= speed;
            if(delta <= 1-strength)
            {
                increment = true;
            }
        }
        titleImage.sizeDelta = new Vector2(startWidth * delta, startHeight * delta);
    }
}
