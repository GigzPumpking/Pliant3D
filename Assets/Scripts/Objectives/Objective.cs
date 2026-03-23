using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class Objective : MonoBehaviour, IObjective {
    [SerializeField] public string description;
    [SerializeField] public ICompletionStrategy CompletionStrategy;
    public bool isComplete;
    public bool showTally;
    public List<UnityEvent> onCompleteEvents;

    [Tooltip("Events invoked when this objective is restored as complete on level reset or save load. Use this to replay world-state changes (e.g. turn on lights) that are not otherwise persisted.")]
    public List<UnityEvent> onRestoreEvents;

    internal void InvokeCompletionEvents()
    {
        foreach(UnityEvent ev in onCompleteEvents) ev?.Invoke();
    }

    internal void InvokeRestoreEvents()
    {
        foreach(UnityEvent ev in onRestoreEvents) ev?.Invoke();
    }

    /// <summary>
    /// Captures the current state of this objective for saving.
    /// Override in subclasses to capture additional data (e.g. fetch items).
    /// </summary>
    public virtual ObjectiveSaveState CaptureState()
    {
        return new ObjectiveSaveState
        {
            objectiveName = gameObject.name,
            description = description,
            isComplete = isComplete
        };
    }

    /// <summary>
    /// Restores subclass-specific state (tally, fetched items, etc.).
    /// Called after isComplete has already been set by the restoration system.
    /// Does NOT re-invoke completion events to avoid duplicate side effects.
    /// </summary>
    public virtual void RestoreState(ObjectiveSaveState state)
    {
        // Base class: nothing extra to restore.
    }
}