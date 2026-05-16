using UnityEngine;

public class UnstickPrompt : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_animator == null) return;

        string deviceType = InputManager.Instance?.ActiveDeviceType;
        bool isKeyboard = deviceType == "Keyboard" || deviceType == "Mouse";
        _animator.SetBool("Keyboard", isKeyboard);
    }
}
