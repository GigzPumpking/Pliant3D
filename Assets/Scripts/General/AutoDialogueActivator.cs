using UnityEngine;

public class AutoDialogueActivator : MonoBehaviour
{
    [Tooltip("Reference to the DialogueTrigger to activate.")]
    public DialogueTrigger dialogueTrigger;
    private bool hasTriggered = false;
    public bool HasTriggered => hasTriggered;

    // Full hierarchy path — unique even when multiple activators share the same name.
    public string ScenePath
    {
        get
        {
            var path = gameObject.name;
            var parent = transform.parent;
            while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
            return path;
        }
    }

    private IsometricCamera isoCam;
    private Transform originalTarget;

    private void Awake()
    {
        isoCam = FindObjectOfType<IsometricCamera>();
    }

    private void Start()
    {
        var pending = GameManager.Instance?.GetPendingAutoDialogueStates();
        if (pending != null && pending.Contains(ScenePath))
            hasTriggered = true;
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