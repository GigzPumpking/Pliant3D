using System;
using UnityEngine;

public class InteractCompletionStrategy : ICompletionStrategy {
    public InteractCompletionStrategy() {
        OnComplete = () => {
            
        };
    }

    public Action OnComplete { get; set; }
}