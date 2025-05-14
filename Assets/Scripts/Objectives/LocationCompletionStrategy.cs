using System;
using System.Collections.Generic;
using UnityEngine;

public class LocationCompletionStrategy : ICompletionStrategy {
    public LocationCompletionStrategy(List<Transform> transforms) {
        OnComplete = () => {
            
        };
    }
    
    public Action OnComplete { get; set; }
    
}