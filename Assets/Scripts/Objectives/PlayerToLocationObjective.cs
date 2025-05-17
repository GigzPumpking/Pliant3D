using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerToLocationObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] List<ObjectiveNode> targetLocations = new();
    static Transform _player;

    private void Awake() {
        //set each looking for 'gameobject' to the player
        foreach (ObjectiveNode node in targetLocations) {
            node.lookingFor = Player.Instance.gameObject;
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