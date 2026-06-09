using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerToLocationObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private bool autoCheckChildrenForNodes = true;
    [SerializeField] List<ObjectiveNode> targetLocations = new();
    static Transform _player;
    private int numCompleted = 0;
    private int cachedTotal;
    
    private void Awake() {
        if(autoCheckChildrenForNodes) FetchNodesFromChildren();
        //set each looking for 'gameobject' to the player
        foreach (ObjectiveNode node in targetLocations) {
            if (!Player.Instance || !Player.Instance.gameObject || !node || !targetLocations.Any()) continue;

            if (node.lookingFor == null)
            {
                node.lookingFor = new List<GameObject>();
            }

            if (!node.lookingFor.Contains(Player.Instance.gameObject))
            {
                node.lookingFor.Add(Player.Instance.gameObject);
            }
        }
        
        RefreshCachedTotal();
        RefreshCompletedCount();
    }

    private void OnEnable() {
        ObjectiveNode.OnNodeCompleted += CheckCompletion;
    }

    private void OnDisable() {
        ObjectiveNode.OnNodeCompleted -= CheckCompletion;
    }
    
    private void Start()
    {
        RefreshCachedTotal();
        RefreshCompletedCount();
        if(showTally) RefreshTallyUI();
    }

    private void CheckCompletion()
    {
        if (isComplete) return;
        if (!targetLocations.Any()) return;

        RefreshCompletedCount();

        if(showTally) RefreshTallyUI();
        
        foreach (ObjectiveNode node in targetLocations)
        {
            if (!node) continue;
            if (!node.isComplete)
            {
                return;
            }
        }

        isComplete = true;
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }
    
    private void OnValidate()
    {
        RefreshCachedTotal();
        RefreshCompletedCount();
    }

    private void RefreshCachedTotal()
    {
        int currentTotal = targetLocations != null ? targetLocations.Count(obj => obj != null) : 0;

        if (!Application.isPlaying)
        {
            cachedTotal = currentTotal;
            return;
        }

        if (cachedTotal <= 0)
        {
            cachedTotal = currentTotal;
        }
    }

    private void RefreshCompletedCount()
    {
        numCompleted = targetLocations != null ? targetLocations.Count(obj => obj != null && obj.isComplete) : 0;
    }

    public override void RefreshTallyUI()
    {
        RefreshCachedTotal();
        RefreshCompletedCount();

        if (showTally)
        {
            TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);
        }
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