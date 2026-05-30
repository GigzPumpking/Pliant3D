using UnityEngine;

public class AnimTrigger : MonoBehaviour 
{
    [SerializeField] private Animator myAnimationController;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private string parameterName = "test";

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only fire after the referenced ButtonScript has been pressed.")]
    [SerializeField] private ButtonScript requiredButton;

    [Tooltip("Dialogue shown when the player enters this trigger but the required button has not yet been pressed. Leave empty to show nothing.")]
    [SerializeField] private DialogueEntry[] blockedDialogue;

    [Tooltip("Portrait sprite shown alongside the blocked dialogue.")]
    [SerializeField] private Sprite blockedDialoguePortrait;

    private bool IsActive => requiredButton == null || requiredButton.HasBeenTriggered;

    private void OnTriggerEnter(Collider other) 
    {
        if (!other.CompareTag(targetTag)) return;

        if (!IsActive)
        {
            TryShowBlockedDialogue();
            return;
        }

        myAnimationController.SetBool(parameterName, true);
    }

    private void OnTriggerExit(Collider other) 
    {
        if (!IsActive) return;

        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, false);
        }
    }

    public void Trigger()
    {
        if (!IsActive) return;

        myAnimationController.SetBool(parameterName, true);
    }

    private void TryShowBlockedDialogue()
    {
        if (blockedDialogue == null || blockedDialogue.Length == 0) return;
        if (UIManager.Instance == null) return;

        Dialogue dialogue = UIManager.Instance.returnDialogue();
        if (dialogue == null || dialogue.IsActive()) return;

        dialogue.SetDialogueEntries(blockedDialogue);
        dialogue.Appear();
        dialogue.SetPortrait(blockedDialoguePortrait);
    }
}
