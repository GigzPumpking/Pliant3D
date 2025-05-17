using System;

public interface ICompletionStrategy {
    public static event Action<Objective> OnCompleteEventChannel; //EVENT CHANNEL
    public abstract Action OnComplete { get; set; }
}