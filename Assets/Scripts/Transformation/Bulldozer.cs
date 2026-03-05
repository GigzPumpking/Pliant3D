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

    [Header("Push Detection")]
    [Tooltip("Half-extents of the box used to detect pushable objects in front of the player.")]
    [SerializeField] private Vector3 pushDetectHalfExtents = new Vector3(0.8f, 0.5f, 0.3f);
    [Tooltip("How far ahead of the player's center to cast the push detection box.")]
    [SerializeField] private float pushDetectDistance = 1.2f;
    [Tooltip("Force applied to pushable objects each physics frame.")]
    [SerializeField] private float pushForce = 60f;

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

    // Currently pushed object (tracked for BeginPush / EndPush lifecycle)
    private Pushable activePushable = null;

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
        // Breakable detection box (orange)
        GetBreakBoxTransform(out Vector3 boxWorldCenter, out Quaternion boxWorldRot);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxWorldCenter, boxWorldRot, breakBoxSize);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMatrix;

        // Push detection box (cyan)
        Vector3 moveDir = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : transform.forward;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            moveDir.y = 0f;
            moveDir.Normalize();
            Quaternion pushRot = Quaternion.LookRotation(moveDir, Vector3.up);
            Vector3 pushOrigin = transform.position + Vector3.up * pushDetectHalfExtents.y;
            Vector3 pushCenter = pushOrigin + moveDir * pushDetectDistance;
            Gizmos.matrix = Matrix4x4.TRS(pushCenter, pushRot, pushDetectHalfExtents * 2f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldMatrix;
        }
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

    private void FixedUpdate()
    {
        if (!isPushing)
        {
            ReleaseActivePushable();
            return;
        }

        // Get the player's current horizontal velocity as the push direction
        Vector3 velocity = rb.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.01f)
        {
            ReleaseActivePushable();
            return;
        }

        Vector3 moveDir = velocity.normalized;
        Quaternion castRot = Quaternion.LookRotation(moveDir, Vector3.up);
        Vector3 origin = transform.position + Vector3.up * pushDetectHalfExtents.y;

        // BoxCast forward in the movement direction to find pushable objects
        Pushable hitPushable = null;
        if (Physics.BoxCast(origin, pushDetectHalfExtents, moveDir, out RaycastHit hit, castRot, pushDetectDistance))
        {
            Pushable pushable = hit.collider.GetComponent<Pushable>();
            if (pushable == null) pushable = hit.collider.GetComponentInParent<Pushable>();

            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable == null) interactable = hit.collider.GetComponentInParent<Interactable>();

            if (pushable != null && interactable != null && interactable.HasProperty("Pushable"))
            {
                hitPushable = pushable;
            }
        }

        // Handle pushable lifecycle transitions
        if (hitPushable != activePushable)
        {
            ReleaseActivePushable();

            if (hitPushable != null)
            {
                activePushable = hitPushable;
                activePushable.BeginPush();
            }
        }

        // Apply force to the active pushable
        if (activePushable != null)
        {
            activePushable.Push(moveDir * pushForce);
        }
    }

    private void ReleaseActivePushable()
    {
        if (activePushable != null)
        {
            activePushable.EndPush();
            activePushable = null;
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

    public bool IsSprinting()
    {
        return isSprinting;
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
    
    // Controls push mode: swaps colliders and enables push detection.
    // Objects are moved via applied force in FixedUpdate, not via mass differential.
    public void PushState(bool state)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);
        isPushing = state;
        if (pushCollider != null) pushCollider.enabled = state;
        if (normalCollider != null) normalCollider.enabled = !state;

        if (!state)
        {
            ReleaseActivePushable();
        }
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