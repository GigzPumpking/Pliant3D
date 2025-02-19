using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Menu : MonoBehaviour
{
    // Assign the default UI element in the Inspector.
    [SerializeField]
    private GameObject defaultSelectedUIElement;

    /// <summary>
    /// When the menu is enabled, this method sets the default selected UI element.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (defaultSelectedUIElement == null)
        {
            Debug.LogWarning($"{gameObject.name} does not have a default selected UI element assigned.");
        }
        else if (EventSystem.current != null)
        {
            // Set the default UI element immediately upon enabling.
            EventSystem.current.SetSelectedGameObject(defaultSelectedUIElement);
        }
        else
        {
            Debug.LogWarning("No EventSystem found in the scene. Please add one to handle UI navigation.");
        }
    }

    /// <summary>
    /// During runtime, if no UI element is selected, this method reselects the default element.
    /// </summary>
    protected virtual void Update()
    {
        // If there's no currently selected object, and a default is set, then reassign it.
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == null &&
            defaultSelectedUIElement != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedUIElement);
        }
    }
}
