using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomEventObjective : Objective, IDialogueProvider
{
    public static event Action<Objective> OnObjectiveComplete;

    private static readonly HashSet<CustomEventObjective> ActiveCustomEventObjectives = new();

    [SerializeField] private List<GameObject> ObjectiveObjects = new();

    [Tooltip("If true, interactables that were already completed before a level reset or save load will be hidden on restore, preventing them from blocking the player.")]
    [SerializeField] private bool hideCompletedInteractablesOnRestore = false;

    private readonly HashSet<string> completedObjectKeys = new();

    private int cachedTotal;
    private int numCompleted = 0;

    public int TotalRequired => cachedTotal;
    public int NumCompleted => numCompleted;

    [Header("NPC Return Dialogue (Optional)")]
    [Tooltip("The NPC the player must talk to for return dialogue/completion. Leave empty to complete automatically.")]
    [SerializeField] private DialogueTrigger returnNPC;

    [Tooltip("If true and a Return NPC is assigned, the player must interact with the Return NPC after conditions are met before the objective completes.")]
    [SerializeField] private bool requiresNPCReturn = false;

    [Tooltip("Shown by the return NPC when conditions are met but the objective hasn't been completed yet.")]
    public DialogueEntry[] readyDialogue;

    [Tooltip("Shown by the return NPC after the objective is fully complete.")]
    public DialogueEntry[] completeDialogue;

    private ObjectiveDialogueProxy returnNPCProxy;
    private bool readyForReturn = false;

    private const int PRIORITY_READY = 10;
    private const int PRIORITY_COMPLETE = 20;

    // 0 = nothing revealed, 1 = ready dialogue revealed, 2 = complete dialogue revealed.
    private const int STAGE_READY = 1;
    private const int STAGE_COMPLETE = 2;

    private int GetTargetStage() => isComplete ? STAGE_COMPLETE : (readyForReturn ? STAGE_READY : 0);

    #region IDialogueProvider Implementation (Return NPC)

    public int Priority
    {
        get
        {
            int stage = ResolveDialogueStage(GetTargetStage());
            if (stage == STAGE_COMPLETE) return PRIORITY_COMPLETE;
            if (stage == STAGE_READY) return PRIORITY_READY;
            return -1;
        }
    }

    public bool HasDialogue
    {
        get
        {
            int stage = ResolveDialogueStage(GetTargetStage());
            if (stage == STAGE_COMPLETE) return completeDialogue != null && completeDialogue.Length > 0;
            if (stage == STAGE_READY) return readyDialogue != null && readyDialogue.Length > 0;
            return false;
        }
    }

    public DialogueEntry[] GetDialogueEntries()
    {
        int stage = ResolveDialogueStage(GetTargetStage());
        if (stage == STAGE_COMPLETE) return completeDialogue;
        if (stage == STAGE_READY) return readyDialogue;
        return null;
    }

    public int EligibilityOrder => DialogueEligibilityOrder;

    public bool ReadyDialogueShown => HasShownReadyDialogue;

    #endregion

    private void Awake()
    {
        RefreshCachedTotal();
    }

    private void OnEnable()
    {
        ActiveCustomEventObjectives.Add(this);
        DialogueTrigger.InteractedObjective += OnReturnNPCInteracted;
    }

    private void OnDisable()
    {
        ActiveCustomEventObjectives.Remove(this);
        DialogueTrigger.InteractedObjective -= OnReturnNPCInteracted;

        if (returnNPCProxy != null)
        {
            Destroy(returnNPCProxy);
            returnNPCProxy = null;

            if (returnNPC != null)
            {
                returnNPC.RefreshDialogueProviders();
            }
        }
    }

    private void Start()
    {
        RefreshCachedTotal();
        RefreshTallyUI();

        EnsureReturnNPCProxy();
    }

    private void EnsureReturnNPCProxy()
    {
        if (returnNPC == null) return;

        if (returnNPCProxy == null)
        {
            returnNPCProxy = returnNPC.gameObject.AddComponent<ObjectiveDialogueProxy>();
            returnNPCProxy.Initialize(this);
        }

        returnNPC.RefreshDialogueProviders();
    }

    private void OnReturnNPCInteracted(DialogueTrigger interactedNPC, IDialogueProvider shownProvider)
    {
        if (returnNPC == null) return;
        if (interactedNPC != returnNPC) return;
        if (shownProvider != returnNPCProxy) return; // another objective's dialogue was shown this time

        // Only mark our own stage as shown once our own dialogue was actually displayed.
        MarkDialogueStageShown(ResolveDialogueStage(GetTargetStage()));

        if (!readyForReturn) return;
        if (isComplete) return;

        CompleteObjective();
    }

    private void OnValidate()
    {
        RefreshCachedTotal();
    }

    private void RefreshCachedTotal()
    {
        int currentTotal = ObjectiveObjects != null ? ObjectiveObjects.Count(obj => obj != null) : 0;

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

    public override void RefreshTallyUI()
    {
        RefreshCachedTotal();

        if (showTally)
        {
            TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);
        }
    }

    public override void CompleteObjective()
    {
        if (isComplete) return;

        base.CompleteObjective();

        if (returnNPC != null) returnNPC.RefreshDialogueProviders();
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

        if (numCompleted >= cachedTotal && cachedTotal > 0 && !isComplete)
        {
            if (!readyForReturn)
            {
                readyForReturn = true;
                _ = DialogueEligibilityOrder; // stamp this objective's place in the return-dialogue order now
                EnsureReturnNPCProxy();
            }

            if (!requiresNPCReturn || returnNPC == null)
            {
                CompleteObjective();
            }
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

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();

        state.numCompleted = numCompleted;
        state.readyForReturn = readyForReturn;

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

        base.RestoreState(state);
        readyForReturn = state.readyForReturn;
        EnsureReturnNPCProxy();

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