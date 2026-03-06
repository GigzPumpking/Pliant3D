using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ControlsMenu : SwappableMenu
{
    [SerializeField] private TextMeshProUGUI controlsText;
    
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

    private void Start()
    {
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse")
        {
            DisplayController();
        }
        else
        {
            DisplayKeyboard();
        }
    }

    public void SwapControls()
    {
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse") 
        {
            DisplayController();
        }
        else
        {
            DisplayKeyboard();
        }
    }

    private void DisplayController()
    {
        controlsText.text = "GAMEPAD";
    }

    private void DisplayKeyboard()
    {
        controlsText.text = "KEYBOARD";
    }
}