using UnityEngine;

public class tim : MonoBehaviour
{
    private bool ready = false;

    void Start()
    {
        Invoke("Ready", 3f);
    }

    private void Ready()
    {
        ready = true;
    }

    void Update()
    {
        if(ready && Input.anyKeyDown)
        {
            gameObject.SetActive(false);
        }
    }
}
