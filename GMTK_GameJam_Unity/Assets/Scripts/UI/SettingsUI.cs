using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text soundPercentText;
    [SerializeField] private TMP_Text musicPercentText;
    [SerializeField] private TMP_Text masterPercentText;
    [SerializeField] private TMP_Text sensitivityPercentText;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sensitivitySlider;

    void Update()
    {
        sliderToPercent(soundSlider, soundPercentText);
        sliderToPercent(musicSlider, musicPercentText);
        sliderToPercent(masterSlider, masterPercentText);
        sliderToPercent(sensitivitySlider, sensitivityPercentText);
    }

    private void sliderToPercent(Slider slider, TMP_Text text)
    {
        text.text = ((slider.value/80) + 1) * 100 + "%";
    }
}
