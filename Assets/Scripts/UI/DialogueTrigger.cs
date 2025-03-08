using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private bool triggered = false;
    private Dialogue dialogue;
    
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
            // If dialogueLines has been set, convert it into DialogueEntry objects.
            if (dialogueLines != null && dialogueLines.Length > 0)
            {
                DialogueEntry[] entries = new DialogueEntry[dialogueLines.Length];
                for (int i = 0; i < dialogueLines.Length; i++)
                {
                    entries[i] = new DialogueEntry();
                    // Set default text, and optionally copy into device-specific fields.
                    entries[i].defaultText = dialogueLines[i];
                    entries[i].keyboardText = keyboardDialogueLines != null && keyboardDialogueLines.Length > i ? keyboardDialogueLines[i] : dialogueLines[i];
                    entries[i].controllerText = controllerDialogueLines != null && controllerDialogueLines.Length > i ? controllerDialogueLines[i] : dialogueLines[i];
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
