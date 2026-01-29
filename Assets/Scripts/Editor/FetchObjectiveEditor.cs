using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for FetchObjective that conditionally shows alternate NPC fields
/// only when useAlternateNPC is enabled.
/// </summary>
[CustomEditor(typeof(FetchObjective))]
[CanEditMultipleObjects]
public class FetchObjectiveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Draw script field (readonly)
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }
        
        // Draw all fields before useAlternateNPC
        DrawPropertiesExcluding(serializedObject, "m_Script", "useAlternateNPC", "alternateNPC", 
            "alternateItemsReadyDialogue", "alternateQuestCompleteDialogue");
        
        // Draw useAlternateNPC toggle
        SerializedProperty useAlternateNPCProp = serializedObject.FindProperty("useAlternateNPC");
        EditorGUILayout.PropertyField(useAlternateNPCProp);
        
        // Only show alternate NPC fields if toggle is enabled
        if (useAlternateNPCProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alternateNPC"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alternateItemsReadyDialogue"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alternateQuestCompleteDialogue"), true);
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
