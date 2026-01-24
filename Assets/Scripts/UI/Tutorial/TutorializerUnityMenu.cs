using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class TutorializerUnityMenu
{
    [MenuItem("GameObject/Tutorials/Transform Prefab", false, 0)]
    static void CreateMyPrefab()
    {
        // Load the prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>("Assets/Resources/Tutorial/[TUTORIALIZATION].prefab");

        if (prefab == null)
        {
            Debug.LogError("Prefab not found at Assets/Resources/Tutorial/[TUTORIALIZATION].prefab");
            return;
        }

        // Instantiate the prefab
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // Set the parent to the currently selected GameObject in the Hierarchy
        if (Selection.activeGameObject != null)
        {
            newObject.transform.SetParent(Selection.activeGameObject.transform);
        }

        // Register the creation for undo
        Undo.RegisterCreatedObjectUndo(newObject, "Create My Custom Prefab");

        // Select the newly created object
        Selection.activeGameObject = newObject;
    }
}
#endif
