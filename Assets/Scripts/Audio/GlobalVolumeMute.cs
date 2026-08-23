using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVolumeMute : CustomToggle
{
    protected override void OnToggleChanged(bool value)
    {
        AudioManager.Instance?.ToggleGlobalMute(value);
    }
}
