using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only gamepad simulator. Attach to any GameObject in the scene to test
/// gamepad input without a physical controller.
///
/// Keyboard Mappings:
///   Arrow Keys / WASD  →  Left Stick
///   Space              →  A Button (confirm / UI click)
///   Escape             →  B Button (cancel)
///   Enter              →  Start Button
/// </summary>
public class GamepadSimulator : MonoBehaviour
{
    private Gamepad virtualGamepad;

    private void OnEnable()
    {
        virtualGamepad = InputSystem.AddDevice<Gamepad>("SimulatedGamepad");
        Debug.Log("[GamepadSimulator] Virtual gamepad added. Use Arrow Keys/WASD to move cursor, Space to click.");
    }

    private void OnDisable()
    {
        if (virtualGamepad != null && virtualGamepad.added)
        {
            InputSystem.RemoveDevice(virtualGamepad);
            virtualGamepad = null;
            Debug.Log("[GamepadSimulator] Virtual gamepad removed.");
        }
    }

    private void Update()
    {
        if (virtualGamepad == null || !virtualGamepad.added) return;

        // --- Left Stick from Arrow Keys or WASD ---
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) y -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) y += 1f;

        // --- Buttons ---
        bool aButton     = Input.GetKey(KeyCode.Space);
        bool bButton     = Input.GetKey(KeyCode.Escape);
        bool startButton = Input.GetKey(KeyCode.Return);

        var state = new GamepadState
        {
            leftStick = new Vector2(x, y),
        };

        if (aButton)     state.buttons |= (uint)(1 << (int)GamepadButton.A);
        if (bButton)     state.buttons |= (uint)(1 << (int)GamepadButton.B);
        if (startButton) state.buttons |= (uint)(1 << (int)GamepadButton.Start);

        InputSystem.QueueStateEvent(virtualGamepad, state);
    }
}

[CustomEditor(typeof(GamepadSimulator))]
public class GamepadSimulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "EDITOR ONLY — Remove before shipping.\n\n" +
            "Arrow Keys / WASD  →  Left Stick (moves gamepad cursor)\n" +
            "Space              →  A Button (UI click)\n" +
            "Escape             →  B Button (cancel)\n" +
            "Enter              →  Start Button",
            MessageType.Info);
    }
}
#endif