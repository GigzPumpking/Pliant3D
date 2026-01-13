using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerToLocationObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private bool autoCheckChildrenForNodes = true;
    [SerializeField] List<ObjectiveNode> targetLocations = new();
    static Transform _player;

    private void Awake() {
        if(autoCheckChildrenForNodes) FetchNodesFromChildren();
        //set each looking for 'gameobject' to the player
        foreach (ObjectiveNode node in targetLocations) {
            if (!Player.Instance.gameObject) continue;
            node.lookingFor.Add(Player.Instance.gameObject);
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
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    private void FetchNodesFromChildren()
    {
        foreach (Transform child in gameObject.transform)
        {
            if(child.GetComponent<ObjectiveNode>() && 
               !targetLocations.Contains(child.GetComponent<ObjectiveNode>())) targetLocations.Add(child.GetComponent<ObjectiveNode>());
        }
    }
}