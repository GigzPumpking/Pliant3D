using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveNode : MonoBehaviour {
    public static event Action OnNodeCompleted;
    public GameObject lookingFor;
    [SerializeField] public bool isComplete {
        get;
        set;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.Equals(lookingFor)) {
            Debug.Log($"Player has SUCCESSFULLY entered the trigger box of {gameObject.name}");
            
            isComplete = true;
            OnNodeCompleted?.Invoke();
        }
    }
}