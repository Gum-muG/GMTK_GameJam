using UnityEngine;
using UnityEngine.SceneManagement;

public class Flag : MonoBehaviour
{
    private bool fire = false;
    private bool ice = false;
    public int nextLevel;
    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.name == "Mesh" && !CharacterSwapManager.instance.fireReady)
        {

            CharacterSwapManager.instance.fireReady = true;
        } else if (other.gameObject.name == "IceMesh" && !CharacterSwapManager.instance.iceReady)
        {
            CharacterSwapManager.instance.iceReady = true;
        }
    }

    void Update()
    {
        if (CharacterSwapManager.instance.fireReady && CharacterSwapManager.instance.iceReady)
        {
            CharacterSwapManager.instance.fireReady = false;
            CharacterSwapManager.instance.iceReady = false;
            SceneManager.LoadSceneAsync(nextLevel);
        }
    }
}
