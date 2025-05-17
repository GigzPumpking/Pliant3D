using System;

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

    private void CheckCompletion(Transformation transformation) {
        if(transformation == desiredTransformation)
            OnObjectiveComplete?.Invoke(this);
    }
}