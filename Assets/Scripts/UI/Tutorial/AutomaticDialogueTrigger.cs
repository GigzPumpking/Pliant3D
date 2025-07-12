using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

public class AutomaticDialogueTrigger : MonoBehaviour
{
    public string[] dialogueLines;
    public string[] keyboardDialogueLines;
    public string[] controllerDialogueLines;
    private bool triggered { get; set; } = false;
    private bool alreadyTriggered = false;
    [SerializeField] private bool retriggerOnContact = false;
    private Dialogue _dialogue;

    public static event Action<AutomaticDialogueTrigger> InteractedObjective;
    public List<UnityEvent> events = new List<UnityEvent>();

    void OnEnable()
    {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(EndDialogue);
    }

    void Start()
    {
        _dialogue = UIManager.Instance.returnDialogue();
    }

    void OnDisable()
    {
        EventDispatcher.RemoveListener<Interact>(PlayerInteract);
        EventDispatcher.RemoveListener<EndDialogue>(EndDialogue);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        SetDialogueDependencies();
        AutomaticallyTriggerDialogue();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        _collidedFresh = true;
        triggered = false;
    }

    void AutomaticallyTriggerDialogue()
    {
        if (_dialogue.IsActive() || triggered || (alreadyTriggered && !retriggerOnContact)) return;
        ForceTerryForm();
        triggered = true;
        alreadyTriggered = true;
        _dialogue.Appear();
        
        InteractedObjective?.Invoke(this); //raise an interact here listened to by 'NPCInteractObjective.cs'
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
    }

    private bool _collidedFresh = true;
    void ForceTerryForm()
    {
        if (!_collidedFresh) return;
        Player.Instance?.transformationWheelScript.AddProgress(Player.Instance.GetTransformation());
        Player.Instance?.SetTransformation(Transformation.TERRY);
        _collidedFresh = false;
    }
    
    void SetDialogueDependencies()
    {
        // Determine the maximum length among all three arrays.
        int maxLength = 0;
        if (dialogueLines != null)
        {
            maxLength = Mathf.Max(maxLength, dialogueLines.Length);
        }

        if (keyboardDialogueLines != null)
        {
            maxLength = Mathf.Max(maxLength, keyboardDialogueLines.Length);
        }

        if (controllerDialogueLines != null)
        {
            maxLength = Mathf.Max(maxLength, controllerDialogueLines.Length);
        }

        if (maxLength > 0)
        {
            DialogueEntry[] entries = new DialogueEntry[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                entries[i] = new DialogueEntry();
                // Use dialogueLines if available, otherwise fallback to empty.
                entries[i].defaultText =
                    (dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "";
                // For keyboard text, use keyboardDialogueLines if available; if not, fall back to dialogueLines.
                entries[i].keyboardText = (keyboardDialogueLines != null && keyboardDialogueLines.Length > i)
                    ? keyboardDialogueLines[i]
                    : ((dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "");
                // For controller text, use controllerDialogueLines if available; if not, fall back to dialogueLines.
                entries[i].controllerText = (controllerDialogueLines != null && controllerDialogueLines.Length > i)
                    ? controllerDialogueLines[i]
                    : ((dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "");
            }

            _dialogue.SetDialogueEntries(entries);
        }
    }

    void PlayerInteract(Interact e)
    {
        if (_dialogue.IsActive() || triggered || (alreadyTriggered && !retriggerOnContact)) return;
        
        triggered = true;
        alreadyTriggered = true;
        _dialogue.Appear();
        
        InteractedObjective?.Invoke(this); //raise an interact here listened to by 'NPCInteractObjective.cs'
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });

    }

    void EndDialogue(EndDialogue e)
    {
        //INVOKE THE LIST OF EVENTS RELATED TO THIS DIALOGUE TRIGGER AFTER THE DIALOGUE IS COMPLETE
        if (e.someEntry != dialogueLines[0]) return;
        foreach (var evt in events)
        {
            evt.Invoke();
        }
    }
}
