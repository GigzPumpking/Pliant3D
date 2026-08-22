using System;
using UnityEngine;

//Completes when the assigned AnimTrigger has been interacted with via its Interact Trigger flow
public class InteractObjective : Objective, IDialogueProvider
{
    public static event Action<Objective> OnObjectiveComplete;

    [Tooltip("The AnimTrigger the player must interact with to complete this objective. Must have Use Interact Trigger enabled.")]
    [SerializeField] private AnimTrigger animTriggerToInteractWith;

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

    private void OnEnable()
    {
        AnimTrigger.Interacted += CheckCompletion;
        DialogueTrigger.InteractedObjective += OnReturnNPCInteracted;
    }

    private void OnDisable()
    {
        AnimTrigger.Interacted -= CheckCompletion;
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

    private void CheckCompletion(AnimTrigger trigger)
    {
        if (isComplete) return;
        if (trigger != animTriggerToInteractWith) return;

        if (!readyForReturn)
        {
            readyForReturn = true;
            _ = DialogueEligibilityOrder; // stamp this objective's place in the return-dialogue order now
            EnsureReturnNPCProxy();
        }

        if (requiresNPCReturn && returnNPC != null) return;

        CompleteObjective();
    }

    public override void CompleteObjective()
    {
        if (isComplete) return;

        base.CompleteObjective();

        if (returnNPC != null) returnNPC.RefreshDialogueProviders();
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.readyForReturn = readyForReturn;
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        base.RestoreState(state);
        readyForReturn = state.readyForReturn;
        EnsureReturnNPCProxy();
    }
}
