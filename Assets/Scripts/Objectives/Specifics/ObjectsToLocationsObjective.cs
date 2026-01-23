using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

//Use for multiple objects that need to be placed at multiple locations
public class ObjectsToLocationsObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private List<ObjectiveNode> targetLocations;
    [SerializeField] private List<GameObject> lookingFor;
    public bool anyObjectToLocation = false;
    
    //LEAVE 0 IF YOU WANT ALL OBJECTS TO BE PLACED
    [Tooltip("Leave 0 if you want all objects to be placed")]
    public int setNumberOfNeeded;
    
    private void Awake()
    {
        if (!targetLocations.Any()) return;
        for (int i = 0; i < targetLocations.Count; ++i) {
            targetLocations[i].lookingFor.Add(i < lookingFor.Count ? lookingFor[i] : lookingFor.Last());
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
                if (node.isComplete)
                {
                    if(showTally) TallyBuilder.UpdateTallyUI(this, targetLocations.Count(curr => curr.isComplete), setNumberOfNeeded == 0 ? targetLocations.Count : setNumberOfNeeded);
                    if (setNumberOfNeeded != 0 &&
                        targetLocations.Count(curr => curr.isComplete) >= setNumberOfNeeded) break;
                }
                else return;
            }

            isComplete = true;
            OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
            InvokeCompletionEvents();
            Debug.Log($"{gameObject.name} has successfully been completed!");
        }
    }
}