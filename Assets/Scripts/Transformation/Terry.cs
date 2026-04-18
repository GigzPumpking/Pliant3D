using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private SpriteRenderer _bubbleSpriteRenderer;
    private Vector3 _originalBubbleScale;

    /// <summary>Called by BurningInteractable via IInteractable.SetInteractBubbleActive.</summary>
    public void SetBurningPromptActive(bool active)
    {
        if (burningInteractBubble == null) return;
        burningInteractBubble.SetActive(active);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);

        if (burningInteractBubble != null)
        {
            _originalBubbleScale = burningInteractBubble.transform.localScale;
            burningInteractBubble.SetActive(false);
        }
    }

    public void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        HasFireExtinguisher = false;
        SetBurningPromptActive(false);
    }

    private void Update()
    {
        UpdateBubbleSprite();
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

    public override void Ability1(InputAction.CallbackContext context)
    {
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
    }
}
