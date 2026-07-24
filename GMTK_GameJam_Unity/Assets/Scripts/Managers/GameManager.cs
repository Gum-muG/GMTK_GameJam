using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private bool recording = false;

    public void Awake()
    {
        instance = this;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!recording) {
                ReplayManager.instance.StartRecording(); 
                recording = true;
            }
            else {
                ReplayManager.instance.Stop(); 
                recording = false;
            }
        } 
        if(Input.GetKeyDown(KeyCode.P))
        {
            ReplayManager.instance.StartPlayback();
        }
    }
}
