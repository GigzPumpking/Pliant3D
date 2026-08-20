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

    /// <summary>
    /// Order this provider first became eligible for return dialogue (ready or complete). Used to
    /// sequence multiple providers sharing one NPC so the earliest-eligible one's ready dialogue is
    /// shown first. Providers with no such concept (e.g. base NPC dialogue) should return -1.
    /// </summary>
    int EligibilityOrder { get; }

    /// <summary>
    /// True once this provider's "ready" tier dialogue has actually been shown to the player at
    /// least once. Providers with no ready/complete staging (e.g. base NPC dialogue) should return
    /// true, since they never need to hold up another provider's ready dialogue.
    /// </summary>
    bool ReadyDialogueShown { get; }
}
