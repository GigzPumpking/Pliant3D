using UnityEngine;
using UnityEditor;

public class TutorializerUnityMenu : MonoBehaviour
{
    private const string TransformTutorialAssetPath = "[TUTORIALIZATION] - Transform";
    private const string ReachDestinationTutorialAssetPath = "[TUTORIALIZATION] - Reach Destination";
    private const string UseAbilityTutorialAssetPath = "[TUTORIALIZATION] - Ability Usage";
    private const string ObjectToLocationTutorialAssetPath = "[TUTORIALIZATION] - Object To Location";

    private const float spawnDist = 10f;
    
    [MenuItem("GameObject/Tutorials/Tutorial TRANSFORM Prefab", false, 0)]
    static void CreateTransformTutorial()
    {
        // Load the prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>(TransformTutorialAssetPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {TransformTutorialAssetPath}");
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
    
    [MenuItem("GameObject/Tutorials/Tutorial PLAYER DESTINATION Prefab", false, 0)]
    static void CreatePlayerDestinationTutorial()
    {
        // Load the prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>(ReachDestinationTutorialAssetPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {ReachDestinationTutorialAssetPath}");
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
    
    [MenuItem("GameObject/Tutorials/Tutorial ABILITY USAGE Prefab", false, 0)]
    static void CreateAbilityUsageTutorial()
    {
        // Load the prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>(UseAbilityTutorialAssetPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {UseAbilityTutorialAssetPath}");
            return;
        }

        Vector3 spawnPos = SceneView.lastActiveSceneView.camera.transform.position + (SceneView.lastActiveSceneView.camera.transform.forward * spawnDist);
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
    
    [MenuItem("GameObject/Tutorials/Tutorial OBJECT TO LOCATION Prefab", false, 0)]
    static void CreateObjectToLocationTutorial()
    {
        // Load the prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>(ObjectToLocationTutorialAssetPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {ObjectToLocationTutorialAssetPath}");
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
