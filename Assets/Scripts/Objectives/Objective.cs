using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class Objective : MonoBehaviour, IObjective {
    [SerializeField] public string description;
    [SerializeField] public ICompletionStrategy CompletionStrategy;
    public bool isComplete;
    public List<UnityEvent> onCompleteEvents;

    internal void InvokeCompletionEvents()
    {
        foreach(UnityEvent ev in onCompleteEvents) ev?.Invoke();
    }
}