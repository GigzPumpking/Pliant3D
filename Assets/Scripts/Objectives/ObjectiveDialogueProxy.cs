using UnityEngine;

/// <summary>
/// Generic proxy that forwards IDialogueProvider calls to a source objective.
/// Added at runtime to a return NPC's GameObject so an objective living elsewhere
/// (e.g. an ObjectivesHolder) can provide dialogue through that NPC's DialogueTrigger.
/// </summary>
public class ObjectiveDialogueProxy : MonoBehaviour, IDialogueProvider
{
    private IDialogueProvider source;

    public void Initialize(IDialogueProvider provider)
    {
        source = provider;
    }

    public int Priority => source?.Priority ?? -1;

    public bool HasDialogue => source != null && source.HasDialogue;

    public DialogueEntry[] GetDialogueEntries() => source?.GetDialogueEntries();

    public int EligibilityOrder => source?.EligibilityOrder ?? -1;

    public bool ReadyDialogueShown => source?.ReadyDialogueShown ?? true;
}
