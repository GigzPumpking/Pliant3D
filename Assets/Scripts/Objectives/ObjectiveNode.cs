using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Enumerable = System.Linq.Enumerable;

[RequireComponent(typeof(Collider))]
public class ObjectiveNode : MonoBehaviour
{
    public static event Action OnNodeCompleted;
    public List<GameObject> lookingFor;
    private Collider coll;
    public bool isComplete { get; set; }

    private void Awake()
    {
        TryGetComponent(out coll);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isComplete) return;
        if (!lookingFor.Contains(other.gameObject)) return;
        lookingFor.Remove(other.gameObject);

        if (lookingFor.Any()) return;
        isComplete = true;
        OnNodeCompleted?.Invoke();
    }

    /// <summary>
    /// Marks this node complete without broadcasting OnNodeCompleted.
    /// Used by objective RestoreState to silently reinstate saved node progress.
    /// </summary>
    public void SetCompleteSilently()
    {
        isComplete = true;
    }
}