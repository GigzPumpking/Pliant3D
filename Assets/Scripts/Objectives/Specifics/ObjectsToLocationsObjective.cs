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

    private int numCompleted = 0;
    private int cachedTotal;
    
    private void Awake()
    {
        RefreshCachedTotal();

        if (targetLocations == null || !targetLocations.Any()) return;
        for (int i = 0; i < targetLocations.Count; ++i) {
            if (!targetLocations[i]) continue;
            if (lookingFor == null || lookingFor.Count == 0) continue;

            if (targetLocations[i].lookingFor == null)
            {
                targetLocations[i].lookingFor = new List<GameObject>();
            }

            GameObject targetObject = i < lookingFor.Count ? lookingFor[i] : lookingFor.Last();

            if (targetObject && !targetLocations[i].lookingFor.Contains(targetObject))
            {
                targetLocations[i].lookingFor.Add(targetObject);
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

    private int GetRequiredTotal()
    {
        if (setNumberOfNeeded > 0)
        {
            return setNumberOfNeeded;
        }

        if (anyObjectToLocation)
        {
            return cachedTotal > 0 ? 1 : 0;
        }

        return cachedTotal;
    }

    private void RefreshCompletedCount()
    {
        int completedCount = targetLocations != null ? targetLocations.Count(curr => curr != null && curr.isComplete) : 0;
        int requiredTotal = GetRequiredTotal();

        numCompleted = requiredTotal > 0 ? Mathf.Clamp(completedCount, 0, requiredTotal) : completedCount;
    }

    public override void RefreshTallyUI()
    {
        RefreshCachedTotal();
        RefreshCompletedCount();

        if (showTally)
        {
            TallyBuilder.UpdateTallyUI(this, numCompleted, GetRequiredTotal());
        }
    }
    
    private void CheckCompletion() {
        if (isComplete) return;
        if (targetLocations == null || !targetLocations.Any()) return;

        RefreshTallyUI();

        if (anyObjectToLocation && targetLocations.Any())
        {
            foreach (ObjectiveNode node in targetLocations)
            {
                if (node != null && node.isComplete)
                {
                    isComplete = true;
                    RefreshTallyUI();
                    OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
                    InvokeCompletionEvents();
                    Debug.Log($"{gameObject.name} has successfully been completed!");
                    return;
                }
            }

            return;
        }
        else
        {
            int completedCount = targetLocations.Count(curr => curr != null && curr.isComplete);
            int requiredTotal = GetRequiredTotal();

            if (setNumberOfNeeded != 0)
            {
                if (completedCount < requiredTotal) return;
            }
            else
            {
                foreach (ObjectiveNode node in targetLocations)
                {
                    if (!node) continue;
                    if (!node.isComplete) return;
                }
            }

            isComplete = true;
            RefreshTallyUI();
            OnObjectiveComplete?.Invoke(this); //Listened to by 'ObjectiveListing.cs'
            InvokeCompletionEvents();
            Debug.Log($"{gameObject.name} has successfully been completed!");
        }
    }
}