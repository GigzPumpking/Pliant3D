using UnityEngine;

public class FetchableInteractable : Interactable, IInteractable
{
    private Animator animator;
    private Renderer[] renderers;
    private Color[] originalColors;

    [SerializeField]
    private Color highlightColor = Color.green;

    public bool isFetched = false;

    [Header("Interact Bubble")]
    [Tooltip("The interact bubble GameObject positioned on this object.")]
    [SerializeField] private GameObject interactBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;
    
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 0f;
    
    [Header("Dialogue")]
    [Tooltip("Dialogue entries shown when this item is fetched. Leave empty for no dialogue.")]
    [SerializeField] private DialogueEntry[] fetchDialogue;
    
    // Cached references
    private Dialogue dialogue;
    private SpriteRenderer _bubbleSpriteRenderer;
    private Vector3 _originalBubbleScale;
    private string currentFirstEntry = "";
    private bool waitingForDialogue = false;

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
        if (isFetched) return false;
        if (!isInteractable) return false;
        if (waitingForDialogue) return false;
        if (dialogue != null && dialogue.IsActive()) return false;
        return true;
    }
    
    public void OnInteract()
    {
        if (isFetched || !isInteractable || waitingForDialogue) return;
        Interact();
    }
    
    public void SetInteractBubbleActive(bool active)
    {
        if (interactBubble != null)
        {
            interactBubble.SetActive(active);
        }
    }
    
    #endregion

    void OnEnable()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Register(this);
        }
        
        EventDispatcher.AddListener<EndDialogue>(OnEndDialogue);
        
        if (interactBubble != null)
        {
            _originalBubbleScale = interactBubble.transform.localScale;
            interactBubble.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Unregister(this);
        }
        
        EventDispatcher.RemoveListener<EndDialogue>(OnEndDialogue);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>();

        if (animator == null)
        {
            Debug.LogWarning("Animator component missing on " + gameObject.name);
        }

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No Renderer components found on " + gameObject.name + " or its children.");
        }
        else
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
        
    }

    void Start()
    {
        dialogue = UIManager.Instance.returnDialogue();
        
        // Register with InteractionManager (in case it wasn't ready in OnEnable)
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Register(this);
        }
    }

    public override void Interact()
    {
        if (!isInteractable)
        {
            Debug.Log("Can't interact with " + gameObject.name);
            return;
        }

        isFetched = true;
        SetInteractBubbleActive(false);
        
        // Raise the fetch event immediately so objectives can track it
        EventDispatcher.Raise<FetchObjectInteract>(new FetchObjectInteract() { fetchableObject = this });
        
        // If we have dialogue, show it before hiding the object
        if (dialogue != null && fetchDialogue != null && fetchDialogue.Length > 0)
        {
            waitingForDialogue = true;
            dialogue.SetDialogueEntries(fetchDialogue);
            currentFirstEntry = fetchDialogue[0].defaultText;
            dialogue.Appear();
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });
        }
        else
        {
            // No dialogue — hide the object immediately
            gameObject.SetActive(false);
        }
    }
    
    private void OnEndDialogue(EndDialogue e)
    {
        if (!waitingForDialogue) return;
        if (string.IsNullOrEmpty(currentFirstEntry) || e.someEntry != currentFirstEntry) return;
        
        waitingForDialogue = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateInteractBubbleSprite();
    }
    
    /// <summary>
    /// Updates the interact bubble sprite based on the active input device.
    /// </summary>
    private void UpdateInteractBubbleSprite()
    {
        if (interactBubble == null || !interactBubble.activeSelf) return;
        
        if (_bubbleSpriteRenderer == null)
        {
            interactBubble.TryGetComponent<SpriteRenderer>(out _bubbleSpriteRenderer);
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
            interactBubble.transform.localScale = _originalBubbleScale * 1f;
        }
    }

    protected override void Highlight()
    {
        base.Highlight();

        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = highlightColor;
            }
        }
    }

    protected override void Unhighlight()
    {
        base.Unhighlight();

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }
}
