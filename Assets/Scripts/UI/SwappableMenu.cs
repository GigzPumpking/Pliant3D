using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class SwappableMenu : Menu
{
    public void RegisterToUIManager(){ UIManager.Instance?.RegisterSwappableMenu(this); }
    public List<UIComponent> MenuOptions;//list of menu options to swap between

    //Activate the actual UI Component Game Object
    public void ActivateUIComponent(UIComponent menuOption)
    {
        //Set all others inactive first since Swappable Menu should only have ONE active UI Component at a time
        foreach (UIComponent option in MenuOptions) option.gameObject.SetActive(false);
        
        //Set the selected menu option to active
        MenuOptions.Find(option => option == menuOption)?.gameObject.SetActive(true);
        Debug.Log($"Activating menu option {menuOption.name} for device {InputManager.Instance.ActiveDeviceType}");
    }

    //Designate which Device UI to use for each UI Component in the menu
    public void DesignateCurrentDeviceUI(String deviceType)
    {
        foreach (UIComponent menuOption in MenuOptions) menuOption.SetCurrentDeviceUI(deviceType);
    }
}