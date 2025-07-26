using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ObjectiveListing : MonoBehaviour {
    [Header("UI")]
    public GameObject objectiveListingPrefab = default;
    public List<ObjectiveUI> objectiveUIList = new();
    
    [Header("Objectives & Status")]
    public List<Objective> objectives = new();
    public bool isComplete;
    [SerializeField] private List<UnityEvent> onCompletionEvents;
    
    public static event Action<ObjectiveListing> OnObjectiveListingComplete;
    private Dictionary<Objective, ObjectiveUI> _objectiveToUI;


    void Start() {
        //need to create an Objective UI for each objective
    }
    
    //will refactor later
    private void OnEnable() {
        PlayerToLocationObjective.OnObjectiveComplete += SetCompletionOfObjective;
        ObjectsToLocationsObjective.OnObjectiveComplete += SetCompletionOfObjective;
        ObjectToManyLocationsObjective.OnObjectiveComplete += SetCompletionOfObjective;
        NPCInteractObjective.OnObjectiveComplete += SetCompletionOfObjective;
        TransformationSwapInteractObjective.OnObjectiveComplete += SetCompletionOfObjective;
        //add logic for the other strategies too
    }

    private void OnDisable() {
        PlayerToLocationObjective.OnObjectiveComplete -= SetCompletionOfObjective;
        ObjectsToLocationsObjective.OnObjectiveComplete -= SetCompletionOfObjective;
        ObjectToManyLocationsObjective.OnObjectiveComplete -= SetCompletionOfObjective;
        NPCInteractObjective.OnObjectiveComplete -= SetCompletionOfObjective;
        TransformationSwapInteractObjective.OnObjectiveComplete -= SetCompletionOfObjective;
    }

    private void CheckCompletion() {
        foreach (Objective objective in objectives)
        {
            if (!objective.isComplete) return;
        }

        isComplete = true;
        
        OnObjectiveListingComplete?.Invoke(this);
        InvokeOnCompleteEvents();
        //will get listened to by 'ObjectiveTracker.cs', will play corresponding animation
    }
    
    //VERY inefficient for now
    private void SetCompletionOfObjective(Objective objective) {
        if (objectives.Contains(objective))
        {
            //handle the animations
            //bad for now
            if(objectiveUIList.Any()) objectiveUIList.ElementAt(objectives.IndexOf(objective)).OnComplete();
        }
        CheckCompletion();
    }

    private void InvokeOnCompleteEvents()
    {
        foreach(UnityEvent ev in onCompletionEvents) ev?.Invoke();
    }
    
}

public enum ObjectiveType {
    PlayerToLocation = 0,
    ObjectToLocation = 1,
    Interact = 2
}