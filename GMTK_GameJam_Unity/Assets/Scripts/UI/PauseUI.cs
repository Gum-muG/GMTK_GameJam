using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    public void closeMenu()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        gameObject.SetActive(false);
    }
    public void GoToMainMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
