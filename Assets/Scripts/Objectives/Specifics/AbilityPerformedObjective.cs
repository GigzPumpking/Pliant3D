using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityPerformedObjective : Objective, IDialogueProvider
{
    private enum AbilityType
    {
        FirstAbility  = 1,
        SecondAbility = 2
    }
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] List<Transformation> whichForm = new();
    [SerializeField] AbilityType whichAbility;
    [SerializeField] List<Interactable> whichInteractable = new();
    
    [Tooltip("If true, interactables that were already completed before a level reset or save load will be hidden on restore, preventing them from blocking the player.")]
    [SerializeField] private bool hideCompletedInteractablesOnRestore = false;
    
    private int numCompleted = 0;
    private int cachedTotal;
    private List<string> _completedInteractableNames = new List<string>();

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
        Bulldozer.AbilityUsed += CheckCompletion;
        Frog.AbilityUsed += CheckCompletion;
        DialogueTrigger.InteractedObjective += OnReturnNPCInteracted;
    }

    private void OnDisable()
    {
        Bulldozer.AbilityUsed -= CheckCompletion;
        Frog.AbilityUsed -= CheckCompletion;
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

        FinalizeCompletion();
    }

    private void OnValidate()
    {
        RefreshCachedTotal();
    }

    private void RefreshCachedTotal()
    {
        int currentTotal = whichInteractable != null ? whichInteractable.Count(obj => obj != null) : 0;

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

    private void CheckCompletion(Transformation transformation, int abilityNumber, Interactable interactable)
    {
        Debug.Log($"Ability Performed by {transformation} with ability number {abilityNumber} on {interactable}");
        //early return so we only get what we're looking for
        if (!interactable || !whichForm.Contains(transformation) || !whichInteractable.Contains(interactable) || abilityNumber != (int)whichAbility) return;

        if (_completedInteractableNames.Contains(interactable.gameObject.name)) return;

        _completedInteractableNames.Add(interactable.gameObject.name);

        numCompleted = Mathf.Clamp(_completedInteractableNames.Count, 0, cachedTotal);
        RefreshTallyUI();
        
        //check if the list is empty
        whichInteractable.Remove(interactable);
        if (whichInteractable.Any(i => i != null)) return;

        if (!readyForReturn)
        {
            readyForReturn = true;
            _ = DialogueEligibilityOrder; // stamp this objective's place in the return-dialogue order now
            EnsureReturnNPCProxy();
        }

        if (requiresNPCReturn && returnNPC != null) return;

        FinalizeCompletion();
    }

    private void FinalizeCompletion()
    {
        CompleteObjective();
        RefreshTallyUI();
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        if (returnNPC != null) returnNPC.RefreshDialogueProviders();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.numCompleted = numCompleted;
        state.readyForReturn = readyForReturn;
        state.completedInteractableNames = new List<string>(_completedInteractableNames);
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        base.RestoreState(state);

        RefreshCachedTotal();

        numCompleted = state.numCompleted;
        readyForReturn = state.readyForReturn;
        _completedInteractableNames = new List<string>(state.completedInteractableNames ?? new List<string>());

        EnsureReturnNPCProxy();

        // Remove already-completed interactables so they aren't required again,
        // and optionally hide them so they don't block the player.
        var completed = whichInteractable.Where(i => i != null && _completedInteractableNames.Contains(i.gameObject.name)).ToList();
        if (hideCompletedInteractablesOnRestore)
        {
            foreach (var interactable in completed)
                interactable.gameObject.SetActive(false);
        }
        whichInteractable.RemoveAll(i => i != null && _completedInteractableNames.Contains(i.gameObject.name));

        numCompleted = Mathf.Clamp(_completedInteractableNames.Count, 0, cachedTotal);

        RefreshTallyUI();
    }
}