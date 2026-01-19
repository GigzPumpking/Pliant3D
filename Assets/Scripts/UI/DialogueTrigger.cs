using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;

public class DialogueTrigger : MonoBehaviour, IDialogueProvider
{
    [Header("Base Dialogue")]
    [Tooltip("Default dialogue entries with keyboard/controller variants. Used when no higher-priority provider is active.")]
    public DialogueEntry[] baseDialogue;
    
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
    private bool inRadius = false;
    public bool triggered { get; set; } = false;
    private bool objectiveGiven = false;
    private Dialogue dialogue;
    private IDialogueProvider[] dialogueProviders;
    
    public static event Action<DialogueTrigger> InteractedObjective;
    public static ObjectiveTracker ObjectiveTracker;
    
    // Track the first entry for EndDialogue matching
    private string currentFirstEntry = "";

    #region IDialogueProvider Implementation
    
    // Base dialogue has lowest priority (0)
    public int Priority => 0;
    
    public bool HasDialogue => baseDialogue != null && baseDialogue.Length > 0;
    
    public DialogueEntry[] GetDialogueEntries() => baseDialogue;
    
    #endregion
    
    void OnEnable() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(OnEndDialogue);
        if (interactBubble != null)
            interactBubble.SetActive(false);
    }

    void Start() {
        dialogue = UIManager.Instance.returnDialogue();
        if (terryRequired != null)
            terryRequired.SetActive(false);
        
        // Cache all dialogue providers on this GameObject
        RefreshDialogueProviders();
        
        // Migrate legacy fields if baseDialogue is empty
        MigrateLegacyDialogue();
    }

    void OnDisable() {
        EventDispatcher.RemoveListener<Interact>(PlayerInteract);
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

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            // Get the best dialogue from all providers
            DialogueEntry[] entries = GetActiveDialogue();
            
            if (entries != null && entries.Length > 0)
            {
                dialogue.SetDialogueEntries(entries);
                currentFirstEntry = entries[0].defaultText;
            }
            
            interactBubble.SetActive(true);
            inRadius = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player"))
        {
            interactBubble.SetActive(false);
            inRadius = false;
            triggered = false;
            
            if (terryRequired != null)
                terryRequired.SetActive(false);
        }
    }

    void PlayerInteract(Interact e) {
        if (inRadius && !dialogue.IsActive() && interactBubble.activeSelf && !triggered) {
            // Refresh dialogue in case state changed since OnTriggerEnter
            DialogueEntry[] entries = GetActiveDialogue();
            if (entries != null && entries.Length > 0)
            {
                dialogue.SetDialogueEntries(entries);
                currentFirstEntry = entries[0].defaultText;
            }
            
            triggered = true;
            dialogue.Appear();
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
            
            Interact thisInteract = new Interact();
            thisInteract.questGiver = this;
            EventDispatcher.Raise<Interact>(thisInteract);
            
            InteractedObjective?.Invoke(this);
        }

        if (interactBubble.activeSelf) {  
            interactBubble.SetActive(false);
        }
    }

    public void AutoTriggerDialogue()
    {
        if (!dialogue.IsActive() && !triggered)
        {
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
        }

        if (interactBubble.activeSelf)
        {
            interactBubble.SetActive(false);
        }
    }

    void OnEndDialogue(EndDialogue e) {
        if (!interactBubble.activeSelf && inRadius) {
            interactBubble.SetActive(true);
        }
        
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
    }

    private SpriteRenderer _sr = null;
    void Update()
    {
        if (interactBubble == null) return;
        
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse")
        {
            if(!_sr) interactBubble.TryGetComponent<SpriteRenderer>(out _sr);
            if (_sr != null)
            {
                _sr.sprite = keyboardSprite;
                interactBubble.transform.localScale = new Vector3(1, 1, 1);
            }
        }
        else
        {
            if(!_sr) interactBubble.TryGetComponent<SpriteRenderer>(out _sr);
            if (_sr != null)
            {
                _sr.sprite = controllerSprite;
                interactBubble.transform.localScale = new Vector3(0.333f, 0.333f, 1f);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && terryRequired != null)
        {
            terryRequired.SetActive(Player.Instance.GetTransformation() != Transformation.TERRY);
        }
    }
}
