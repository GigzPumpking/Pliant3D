using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ControlsMenu : SwappableMenu
{
    private enum ControlType
    {
        Keyboard,
        Controller
    }

    [SerializeField] private ControlType currentControlType;

    protected override void OnEnable()
    {
        base.OnEnable();
        ((SwappableMenu)this).RegisterToUIManager(); //Register to the UIManager as a swappable menu
    }

    private void OnValidate()
    {
        ActivateUIComponent(GetCurrentDeviceUI());
    }
}