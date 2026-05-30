using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Terry : FormScript
{
    protected override float baseSpeed { get; set; } = 6.0f;

    /// <summary>
    /// Whether Terry is currently carrying the fire extinguisher.
    /// Persists across scene loads (static). Reset this when needed by level logic.
    /// </summary>
    public static bool HasFireExtinguisher { get; set; } = false;

    [Header("Burning Interact Bubble")]
    [Tooltip("The interact bubble shown on Terry when a Burning object is in range.")]
    [SerializeField] private GameObject burningInteractBubble;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;

    [Header("Extinguish Progress Bar")]
    [Tooltip("Slider displayed beneath Terry to show hold-to-extinguish progress. Assign the slider that is a child of Terry's world-space canvas.")]
    [SerializeField] private Slider extinguishProgressSlider;
    [Tooltip("(Optional) Canvas Group on the slider's parent for alpha fade in/out.")]
    [SerializeField] private CanvasGroup extinguishProgressCanvasGroup;

    public Slider ExtinguishProgressSlider => extinguishProgressSlider;
    public CanvasGroup ExtinguishProgressCanvasGroup => extinguishProgressCanvasGroup;

    [Header("Hold to Extinguish Bubble")]
    [Tooltip("Shown during the extinguish minigame prompting the player to hold the interact button.")]
    [SerializeField] private GameObject holdExtinguishBubble;
    [SerializeField] private Sprite holdKeyboardSprite;
    [SerializeField] private Sprite holdControllerSprite;

    private SpriteRenderer _holdBubbleSpriteRenderer;
    private Vector3 _originalHoldBubbleScale;
    private bool _holdBubbleScaleInitialized = false;

    private SpriteRenderer _bubbleSpriteRenderer;
    private Vector3 _originalBubbleScale;
    private bool _bubbleScaleInitialized = false;

    /// <summary>Called by BurningInteractable via IInteractable.SetInteractBubbleActive.</summary>
    public void SetBurningPromptActive(bool active)
    {
        if (burningInteractBubble == null) return;
        burningInteractBubble.SetActive(active);
    }

    /// <summary>Called by BurningInteractable during the hold minigame to prompt the player to hold the interact button.</summary>
    public void SetHoldExtinguishPromptActive(bool active)
    {
        if (holdExtinguishBubble == null) return;
        holdExtinguishBubble.SetActive(active);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);

        if (burningInteractBubble != null)
        {
            if (!_bubbleScaleInitialized)
            {
                _originalBubbleScale = burningInteractBubble.transform.localScale;
                _bubbleScaleInitialized = true;
            }
            else
            {
                // Reset scale in case it was left at a modified value before this OnEnable
                burningInteractBubble.transform.localScale = _originalBubbleScale;
            }
            burningInteractBubble.SetActive(false);
        }

        if (holdExtinguishBubble != null)
        {
            holdExtinguishBubble.SetActive(false);
        }

        if (extinguishProgressCanvasGroup != null)
            extinguishProgressCanvasGroup.alpha = 0f;
        if (extinguishProgressSlider != null)
            extinguishProgressSlider.value = 0f;
    }

    public void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        HasFireExtinguisher = false;
        SetBurningPromptActive(false);
        SetHoldExtinguishPromptActive(false);
        if (extinguishProgressCanvasGroup != null)
            extinguishProgressCanvasGroup.alpha = 0f;
        if (extinguishProgressSlider != null)
            extinguishProgressSlider.value = 0f;
    }

    private void Update()
    {
        UpdateBubbleSprite();
        UpdateHoldBubbleSprite();
    }

    private void UpdateBubbleSprite()
    {
        if (burningInteractBubble == null || !burningInteractBubble.activeSelf) return;

        if (_bubbleSpriteRenderer == null)
            burningInteractBubble.TryGetComponent(out _bubbleSpriteRenderer);

        if (_bubbleSpriteRenderer == null) return;

        bool isKeyboard = InputManager.Instance?.ActiveDeviceType == "Keyboard"
                       || InputManager.Instance?.ActiveDeviceType == "Mouse";

        if (isKeyboard)
        {
            _bubbleSpriteRenderer.sprite = keyboardSprite;
            burningInteractBubble.transform.localScale = _originalBubbleScale * 3f;
        }
        else
        {
            _bubbleSpriteRenderer.sprite = controllerSprite;
            burningInteractBubble.transform.localScale = _originalBubbleScale;
        }
    }

    private void UpdateHoldBubbleSprite()
    {
        if (holdExtinguishBubble == null || !holdExtinguishBubble.activeSelf) return;

        if (_holdBubbleSpriteRenderer == null)
            _holdBubbleSpriteRenderer = holdExtinguishBubble.GetComponentInChildren<SpriteRenderer>();

        if (_holdBubbleSpriteRenderer == null) return;

        if (!_holdBubbleScaleInitialized)
        {
            _originalHoldBubbleScale = _holdBubbleSpriteRenderer.transform.localScale;
            _holdBubbleScaleInitialized = true;
        }

        bool isKeyboard = InputManager.Instance?.ActiveDeviceType == "Keyboard"
                       || InputManager.Instance?.ActiveDeviceType == "Mouse";

        if (isKeyboard)
        {
            _holdBubbleSpriteRenderer.sprite = holdKeyboardSprite;
            _holdBubbleSpriteRenderer.transform.localScale = _originalHoldBubbleScale * 3f;
        }
        else
        {
            _holdBubbleSpriteRenderer.sprite = holdControllerSprite;
            _holdBubbleSpriteRenderer.transform.localScale = _originalHoldBubbleScale;
        }
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
    }
}
