using UnityEngine;

public class AnimTrigger : MonoBehaviour, IInteractable
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

    [Header("Interact Trigger")]
    [Tooltip("If true, this trigger will not fire automatically on trigger enter. Instead, the player must press Interact while inside the trigger volume.")]
    [SerializeField] private bool useInteractTrigger = false;

    [Tooltip("Interact bubble shown while the player is inside the trigger volume and can interact.")]
    [SerializeField] private GameObject interactBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;

    public bool IsTriggered { get; private set; } = false;

    private ColoredInteractable coloredInteractable;

    // Whether the player is currently within this trigger's collider (interact mode only)
    private bool _playerInRange = false;
    private SpriteRenderer _bubbleSpriteRenderer;
    private Vector3 _originalBubbleScale;

    // Guards against Dialogue.Appear() being called again while the blocked dialogue is still opening/active
    private bool _blockedDialogueActive = false;
    private string _currentBlockedFirstEntry = "";
    private static float lastBlockedDialogueEndTime = float.NegativeInfinity;
    private const float blockedDialogueCooldown = 1f;

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

        if (useInteractTrigger)
        {
            InteractionManager.Instance?.Register(this);
        }

        if (interactBubble != null)
        {
            _originalBubbleScale = interactBubble.transform.localScale;
            interactBubble.SetActive(false);
        }

        EventDispatcher.AddListener<EndDialogue>(OnBlockedDialogueEnd);
    }

    private void Start()
    {
        // Register with InteractionManager (in case it wasn't ready in OnEnable)
        if (useInteractTrigger)
        {
            InteractionManager.Instance?.Register(this);
        }
    }

    private void OnDisable()
    {
        Bulldozer.AbilityUsed -= OnAbilityUsed;
        Frog.AbilityUsed -= OnAbilityUsed;

        if (useInteractTrigger)
        {
            InteractionManager.Instance?.Unregister(this);
        }

        EventDispatcher.RemoveListener<EndDialogue>(OnBlockedDialogueEnd);
    }

    private void Update()
    {
        if (useInteractTrigger)
        {
            UpdateInteractBubbleSprite();
        }
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

        if (useInteractTrigger)
        {
            _playerInRange = true;
            InteractionManager.Instance?.ForceUpdate();
            return;
        }

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
        if (targetTag == null || targetTag == "") return;

        if (!other.CompareTag(targetTag)) return;

        if (useInteractTrigger)
        {
            _playerInRange = false;
            SetInteractBubbleActive(false);
            InteractionManager.Instance?.ForceUpdate();
            return;
        }

        if (!IsActive) return;

        myAnimationController.SetBool(parameterName, false);
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
        if (_blockedDialogueActive) return;
        if (Time.time - lastBlockedDialogueEndTime < blockedDialogueCooldown) return;

        Dialogue dialogue = UIManager.Instance.returnDialogue();
        if (dialogue == null || dialogue.IsActive()) return;

        // Set before Appear() since Dialogue takes a moment to actually become Active, so this closes the re-entrancy window.
        _blockedDialogueActive = true;
        _currentBlockedFirstEntry = blockedDialogue[0].defaultText;

        dialogue.SetDialogueEntries(blockedDialogue);
        dialogue.Appear();
        dialogue.SetPortrait(blockedDialoguePortrait);
    }

    private void OnBlockedDialogueEnd(EndDialogue e)
    {
        if (!_blockedDialogueActive) return;
        if (string.IsNullOrEmpty(_currentBlockedFirstEntry) || e.someEntry != _currentBlockedFirstEntry) return;

        _blockedDialogueActive = false;
        lastBlockedDialogueEndTime = Time.time;
        _currentBlockedFirstEntry = "";
    }

    #region IInteractable Implementation

    public Vector3 GetPosition() => transform.position;

    // Proximity is gated by the trigger collider itself (_playerInRange), not distance.
    public float GetInteractionDistance() => float.MaxValue;

    public bool IsInteractable()
    {
        if (!useInteractTrigger) return false;
        if (!_playerInRange) return false;
        if (IsTriggered) return false;

        return true;
    }

    public void OnInteract()
    {
        if (!IsInteractable()) return;

        if (!IsActive)
        {
            TryShowBlockedDialogue();
            return;
        }

        Trigger();
        SetInteractBubbleActive(false);
        TriggerSiblingInteractTriggers();
    }

    public void SetInteractBubbleActive(bool active)
    {
        if (interactBubble != null)
        {
            interactBubble.SetActive(active);
        }
    }

    // Fires every other interact-mode AnimTrigger on this GameObject so a single Interact activates them all together.
    private void TriggerSiblingInteractTriggers()
    {
        foreach (AnimTrigger sibling in GetComponents<AnimTrigger>())
        {
            if (sibling == this || !sibling.useInteractTrigger) continue;

            sibling.Trigger();
            sibling.SetInteractBubbleActive(false);
        }
    }

    #endregion

    private void UpdateInteractBubbleSprite()
    {
        if (interactBubble == null || !interactBubble.activeSelf) return;

        if (_bubbleSpriteRenderer == null)
        {
            interactBubble.TryGetComponent(out _bubbleSpriteRenderer);
        }

        if (_bubbleSpriteRenderer == null) return;

        bool isKeyboard = InputManager.Instance?.ActiveDeviceType == "Keyboard"
                       || InputManager.Instance?.ActiveDeviceType == "Mouse";

        if (isKeyboard)
        {
            _bubbleSpriteRenderer.sprite = keyboardSprite;
            interactBubble.transform.localScale = _originalBubbleScale * 3f;
        }
        else
        {
            _bubbleSpriteRenderer.sprite = controllerSprite;
            interactBubble.transform.localScale = _originalBubbleScale;
        }
    }
}
