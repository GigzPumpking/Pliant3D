using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Enumerable = System.Linq.Enumerable;

[RequireComponent(typeof(Collider))]
public class ObjectiveNode : MonoBehaviour {
    public static event Action OnNodeCompleted;
    public List<GameObject> lookingFor;
    [SerializeField] public bool isComplete {
        get;
        set;
    }

    private void OnTriggerEnter(Collider other) {
        if (lookingFor.Contains(other.gameObject)) {
            Debug.Log($"{other.gameObject.name} has SUCCESSFULLY entered the trigger box of {gameObject.name}");
            lookingFor.RemoveAt(lookingFor.IndexOf(other.gameObject));
            
            if (!lookingFor.Any()) {
                isComplete = true;
                OnNodeCompleted?.Invoke();
            }
        }
    }
}