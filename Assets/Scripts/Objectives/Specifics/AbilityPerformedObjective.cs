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
    
    private int numCompleted = 0;
    private int cachedTotal;
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

        if (showTally) TallyBuilder.UpdateTallyUI(this, ++numCompleted, cachedTotal);
        
        //check if the list is empty
        whichInteractable.Remove(interactable);
        if (whichInteractable.Any()) return;
        
        isComplete = true;
        OnObjectiveComplete?.Invoke(this); //this needs to update the objective listing to mark the objective off as complete
        InvokeCompletionEvents();
        Debug.Log($"{gameObject.name} has successfully been completed!");
    }
}
