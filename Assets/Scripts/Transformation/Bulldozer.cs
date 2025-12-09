using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; 

public class Bulldozer : FormScript
{
    protected override float baseSpeed { get; set; } = 6f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    private Interactable highlightedInteractable;

    [Header("Breakable Detection Box")]
    [Tooltip("Center (local) of the Breakable detection box, relative to the player's facing direction.")]
    [SerializeField] private Vector3 breakBoxCenter = new Vector3(0f, 0.5f, 1.5f);
    [Tooltip("Full size (width, height, depth) of the Breakable detection box.")]
    [SerializeField] private Vector3 breakBoxSize = new Vector3(2f, 1f, 3f);

    [Header("Sprint Settings")]
    [SerializeField] private float sprintModifier = 2f;

    [Header("Sprint Stamina")]
    [Tooltip("The maximum amount of sprint stamina.")]
    [SerializeField] private float maxSprintStamina = 100f;
    [Tooltip("How much stamina is used per second while sprinting.")]
    [SerializeField] private float staminaDepletionRate = 25f;
    [Tooltip("How much stamina regenerates per second.")]
    [SerializeField] private float staminaRegenRate = 30f;
    [Tooltip("The delay in seconds after stopping a sprint before regeneration begins.")]
    [SerializeField] private float staminaRegenDelay = 1.0f;

    [Header("UI")]
    [Tooltip("Assign a UI Slider to display the stamina bar.")]
    [SerializeField] private Slider staminaSlider;

    [Tooltip("(Optional) Assign the Canvas Group of the stamina UI to allow fading.")]
    [SerializeField] private CanvasGroup staminaCanvasGroup;
    [Tooltip("How long to wait after stamina is full before fading the bar out.")]
    [SerializeField] private float staminaFadeOutDelay = 1.0f;
    [Tooltip("How long the fade-out animation takes.")]
    [SerializeField] private float staminaFadeDuration = 0.5f;
    
    private float currentSprintStamina;
    private float timeSinceSprintStopped = 0f;
    private bool isSprinting = false;

    private Coroutine fadeCoroutine;
    private bool isFadingOut = false;

    private bool isPushing = false;
    private BoxCollider pushCollider;
    private CapsuleCollider normalCollider;

    public override void Awake()
    {
        base.Awake();
        pushCollider = GetComponentInChildren<BoxCollider>();
        normalCollider = GetComponentInChildren<CapsuleCollider>();

        currentSprintStamina = maxSprintStamina;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxSprintStamina;
            staminaSlider.value = currentSprintStamina;
        }
        
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        currentSprintStamina = maxSprintStamina;
        isSprinting = false;
        timeSinceSprintStopped = staminaRegenDelay;
        UpdateStaminaUI(); 

        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
        isFadingOut = false;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    public void OnDisable()
    {
        PushState(false);
        StopSprint(); 

        if (highlightedInteractable != null)
        {
            highlightedInteractable.IsHighlighted = false;
            highlightedInteractable = null;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        GetBreakBoxTransform(out Vector3 boxWorldCenter, out Quaternion boxWorldRot);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxWorldCenter, boxWorldRot, breakBoxSize);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMatrix;
        #endif
    }
    
    public override void Ability1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PushState(true);
        }
        else if (context.canceled)
        {
            PushState(false);
        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (highlightedInteractable != null && highlightedInteractable.HasProperty("Breakable"))
            {
                highlightedInteractable.gameObject.SetActive(false);
                highlightedInteractable = null;
            }   
            StartSprint();
        }
        else if (context.canceled)
        {
            StopSprint();
        }
    }

    private void Update()
    {
        DetectAndHighlightBreakables();
        HandleStamina();

        if (isPushing)
        {
            animator?.SetBool("isPushing", true);
        }
        else
        {
            animator?.SetBool("isPushing", false);
        }
    }

    private void HandleStamina()
    {
        if (isSprinting)
        {
            currentSprintStamina -= staminaDepletionRate * Time.deltaTime;
            UpdateStaminaUI(); 

            if (currentSprintStamina <= 0)
            {
                currentSprintStamina = 0;
                StopSprint(); 
            }
        }
        else
        {
            timeSinceSprintStopped += Time.deltaTime;

            if (timeSinceSprintStopped >= staminaRegenDelay && currentSprintStamina < maxSprintStamina)
            {
                currentSprintStamina += staminaRegenRate * Time.deltaTime;
                currentSprintStamina = Mathf.Min(currentSprintStamina, maxSprintStamina);
                UpdateStaminaUI(); 
                
                if (currentSprintStamina >= maxSprintStamina && !isFadingOut)
                {
                    if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(FadeOutStaminaBar());
                }
            }
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentSprintStamina;
        }
    }
    
    private void StartSprint()
    {
        if (currentSprintStamina <= 0) return;

        if (staminaCanvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            isFadingOut = false;
            staminaCanvasGroup.alpha = 1f;
        }

        isSprinting = true;
        speed = baseSpeed * sprintModifier;
        animator?.SetBool("isSprinting", true);
        timeSinceSprintStopped = 0f;
    }

    private void StopSprint()
    {
        if (!isSprinting && speed == baseSpeed) return;
        
        isSprinting = false;
        speed = baseSpeed;
        animator?.SetBool("isSprinting", false);
    }

    private IEnumerator FadeOutStaminaBar()
    {
        isFadingOut = true;
        yield return new WaitForSeconds(staminaFadeOutDelay);

        float elapsedTime = 0f;
        float startAlpha = staminaCanvasGroup.alpha; 
        while (elapsedTime < staminaFadeDuration)
        {
            if (staminaCanvasGroup == null) yield break;
            staminaCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / staminaFadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
        isFadingOut = false;
        fadeCoroutine = null;
    }
    
    // This method now also directly controls the "isPushing" animator parameter.
    public void PushState(bool state)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);
        rb.mass = state ? 500f : 1f;
        isPushing = state;
        if (pushCollider != null) pushCollider.enabled = state;
        if (normalCollider != null) normalCollider.enabled = !state;
    }

    private void GetBreakBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : transform.forward;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * breakBoxCenter;
    }

    private void DetectAndHighlightBreakables()
    {
        GetBreakBoxTransform(out Vector3 boxWorldCenter, out Quaternion boxWorldRot);
        Vector3 halfExtents = breakBoxSize * 0.5f;
        Collider[] colliders = Physics.OverlapBox(boxWorldCenter, halfExtents, boxWorldRot);
        var breakables = colliders
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Breakable"))
            .ToList();
        var closest = breakables
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();
        if (closest != highlightedInteractable)
        {
            if (highlightedInteractable != null)
                highlightedInteractable.IsHighlighted = false;
            if (closest != null)
                closest.IsHighlighted = true;
            highlightedInteractable = closest;
        }
    }
}