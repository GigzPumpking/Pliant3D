using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorButton : ButtonScript
{
    public Light[] lights;

    public override void OnPress()
    {
        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.color = Color.white; // Hex FFFFFF
            }
        }
    }

    public override void OnRelease()
    {
        // Nothing happens when the button is released
    }
}