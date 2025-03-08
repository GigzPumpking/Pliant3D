using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class KeyActionReceiver<T> : MonoBehaviour, IKeyActionReceiver where T : KeyActionReceiver<T>
{
    // Static list of active instances for this type.
    public static List<T> instances = new List<T>();

    // Each subclass must implement this property to provide a key mapping.
    // For example, mapping an action name to a callback.
    protected abstract Dictionary<string, Action<T, InputAction.CallbackContext>> KeyMapping { get; }

    // This static dispatcher is registered once per type with the InputManager.
    // It will be called by the InputManager when an input event occurs for this type.
    private static void Dispatcher(string action, InputAction.CallbackContext context)
    {
        foreach (var instance in instances)
        {
            instance.HandleKeyAction(action, context);
        }
    }

    // Handles the key action for this instance.
    protected virtual void HandleKeyAction(string action, InputAction.CallbackContext context)
    {
        if (KeyMapping.TryGetValue(action, out var callback) && callback != null)
        {
            callback((T)this, context);
        }
        else
        {
            Debug.LogWarning($"{typeof(T).Name} did not handle action: {action}");
        }
    }

    // Implementation of IKeyActionReceiver.
    // This method can be used if you want per‑instance handling directly.
    public virtual void OnKeyAction(string action, InputAction.CallbackContext context)
    {
        HandleKeyAction(action, context);
    }

    protected virtual void OnEnable()
    {
        if (!instances.Contains((T)this))
            instances.Add((T)this);

        // Register the dispatcher for this type only once (when the first instance is enabled).
        if (instances.Count == 1)
        {
            InputManager.RegisterTypeDispatcher(typeof(T).Name, Dispatcher);
        }
    }

    protected virtual void OnDisable()
    {
        instances.Remove((T)this);

        // Unregister the dispatcher if no instances remain.
        if (instances.Count == 0)
        {
            InputManager.UnregisterTypeDispatcher(typeof(T).Name);
        }
    }
}