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

    [Header("Push / Pull Detection")]
    [Tooltip("Half-extents of the box used to detect pushable objects in front of the player.")]
    [SerializeField] private Vector3 pushDetectHalfExtents = new Vector3(0.8f, 0.5f, 0.3f);
    [Tooltip("How far ahead of the player's center to cast the push detection box.")]
    [SerializeField] private float pushDetectDistance = 1.2f;
    [Tooltip("Force applied to pushable objects each physics frame when pushing.")]
    [SerializeField] private float pushForce = 2000f;
    [Tooltip("Force applied to pull (magnetize) an attached object toward the player.")]
    [SerializeField] private float pullForce = 2000f;
    [Tooltip("Maximum distance the object can be from the player and still receive push/pull force.")]
    [SerializeField] private float maxContactDistance = 2.5f;
    [Tooltip("Fraction of normal speed the bulldozer moves at while pulling (0-1).")]
    [SerializeField] private float pullSpeedFraction = 0.5f;
    [Tooltip("How quickly the pulled object lerps toward its anchor point (higher = stiffer).")]
    [SerializeField] private float pullStiffness = 15f;

    [Header("Mass Influence")]
    [Tooltip("Reference mass (kg) that receives the full push/pull force with no scaling.")]
    [SerializeField] private float referenceMass = 50f;
    [Tooltip("How strongly mass affects push/pull force (0 = no effect, 1 = full effect).")]
    [Range(0f, 1f)]
    [SerializeField] private float massInfluence = 0.7f;
    [Tooltip("Minimum force multiplier so very heavy objects can still be moved.")]
    [SerializeField] private float minForceMultiplier = 0.15f;
    [Tooltip("Maximum force multiplier so very light objects don't fly away.")]
    [SerializeField] private float maxForceMultiplier = 2.5f;
    [Tooltip("How much the bulldozer slows down when pushing heavy objects (0 = no slowdown, 1 = full slowdown).")]
    [Range(0f, 1f)]
    [SerializeField] private float pushSpeedPenalty = 0.5f;

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
    // Cached mass multiplier for the current attached object
    private float activeMassMultiplier = 1f;
    // True when the object is being pulled (dragged behind the bulldozer)
    private bool isPulling = false;
    // Local-space offset from the bulldozer to the object, captured when pull begins
    private Vector3 pullAnchorOffset;

    /// <summary>
    /// Lock the player's facing direction while actively pushing/pulling.
    /// </summary>
    public override bool IsDirectionLocked => isPushing;

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

        // If we already have an attached object, maintain it (push or pull)
        if (activePushable != null)
        {
            HandleAttachedPushable();
            return;
        }

        // No attached object yet — try to acquire one via boxcast in facing direction
        Vector3 facingDir = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : transform.forward;
        facingDir.y = 0f;
        if (facingDir.sqrMagnitude < 0.001f) return;
        facingDir.Normalize();

        Quaternion castRot = Quaternion.LookRotation(facingDir, Vector3.up);
        Vector3 origin = transform.position + Vector3.up * pushDetectHalfExtents.y;

        if (Physics.BoxCast(origin, pushDetectHalfExtents, facingDir, out RaycastHit hit, castRot, pushDetectDistance))
        {
            Pushable pushable = hit.collider.GetComponent<Pushable>();
            if (pushable == null) pushable = hit.collider.GetComponentInParent<Pushable>();

            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable == null) interactable = hit.collider.GetComponentInParent<Interactable>();

            if (pushable != null && interactable != null && interactable.HasProperty("Pushable"))
            {
                activePushable = pushable;
                activePushable.BeginPush();
                activeMassMultiplier = GetMassMultiplier(activePushable);
            }
        }

        // Apply initial force if we just locked on and are moving
        if (activePushable != null)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude >= 0.01f)
            {
                Vector3 moveDir = velocity.normalized;
                Vector3 dirToObject = (activePushable.transform.position - transform.position);
                dirToObject.y = 0f;
                float dot = dirToObject.sqrMagnitude > 0.001f ? Vector3.Dot(moveDir, dirToObject.normalized) : 1f;

                if (dot >= 0f)
                    activePushable.Push(moveDir * pushForce * activeMassMultiplier);
                else
                {
                    // Immediately enter pull mode with anchor offset
                    isPulling = true;
                    Vector3 offset = activePushable.transform.position - transform.position;
                    pullAnchorOffset = Quaternion.Inverse(transform.rotation) * offset;
                }
            }
        }
    }

    /// <summary>
    /// Once attached, either push (moving toward the object) or pull (moving away).
    /// Pulling uses position-based attachment so the object stays locked to the
    /// bulldozer rather than chasing it with forces.
    /// </summary>
    private void HandleAttachedPushable()
    {
        // Detach if the object is falling so physics can handle it naturally
        Rigidbody objRb = activePushable.GetComponent<Rigidbody>();
        if (objRb != null && objRb.velocity.y < -1f)
        {
            ReleaseActivePushable();
            return;
        }

        Vector3 toObject = activePushable.transform.position - transform.position;
        toObject.y = 0f;
        float distance = toObject.magnitude;

        // Detach if the object drifts beyond contact range (only matters for pushing)
        if (!isPulling && distance > maxContactDistance)
        {
            ReleaseActivePushable();
            return;
        }

        // --- Pulling: position-based attachment ---
        if (isPulling)
        {
            // Slow the bulldozer based on mass
            float massPullFraction = GetMassScaledPullFraction();
            Vector3 hVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            float maxPullSpeed = baseSpeed * massPullFraction;
            if (hVel.magnitude > maxPullSpeed)
            {
                hVel = hVel.normalized * maxPullSpeed;
                rb.velocity = new Vector3(hVel.x, rb.velocity.y, hVel.z);
            }

            // Move the object to its anchor position behind the bulldozer
            Vector3 targetPos = transform.position + transform.rotation * pullAnchorOffset;
            targetPos.y = activePushable.transform.position.y; // preserve vertical position

            Vector3 newPos = Vector3.Lerp(activePushable.transform.position, targetPos,
                                          pullStiffness * Time.fixedDeltaTime);

            if (objRb != null)
            {
                objRb.velocity = Vector3.zero; // clear residual forces
                objRb.MovePosition(newPos);
            }

            // Check if the player reversed direction (now moving toward the object)
            Vector3 vel = rb.velocity; vel.y = 0f;
            if (vel.sqrMagnitude >= 0.01f)
            {
                Vector3 dirToObj = (activePushable.transform.position - transform.position);
                dirToObj.y = 0f;
                if (dirToObj.sqrMagnitude > 0.001f &&
                    Vector3.Dot(vel.normalized, dirToObj.normalized) > 0.3f)
                {
                    // Player started pushing again — switch back to push mode
                    isPulling = false;
                }
            }
            return;
        }

        // --- Pushing: force-based ---
        Vector3 velocity = rb.velocity;
        velocity.y = 0f;

        // Not moving — do nothing but keep attached
        if (velocity.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = velocity.normalized;
        Vector3 dirToObject = distance > 0.001f ? toObject.normalized : moveDir;

        // Dot > 0 means moving toward the object (push), < 0 means moving away (pull)
        float dot = Vector3.Dot(moveDir, dirToObject);

        if (dot >= 0f)
        {
            // Pushing — apply force in the player's movement direction, scaled by mass
            activePushable.Push(moveDir * pushForce * activeMassMultiplier);

            // Slow the bulldozer when pushing heavy objects
            ApplyPushSpeedPenalty();
        }
        else
        {
            // Transition to pull mode — capture the current offset so the object
            // stays at the same relative position behind the bulldozer.
            isPulling = true;
            Vector3 offset = activePushable.transform.position - transform.position;
            pullAnchorOffset = Quaternion.Inverse(transform.rotation) * offset;
        }
    }

    private void ReleaseActivePushable()
    {
        if (activePushable != null)
        {
            activePushable.EndPush();
            activePushable = null;
            activeMassMultiplier = 1f;
            isPulling = false;
        }
    }

    /// <summary>
    /// Returns the pull speed fraction adjusted for the attached object's mass.
    /// Heavier objects reduce the fraction further; lighter objects leave it
    /// closer to (or above) the base <see cref="pullSpeedFraction"/>.
    /// </summary>
    private float GetMassScaledPullFraction()
    {
        if (activePushable == null) return pullSpeedFraction;

        Rigidbody objRb = activePushable.GetComponent<Rigidbody>();
        if (objRb == null) return pullSpeedFraction;

        // massRatio > 1 for heavy objects, < 1 for light
        float massRatio = objRb.mass / Mathf.Max(referenceMass, 0.01f);
        // Scale pullSpeedFraction inversely with mass: heavy → slower, light → faster
        float scaled = pullSpeedFraction / Mathf.Lerp(1f, massRatio, massInfluence);
        // Clamp so it never exceeds full speed or drops to zero
        return Mathf.Clamp(scaled, 0.05f, 1f);
    }

    /// <summary>
    /// Returns a force multiplier based on the object's Rigidbody mass relative
    /// to <see cref="referenceMass"/>. Lighter objects receive more force (up to
    /// <see cref="maxForceMultiplier"/>) and heavier objects receive less (down
    /// to <see cref="minForceMultiplier"/>). The <see cref="massInfluence"/>
    /// parameter controls how strongly mass affects the result.
    /// </summary>
    private float GetMassMultiplier(Pushable pushable)
    {
        Rigidbody objRb = pushable.GetComponent<Rigidbody>();
        if (objRb == null) return 1f;

        float rawMultiplier = referenceMass / Mathf.Max(objRb.mass, 0.01f);
        float multiplier = Mathf.Lerp(1f, rawMultiplier, massInfluence);
        return Mathf.Clamp(multiplier, minForceMultiplier, maxForceMultiplier);
    }

    /// <summary>
    /// Reduces the bulldozer's movement speed proportionally when pushing
    /// objects heavier than <see cref="referenceMass"/>.
    /// </summary>
    private void ApplyPushSpeedPenalty()
    {
        if (activePushable == null || pushSpeedPenalty <= 0f) return;

        Rigidbody objRb = activePushable.GetComponent<Rigidbody>();
        if (objRb == null) return;

        // massRatio > 1 for heavy objects, < 1 for light ones
        float massRatio = objRb.mass / Mathf.Max(referenceMass, 0.01f);

        if (massRatio > 1f)
        {
            // speedFraction goes from 1.0 (at reference mass) toward
            // (1 - pushSpeedPenalty) as massRatio increases
            float speedFraction = Mathf.Lerp(1f, 1f - pushSpeedPenalty, Mathf.Clamp01((massRatio - 1f) / massRatio));
            float maxPushSpeed = baseSpeed * speedFraction;

            Vector3 hVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (hVel.magnitude > maxPushSpeed)
            {
                hVel = hVel.normalized * maxPushSpeed;
                rb.velocity = new Vector3(hVel.x, rb.velocity.y, hVel.z);
            }
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