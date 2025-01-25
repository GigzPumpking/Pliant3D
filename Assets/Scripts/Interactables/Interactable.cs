using UnityEngine;
using System.Collections.Generic;

public abstract class Interactable : MonoBehaviour
{
    private bool _isInteractable = true; // Determines if this object can be interacted with
    public bool isInteractable
    {
        get => _isInteractable;
        set
        {
            if (_isInteractable != value) // Only act if the value changes
            {
                _isInteractable = value;

                // Automatically unhighlight if interactable becomes false
                if (!_isInteractable)
                {
                    IsHighlighted = false;
                }
            }
        }
    }

    private bool _isHighlighted; // Tracks whether the object is currently highlighted
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted != value) // Only act if the value changes
            {
                _isHighlighted = value;

                if (_isHighlighted)
                {
                    Highlight();
                }
                else
                {
                    Unhighlight();
                }
            }
        }
    }

    // List of properties this object supports (e.g., Hookable, Pullable, Breakable)
    [SerializeField]
    private List<InteractableProperty> properties = new List<InteractableProperty>();
    public List<InteractableProperty> Properties => properties;

    public bool HasProperty(string propertyName)
    {
        return properties.Exists(p => p.PropertyName == propertyName);
    }

    public InteractableProperty GetProperty(string propertyName)
    {
        return properties.Find(p => p.PropertyName == propertyName);
    }

    public void AddProperty(InteractableProperty property)
    {
        if (!HasProperty(property.PropertyName))
        {
            properties.Add(property);
        }
    }

    public void RemoveProperty(string propertyName)
    {
        properties.RemoveAll(p => p.PropertyName == propertyName);
    }

    public abstract void Interact();

    protected virtual void Highlight()
    {
        // Logic to visually highlight the object (e.g., change material or add outline)
        Debug.Log($"{name} is now highlighted.");
    }

    protected virtual void Unhighlight()
    {
        // Logic to visually unhighlight the object (e.g., revert material or remove outline)
        Debug.Log($"{name} is no longer highlighted.");
    }
}
