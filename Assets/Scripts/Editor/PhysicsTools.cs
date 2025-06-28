using UnityEditor;
using UnityEngine;

public class PhysicsTools
{
    [MenuItem("PhysicsTools/SetIceMaterial")]

    public static void SetIceMaterial()
    {
        var ice = AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/PhysicsMaterials/Ice.physicmaterial");

        var layerToTest = LayerMask.NameToLayer("No Static");

        var allObjs = Component.FindObjectsOfType<GameObject>();
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
}