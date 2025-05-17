using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToManyLocationsObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] List<ObjectiveNode> targetLocations = new();
    [SerializeField] GameObject lookingFor = new();
    
    private void Awake() {
        //set each looking for 'gameobject' to the player
        for (int i = 0; i < targetLocations.Count; ++i) {
            if(targetLocations[i] != null)
                targetLocations[i].lookingFor.Add(lookingFor);
        }
    }
    
    private void OnEnable() {
        ObjectiveNode.OnNodeCompleted += CheckCompletion;
    }

    private void OnDisable() {
        ObjectiveNode.OnNodeCompleted -= CheckCompletion;
    }
    
    private void CheckCompletion() {
        foreach (ObjectiveNode node in targetLocations) {
            if (node.isComplete) continue;
            else return;
        }

        isComplete = true;
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }
}

public class ManyObjectsToLocationObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] ObjectiveNode targetLocation = new();
    [SerializeField] List<GameObject> lookingFor = new();
    
    private void Awake() {
        //set each looking for 'gameobject' to the player
        for (int i = 0; i < lookingFor.Count; ++i) {
            targetLocation.lookingFor.Add(lookingFor[i]);
        }
    }
    
    private void OnEnable() {
        ObjectiveNode.OnNodeCompleted += CheckCompletion;
    }

    private void OnDisable() {
        ObjectiveNode.OnNodeCompleted -= CheckCompletion;
    }
    
    private void CheckCompletion() {
        if (!targetLocation.isComplete) return;

        isComplete = true;
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }
}