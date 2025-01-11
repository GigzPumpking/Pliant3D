using UnityEngine.InputSystem;

public interface IKeyActionReceiver
{
    void OnKeyAction(string action, InputAction.CallbackContext context);
}
