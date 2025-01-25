using UnityEngine;

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
