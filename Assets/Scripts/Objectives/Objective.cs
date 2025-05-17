using UnityEngine;

[System.Serializable]
public class Objective : MonoBehaviour, IObjective {
    [SerializeField] public string description;
    [SerializeField] public ICompletionStrategy CompletionStrategy;
    public bool isComplete;
}