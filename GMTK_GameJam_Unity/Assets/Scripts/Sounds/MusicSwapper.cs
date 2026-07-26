using UnityEngine;

public class MusicSwapper : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip iceMusic;
    public AudioClip fireMusic;
    private int time;
    private bool ice = false;
    void Update()
    {
        if (CharacterSwapManager.instance.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Ice && !ice)
        {
            time = audioSource.timeSamples;
            audioSource.Stop();
            audioSource.clip = iceMusic;
            audioSource.timeSamples = time;
            audioSource.Play();
            ice = true;
        } else if (CharacterSwapManager.instance.ActiveCharacter == CharacterSwapManager.PlayableCharacter.Fire && ice)
        {
            time = audioSource.timeSamples;
            audioSource.Stop();
            audioSource.clip = fireMusic;
            audioSource.timeSamples = time;
            audioSource.Play();
            ice = false;
        }
    }
}
