using UnityEditor;
using UnityEngine;
using TMPro;

[CustomPropertyDrawer(typeof(FontStyles))]
public class FontStylesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        // Get current value
        FontStyles currentStyle = (FontStyles)property.intValue;
        
        // Define the styles we want to show as buttons
        string[] styleNames = { "B", "I", "U", "S", "ab", "AB","SC", "Spc", "Sbc" };
        string[] styleTooltips = { 
            "Bold", 
            "Italic", 
            "Underline", 
            "Strikethrough",
            "Lowercase",
            "Uppercase",
            "SmallCaps",
            "Superscript",
            "Subscript"
        };
        FontStyles[] styleValues = { 
            FontStyles.Bold, 
            FontStyles.Italic, 
            FontStyles.Underline, 
            FontStyles.Strikethrough,
            FontStyles.LowerCase,
            FontStyles.UpperCase,
            FontStyles.SmallCaps,
            FontStyles.Superscript,
            FontStyles.Subscript
        };
        
        // Calculate button width
        float buttonWidth = (position.width - (styleNames.Length - 1) * 2) / styleNames.Length;
        float buttonHeight = position.height;
        
        for (int i = 0; i < styleNames.Length; i++)
        {
            Rect buttonRect = new Rect(
                position.x + i * (buttonWidth + 2), 
                position.y, 
                buttonWidth, 
                buttonHeight
            );
            
            bool isActive = (currentStyle & styleValues[i]) != 0;
            
            // Create GUIContent with tooltip
            GUIContent buttonContent = new GUIContent(styleNames[i], styleTooltips[i]);
            
            // Draw toggle-style button with tooltip
            if (GUI.Toggle(buttonRect, isActive, buttonContent, EditorStyles.miniButton) != isActive)
            {
                // Toggle the style
                if (isActive)
                {
                    currentStyle &= ~styleValues[i]; // Remove flag
                }
                else
                {
                    currentStyle |= styleValues[i]; // Add flag
                }
            }
        }
        
        property.intValue = (int)currentStyle;
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label);
    }
}
