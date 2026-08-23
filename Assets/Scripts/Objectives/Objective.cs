using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class Objective : MonoBehaviour, IObjective {
    public static event Action<Objective> OnObjectiveComplete;
    [SerializeField] public string description;
    [SerializeField] public ICompletionStrategy CompletionStrategy;
    public bool isComplete;
    public bool showTally;
    public List<UnityEvent> onCompleteEvents;

    [Tooltip("Events invoked when this objective is restored as complete on level reset or save load. Use this to replay world-state changes (e.g. turn on lights) that are not otherwise persisted.")]
    public List<UnityEvent> onRestoreEvents;

    [Header("Tracking Rules")]
    [Tooltip("If false, this task is ignored by the GameManager's proficiency score (e.g., Tutorials).")]
    public bool countsTowardsProficiency = true;

    // How much of the ready/complete return-dialogue progression has actually been shown to the
    // player (0 = none, 1 = ready, 2 = complete). Only ever set when THIS objective's own dialogue
    // was the one displayed, via MarkDialogueStageShown - never advances just because the player
    // talked to the shared NPC about something else.
    protected int revealedDialogueStage = 0;

    // Global order in which objectives become eligible for return dialogue (ready or complete),
    // used to sequence multiple objectives that share the same return NPC: whichever became
    // eligible first gets its Ready dialogue shown first.
    private static int nextDialogueEligibilityOrder = 0;
    private int dialogueEligibilityOrder = -1;

    protected int DialogueEligibilityOrder
    {
        get
        {
            if (dialogueEligibilityOrder < 0) dialogueEligibilityOrder = nextDialogueEligibilityOrder++;
            return dialogueEligibilityOrder;
        }
    }

    protected bool HasShownReadyDialogue => revealedDialogueStage >= 1;

    // Marks that the given stage (1 = ready, 2 = complete) has now actually been shown to the
    // player. The stage only ever increases, so an already-revealed tier is never "unshown".
    protected void MarkDialogueStageShown(int stage)
    {
        revealedDialogueStage = Math.Max(revealedDialogueStage, stage);
    }

    // Resolves which stage to actually display: forces "ready" (1) first if this objective's
    // target jumped straight to "complete" (2) before its ready dialogue was ever shown, so a
    // quest completed before it was even given still plays ready -> complete instead of skipping
    // straight to complete.
    protected int ResolveDialogueStage(int targetStage)
    {
        if (targetStage <= 0) return 0;
        if (targetStage >= 2 && !HasShownReadyDialogue) return 1;
        return targetStage;
    }

    internal void InvokeCompletionEvents()
    {
        foreach(UnityEvent ev in onCompleteEvents) ev?.Invoke();
    }

    internal void InvokeRestoreEvents()
    {
        foreach(UnityEvent ev in onRestoreEvents) ev?.Invoke();
    }
    

    /// <summary>
    /// Captures the current state of this objective for saving.
    /// Override in subclasses to capture additional data (e.g. fetch items).
    /// </summary>
    public virtual ObjectiveSaveState CaptureState()
    {
        return new ObjectiveSaveState
        {
            objectiveName = gameObject.name,
            description = description,
            isComplete = isComplete,
            revealedDialogueStage = revealedDialogueStage
        };
    }

    /// <summary>
    /// Restores subclass-specific state (tally, fetched items, etc.).
    /// Called after isComplete has already been set by the restoration system.
    /// Does NOT re-invoke completion events to avoid duplicate side effects.
    /// </summary>
    public virtual void RestoreState(ObjectiveSaveState state)
    {
        revealedDialogueStage = state.revealedDialogueStage;
    }
    
    //TALLY STUFF
    public virtual void RefreshTallyUI()
    {
        if (!showTally) return;

        TallyBuilder.UpdateTallyUI(this, 0, 1);
    }

    public virtual void CompleteObjective()
    {
        if (isComplete) return; // Prevent double completion

        isComplete = true;
        InvokeCompletionEvents();
        OnObjectiveComplete?.Invoke(this);
        ObjectiveTracker.Instance?.PlayTaskCompletedSound();

        if (GameManager.Instance != null && countsTowardsProficiency)
        {
            GameManager.Instance.AddQueuedTaskComplete();
        }
        
        Debug.Log($"[Objective] '{gameObject.name}' ({description}) has successfully been completed!");
    }
}