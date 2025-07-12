using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class TransformationSwapInteractObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    public Transformation desiredTransformation;
    
    private void OnEnable() {
        //subscribe to dialogue trigger event
        TransformationWheel.TransformedObjective += CheckCompletion;
    }

    private void OnDisable() {
        TransformationWheel.TransformedObjective -= CheckCompletion;
    }

    private void CheckCompletion(Transformation transformation)
    {
        if (transformation != desiredTransformation) return;
        isComplete = true;
        OnObjectiveComplete?.Invoke(this);
        InvokeCompletionEvents();
    }
}