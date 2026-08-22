using System;
using UnityEngine;

public class AnimTrigger : MonoBehaviour, IInteractable
{
    // Raised when this trigger fires via the Interact Trigger flow (useInteractTrigger). Listened to by InteractObjective.
    public static event Action<AnimTrigger> Interacted;

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

    [Header("Audio (Optional)")]
    [Tooltip("Optional ambient sound that loops for as long as this object is enabled. Leave the clip empty to skip.")]
    [SerializeField] private AudioData ambientSound;
    [Tooltip("Optional one-shot sound played the moment this trigger fires. Leave the clip empty to skip.")]
    [SerializeField] private AudioData triggerSound;
    [Tooltip("If true, the ambient sfx stops as soon as this trigger fires.")]
    [SerializeField] private bool disableAmbientOnTrigger = false;

    private AudioSource _ambientAudioSource;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;

    public bool IsTriggered { get; private set; } = false;

    private ColoredInteractable coloredInteractable;

    // Whether the player is currently within this trigger's collider (interact mode only)
    private bool _playerInRange = false;
    private SpriteRenderer _bubbleSpriteRenderer;

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
            interactBubble.SetActive(false);
        }

        EventDispatcher.AddListener<EndDialogue>(OnBlockedDialogueEnd);

        StartAmbientAudio();
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

        StopAmbientAudio();
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
        PlayTriggerSound();
        if (disableAmbientOnTrigger) StopAmbientAudio();
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
        PlayTriggerSound();
        if (disableAmbientOnTrigger) StopAmbientAudio();

        if (coloredInteractable != null)
            coloredInteractable.isInteractable = false;

        if (useInteractTrigger) Interacted?.Invoke(this);
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

    #region Audio

    private void StartAmbientAudio()
    {
        if (ambientSound == null || ambientSound.clip == null) return;
        if (!ambientSound.loop) ambientSound.loop = true;
        _ambientAudioSource = AudioManager.Instance?.PlaySound(ambientSound, transform);
    }

    private void StopAmbientAudio()
    {
        if (_ambientAudioSource == null) return;
        AudioManager.Instance?.StopSound(ambientSound);
        _ambientAudioSource = null;
    }

    private void PlayTriggerSound()
    {
        if (triggerSound == null || triggerSound.clip == null) return;
        AudioManager.Instance?.PlayOneShot(triggerSound, transform);
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
            _bubbleSpriteRenderer.sprite = InteractBubbleIcons.Keyboard;
        }
        else
        {
            _bubbleSpriteRenderer.sprite = InteractBubbleIcons.Controller;
        }
    }
}
