using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVolumeSlider : CustomSlider
{
    protected override void OnSliderChanged(float value)
    {
        AudioManager.Instance?.SetGlobalVolume(value);
    }
}
