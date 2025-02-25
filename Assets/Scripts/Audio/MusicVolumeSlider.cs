using UnityEngine;

public class MusicVolumeSlider : CustomSlider
{
    protected override void OnSliderChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }
}