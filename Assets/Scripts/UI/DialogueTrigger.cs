using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DialogueTrigger : MonoBehaviour
{
    public string[] dialogueLines;
    public GameObject interactBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;
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

    void Update() {
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse") {
            interactBubble.GetComponent<SpriteRenderer>().sprite = keyboardSprite;
            // reset scale of interactBubble
            interactBubble.transform.localScale = new Vector3(1, 1, 1);
        } else {
            interactBubble.GetComponent<SpriteRenderer>().sprite = controllerSprite;
            // scale interactBubble to 0.333
            interactBubble.transform.localScale = new Vector3(0.333f, 0.333f, 1f);
        }
    }

    
}
