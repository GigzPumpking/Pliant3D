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

    private void CheckCompletion(DialogueTrigger trigger) {
        if(trigger == npcToInteractWith)
            OnObjectiveComplete?.Invoke(this);
    }
}