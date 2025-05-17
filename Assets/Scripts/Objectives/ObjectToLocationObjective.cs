using System.Collections.Generic;
using UnityEngine;

public class ObjectToLocationObjective : Objective {
    [SerializeField] List<Transform> targetLocations = new();
    [SerializeField] List<Transform> objectiveNodes  = new();
}