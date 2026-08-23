using UnityEngine;
using UnityEngine.UI;

public abstract class CustomSlider : MonoBehaviour
{
    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Start()
    {
        // Clear any existing listeners.
        slider.onValueChanged.RemoveAllListeners();
        // In case their is an existing value before subscribed to event
        OnSliderChanged(slider.value);
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    protected abstract void OnSliderChanged(float value);
}
