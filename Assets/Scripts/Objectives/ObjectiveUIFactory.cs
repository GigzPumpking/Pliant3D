using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectiveUIFactory {
    public static ObjectiveUI CreateObjectiveUI(Objective objective, GameObject prefab, GameObject parent, Dictionary<Objective, ObjectiveUI> objectiveUIDict = null) {
        if (!objective || !prefab || !parent) return null;
        
        GameObject currentObj = GameObject.Instantiate(prefab, parent.transform);
        
        ObjectiveUI currentObjectiveUI = currentObj.GetComponent<ObjectiveUI>();
        currentObjectiveUI.DescriptionTXT.text = objective.description;
        //Link to ObjectiveTracker
        if(objectiveUIDict != null) objectiveUIDict.TryAdd(objective, currentObjectiveUI);
        if (objectiveUIDict.ContainsKey(objective))
        {
            //Debug.LogWarning($"Mapping objective UI for objective: {objective.description} to {currentObjectiveUI.name}");
            if(objective.showTally) TallyBuilder.InitializeTallyUI(objective, "?");
        }
        
        return currentObjectiveUI;
    }

    public static GameObject CreateObjectiveListingUI(ObjectiveListing objectiveListing, GameObject prefab, GameObject parent) {
        if (!objectiveListing || !prefab || !parent) return null;
        
        return GameObject.Instantiate(prefab, parent.transform);
    }
    
    public static GameObject CreateObjectiveListingUI(ObjectiveListing objectiveListing, GameObject objectiveListingsPrefab, GameObject objectiveUIPrefab, GameObject parent, 
        Dictionary<Objective, ObjectiveUI> objectiveUIDict = null) {
        //instantiate an instance of the listing UI
        GameObject currentListing = GameObject.Instantiate(objectiveListingsPrefab, parent.transform);
        
        //instantiate an instance of it's objective UI
        foreach (Objective objective in objectiveListing.objectives) {
            if (!objective) continue;
            objectiveListing.objectiveUIList.Add(CreateObjectiveUI(objective, objectiveUIPrefab, currentListing, objectiveUIDict));
        }
        
        return currentListing;
    }

    public static GameObject AddToObjectiveToListingUI(ObjectiveListing objectiveListing, List<Objective> objectives, GameObject objectiveListingPrefab, GameObject objectiveUIPrefab, GameObject parent,
        Dictionary<Objective, ObjectiveUI> objectiveUIDict = null)
    {
        foreach(Objective obj in objectives)
        {
            objectiveListing.objectives.Add(obj);
            objectiveListing.objectiveUIList.Add(CreateObjectiveUI(obj, objectiveUIPrefab, parent, objectiveUIDict));
            //Debug.LogWarning($"Attached to {objectiveListing.gameObject.name}");
        }
        return objectiveListing.gameObject;
    }
}