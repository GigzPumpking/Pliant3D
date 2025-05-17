using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Enumerable = System.Linq.Enumerable;

[RequireComponent(typeof(Collider))]
public class ObjectiveNode : MonoBehaviour {
    public static event Action OnNodeCompleted;
    public List<GameObject> lookingFor;
    public bool isComplete {get;set;}

    private void OnTriggerEnter(Collider other) {
        if (!lookingFor.Contains(other.gameObject)) return;
        lookingFor.Remove(other.gameObject);

        if (lookingFor.Any()) return;
        isComplete = true;
        OnNodeCompleted?.Invoke();
    }
}