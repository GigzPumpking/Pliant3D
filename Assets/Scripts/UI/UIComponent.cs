using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class UIComponent : MonoBehaviour
{
    public GameObject KeyboardUI;
    public GameObject ControllerUI;
    public GameObject AlternativeUI;
    private GameObject CurrentDeviceUI;
    
   [HideInInspector] public Dictionary<String, GameObject> DeviceUIMap = new Dictionary<String, GameObject>();

    // MonoBehaviour constructor runs before Unity deserializes inspector-assigned fields,
    // so do initialization that depends on those fields in Awake/OnValidate instead.
    private void Awake()
    {
        PopulateDeviceUIMap();
    }

    // Keep the map in-sync in the editor when values are changed in the Inspector
    private void OnValidate()
    {
        // OnValidate is called in the editor; guard against runtime-only behavior.
        PopulateDeviceUIMap();
    }

    private void PopulateDeviceUIMap()
    {
        DeviceUIMap.Clear();
        if (KeyboardUI != null)
        {
            DeviceUIMap["Keyboard"] = KeyboardUI;
            DeviceUIMap["Mouse"] = KeyboardUI;
        }
        if (ControllerUI != null)
        {
            DeviceUIMap["Gamepad"] = ControllerUI;
            DeviceUIMap["Xbox Controller"] = ControllerUI;
        }
    }
    private void Start()
    {
        UIManager.Instance?.RegisterUIComponent(this);
        CurrentDeviceUI = KeyboardUI;
    }

    public void DisableAll()
    {
        if(KeyboardUI) KeyboardUI.SetActive(false);
        if(ControllerUI) ControllerUI.SetActive(false);
        if(AlternativeUI) AlternativeUI.SetActive(false);
    }
    
    public void SetCurrentDeviceUI(string deviceName)
    {
        if (!DeviceUIMap.TryGetValue(deviceName, out var value)) return;
        DisableAll();
        CurrentDeviceUI = value; //This is the UI that will be activated when this component is enabled
        if(CurrentDeviceUI) CurrentDeviceUI.SetActive(true);
    }
}