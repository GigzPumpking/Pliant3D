using UnityEngine;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

public class ObjectsToLocationsObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private List<ObjectiveNode> targetLocations;
    [SerializeField] private List<GameObject> lookingFor;
    
    private void Awake() {
        for (int i = 0; i < targetLocations.Count; ++i) {
            targetLocations[i].lookingFor.Add(lookingFor[i]);
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
        OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }
}