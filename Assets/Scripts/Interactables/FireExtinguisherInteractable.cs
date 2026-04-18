using UnityEngine;

/// <summary>
/// A pickup interactable for the fire extinguisher. Only Terry can pick it up.
/// When interacted with, it disappears and sets Terry.HasFireExtinguisher = true,
/// allowing Terry to extinguish BurningInteractable objects.
/// </summary>
public class FireExtinguisherInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 0f;

    [Header("Interact Bubble")]
    [Tooltip("The interact bubble GameObject positioned on this object.")]
    [SerializeField] private GameObject interactBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;

    private SpriteRenderer _bubbleSpriteRenderer;
    private Vector3 _originalBubbleScale;

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
        return true;
    }

    public void OnInteract()
    {
        if (!IsInteractable()) return;

        Terry.HasFireExtinguisher = true;

        SetInteractBubbleActive(false);

        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);

        gameObject.SetActive(false);

        Debug.Log("[FireExtinguisher] Picked up. Terry now has the fire extinguisher.");
    }

    public void SetInteractBubbleActive(bool active)
    {
        if (interactBubble != null)
            interactBubble.SetActive(active);
    }

    #endregion

    private void OnEnable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Register(this);

        if (interactBubble != null)
        {
            _originalBubbleScale = interactBubble.transform.localScale;
            interactBubble.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);
    }

    private void Start()
    {
        // Re-register in case InteractionManager wasn't ready during OnEnable
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Register(this);
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
