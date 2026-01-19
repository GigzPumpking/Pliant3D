using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;

public class DialogueTrigger : MonoBehaviour, IDialogueProvider, IInteractable
{
    [Header("Base Dialogue")]
    [Tooltip("Default dialogue entries with keyboard/controller variants. Used when no higher-priority provider is active.")]
    public DialogueEntry[] baseDialogue;
    
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact with this NPC. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 0f;
    
    [Tooltip("If true, this NPC requires Terry form to interact. Shows a 'Terry Required' indicator otherwise.")]
    [SerializeField] private bool requiresTerryForm = false;
    
    [Header("Legacy Fields - Use baseDialogue instead")]
    [HideInInspector] public string[] dialogueLines;
    [HideInInspector] public string[] keyboardDialogueLines;
    [HideInInspector] public string[] controllerDialogueLines;
    
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
    
    public static event Action<DialogueTrigger> InteractedObjective;
    public static ObjectiveTracker ObjectiveTracker;
    
    // Track the first entry for EndDialogue matching
    private string currentFirstEntry = "";
    
    // Cached sprite renderer for interact bubble
    private SpriteRenderer _bubbleSpriteRenderer = null;

    #region IDialogueProvider Implementation
    
    // Base dialogue has lowest priority (0)
    public int Priority => 0;
    
    public bool HasDialogue => baseDialogue != null && baseDialogue.Length > 0;
    
    public DialogueEntry[] GetDialogueEntries() => baseDialogue;
    
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
            interactBubble.SetActive(false);
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
            if (provider.HasDialogue && provider.Priority > highestPriority)
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
            interactBubble.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            _bubbleSpriteRenderer.sprite = controllerSprite;
            interactBubble.transform.localScale = new Vector3(0.333f, 0.333f, 1f);
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
