using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectiveUIFactory {
    public static ObjectiveUI CreateObjectiveUI(Objective objective, GameObject prefab, GameObject parent) {
        if (!objective || !prefab || !parent) return null;
        
        GameObject currentObj = GameObject.Instantiate(prefab, parent.transform);
        
        ObjectiveUI currentObjectiveUI = currentObj.GetComponent<ObjectiveUI>();
        currentObjectiveUI.DescriptionTXT.text = objective.description;
        //Debug.LogWarning($"Creating objective UI for objective: {objective.description} at {currentObjectiveUI}");
        return currentObjectiveUI;
    }

    public static GameObject CreateObjectiveListingUI(ObjectiveListing objectiveListing, GameObject prefab, GameObject parent) {
        if (!objectiveListing || !prefab || !parent) return null;
        
        return GameObject.Instantiate(prefab, parent.transform);
    }
    
    public static GameObject CreateObjectiveListingUI(ObjectiveListing objectiveListing, GameObject objectiveListingsPrefab, GameObject objectiveUIPrefab, GameObject parent) {
        //instantiate an instance of the listing UI
        GameObject currentListing = GameObject.Instantiate(objectiveListingsPrefab, parent.transform);
        
        //instantiate an instance of it's objective UI
        foreach (Objective objective in objectiveListing.objectives) {
            if (!objective) continue;
            objectiveListing.objectiveUIList.Add(CreateObjectiveUI(objective, objectiveUIPrefab, currentListing));
        }

        return currentListing;
    }

    public static GameObject AddToObjectiveToListingUI(ObjectiveListing objectiveListing, List<Objective> objectives, GameObject objectiveListingPrefab, GameObject objectiveUIPrefab, GameObject parent)
    {
        foreach(Objective obj in objectives)
        {
            objectiveListing.objectives.Add(obj);
            objectiveListing.objectiveUIList.Add(CreateObjectiveUI(obj, objectiveUIPrefab, parent));
            //Debug.LogWarning($"Attached to {objectiveListing.gameObject.name}");
        }
        return objectiveListing.gameObject;
    }
}