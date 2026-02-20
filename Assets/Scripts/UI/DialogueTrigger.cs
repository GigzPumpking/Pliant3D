using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;

public class DialogueTrigger : MonoBehaviour, IDialogueProvider, IInteractable
{
    [System.Serializable]
    public class DialogueSequence
    {
        public List<DialogueEntry> entries = new List<DialogueEntry>();
    }

    [Header("Base Dialogue")]
    [Tooltip("Default dialogue entries with keyboard/controller variants. Used when no higher-priority provider is active.")]
    public DialogueEntry[] baseDialogue;
    
    [Header("Alternate Dialogue")]
    [Tooltip("Dialogue shown on the second interaction.")]
    public DialogueEntry[] secondaryDialogue;
    
    [Tooltip("Dialogue shown on the third interaction and beyond (if not randomizing).")]
    public DialogueEntry[] tertiaryDialogue;
    
    [Header("Randomized Dialogue")]
    [Tooltip("Enable to randomly select from a pool of dialogue options after initial interactions.")]
    [SerializeField] private bool randomizeDialogue = false;
    
    [Tooltip("If true, the first dialogue (baseDialogue) is always shown on first interaction, then randomized afterwards.")]
    [SerializeField] private bool keepFirstDialogueStatic = true;
    
    [Tooltip("Pool of random dialogue sequences. Each element is a full dialogue (multiple entries played in order). One is randomly picked per interaction.")]
    public List<DialogueSequence> randomDialogueList = new List<DialogueSequence>();
    
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact with this NPC. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 0f;
    
    [Tooltip("If true, this NPC requires Terry form to interact. Shows a 'Terry Required' indicator otherwise.")]
    [SerializeField] private bool requiresTerryForm = false;
    
    [Header("Legacy Fields - Use baseDialogue instead")]
    public string[] dialogueLines;
    public string[] keyboardDialogueLines;
    public string[] controllerDialogueLines;
    
    [Header("Interact Bubble")]
    public GameObject interactBubble;
    public GameObject terryRequired;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;
    
    [Header("Objectives")]
    [SerializeField] private List<Objective> objectiveToGive = new List<Objective>();
    
    [Header("Events")]
    public List<UnityEvent> events = new List<UnityEvent>();
    
    // State
    public bool triggered { get; set; } = false;
    private bool objectiveGiven = false;
    private Dialogue dialogue;
    private IDialogueProvider[] dialogueProviders;
    
    // Track how many times the player has completed a dialogue with this NPC
    private int interactionCount = 0;
    
    // Track last random index to avoid repeating the same dialogue twice in a row
    private int lastRandomIndex = -1;
    
    public static event Action<DialogueTrigger> InteractedObjective;
    public static ObjectiveTracker ObjectiveTracker;
    
    // Track the first entry for EndDialogue matching
    private string currentFirstEntry = "";
    
    // Cached sprite renderer for interact bubble
    private SpriteRenderer _bubbleSpriteRenderer = null;
    private Vector3 _originalBubbleScale;

    #region IDialogueProvider Implementation
    
    // Base dialogue has lowest priority
    public int Priority => int.MinValue;
    
    public bool HasDialogue => GetOwnDialogue() != null && GetOwnDialogue().Length > 0;
    
    public DialogueEntry[] GetDialogueEntries() => GetOwnDialogue();
    
    #endregion
    
    #region IInteractable Implementation
    
    public Vector3 GetPosition() => transform.position;
    
    public float GetInteractionDistance()
    {
        if (interactionDistance > 0f)
            return interactionDistance;
        
        return InteractionManager.Instance?.GetDefaultInteractionDistance() ?? 3f;
    }
    
    public bool IsInteractable()
    {
        // Can't interact if dialogue is already active or already triggered
        if (dialogue != null && dialogue.IsActive()) return false;
        if (triggered) return false;
        
        // Check if we have any dialogue to show
        DialogueEntry[] entries = GetActiveDialogue();
        if (entries == null || entries.Length == 0) return false;
        
        // Check Terry form requirement
        if (requiresTerryForm && Player.Instance != null)
        {
            if (Player.Instance.GetTransformation() != Transformation.TERRY)
            {
                // Still interactable but will show Terry Required indicator
                return true;
            }
        }
        
        return true;
    }
    
    public void OnInteract()
    {
        if (dialogue == null || dialogue.IsActive() || triggered) return;
        
        // Check Terry form requirement
        if (requiresTerryForm && Player.Instance != null)
        {
            if (Player.Instance.GetTransformation() != Transformation.TERRY)
            {
                // Show Terry Required indicator but don't trigger dialogue
                if (terryRequired != null)
                {
                    terryRequired.SetActive(true);
                }
                return;
            }
        }
        
        // Get the best dialogue from all providers
        DialogueEntry[] entries = GetActiveDialogue();
        if (entries != null && entries.Length > 0)
        {
            dialogue.SetDialogueEntries(entries);
            currentFirstEntry = entries[0].defaultText;
        }
        
        triggered = true;
        dialogue.Appear();
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
        
        // Raise interact event with this as the quest giver for objectives to listen
        Interact thisInteract = new Interact();
        thisInteract.questGiver = this;
        EventDispatcher.Raise<Interact>(thisInteract);
        
        InteractedObjective?.Invoke(this);
        
        // Hide interact bubble during dialogue
        SetInteractBubbleActive(false);
    }
    
