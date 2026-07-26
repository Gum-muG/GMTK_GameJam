using UnityEngine;

public class Pause : MonoBehaviour
{

    public GameObject pauseMenu;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu.activeSelf)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                pauseMenu.SetActive(false);
            } else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                pauseMenu.SetActive(true);
            }
            
        }
    }
}
