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
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }
    
    protected abstract void OnToggleChanged(bool value);
}
