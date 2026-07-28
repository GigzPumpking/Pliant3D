using UnityEngine;

public class AnimTrigger : MonoBehaviour 
{
    [SerializeField] private Animator myAnimationController;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private string parameterName = "test";

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only fire after the referenced AnimTrigger has been triggered.")]
    [SerializeField] private AnimTrigger requiredAnimTrigger;

    [Tooltip("Dialogue shown when the player enters this trigger but the required AnimTrigger has not yet been triggered. Leave empty to show nothing.")]
    [SerializeField] private DialogueEntry[] blockedDialogue;

    [Tooltip("Portrait sprite shown alongside the blocked dialogue.")]
    [SerializeField] private Sprite blockedDialoguePortrait;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;

    public bool IsTriggered { get; private set; } = false;

    private ColoredInteractable coloredInteractable;

    private void Awake()
    {
        coloredInteractable = GetComponent<ColoredInteractable>();
    }

    private void OnEnable()
    {
        if (coloredInteractable != null)
        {
            Bulldozer.AbilityUsed += OnAbilityUsed;
            Frog.AbilityUsed += OnAbilityUsed;
        }
    }

    private void OnDisable()
    {
        Bulldozer.AbilityUsed -= OnAbilityUsed;
        Frog.AbilityUsed -= OnAbilityUsed;
    }

    private void OnAbilityUsed(Transformation transformation, int abilityIndex, Interactable interactable)
    {
        if (interactable == null || interactable != coloredInteractable) return;
        Trigger();
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (targetTag == null || targetTag == "") return;

        if (!other.CompareTag(targetTag)) return;

        if (!IsActive)
        {
            TryShowBlockedDialogue();
            return;
        }

        myAnimationController.SetBool(parameterName, true);
        IsTriggered = true;
    }

    private void OnTriggerExit(Collider other) 
    {
        if (!IsActive || targetTag == null || targetTag == "") return;
        
        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, false);
        }
    }

    public void Trigger()
    {
        if (!IsActive) return;

        myAnimationController.SetBool(parameterName, true);
        IsTriggered = true;

        if (coloredInteractable != null)
            coloredInteractable.isInteractable = false;
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
