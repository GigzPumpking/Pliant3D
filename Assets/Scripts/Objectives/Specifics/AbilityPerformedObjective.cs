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
        RefreshCachedTotal();
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
        RefreshCachedTotal();
        RefreshTallyUI();
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
        state.completedInteractableNames = new List<string>(_completedInteractableNames);
        return state;
    }

    public override void RestoreState(ObjectiveSaveState state)
    {
        RefreshCachedTotal();

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

        numCompleted = Mathf.Clamp(_completedInteractableNames.Count, 0, cachedTotal);

        RefreshTallyUI();
    }
}