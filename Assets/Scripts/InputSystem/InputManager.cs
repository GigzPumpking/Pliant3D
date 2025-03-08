using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public bool isListening = true;
    private InputDevice activeDevice;
    public string ActiveDeviceType => activeDevice?.displayName ?? "Unknown";
    private readonly HashSet<string> validDeviceTypes = new HashSet<string> { "Keyboard", "Gamepad", "Mouse" };

    [SerializeField]
    private InputActionAsset inputActions; // Reference to the InputActions asset

    private Dictionary<string, InputAction> actionMap = new Dictionary<string, InputAction>();

    // Dictionary mapping a type name (typically the action map name) to a dispatcher.
    // The dispatcher is an Action that takes an action name and a CallbackContext,
    // and it is responsible for dispatching the event to every active instance of that type.
    private static Dictionary<string, Action<string, InputAction.CallbackContext>> typeDispatchers = new Dictionary<string, Action<string, InputAction.CallbackContext>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes if needed.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeInputActions();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void InitializeInputActions()
    {
        // Populate the actionMap dictionary with input actions from the asset.
        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                actionMap[action.name] = action;
                action.performed += OnActionPerformed;
                action.canceled += OnActionPerformed; // Optionally handle release events.
                action.Enable();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        InputSystem.onDeviceChange -= OnDeviceChange;

        foreach (var action in actionMap.Values)
        {
            action.performed -= OnActionPerformed;
            action.canceled -= OnActionPerformed;
            action.Disable();
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.UsageChanged || change == InputDeviceChange.Added)
        {
            activeDevice = device;
            Debug.Log($"Active device changed to: {device.displayName}");
        }
    }

    // Called whenever an input action is performed or canceled.
    // We assume that the action map's name corresponds to the script type's name.
    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        if (!isListening)
            return;

        activeDevice = context.control.device; // Update the active device.
        string actionName = context.action.name;
        // Use the action map's name as the key.
        string typeName = context.action.actionMap.name;
        if (typeDispatchers.TryGetValue(typeName, out var dispatcher))
        {
            dispatcher(actionName, context);
        }
        else
        {
            Debug.LogWarning($"No dispatcher registered for type: {typeName}");
        }
    }

    // Registers a type dispatcher for a given script type name.
    // This should be called once per type (for example, in the first instance's OnEnable).
    public static void RegisterTypeDispatcher(string typeName, Action<string, InputAction.CallbackContext> dispatcher)
    {
        if (!typeDispatchers.ContainsKey(typeName))
        {
            typeDispatchers.Add(typeName, dispatcher);
            Debug.Log($"Registered input dispatcher for type: {typeName}");
        }
        else
        {
            Debug.LogWarning($"Dispatcher for type {typeName} is already registered.");
        }
    }

    // Unregisters the dispatcher for a given script type name.
    public static void UnregisterTypeDispatcher(string typeName)
    {
        if (typeDispatchers.ContainsKey(typeName))
        {
            typeDispatchers.Remove(typeName);
            Debug.Log($"Unregistered input dispatcher for type: {typeName}");
        }
    }

    // Utility to retrieve a value from an action (e.g., for axis values).
    public T GetActionValue<T>(string actionName) where T : struct
    {
        if (actionMap.TryGetValue(actionName, out var action))
        {
            return action.ReadValue<T>();
        }
        Debug.LogWarning($"Action '{actionName}' not found in InputManager.");
        return default;
    }

    // Enable or disable input processing.
    public void ToggleListening(bool listen)
    {
        isListening = listen;
    }

    // Returns the effective binding path for a given action and device type.
    public string ReturnActionBinding(string actionName, string deviceType)
    {
        if (!validDeviceTypes.Contains(deviceType))
        {
            throw new ArgumentException($"Invalid device type: {deviceType}. Valid types are: {string.Join(", ", validDeviceTypes)}");
        }

        if (actionMap.TryGetValue(actionName, out var action))
        {
            foreach (var binding in action.bindings)
            {
                if (binding.groups.Contains(deviceType, StringComparison.OrdinalIgnoreCase))
                {
                    return binding.effectivePath;
                }
            }
        }
        return "Binding not found";
    }

    // Remaps an action's binding for a specific device type.
    public void RemapActionBinding(string actionName, string deviceType, string oldKeybind, string newKeybind)
    {
        if (!validDeviceTypes.Contains(deviceType))
        {
            Debug.LogError($"Invalid device type: {deviceType}. Valid types are: {string.Join(", ", validDeviceTypes)}");
            return;
        }

        if (actionMap.TryGetValue(actionName, out InputAction action))
        {
            bool bindingFound = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.groups.Contains(deviceType, StringComparison.OrdinalIgnoreCase))
                {
                    if (binding.effectivePath.Equals(oldKeybind, StringComparison.OrdinalIgnoreCase))
                    {
                        action.ApplyBindingOverride(i, newKeybind);
                        Debug.Log($"Remapped action '{actionName}' for device '{deviceType}' from '{oldKeybind}' to '{newKeybind}'.");
                        bindingFound = true;
                        break;
                    }
                }
            }
            if (!bindingFound)
            {
                Debug.LogWarning($"No binding found for action '{actionName}' on device type '{deviceType}' with keybind '{oldKeybind}'.");
            }
        }
        else
        {
            Debug.LogWarning($"Action '{actionName}' not found in the input map.");
        }
    }
}