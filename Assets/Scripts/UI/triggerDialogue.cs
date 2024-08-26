using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class triggerDialogue : MonoBehaviour
{
    public string[] dialogueLines;
    public GameObject interactButton;

    public Sprite keyboardSprite;
    public Sprite gamepadSprite;

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player") && IsometricCharacterController.Instance.transformation == Transformation.TERRY) {
            UIManager.Instance.dialogue().setSentences(dialogueLines);
            IsometricCharacterController.Instance.canTalk();
            ShowInteractButton();
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            IsometricCharacterController.Instance.cannotTalk();
            interactButton.SetActive(false);
        }
    }

    void ShowInteractButton() {
        if (IsometricCharacterController.Instance.GetLastInputDevice() == 0) {
            interactButton.GetComponent<SpriteRenderer>().sprite = keyboardSprite;
        }
        else {
            interactButton.GetComponent<SpriteRenderer>().sprite = gamepadSprite;
        }
        interactButton.SetActive(true);
    }
}
