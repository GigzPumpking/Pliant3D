using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom PropertyDrawer for DialogueEntry that shows a compact view by default.
/// Keyboard/Controller text fields are hidden unless hasDeviceSpecificText is enabled.
/// </summary>
[CustomPropertyDrawer(typeof(DialogueEntry))]
public class DialogueEntryDrawer : PropertyDrawer
{
    private const float LINE_HEIGHT = 18f;
    private const float PADDING = 4f;
    private const float MAX_TEXT_AREA_HEIGHT = 300f; // Max height before scrolling
    private const float MIN_TEXT_AREA_HEIGHT = 18f; // Minimum 1 line
    private const float CHAR_WIDTH_ESTIMATE = 7f; // Average character width in pixels
    
    private static GUIStyle _wrappedTextAreaStyle;
    
    // Cache the last known width for GetPropertyHeight (which doesn't have access to position)
    private static float _lastKnownWidth = 300f;
    
    /// <summary>
    /// Gets a cached text area style with word wrapping enabled.
    /// </summary>
    private static GUIStyle WrappedTextAreaStyle
    {
        get
        {
            if (_wrappedTextAreaStyle == null)
            {
                _wrappedTextAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true
                };
            }
            return _wrappedTextAreaStyle;
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty hasDeviceSpecific = property.FindPropertyRelative("hasDeviceSpecificText");
        SerializedProperty defaultText = property.FindPropertyRelative("defaultText");
        
        // Estimate available width (account for indent)
        float availableWidth = _lastKnownWidth - (EditorGUI.indentLevel + 1) * 15f;
        
        float height = LINE_HEIGHT + PADDING; // Foldout/label line
        
        if (property.isExpanded)
        {
            // Default text label + area
            height += LINE_HEIGHT; // Label
            height += CalculateTextAreaHeight(defaultText.stringValue, availableWidth) + PADDING;
            
            // Toggle for device-specific text
            height += LINE_HEIGHT + PADDING;
            
            // If device-specific is enabled, show keyboard and controller fields
            if (hasDeviceSpecific.boolValue)
            {
                SerializedProperty keyboardText = property.FindPropertyRelative("keyboardText");
                SerializedProperty controllerText = property.FindPropertyRelative("controllerText");
                
                height += LINE_HEIGHT; // "Keyboard Text" label
                height += CalculateTextAreaHeight(keyboardText.stringValue, availableWidth) + PADDING;
                height += LINE_HEIGHT; // "Controller Text" label
                height += CalculateTextAreaHeight(controllerText.stringValue, availableWidth) + PADDING;
            }
        }
        
        return height;
    }
    
