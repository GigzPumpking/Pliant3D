using UnityEngine;

public class TaskDebugger : MonoBehaviour
{
    // Toggle this off in the inspector when building the real game
    public bool showDebug = true; 

    private void OnGUI()
    {
        if (!showDebug || GameManager.Instance == null) return;

        // Set up the style for the debug text
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;

        // Fetch the numbers
        int completed = GameManager.Instance.GetNumTasksCompleted();
        int assigned = GameManager.Instance.GetNumTasksAssigned();
        float ratio = GameManager.Instance.GetRatioOfTasksCompleted() * 100f;

        // Draw the text in the top left corner (x, y, width, height)
        GUI.Label(new Rect(20, 20, 400, 100), $"GLOBAL Completed: {completed}", style);
        GUI.Label(new Rect(20, 50, 400, 100), $"GLOBAL Assigned: {assigned}", style);
        GUI.Label(new Rect(20, 80, 400, 100), $"Current Proficiency: {ratio:F1}%", style);
    }
}