using UnityEngine;

/// <summary>
/// Interface for components that can provide dialogue to a DialogueTrigger.
/// Allows modular, quest-specific dialogue without cluttering DialogueTrigger.
/// </summary>
public interface IDialogueProvider
{
    /// <summary>
    /// Priority determines which provider's dialogue is used when multiple providers are active.
    /// Higher values = higher priority. Default dialogue should use 0.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Whether this provider currently has dialogue to show.
    /// Return false if the provider's conditions aren't met (e.g., quest not in correct state).
    /// </summary>
    bool HasDialogue { get; }
    
    /// <summary>
    /// Get the dialogue entries to display.
    /// Each entry contains default, keyboard, and controller text variants.
    /// </summary>
    DialogueEntry[] GetDialogueEntries();
}
