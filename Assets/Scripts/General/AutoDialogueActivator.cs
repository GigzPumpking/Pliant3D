using UnityEngine;

public class AutoDialogueActivator : MonoBehaviour
{
    [Tooltip("Reference to the DialogueTrigger to activate.")]
    public DialogueTrigger dialogueTrigger;
    private bool hasTriggered = false;
    private IsometricCamera isoCam;
    private Transform originalTarget;

    private void Awake()
    {
        isoCam = FindObjectOfType<IsometricCamera>();
    }

    void OnEnable()
    {
        EventDispatcher.AddListener<EndDialogue>(OnDialogueEnd);
    }

    void OnDisable()
    {
        EventDispatcher.RemoveListener<EndDialogue>(OnDialogueEnd);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player") && dialogueTrigger != null)
        {
            dialogueTrigger.triggered = false; // Ensure not already triggered
            dialogueTrigger.AutoTriggerDialogue();
            hasTriggered = true;

            // Pan camera to DialogueTrigger
            if (isoCam != null)
            {
                originalTarget = isoCam.followTarget;
                isoCam.SetFollowTarget(dialogueTrigger.transform);
            }
        }
    }

    void OnDialogueEnd(EndDialogue e)
    {
        if (isoCam != null && originalTarget != null)
        {
            isoCam.SetFollowTarget(originalTarget);
        }
    }
}