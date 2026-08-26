using UnityEngine;

/// <summary>
/// A pickup interactable for the fire extinguisher. Only Terry can pick it up.
/// When interacted with, it disappears and sets Terry.HasFireExtinguisher = true,
/// allowing Terry to extinguish BurningInteractable objects.
/// </summary>
public class FireExtinguisherInteractable : MonoBehaviour, IInteractable, IFetchable
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 0f;

    [Tooltip("Optional. If assigned, the fire extinguisher can only be picked up after this AnimTrigger has been triggered.")]
    [SerializeField] private AnimTrigger requiredAnimTrigger;

    [Header("Interact Bubble")]
    [Tooltip("The interact bubble GameObject positioned on this object.")]
    [SerializeField] private GameObject interactBubble;
    [SerializeField] private AudioData interactBubbleSound;
    [SerializeField] private AudioData pickUpSound;

    [Header("Dialogue")]
    [Tooltip("Dialogue entries shown when the fire extinguisher is picked up. Leave empty for no dialogue.")]
    [SerializeField] private DialogueEntry[] fetchDialogue;

    private SpriteRenderer _bubbleSpriteRenderer;
    public bool isFetched { get; private set; }

    // Dialogue state
    private Dialogue _dialogue;
    private string _currentFirstEntry = "";
    private bool _waitingForDialogue = false;

    #region IInteractable Implementation

    public Vector3 GetPosition() => transform.position;

    public float GetInteractionDistance()
    {
        if (interactionDistance > 0f)
            return interactionDistance;
        return InteractionManager.Instance?.GetDefaultInteractionDistance() ?? 3f;
    }

    public bool IsInteractable()
    {
        // Only interactable in Terry form, and only if not already picked up
        if (Player.Instance == null) return false;
        if (Player.Instance.transformation != Transformation.TERRY) return false;
        if (Terry.HasFireExtinguisher) return false;
        if (requiredAnimTrigger != null && !requiredAnimTrigger.IsTriggered) return false;
        if (_waitingForDialogue) return false;
        if (_dialogue != null && _dialogue.IsActive()) return false;
        return true;
    }

    public void OnInteract()
    {
        if (!IsInteractable()) return;

        isFetched = true;
        Terry.HasFireExtinguisher = true;

        AudioManager.Instance?.PlayOneShot(pickUpSound);

        SetInteractBubbleActive(false);

        EventDispatcher.Raise<FetchObjectInteract>(new FetchObjectInteract() { fetchableObject = this });

        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);

        if (_dialogue != null && fetchDialogue != null && fetchDialogue.Length > 0)
        {
            _waitingForDialogue = true;
            _dialogue.SetDialogueEntries(fetchDialogue);
            _currentFirstEntry = fetchDialogue[0].defaultText;
            _dialogue.Appear();
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log("[FireExtinguisher] Picked up. Terry now has the fire extinguisher.");
    }

    private void OnEndDialogue(EndDialogue e)
    {
        if (!_waitingForDialogue) return;
        if (string.IsNullOrEmpty(_currentFirstEntry) || e.someEntry != _currentFirstEntry) return;

        _waitingForDialogue = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Marks this fire extinguisher as fetched and hides it without raising events or showing dialogue.
    /// Used when restoring saved / game-over state.
    /// </summary>
    public void SetFetchedSilently()
    {
        isFetched = true;
        Terry.HasFireExtinguisher = true;
        SetInteractBubbleActive(false);
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);
        gameObject.SetActive(false);
    }

    public void SetInteractBubbleActive(bool active)
    {
        if (interactBubble != null)
        {
            interactBubble.SetActive(active);
            AudioManager.Instance?.PlayOneShot(interactBubbleSound);
        }

    }

    #endregion

    private void OnEnable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Register(this);

        EventDispatcher.AddListener<EndDialogue>(OnEndDialogue);

        if (interactBubble != null)
        {
            interactBubble.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);

        EventDispatcher.RemoveListener<EndDialogue>(OnEndDialogue);
    }

    private void Start()
    {
        // Re-register in case InteractionManager wasn't ready during OnEnable
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Register(this);

        _dialogue = UIManager.Instance.returnDialogue();
    }

    private void Update()
    {
        UpdateInteractBubbleSprite();
    }

    private void UpdateInteractBubbleSprite()
    {
        if (interactBubble == null || !interactBubble.activeSelf) return;

        if (_bubbleSpriteRenderer == null)
            interactBubble.TryGetComponent(out _bubbleSpriteRenderer);

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
