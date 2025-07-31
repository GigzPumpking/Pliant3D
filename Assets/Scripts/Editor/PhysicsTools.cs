using UnityEditor;
using UnityEngine;

public class PhysicsTools
{
    [MenuItem("PhysicsTools/SetIceMaterial")]
    public static void SetIceMaterial()
    {
        var ice = AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/PhysicsMaterials/Ice.physicmaterial");
        var layerToTest = LayerMask.NameToLayer("No Static");
        
        // --- CORRECTED ---
        // Finds all GameObjects in the scene, including inactive ones.
        var allObjs = GameObject.FindObjectsOfType<GameObject>(true);

        foreach (var obj in allObjs)
        {
            if (obj.layer == layerToTest)
            {
                var collider = obj.GetComponent<Collider>();
                if (collider != null && !collider.isTrigger)
                {
                    collider.sharedMaterial = ice;
                }
            }
        }
        Debug.Log(ice);
    }

    /// <summary>
    /// Finds all GameObjects in the open scenes with the name "Interact Bubble"
    /// and sets their layer to "WorldSpaceUI".
    /// </summary>
    [MenuItem("PhysicsTools/Set Interact Bubble Layer")]
    public static void SetInteractBubbleLayer()
    {
        const string targetName = "Interact Bubble";
        const string targetLayerName = "WorldSpaceUI";
        int objectsChanged = 0;

        int worldSpaceUiLayer = LayerMask.NameToLayer(targetLayerName);

        if (worldSpaceUiLayer == -1)
        {
            Debug.LogWarning($"Layer '{targetLayerName}' not found. Please add it in Project Settings > Tags and Layers.");
            return;
        }

        // Use GameObject.FindObjectsOfType to avoid ambiguity with System.Object.
        // The 'true' argument ensures it includes inactive GameObjects in the search.
        var allGameObjects = GameObject.FindObjectsOfType<GameObject>(true);

        foreach (var go in allGameObjects)
        {
            if (go.name == targetName)
            {
                if (go.layer != worldSpaceUiLayer)
                {
                    Undo.RecordObject(go, "Set Layer for Interact Bubble");
                    go.layer = worldSpaceUiLayer;
                    objectsChanged++;
                }
            }
        }

        Debug.Log($"Set the layer for {objectsChanged} object(s) named '{targetName}' to '{targetLayerName}'.");
    }
}