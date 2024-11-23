using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DialogueTrigger : MonoBehaviour
{
    public string[] dialogueLines;
    public GameObject interactBubble;
    private bool inRadius = false;
    private bool triggered = false;
    private Dialogue dialogue;
    
    void OnEnable() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(EndDialogue);
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
            UIManager.Instance.returnDialogue().setSentences(dialogueLines);
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
        if (inRadius && !dialogue.isActive() && interactBubble.activeSelf && !triggered) {
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

    
}