    /// <summary>
    /// Calculates text area height based on content and available width, accounting for word wrap.
    /// </summary>
    private float CalculateTextAreaHeight(string text, float availableWidth)
    {
        if (string.IsNullOrEmpty(text))
            return MIN_TEXT_AREA_HEIGHT;
        
        // Calculate how many characters fit per line based on available width
        float charsPerLine = Mathf.Max(10f, availableWidth / CHAR_WIDTH_ESTIMATE);
        
        // For each segment between newlines, estimate wrapped lines
        string[] segments = text.Split('\n');
        int totalLines = 0;
        foreach (string segment in segments)
        {
            // Each segment takes at least 1 line, plus additional for wrapping
            int wrappedLines = Mathf.Max(1, Mathf.CeilToInt(segment.Length / charsPerLine));
            totalLines += wrappedLines;
        }
        
        // Ensure minimum of 1 line
        totalLines = Mathf.Max(1, totalLines);
        
        float calculatedHeight = totalLines * LINE_HEIGHT;
        return Mathf.Clamp(calculatedHeight, MIN_TEXT_AREA_HEIGHT, MAX_TEXT_AREA_HEIGHT);
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Cache the width for GetPropertyHeight to use
        _lastKnownWidth = position.width;
        
        EditorGUI.BeginProperty(position, label, property);
        
        SerializedProperty defaultText = property.FindPropertyRelative("defaultText");
        SerializedProperty hasDeviceSpecific = property.FindPropertyRelative("hasDeviceSpecificText");
        SerializedProperty keyboardText = property.FindPropertyRelative("keyboardText");
        SerializedProperty controllerText = property.FindPropertyRelative("controllerText");
        
        Rect foldoutRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
        
        // Extract element index from property path (e.g., "baseDialogue.Array.data[0]" -> "Element 0")
        string elementLabel = GetElementLabel(property.propertyPath);
        
        // Create a preview of the default text for the foldout label
        string preview = defaultText.stringValue;
        if (!string.IsNullOrEmpty(preview))
        {
            // Limit preview length to prevent horizontal overflow
            preview = preview.Replace("\n", " ");
            if (preview.Length > 30)
            {
                preview = preview.Substring(0, 27) + "...";
            }
        }
        else
        {
            preview = "(empty)";
        }
        
        // Show device-specific indicator
        string deviceIndicator = hasDeviceSpecific.boolValue ? " [KB/Ctrl]" : "";
        
        // Build the foldout label: "Element #: preview text [KB/Ctrl]"
        string foldoutLabel = $"{elementLabel}: {preview}{deviceIndicator}";
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + LINE_HEIGHT + PADDING;
            float indentOffset = EditorGUI.indentLevel * 15f;
            float indentedX = position.x + indentOffset;
            float indentedWidth = position.width - indentOffset;
            
            // Default text label
            Rect defaultLabelRect = new Rect(indentedX, y, indentedWidth, LINE_HEIGHT);
            EditorGUI.LabelField(defaultLabelRect, "Default Text", EditorStyles.boldLabel);
            y += LINE_HEIGHT;
            
            // Default text area with word wrap - height based on actual width
            float defaultTextHeight = CalculateTextAreaHeight(defaultText.stringValue, indentedWidth);
            Rect defaultTextRect = new Rect(indentedX, y, indentedWidth, defaultTextHeight);
            defaultText.stringValue = EditorGUI.TextArea(defaultTextRect, defaultText.stringValue, WrappedTextAreaStyle);
            y += defaultTextHeight + PADDING;
            
            // Device-specific toggle - label and toggle on same line
            Rect toggleLabelRect = new Rect(indentedX, y, 130f, LINE_HEIGHT);
            Rect toggleRect = new Rect(indentedX + 132f, y, 20f, LINE_HEIGHT);
            EditorGUI.LabelField(toggleLabelRect, "Device-Specific");
            hasDeviceSpecific.boolValue = EditorGUI.Toggle(toggleRect, hasDeviceSpecific.boolValue);
            y += LINE_HEIGHT + PADDING;
            
            // Show keyboard/controller fields if enabled
            if (hasDeviceSpecific.boolValue)
            {
                // Keyboard text label
                Rect kbLabelRect = new Rect(indentedX, y, indentedWidth, LINE_HEIGHT);
                EditorGUI.LabelField(kbLabelRect, "Keyboard Text", EditorStyles.boldLabel);
                y += LINE_HEIGHT;
                
                // Keyboard text area with word wrap - height based on actual width
                float kbTextHeight = CalculateTextAreaHeight(keyboardText.stringValue, indentedWidth);
                Rect kbTextRect = new Rect(indentedX, y, indentedWidth, kbTextHeight);
                keyboardText.stringValue = EditorGUI.TextArea(kbTextRect, keyboardText.stringValue, WrappedTextAreaStyle);
                y += kbTextHeight + PADDING;
                
                // Controller text label
                Rect ctrlLabelRect = new Rect(indentedX, y, indentedWidth, LINE_HEIGHT);
                EditorGUI.LabelField(ctrlLabelRect, "Controller Text", EditorStyles.boldLabel);
                y += LINE_HEIGHT;
                
                // Controller text area with word wrap - height based on actual width
                float ctrlTextHeight = CalculateTextAreaHeight(controllerText.stringValue, indentedWidth);
                Rect ctrlTextRect = new Rect(indentedX, y, indentedWidth, ctrlTextHeight);
                controllerText.stringValue = EditorGUI.TextArea(ctrlTextRect, controllerText.stringValue, WrappedTextAreaStyle);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    /// <summary>
    /// Extracts the element label from a property path.
    /// E.g., "baseDialogue.Array.data[2]" -> "Element 2"
    /// </summary>
    private string GetElementLabel(string propertyPath)
    {
        // Find the array index in the path (e.g., "data[2]")
        int bracketStart = propertyPath.LastIndexOf('[');
        int bracketEnd = propertyPath.LastIndexOf(']');
        
        if (bracketStart >= 0 && bracketEnd > bracketStart)
        {
            string indexStr = propertyPath.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            if (int.TryParse(indexStr, out int index))
            {
                return $"Element {index}";
            }
        }
        
        // Fallback if we can't parse
        return "Element";
    }
}
