using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An interactable for objects that are on fire. Only Terry can extinguish them,
/// and only while Terry.HasFireExtinguisher is true. The player must press and hold
/// the interact button to fill a progress bar. The extinguish animation plays
/// progressively as the button is held. Once the hold threshold (75% of the
/// animation) is reached, the player is released and the remaining animation
/// plays automatically before the object is hidden.
/// </summary>
public class BurningInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from which the player can interact. Set to 0 to use the global default.")]
    [SerializeField] private float interactionDistance = 10f;

    [Header("Extinguish Animation")]
    [Tooltip("Child GameObject with an Animator that has an 'Extinguish' trigger. Shown in place of the fire while the animation plays.")]
    [SerializeField] private GameObject extinguishAnimObject;

    [Header("Hold Minigame Settings")]
    [Tooltip("Total duration of the extinguish animation in seconds.")]
    [SerializeField] private float extinguishDuration = 9f;
    [Tooltip("Fraction of the animation the player must hold through before it auto-completes (0–1). Default 0.75 = first 75%.")]
    [SerializeField] [Range(0f, 1f)] private float holdCompletionThreshold = 0.75f;

    private bool _isExtinguished = false;
    private bool _isMinigameActive = false;
    private float _currentProgress = 0f;
    private Animator _animator;
    private bool _animatorTriggered = false;

    private Terry GetTerry()
    {
        return Player.Instance != null ? Player.Instance.GetComponentInChildren<Terry>(true) : null;
    }

    private Slider GetProgressSlider() => GetTerry()?.ExtinguishProgressSlider;

    private CanvasGroup GetProgressCanvasGroup() => GetTerry()?.ExtinguishProgressCanvasGroup;

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
        if (_isMinigameActive) return;

        StartCoroutine(ExtinguishHoldCoroutine());
    }

    private IEnumerator ExtinguishHoldCoroutine()
    {
        _isMinigameActive = true;

        float holdTarget = extinguishDuration * holdCompletionThreshold;

        // Disable player movement
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });

        // Show UI progress bar (only covers the hold portion — slider max = holdTarget)
        Slider slider = GetProgressSlider();
        CanvasGroup cg = GetProgressCanvasGroup();
        if (slider != null)
        {
            slider.maxValue = holdTarget;
            slider.value = _currentProgress;
        }
        if (cg != null)
            cg.alpha = 1f;

        // Hide bubble prompt
        SetInteractBubbleActive(false);

        // Activate animator and fire trigger once
        if (extinguishAnimObject != null)
        {
            extinguishAnimObject.SetActive(true);
            if (_animator == null)
                _animator = extinguishAnimObject.GetComponent<Animator>();

            if (_animator != null && !_animatorTriggered)
            {
                _animator.SetTrigger("Extinguish");
                _animatorTriggered = true;
                _animator.speed = 0f; // Start paused
            }
        }

        // === PHASE 1: player must hold the button up to holdTarget ===
        while (_isMinigameActive && _currentProgress < holdTarget)
        {
            if (!IsInteractable())
                break;

            if (Player.Instance != null)
            {
                float distance = Vector3.Distance(Player.Instance.transform.position, transform.position);
                if (distance > GetInteractionDistance() + 1.5f)
                    break;
            }

            bool isHolding = InputManager.Instance != null && InputManager.Instance.IsActionPressed("Interact");

            if (isHolding)
            {
                _currentProgress = Mathf.Min(_currentProgress + Time.deltaTime, holdTarget);
                if (_animator != null) _animator.speed = 1f;
            }
            else
            {
                if (_animator != null) _animator.speed = 0f;
                break; // Released — pause and let the player resume later
            }

            if (slider != null)
                slider.value = _currentProgress;

            yield return null;
        }

        bool completedHold = _currentProgress >= holdTarget;

        _isMinigameActive = false;

        // Re-enable player movement regardless of outcome
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });

        if (!completedHold)
        {
            // Player released early — hide bar, pause animator, restore prompt
            if (cg != null) cg.alpha = 0f;
            if (_animator != null) _animator.speed = 0f;
            SetInteractBubbleActive(true);
            yield break;
        }

        // === PHASE 2: hold complete — hide bar, let animation finish on its own ===
        if (cg != null) cg.alpha = 0f;
        if (slider != null) slider.value = 0f;

        if (_animator != null) _animator.speed = 1f;

        // Wait for the remaining animation time (the last 25%)
        float remainingTime = extinguishDuration - holdTarget;
        yield return new WaitForSeconds(remainingTime);

        _isExtinguished = true;

        if (InteractionManager.Instance != null)
            InteractionManager.Instance.Unregister(this);

        Debug.Log($"[BurningInteractable] {gameObject.name} was fully extinguished.");
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
        if (_isMinigameActive)
        {
            _isMinigameActive = false;
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
            
            CanvasGroup cg = GetProgressCanvasGroup();
            if (cg != null) cg.alpha = 0f;
        }

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
