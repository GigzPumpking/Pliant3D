using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class TransformationSwapInteractObjective : Objective {
    public static event Action<Objective> OnObjectiveComplete;
    public Transformation desiredTransformation;
    public bool transformIntoAny = false;
    
    private void OnEnable() {
        //subscribe to dialogue trigger event
        TransformationWheel.TransformedObjective += CheckCompletion;
    }

    private void OnDisable() {
        TransformationWheel.TransformedObjective -= CheckCompletion;
    }

    private void CheckCompletion(Transformation transformation)
    {
        if (transformIntoAny == true)
        {
            CompleteObjective();
        } else if (transformation == desiredTransformation)
        {
            CompleteObjective();
        }
    }
}