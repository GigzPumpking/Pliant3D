using UnityEngine;

/// <summary>
/// An interactable for objects that are on fire. Only Terry can extinguish them,
/// and only while Terry.HasFireExtinguisher is true. Extinguishing causes the
/// object to disappear (SetActive false).
/// Add a "Burning" InteractableProperty in the Inspector if you need other systems
/// to identify this object by that tag; the extinguish logic here is self-contained.
/// </summary>
public class BurningInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 10f;

    private bool _isExtinguished = false;

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
        if (_isExtinguished) return false;

        // Only Terry with the fire extinguisher can interact
        if (Player.Instance == null) return false;
        if (Player.Instance.transformation != Transformation.TERRY) return false;
        if (!Terry.HasFireExtinguisher) return false;

        return true;
    }

    public void OnInteract()
    {
        if (!IsInteractable()) return;

        _isExtinguished = true;
        SetInteractBubbleActive(false);

        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);

        Debug.Log($"[BurningInteractable] {gameObject.name} was extinguished.");

        gameObject.SetActive(false);
    }

    public void SetInteractBubbleActive(bool active)
    {
        // The bubble lives on Terry, not on this object.
        Terry terry = Player.Instance?.GetComponentInChildren<Terry>();
        terry?.SetBurningPromptActive(active);
    }

    #endregion

    private void OnEnable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Register(this);
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

}
