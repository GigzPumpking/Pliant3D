using System.Collections.Generic;
using UnityEngine;

public class ObjectiveUIFactory
{
    public static ObjectiveUI CreateObjectiveUI(
        Objective objective,
        GameObject prefab,
        GameObject parent,
        Dictionary<Objective, ObjectiveUI> objectiveUIDict = null)
    {
        if (!objective || !prefab || !parent)
        {
            return null;
        }

        GameObject currentObj = GameObject.Instantiate(prefab, parent.transform);
        Debug.LogWarning($"Instantiating {currentObj.name}");

        ObjectiveUI currentObjectiveUI = currentObj.GetComponent<ObjectiveUI>();

        if (!currentObjectiveUI)
        {
            Debug.LogError($"Objective UI prefab '{prefab.name}' does not have an ObjectiveUI component.");
            return null;
        }

        if (currentObjectiveUI.DescriptionTXT)
        {
            currentObjectiveUI.DescriptionTXT.text = objective.description;
        }

        if (objectiveUIDict != null)
        {
            objectiveUIDict[objective] = currentObjectiveUI;

            Debug.LogWarning($"Mapping objective UI for objective: {objective.description} to {currentObjectiveUI.name}");

            if (objective.showTally)
            {
                if (objective is CustomEventObjective customEventObjective)
                {
                    customEventObjective.RefreshTallyUI();
                }
                else
                {
                    TallyBuilder.UpdateTallyUI(objective, 0, 1);
                }
            }
        }

        if (objective.isComplete)
        {
            currentObjectiveUI.SetCompletedVisual();
        }

        GameManager.Instance?.AddQueuedTaskAssigned();

        return currentObjectiveUI;
    }

    public static GameObject CreateObjectiveListingUI(
        ObjectiveListing objectiveListing,
        GameObject objectiveListingUIPrefab,
        GameObject objectiveUIPrefab,
        GameObject parent,
        Dictionary<Objective, ObjectiveUI> objectiveUIDict = null)
    {
        if (!objectiveListing || !objectiveListingUIPrefab || !objectiveUIPrefab || !parent)
        {
            return null;
        }

        GameObject currentListingUI = GameObject.Instantiate(objectiveListingUIPrefab, parent.transform);

        objectiveListing.objectiveUIList.Clear();

        foreach (Objective objective in objectiveListing.objectives)
        {
            if (!objective) continue;

            ObjectiveUI objectiveUI = CreateObjectiveUI(
                objective,
                objectiveUIPrefab,
                currentListingUI,
                objectiveUIDict
            );

            if (objectiveUI)
            {
                objectiveListing.objectiveUIList.Add(objectiveUI);
            }
        }

        return currentListingUI;
    }

    public static GameObject AddToObjectiveToListingUI(
        ObjectiveListing objectiveListing,
        List<Objective> objectives,
        GameObject objectiveListingUI,
        GameObject objectiveUIPrefab,
        Dictionary<Objective, ObjectiveUI> objectiveUIDict = null)
    {
        if (!objectiveListing || objectives == null || !objectiveListingUI || !objectiveUIPrefab)
        {
            return null;
        }

        foreach (Objective obj in objectives)
        {
            if (!obj) continue;

            if (!objectiveListing.objectives.Contains(obj))
            {
                objectiveListing.objectives.Add(obj);
            }

            ObjectiveUI objectiveUI = CreateObjectiveUI(
                obj,
                objectiveUIPrefab,
                objectiveListingUI,
                objectiveUIDict
            );

            if (objectiveUI)
            {
                objectiveListing.objectiveUIList.Add(objectiveUI);
            }
        }

        return objectiveListingUI;
    }
}