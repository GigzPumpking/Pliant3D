using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControlsMenu : Menu
{
    [SerializeField] private Image keyboardImage;
    [SerializeField] private Image controllerImage;
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

        if (InputManager.Instance.ActiveDeviceType == "Keyboard" || InputManager.Instance.ActiveDeviceType == "Mouse")
        {
            DisplayKeyboard();
        }
        else
        {
            DisplayController();
        }
    }

    public void SwapControls()
    {
        if (currentControlType == ControlType.Keyboard)
        {
            DisplayController();
        }
        else
        {
            DisplayKeyboard();
        }
    }

    public void DisplayController()
    {
        currentControlType = ControlType.Controller;
        keyboardImage.enabled = false;
        controllerImage.enabled = true;
        controlsText.text = "GAMEPAD";
    }

    public void DisplayKeyboard()
    {
        currentControlType = ControlType.Keyboard;
        keyboardImage.enabled = true;
        controllerImage.enabled = false;
        controlsText.text = "KEYBOARD";
    }
}