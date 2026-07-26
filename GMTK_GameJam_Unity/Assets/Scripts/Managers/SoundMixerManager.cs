using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    public void SetMasterVolume(float level)
    {
        audioMixer.SetFloat("masterVolume", level);
    }
    public void SetSoundsVolume(float level)
    {
        audioMixer.SetFloat("soundsVolume", level);
    }
    public void SetMusicVolume(float level)
    {
        audioMixer.SetFloat("musicVolume", level);
    }
}
