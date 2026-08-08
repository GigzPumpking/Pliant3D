#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoTaskCounter : EditorWindow
{
    // This creates a clickable button at the top of your Unity Editor!
    [MenuItem("Tools/Pliant3D/Auto-Count All Tasks")]
    public static void CountTasks()
    {
        int totalObjectives = 0;
        string originalScenePath = SceneManager.GetActiveScene().path;

        // Save the user's current scene before we start opening other ones
        EditorSceneManager.SaveOpenScenes();

        // Loop through every scene added to File > Build Settings
        foreach (var sceneAsset in EditorBuildSettings.scenes)
        {
            if (!sceneAsset.enabled) continue;

            // Open the scene silently
            Scene scene = EditorSceneManager.OpenScene(sceneAsset.path, OpenSceneMode.Single);
            
            // Find every script that inherits from Objective.cs (true = includes inactive/hidden objects)
            Objective[] objectivesInScene = Object.FindObjectsOfType<Objective>(true);
            
            // UPDATE HERE: Only count objectives that are flagged for proficiency (ignores tutorials)
            foreach (Objective obj in objectivesInScene)
            {
                if (obj.countsTowardsProficiency)
                {
                    totalObjectives++;
                }
            }
        }

        // Return the user to the scene they were originally working in
        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"<color=green>SUCCESS:</color> Found a total of {totalObjectives} valid objectives across all build scenes!");
        
        // Find the GameManager prefab and permanently save this number to it
        string[] guids = AssetDatabase.FindAssets("t:Prefab GameManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameManager gm = gmPrefab.GetComponent<GameManager>();
            
            if (gm != null)
            {
                gm.totalTasksInGame = totalObjectives;
                EditorUtility.SetDirty(gmPrefab);
                AssetDatabase.SaveAssets();
                Debug.Log("GameManager Prefab successfully updated with the new total!");
            }
        }
        else
        {
            Debug.LogWarning("Could not find GameManager prefab automatically. Please type the number in manually.");
        }
    }
}
#endif