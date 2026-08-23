using UnityEngine;
using UnityEngine.Events;
using System;

public class NPCInteractObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    public DialogueTrigger npcToInteractWith = default;
    
    private void OnEnable() {
        //subscribe to dialogue trigger event
        DialogueTrigger.InteractedObjective += CheckCompletion;
    }

    private void OnDisable() {
        DialogueTrigger.InteractedObjective -= CheckCompletion;
    }

    private void CheckCompletion(DialogueTrigger trigger, IDialogueProvider shownProvider)
    {
        if (trigger != npcToInteractWith)
        {
            Debug.LogWarning($"Spoke with {trigger.gameObject.name} but need to speak with {npcToInteractWith.gameObject.name} to complete objective.");
            return;
        }
        CompleteObjective();
    }
}