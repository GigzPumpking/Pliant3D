#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.InputSystem;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector.
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Registered Type Dispatchers", EditorStyles.boldLabel);

        // Get the static 'typeDispatchers' field from InputManager.
        FieldInfo dispatchersField = typeof(InputManager).GetField("typeDispatchers", BindingFlags.NonPublic | BindingFlags.Static);
        if (dispatchersField != null)
        {
            var typeDispatchers = dispatchersField.GetValue(null) as Dictionary<string, Action<string, InputAction.CallbackContext>>;
            if (typeDispatchers != null && typeDispatchers.Count > 0)
            {
                foreach (var kvp in typeDispatchers)
                {
                    EditorGUILayout.LabelField($"Type: {kvp.Key} - Dispatcher Registered", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // Locate the receiver type by name.
                    // We assume that kvp.Key is the derived type name (e.g., "GameManager").
                    Type receiverType = AppDomain.CurrentDomain.GetAssemblies()
                                            .Select(asm => asm.GetType(kvp.Key))
                                            .FirstOrDefault(t => t != null);

                    if (receiverType != null)
                    {
                        // Construct the generic base type KeyActionReceiver<receiverType>.
                        Type baseReceiverType = typeof(KeyActionReceiver<>).MakeGenericType(receiverType);
                        // Look for the static 'instances' field on the generic base.
                        FieldInfo instancesField = baseReceiverType.GetField("instances", BindingFlags.Public | BindingFlags.Static);
                        if (instancesField != null)
                        {
                            var instances = instancesField.GetValue(null) as IList;
                            if (instances != null)
                            {
                                EditorGUILayout.LabelField($"Instances Count: {instances.Count}");
                                EditorGUI.indentLevel++;
                                foreach (var instance in instances)
                                {
                                    if (instance is MonoBehaviour mb)
                                    {
                                        EditorGUILayout.LabelField($"{mb.GetType().Name} (ID: {mb.GetInstanceID()})");
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField("Non-MonoBehaviour instance");
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                            else
                            {
                                EditorGUILayout.LabelField("No instances found.");
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"No static 'instances' field on generic base for type: {receiverType.Name}");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Could not locate type: {kvp.Key}");
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No type dispatchers registered.");
            }
        }
        else
        {
            EditorGUILayout.LabelField("Could not find 'typeDispatchers' field.");
        }
    }
}
#endif