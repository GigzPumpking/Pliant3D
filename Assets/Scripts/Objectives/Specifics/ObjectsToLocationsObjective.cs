using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

public class ObjectsToLocationsObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private List<ObjectiveNode> targetLocations;
    [SerializeField] private List<GameObject> lookingFor;
    public bool anyObjectToLocation = false;
    
    private void Awake()
    {
        if (!targetLocations.Any()) return;
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
        if (anyObjectToLocation)
        {
            foreach (ObjectiveNode node in targetLocations)
            {
                if (node.isComplete) break;
            }
            
            isComplete = true;
            OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
            InvokeCompletionEvents();
            Debug.Log($"{gameObject.name} has successfully been completed!");
        }
        else
        {
            foreach (ObjectiveNode node in targetLocations)
            {
                if (node.isComplete) continue;
                else return;
            }

            isComplete = true;
            OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
            InvokeCompletionEvents();
            Debug.Log($"{gameObject.name} has successfully been completed!");
        }
    }
}