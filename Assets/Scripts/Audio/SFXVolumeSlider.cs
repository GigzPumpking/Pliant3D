using UnityEngine;

public class SFXVolumeSlider : CustomSlider
{
    protected override void OnSliderChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }
}