using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class KeyActionReceiver : MonoBehaviour
{
    protected Dictionary<string, Action<InputAction.CallbackContext>> actionMap = 
        new Dictionary<string, Action<InputAction.CallbackContext>>();

    // Expose the dictionary through the interface.
    public virtual Dictionary<string, Action<InputAction.CallbackContext>> ActionMap => actionMap;

    // Provide a default implementation.
    public virtual void OnKeyAction(string action, InputAction.CallbackContext context)
    {
        if (actionMap.TryGetValue(action, out var actionHandler))
        {
            actionHandler(context);
        }
        else
        {
            Debug.LogWarning($"Unhandled action: {action}");
        }
    }
}