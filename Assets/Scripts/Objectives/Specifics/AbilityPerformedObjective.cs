using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityPerformedObjective : Objective
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

    private void Awake()
    {
        cachedTotal = whichInteractable.Count;
    }

    private void OnEnable()
    {
        
        Bulldozer.AbilityUsed += CheckCompletion;
        Frog.AbilityUsed += CheckCompletion;
    }

    private void OnDisable()
    {
        Bulldozer.AbilityUsed -= CheckCompletion;
        Frog.AbilityUsed -= CheckCompletion;
    }

    private void Start()
    {
        if (showTally) TallyBuilder.UpdateTallyUI(this, 0, cachedTotal);
    }

    private void CheckCompletion(Transformation transformation, int abilityNumber, Interactable interactable)
    {
        Debug.Log($"Ability Performed by {transformation} with ability number {abilityNumber} on {interactable}");
        //early return so we only get what we're looking for
        if (!whichForm.Contains(transformation) || !whichInteractable.Contains(interactable) || abilityNumber != (int)whichAbility) return;

        _completedInteractableNames.Add(interactable.gameObject.name);

        if (showTally) TallyBuilder.UpdateTallyUI(this, ++numCompleted, cachedTotal);
        
        //check if the list is empty
        whichInteractable.Remove(interactable);
        if (whichInteractable.Any()) return;
        
        isComplete = true;
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }

    public override ObjectiveSaveState CaptureState()
    {
        var state = base.CaptureState();
        state.numCompleted = numCompleted;
        state.completedInteractableNames = new List<string>(_completedInteractableNames);
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        numCompleted = state.numCompleted;
        _completedInteractableNames = new List<string>(state.completedInteractableNames ?? new List<string>());

        // Remove already-completed interactables so they aren't required again,
        // and optionally hide them so they don't block the player.
        var completed = whichInteractable.Where(i => i != null && _completedInteractableNames.Contains(i.gameObject.name)).ToList();
        if (hideCompletedInteractablesOnRestore)
        {
            foreach (var interactable in completed)
                interactable.gameObject.SetActive(false);
        }
        whichInteractable.RemoveAll(i => i != null && _completedInteractableNames.Contains(i.gameObject.name));

        if (showTally)
            TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);
    }
}
