using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IInputStateProvider
{
    public static InputManager Instance { get; private set; }

    private bool isListening = true;

    private InputDevice activeDevice;
    public string ActiveDeviceType => activeDevice?.displayName ?? "Unknown";

    [System.Serializable]
    public class KeyBindPair
    {
        public MonoBehaviour script; // Associated script
        public List<KeyBindAction> keyBindActions; // Actions for this script

        public KeyBindPair(MonoBehaviour scriptObject)
        {
            this.script = scriptObject;
            this.keyBindActions = new List<KeyBindAction>();
        }
    }

    [System.Serializable]
    public class KeyBindAction
    {
        public enum ActionType
        {
            Core,      // Always active, bypasses isListening
            Gameplay,  // Requires isListening to be true
            UI         // For specific UI-related actions
        }

        public string action; 
        public ActionType actionType; // The type of action

        public KeyBindAction(string action, string actionTypeString = "Gameplay")
        {
            this.action = action;
            this.actionType = ParseActionType(actionTypeString);
        }

        public static ActionType ParseActionType(string actionTypeString)
        {
            if (string.IsNullOrWhiteSpace(actionTypeString))
                return ActionType.Gameplay;

            if (Enum.TryParse(actionTypeString, true, out ActionType parsedType))
            {
                return parsedType;
            }

            Debug.LogWarning($"Invalid ActionType '{actionTypeString}'. Defaulting to Gameplay.");
            return ActionType.Gameplay;
        }
    }


    [SerializeField]
    private List<KeyBindPair> keyBindPairs = new List<KeyBindPair>();

    [SerializeField]
    private InputActionAsset inputActions; // Reference to the InputActions asset

    private Dictionary<string, InputAction> actionMap = new Dictionary<string, InputAction>();

    public bool IsInputEnabled => isListening;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }

        InitializeInputActions();

        // Listen for device changes
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void InitializeInputActions()
    {
        // Populate the actionMap dictionary with input actions
        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                actionMap[action.name] = action;
                action.performed += OnActionPerformed;
                action.canceled += OnActionPerformed; // Handle release events if needed
                action.Enable();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        InputSystem.onDeviceChange -= OnDeviceChange;

        foreach (var action in actionMap.Values)
        {
            action.performed -= OnActionPerformed;
            action.canceled -= OnActionPerformed;
            action.Disable();
        }
    }


    // Called whenever a device is connected, disconnected, or changed
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.UsageChanged || change == InputDeviceChange.Added)
        {
            activeDevice = device;
            Debug.Log($"Active device changed to: {device.displayName}");
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        Debug.Log($"Action performed: {context.action.name}, isListening: {isListening}");
        if (!isListening) return;

        activeDevice = context.control.device; // Update active device
        string actionName = context.action.name;
        string actionMapName = context.action.actionMap.name; // Get the action map name

        foreach (KeyBindPair pair in keyBindPairs)
        {
            if (pair.script.GetType().Name == actionMapName) // Match the script name to the action map name
            {
                foreach (KeyBindAction keyBindAction in pair.keyBindActions)
                {
                    if (keyBindAction.action == actionName && ShouldProcessAction(keyBindAction))
                    {
                        if (pair.script is IKeyActionReceiver receiver)
                        {
                            receiver.OnKeyAction(keyBindAction.action, context);
                        }
                    }
                }
            }
        }
    }

    private bool ShouldProcessAction(KeyBindAction keyBindAction)
    {
        // Core actions bypass the isListening check
        if (keyBindAction.actionType == KeyBindAction.ActionType.Core)
        {
            return true;
        }

        // Otherwise, check isListening
        return isListening;
    }

    public void AddKeyBind(MonoBehaviour script, string action, string actionType = "Gameplay")
    {
        KeyBindPair existingPair = keyBindPairs.Find(pair => pair.script == script);

        if (existingPair != null)
        {
            if (!existingPair.keyBindActions.Exists(kba => kba.action == action))
            {
                existingPair.keyBindActions.Add(new KeyBindAction(action, actionType));
            }
        }
        else
        {
            KeyBindPair newPair = new KeyBindPair(script);
            newPair.keyBindActions.Add(new KeyBindAction(action, actionType));
            keyBindPairs.Add(newPair);
        }
    }

    public void RemoveKeyBind(MonoBehaviour script, string action, string actionTypeString = "Gameplay")
    {
        KeyBindPair existingPair = keyBindPairs.Find(pair => pair.script == script);

        if (existingPair != null)
        {
            KeyBindAction.ActionType actionType = KeyBindAction.ParseActionType(actionTypeString);

            existingPair.keyBindActions.RemoveAll(
                kba => kba.action == action && kba.actionType == actionType
            );

            if (existingPair.keyBindActions.Count == 0)
            {
                keyBindPairs.Remove(existingPair);
            }
        }
    }

    public T GetActionValue<T>(string actionName) where T : struct
    {
        if (actionMap.TryGetValue(actionName, out var action))
        {
            return action.ReadValue<T>();
        }
        Debug.LogWarning($"Action '{actionName}' not found in InputManager.");
        return default;
    }

    public void ToggleListening(bool listen)
    {
        isListening = listen;
    }
}
