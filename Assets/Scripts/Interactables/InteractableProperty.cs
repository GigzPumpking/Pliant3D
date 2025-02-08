using UnityEngine;

[CreateAssetMenu(menuName = "Interactable Property")]
public class InteractableProperty : ScriptableObject
{
    public string PropertyName; // E.g., "Hookable", "Pullable", "Breakable"
    public bool IsActive = true; // Whether the property is active (optional)
    public string Description; // Optional description of the property
}
