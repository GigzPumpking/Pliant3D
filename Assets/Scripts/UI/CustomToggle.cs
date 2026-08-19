using UnityEngine;
using UnityEngine.UI;

public abstract class CustomToggle : MonoBehaviour
{
    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
    }
    
    void Start()
    {
        // Clear any existing listeners.
        toggle.onValueChanged.RemoveAllListeners();
        // In case their is an existing value before subscribed to event set adjust accordingly.
        if (toggle.isOn)
        {
            OnToggleChanged(true);
        }
        else
        {
            OnToggleChanged(false);
        }
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }
    
    protected abstract void OnToggleChanged(bool value);
}
