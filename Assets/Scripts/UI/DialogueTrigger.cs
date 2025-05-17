using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueTrigger : MonoBehaviour
{
    // Existing field maintained for backwards compatibility.
    public string[] dialogueLines;
    public string[] keyboardDialogueLines;
    public string[] controllerDialogueLines;
    public GameObject interactBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;
    private bool inRadius = false;
    public bool triggered { get; set; } = false;
    private Dialogue dialogue;
    
    public static event Action<DialogueTrigger> InteractedObjective;
    
    void OnEnable() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(EndDialogue);
        if (interactBubble != null)
            interactBubble.SetActive(false);
    }

    void Start() {
        dialogue = UIManager.Instance.returnDialogue();
    }

    void OnDisable() {
        EventDispatcher.RemoveListener<Interact>(PlayerInteract);
        EventDispatcher.RemoveListener<EndDialogue>(EndDialogue);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            // Determine the maximum length among all three arrays.
            int maxLength = 0;
            if (dialogueLines != null) {
                maxLength = Mathf.Max(maxLength, dialogueLines.Length);
            }
            if (keyboardDialogueLines != null) {
                maxLength = Mathf.Max(maxLength, keyboardDialogueLines.Length);
            }
            if (controllerDialogueLines != null) {
                maxLength = Mathf.Max(maxLength, controllerDialogueLines.Length);
            }

            if (maxLength > 0)
            {
                DialogueEntry[] entries = new DialogueEntry[maxLength];
                for (int i = 0; i < maxLength; i++)
                {
                    entries[i] = new DialogueEntry();
                    // Use dialogueLines if available, otherwise fallback to empty.
                    entries[i].defaultText = (dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "";
                    // For keyboard text, use keyboardDialogueLines if available; if not, fall back to dialogueLines.
                    entries[i].keyboardText = (keyboardDialogueLines != null && keyboardDialogueLines.Length > i) 
                        ? keyboardDialogueLines[i] 
                        : ((dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "");
                    // For controller text, use controllerDialogueLines if available; if not, fall back to dialogueLines.
                    entries[i].controllerText = (controllerDialogueLines != null && controllerDialogueLines.Length > i) 
                        ? controllerDialogueLines[i] 
                        : ((dialogueLines != null && dialogueLines.Length > i) ? dialogueLines[i] : "");
                }
                dialogue.SetDialogueEntries(entries);
            }
            interactBubble.SetActive(true);
            inRadius = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            interactBubble.SetActive(false);
            inRadius = false;
            triggered = false;
        }
    }

    void PlayerInteract(Interact e) {
        if (inRadius && !dialogue.IsActive() && interactBubble.activeSelf && !triggered) {
            triggered = true;
            dialogue.Appear();
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
            
            //raise an interact here listened to by 'NPCInteractObjective.cs'
            InteractedObjective?.Invoke(this);
        }

        if (interactBubble.activeSelf) {  
            interactBubble.SetActive(false);
        }
    }

    void EndDialogue(EndDialogue e) {
        if (!interactBubble.activeSelf && inRadius) {
            interactBubble.SetActive(true);
        }
    }

    void Update() {
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse") {
            SpriteRenderer sr = interactBubble.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sprite = keyboardSprite;
                interactBubble.transform.localScale = new Vector3(1, 1, 1);
            }
        } else {
            SpriteRenderer sr = interactBubble.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sprite = controllerSprite;
                interactBubble.transform.localScale = new Vector3(0.333f, 0.333f, 1f);
            }
        }
    }
}
