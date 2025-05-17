using UnityEngine;

public class InteractObjective : Objective {
    public GameObject objectToInteractWith = default;
    public float interactRadius;
}

public class InteractObjectiveFactory {
    public static void CreateInteractObjective(GameObject obj, float radius) {
        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = radius;
    }
}