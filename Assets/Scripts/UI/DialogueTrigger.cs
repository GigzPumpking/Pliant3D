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
    private void Start() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(EndDialogue);
        interactBubble.SetActive(false);
        dialogue = UIManager.Instance.returnDialogue();
    }

    private void OnDestroy() {
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
