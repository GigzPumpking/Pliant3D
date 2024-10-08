using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DialogueTrigger : MonoBehaviour
{
    public string[] dialogueLines;
    public GameObject interactBubble;
    private bool inRadius = false;
    private Dialogue dialogue;
    private void Start() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        EventDispatcher.AddListener<EndDialogue>(EndDialogue);
        interactBubble.SetActive(false);
        dialogue = UIManager.Instance.returnDialogue();
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
        }
    }

    void PlayerInteract(Interact e) {
        if (interactBubble.activeSelf) {  
            interactBubble.SetActive(false);
        }

        if (inRadius && !dialogue.isActive()) {
            dialogue.Appear();
        }
    }

    void EndDialogue(EndDialogue e) {
        if (!interactBubble.activeSelf && inRadius) {
            interactBubble.SetActive(true);
        }
    }

    
}