    public void SetInteractBubbleActive(bool active)
    {
        if (interactBubble != null)
        {
            interactBubble.SetActive(active);
        }
        
        // Hide Terry Required when bubble is hidden
        if (!active && terryRequired != null)
        {
            terryRequired.SetActive(false);
        }
    }
    
    #endregion
    
    void OnEnable() 
    {
        // Register with InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Register(this);
        }
        
        EventDispatcher.AddListener<EndDialogue>(OnEndDialogue);
        
        if (interactBubble != null)
        {
            _originalBubbleScale = interactBubble.transform.localScale;
            interactBubble.SetActive(false);
        }
    }

    void Start() 
    {
        dialogue = UIManager.Instance.returnDialogue();
        if (terryRequired != null)
            terryRequired.SetActive(false);
        
        // Cache all dialogue providers on this GameObject
        RefreshDialogueProviders();
        
        // Migrate legacy fields if baseDialogue is empty
        MigrateLegacyDialogue();
        
        // Register with InteractionManager (in case it wasn't ready in OnEnable)
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Register(this);
        }
    }

    void OnDisable() 
    {
        // Unregister from InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Unregister(this);
        }
        
        EventDispatcher.RemoveListener<EndDialogue>(OnEndDialogue);
    }
    
    /// <summary>
    /// Refreshes the cached list of dialogue providers. Call this if providers are added/removed at runtime.
    /// </summary>
    public void RefreshDialogueProviders()
    {
        dialogueProviders = GetComponents<IDialogueProvider>();
    }
    
    /// <summary>
    /// Gets the highest priority dialogue entries from all providers on this GameObject.
    /// </summary>
    private DialogueEntry[] GetActiveDialogue()
    {
        if (dialogueProviders == null || dialogueProviders.Length == 0)
        {
            RefreshDialogueProviders();
        }
        
        IDialogueProvider bestProvider = null;
        int highestPriority = int.MinValue;
        
        foreach (var provider in dialogueProviders)
        {
            if (provider.HasDialogue && provider.Priority >= highestPriority)
            {
                highestPriority = provider.Priority;
                bestProvider = provider;
            }
        }
        
        return bestProvider?.GetDialogueEntries();
    }
    
    /// <summary>
    /// Migrates old string[] fields to the new DialogueEntry[] format for backwards compatibility.
    /// </summary>
    private void MigrateLegacyDialogue()
    {
        // Only migrate if baseDialogue is empty and legacy fields have content
        if (baseDialogue != null && baseDialogue.Length > 0) return;
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        
        int maxLength = Mathf.Max(
            dialogueLines?.Length ?? 0,
            keyboardDialogueLines?.Length ?? 0,
            controllerDialogueLines?.Length ?? 0
        );
        
        if (maxLength > 0)
        {
            baseDialogue = new DialogueEntry[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                baseDialogue[i] = new DialogueEntry();
                baseDialogue[i].defaultText = (dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "";
                baseDialogue[i].keyboardText = (keyboardDialogueLines != null && keyboardDialogueLines.Length > i) 
                    ? keyboardDialogueLines[i] : "";
                baseDialogue[i].controllerText = (controllerDialogueLines != null && controllerDialogueLines.Length > i) 
                    ? controllerDialogueLines[i] : "";
            }
        }
    }

    public void AutoTriggerDialogue()
    {
        if (dialogue == null) return;
        if (dialogue.IsActive() || triggered) return;
        
        DialogueEntry[] entries = GetActiveDialogue();
        
        if (entries != null && entries.Length > 0)
        {
            dialogue.SetDialogueEntries(entries);
            currentFirstEntry = entries[0].defaultText;
        }

        triggered = true;
        dialogue.Appear();
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
        InteractedObjective?.Invoke(this);
        
        SetInteractBubbleActive(false);
    }

    void OnEndDialogue(EndDialogue e) 
    {
        // Reset triggered state so the NPC can be interacted with again
        triggered = false;
        
        // Check if this dialogue belongs to us using the tracked first entry
        if (string.IsNullOrEmpty(currentFirstEntry) || e.someEntry != currentFirstEntry) return;
        
        // Increment interaction count for alternate dialogue tracking
        interactionCount++;
        
        foreach(var evt in events)
        {
            evt.Invoke();
        }
        
        if (!objectiveGiven && objectiveToGive != null && objectiveToGive.Count > 0)
        {
            if (!ObjectiveTracker) ObjectiveTracker = GameObject.FindObjectOfType<ObjectiveTracker>();
            ObjectiveTracker.AddObjective(objectiveToGive);
            objectiveGiven = true;
        }
        
        // Force InteractionManager to update so bubble reappears if still in range
        InteractionManager.Instance?.ForceUpdate();
    }

    void Update()
    {
        UpdateInteractBubbleSprite();
        UpdateTerryRequiredIndicator();
    }
    
    /// <summary>
    /// Gets this NPC's own dialogue based on interaction count and settings.
    /// Does not consider other IDialogueProvider components.
    /// </summary>
    private DialogueEntry[] GetOwnDialogue()
    {
        // If randomizing dialogue
        if (randomizeDialogue)
        {
            // First interaction and keepFirstDialogueStatic is true: use base dialogue
            if (interactionCount == 0 && keepFirstDialogueStatic)
            {
                return baseDialogue;
            }
            
            // Try to get random dialogue from the pool
            DialogueEntry[] randomDialogue = GetRandomDialogue();
            if (randomDialogue != null && randomDialogue.Length > 0)
            {
                return randomDialogue;
            }
            
            // If no random pool entries, fall through to sequential dialogue logic
        }
        
        // Sequential dialogue stages (base -> secondary -> tertiary)
        if (interactionCount == 0)
        {
            return baseDialogue;
        }
        
        // Second interaction: secondary dialogue (or fallback to tertiary, then base)
        if (interactionCount == 1)
        {
            if (secondaryDialogue != null && secondaryDialogue.Length > 0)
                return secondaryDialogue;
            if (tertiaryDialogue != null && tertiaryDialogue.Length > 0)
                return tertiaryDialogue;
            return baseDialogue;
        }
        
        // Third+ interaction: tertiary dialogue (or fallback to secondary, then base)
        if (tertiaryDialogue != null && tertiaryDialogue.Length > 0)
            return tertiaryDialogue;
        if (secondaryDialogue != null && secondaryDialogue.Length > 0)
            return secondaryDialogue;
        return baseDialogue;
    }
    
    /// <summary>
    /// Gets a random dialogue from the random dialogue list, avoiding immediate repeats.
    /// </summary>
    private DialogueEntry[] GetRandomDialogue()
    {
        if (randomDialogueList == null || randomDialogueList.Count == 0)
            return null;
        
        // If only one option, just return it
        if (randomDialogueList.Count == 1)
            return randomDialogueList[0].entries?.ToArray();
        
        // Pick a random index, avoiding the last one used
        int newIndex;
        do
        {
            newIndex = UnityEngine.Random.Range(0, randomDialogueList.Count);
        } while (newIndex == lastRandomIndex && randomDialogueList.Count > 1);
        
        lastRandomIndex = newIndex;
        return randomDialogueList[newIndex].entries?.ToArray();
    }
    
    /// <summary>
    /// Resets the interaction count. Useful for quests that need to reset NPC dialogue.
    /// </summary>
    public void ResetInteractionCount()
    {
        interactionCount = 0;
        lastRandomIndex = -1;
    }
    
    /// <summary>
    /// Gets the current interaction count.
    /// </summary>
    public int GetInteractionCount() => interactionCount;
    
    /// <summary>
    /// Updates the interact bubble sprite based on current input device.
    /// </summary>
    private void UpdateInteractBubbleSprite()
    {
        if (interactBubble == null || !interactBubble.activeSelf) return;
        
        if (_bubbleSpriteRenderer == null)
        {
            interactBubble.TryGetComponent<SpriteRenderer>(out _bubbleSpriteRenderer);
        }
        
        if (_bubbleSpriteRenderer == null) return;
        
        bool isKeyboard = InputManager.Instance?.ActiveDeviceType == "Keyboard" 
                       || InputManager.Instance?.ActiveDeviceType == "Mouse";
        
        if (isKeyboard)
        {
            _bubbleSpriteRenderer.sprite = keyboardSprite;
            interactBubble.transform.localScale = _originalBubbleScale * 3f;
        }
        else
        {
            _bubbleSpriteRenderer.sprite = controllerSprite;
            interactBubble.transform.localScale = _originalBubbleScale * 1f;
        }
    }
    
    /// <summary>
    /// Shows the Terry Required indicator if the player is not in Terry form and this NPC requires it.
    /// </summary>
    private void UpdateTerryRequiredIndicator()
    {
        if (terryRequired == null) return;
        if (!interactBubble || !interactBubble.activeSelf)
        {
            terryRequired.SetActive(false);
            return;
        }
        
        if (requiresTerryForm && Player.Instance != null)
        {
            terryRequired.SetActive(Player.Instance.GetTransformation() != Transformation.TERRY);
        }
        else
        {
            terryRequired.SetActive(false);
        }
    }
}
