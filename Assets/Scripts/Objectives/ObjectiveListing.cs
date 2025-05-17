using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveListing : MonoBehaviour {
    public static event Action<ObjectiveListing> OnObjectiveListingComplete;
    private Dictionary<Objective, ObjectiveUI> _objectiveToUI;
    public List<ObjectiveUI> objectiveUIs = new();
    
    [Header("Objectives & Status")]
    public List<Objective> objectives = new();
    public bool isComplete;

    void Start() {
        //need to create an Objective UI for each objective
    }
    private void OnEnable() {
        PlayerToLocationObjective.OnObjectiveComplete += SetCompletionOfObjective;
        //add logic for the other strategies too
    }

    private void OnDisable() {
        PlayerToLocationObjective.OnObjectiveComplete -= SetCompletionOfObjective;
    }

    private void CheckCompletion() {
        foreach (Objective objective in objectives) {
            if (!objective || objective.isComplete) continue;
            else return;
        }

        isComplete = true;
        OnObjectiveListingComplete?.Invoke(this);
    }
    
    private void SetCompletionOfObjective(Objective objective) {
        if (objectives.Contains(objective)) {
            
        }
        CheckCompletion();
    }
    
}

public enum ObjectiveType {
    PlayerToLocation = 0,
    ObjectToLocation = 1,
    Interact = 2
}