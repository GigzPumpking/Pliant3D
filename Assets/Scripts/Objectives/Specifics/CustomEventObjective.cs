using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomEventObjective : Objective
{
    public static event Action<Objective> OnObjectiveComplete;

    private static readonly HashSet<CustomEventObjective> ActiveCustomEventObjectives = new();

    [SerializeField] private List<GameObject> ObjectiveObjects = new();

    [Tooltip("If true, interactables that were already completed before a level reset or save load will be hidden on restore, preventing them from blocking the player.")]
    [SerializeField] private bool hideCompletedInteractablesOnRestore = false;

    private readonly HashSet<string> completedObjectKeys = new();

    private int cachedTotal;
    private int numCompleted = 0;

    public int TotalRequired => ObjectiveObjects != null ? ObjectiveObjects.Count(obj => obj != null) : 0;
    public int NumCompleted => numCompleted;

    private void Awake()
    {
        RefreshCachedTotal();
    }

    private void OnEnable()
    {
        ActiveCustomEventObjectives.Add(this);
    }

    private void OnDisable()
    {
        ActiveCustomEventObjectives.Remove(this);
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
        cachedTotal = ObjectiveObjects != null ? ObjectiveObjects.Count(obj => obj != null) : 0;
    }

    public void RefreshTallyUI()
    {
        RefreshCachedTotal();

        if (showTally)
        {
            TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);
        }
    }

    public static bool TryCompleteAnyForObject(GameObject completedObject, out CustomEventObjective completedObjective)
    {
        completedObjective = null;

        if (!completedObject)
        {
            return false;
        }

        foreach (CustomEventObjective objective in ActiveCustomEventObjectives)
        {
            if (!objective) continue;

            if (objective.TryCompleteForObject(completedObject))
            {
                completedObjective = objective;
                return true;
            }
        }

        Debug.LogWarning($"No active CustomEventObjective matched pulled object '{completedObject.name}'.");
        return false;
    }

    public bool TryCompleteForObject(GameObject completedObject)
    {
        if (!completedObject)
        {
            return false;
        }

        RefreshCachedTotal();

        // Important:
        // If this was restored partially, make sure it is allowed to progress again.
        if (numCompleted < cachedTotal)
        {
            isComplete = false;
        }

        if (isComplete)
        {
            Debug.LogWarning($"Objective '{description}' is already complete, so '{completedObject.name}' was ignored.");
            return false;
        }

        GameObject matchedObjectiveObject = GetMatchingObjectiveObject(completedObject);

        if (!matchedObjectiveObject)
        {
            return false;
        }

        string matchedKey = GetObjectKey(matchedObjectiveObject);

        if (completedObjectKeys.Contains(matchedKey))
        {
            Debug.LogWarning($"'{matchedObjectiveObject.name}' was already counted for objective '{description}'.");
            return false;
        }

        completedObjectKeys.Add(matchedKey);
        numCompleted = completedObjectKeys.Count;

        RefreshTallyUI();

        Debug.Log($"CustomEventObjective progress: {numCompleted}/{cachedTotal} for {gameObject.name}");

        if (numCompleted >= cachedTotal && cachedTotal > 0)
        {
            CompleteObjective();
        }

        return true;
    }

    private GameObject GetMatchingObjectiveObject(GameObject completedObject)
    {
        if (ObjectiveObjects == null)
        {
            return null;
        }

        foreach (GameObject objectiveObject in ObjectiveObjects)
        {
            if (!objectiveObject) continue;

            if (SameObjectOrHierarchy(objectiveObject, completedObject))
            {
                return objectiveObject;
            }
        }

        return null;
    }

    private bool SameObjectOrHierarchy(GameObject objectiveObject, GameObject completedObject)
    {
        if (!objectiveObject || !completedObject)
        {
            return false;
        }

        if (objectiveObject == completedObject)
        {
            return true;
        }

        if (completedObject.transform.IsChildOf(objectiveObject.transform))
        {
            return true;
        }

        if (objectiveObject.transform.IsChildOf(completedObject.transform))
        {
            return true;
        }

        return false;
    }

    private string GetObjectKey(GameObject obj)
    {
        if (!obj)
        {
            return "";
        }

        // Stable enough for scene reloads as long as the hierarchy/name stays the same.
        return GetHierarchyPath(obj.transform);
    }

    private string GetHierarchyPath(Transform t)
    {
        if (t == null)
        {
            return "";
        }

        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private void CompleteObjective()
    {
        if (isComplete)
        {
            return;
        }

        isComplete = true;

        OnObjectiveComplete?.Invoke(this);
        InvokeCompletionEvents();

        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();

        state.numCompleted = numCompleted;

        // Reuse your existing save field, but store stable object keys instead of only raw names.
        state.completedInteractableNames = completedObjectKeys.ToList();

        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        if (state == null)
        {
            return;
        }

        RefreshCachedTotal();
        completedObjectKeys.Clear();

        List<string> savedCompletedKeys = state.completedInteractableNames ?? new List<string>();
        foreach (string savedKey in savedCompletedKeys)
        {
            if (!string.IsNullOrEmpty(savedKey))
            {
                completedObjectKeys.Add(savedKey);
            }
        }
        
        // convert old names to current hierarchy keys
        if (ObjectiveObjects != null)
        {
            foreach (GameObject objectiveObject in ObjectiveObjects)
            {
                if (!objectiveObject) continue;

                string hierarchyKey = GetObjectKey(objectiveObject);
                string oldNameKey = objectiveObject.name;

                if (completedObjectKeys.Contains(oldNameKey))
                {
                    completedObjectKeys.Add(hierarchyKey);
                }
            }
        }

        // Count only completed objects that still exist in this objectives object list
        numCompleted = 0;

        if (ObjectiveObjects != null)
        {
            foreach (GameObject objectiveObject in ObjectiveObjects)
            {
                if (!objectiveObject) continue;

                string key = GetObjectKey(objectiveObject);

                if (completedObjectKeys.Contains(key) || completedObjectKeys.Contains(objectiveObject.name))
                {
                    numCompleted++;

                    if (hideCompletedInteractablesOnRestore)
                    {
                        objectiveObject.SetActive(false);
                    }
                }
            }
        }
        
        // If this objective was only partially complete, it must be allowed to progress.
        isComplete = cachedTotal > 0 && numCompleted >= cachedTotal;

        RefreshTallyUI();
    }
}