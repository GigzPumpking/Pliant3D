using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

//Use for multiple objects that need to be placed at multiple locations
public class ObjectsToLocationsObjective : Objective, IDialogueProvider {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] private List<ObjectiveNode> targetLocations;
    [SerializeField] private List<GameObject> lookingFor;
    public bool anyObjectToLocation = false;
    
    //LEAVE 0 IF YOU WANT ALL OBJECTS TO BE PLACED
    [Tooltip("Leave 0 if you want all objects to be placed")]
    public int setNumberOfNeeded;

    [Tooltip("If true, target location nodes that were already completed before a level reset or save load will be hidden on restore, preventing them from blocking the player.")]
    [SerializeField] private bool hideCompletedInteractablesOnRestore = false;

    private int numCompleted = 0;
    private int cachedTotal;

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
        DialogueTrigger.InteractedObjective += OnReturnNPCInteracted;
    }

    private void OnDisable() {
        ObjectiveNode.OnNodeCompleted -= CheckCompletion;
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

    public override void CompleteObjective()
    {
        if (isComplete) return;

        base.CompleteObjective();

        RefreshTallyUI();

        if (returnNPC != null) returnNPC.RefreshDialogueProviders();
    }
    
    private void CheckCompletion() {
        if (isComplete) return;
        if (targetLocations == null || !targetLocations.Any()) return;

        RefreshTallyUI();

        bool conditionsMet;
        if (anyObjectToLocation)
        {
            conditionsMet = targetLocations.Any(node => node != null && node.isComplete);
        }
        else
        {
            int completedCount = targetLocations.Count(curr => curr != null && curr.isComplete);
            int requiredTotal = GetRequiredTotal();

            conditionsMet = setNumberOfNeeded != 0
                ? completedCount >= requiredTotal
                : targetLocations.All(node => node == null || node.isComplete);
        }

        if (!conditionsMet) return;

        if (!readyForReturn)
        {
            readyForReturn = true;
            _ = DialogueEligibilityOrder; // stamp this objective's place in the return-dialogue order now
            EnsureReturnNPCProxy();
        }

        if (requiresNPCReturn && returnNPC != null) return;

        CompleteObjective();
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.numCompleted = numCompleted;
        state.readyForReturn = readyForReturn;
        state.completedInteractableNames = targetLocations
            .Where(n => n != null && n.isComplete)
            .Select(n => GetNodePath(n))
            .ToList();
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        if (state == null) return;

        base.RestoreState(state);
        readyForReturn = state.readyForReturn;
        EnsureReturnNPCProxy();

        var savedPaths = state.completedInteractableNames;
        if (savedPaths == null || savedPaths.Count == 0) return;

        foreach (ObjectiveNode node in targetLocations)
        {
            if (node == null) continue;
            if (savedPaths.Contains(GetNodePath(node)))
            {
                node.SetCompleteSilently();
                if (hideCompletedInteractablesOnRestore)
                {
                    node.gameObject.SetActive(false);
                }
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