using UnityEngine;
using System.Collections.Generic;

public abstract class Objective : MonoBehaviour, IObjective {
    public string description { get; set; }
    public ICompletionStrategy completionStrategy { get; set; }
}

public class PlayerToLocationObjective : Objective {
    [SerializeField] List<Transform> targetLocations = new();
    [SerializeField] Transform player;
}

public class ObjectToLocationObjective : Objective {
    [SerializeField] List<Transform> targetLocations = new();
    [SerializeField] List<Transform> objectiveNodes  = new();
}

public class InteractObjective : Objective {
    
}

public class ObjectiveNode{
    
}