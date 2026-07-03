using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Use for 1 Object that needs to go to many locations
public class ObjectToManyLocationsObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private List<ObjectiveNode> targetLocations = new();
    [SerializeField] private GameObject lookingFor;

    private int numCompleted = 0;
    private int cachedTotal;
    
    private void Awake() {
        RefreshCachedTotal();

        //set each looking for 'gameobject' to the player
        for (int i = 0; i < targetLocations.Count; ++i) {
            if(targetLocations[i] != null)
            {
                if (targetLocations[i].lookingFor == null)
                {
                    targetLocations[i].lookingFor = new List<GameObject>();
                }

                if (lookingFor && !targetLocations[i].lookingFor.Contains(lookingFor))
                {
                    targetLocations[i].lookingFor.Add(lookingFor);
                }
            }
        }
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
        RefreshTallyUI();
    }

    private void OnValidate()
    {
        RefreshCachedTotal();
    }

    private void RefreshCachedTotal()
    {
        int currentTotal = targetLocations != null ? targetLocations.Count(node => node != null) : 0;

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
        numCompleted = targetLocations != null ? targetLocations.Count(curr => curr != null && curr.isComplete) : 0;
        numCompleted = Mathf.Clamp(numCompleted, 0, cachedTotal);
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
    
    private void CheckCompletion() {
        if (isComplete) return;

        RefreshTallyUI();

        foreach (ObjectiveNode node in targetLocations) {
            if (!node) continue;
            if (!node.isComplete) return;
        }

        isComplete = true;
        RefreshTallyUI();
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.numCompleted = numCompleted;
        state.completedInteractableNames = targetLocations
            .Where(n => n != null && n.isComplete)
            .Select(n => GetNodePath(n))
            .ToList();
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        if (state == null) return;

        var savedPaths = state.completedInteractableNames;
        if (savedPaths == null || savedPaths.Count == 0) return;

        foreach (ObjectiveNode node in targetLocations)
        {
            if (node == null) continue;
            if (savedPaths.Contains(GetNodePath(node)))
            {
                node.SetCompleteSilently();
            }
        }

        RefreshCompletedCount();
        RefreshTallyUI();
    }

    private string GetNodePath(ObjectiveNode node) => GetHierarchyPath(node.transform);

    private string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}

public class ManyObjectsToLocationObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] ObjectiveNode targetLocation = new();
    [SerializeField] List<GameObject> lookingFor = new();

    private int numCompleted = 0;
    private int cachedTotal;
    
    private void Awake() {
        RefreshCachedTotal();

        if (targetLocation != null && targetLocation.lookingFor == null)
        {
            targetLocation.lookingFor = new List<GameObject>();
        }

        //set each looking for 'gameobject' to the player
        for (int i = 0; i < lookingFor.Count; ++i) {
            if (targetLocation != null && lookingFor[i] != null && !targetLocation.lookingFor.Contains(lookingFor[i]))
            {
                targetLocation.lookingFor.Add(lookingFor[i]);
            }
        }
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
        RefreshTallyUI();
    }

    private void OnValidate()
    {
        RefreshCachedTotal();
    }

    private void RefreshCachedTotal()
    {
        int currentTotal = lookingFor != null ? lookingFor.Count(obj => obj != null) : 0;

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
        if (targetLocation == null)
        {
            numCompleted = 0;
            return;
        }

        int remaining = targetLocation.lookingFor != null ? targetLocation.lookingFor.Count(obj => obj != null) : 0;
        numCompleted = Mathf.Clamp(cachedTotal - remaining, 0, cachedTotal);
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
    
    private void CheckCompletion() {
        if (isComplete) return;

        RefreshTallyUI();

        if (targetLocation == null || !targetLocation.isComplete) return;

        isComplete = true;
        RefreshTallyUI();
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.numCompleted = numCompleted;
        // "placed" items = in the master lookingFor list but no longer in the node's live list
        if (targetLocation != null && targetLocation.lookingFor != null)
        {
            state.completedInteractableNames = lookingFor
                .Where(obj => obj != null && !targetLocation.lookingFor.Contains(obj))
                .Select(obj => GetHierarchyPath(obj.transform))
                .ToList();
        }
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        if (state == null) return;
        if (targetLocation == null || targetLocation.lookingFor == null) return;

        var savedPaths = state.completedInteractableNames;
        if (savedPaths == null || savedPaths.Count == 0) return;

        foreach (GameObject obj in lookingFor)
        {
            if (obj == null) continue;
            // Accept hierarchy path (current format) or plain name (old save fallback)
            if (savedPaths.Contains(GetHierarchyPath(obj.transform)) || savedPaths.Contains(obj.name))
            {
                targetLocation.lookingFor.Remove(obj);
            }
        }

        RefreshCompletedCount();
        RefreshTallyUI();
    }

    private string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}