using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

public class Frog : FormScript
{
    #region Configuration Variables
    protected override float baseSpeed { get; set; } = 6.0f;
    public float jumpForce = 6.0f;
    private bool isGrounded = true;

    [Tooltip("The time in seconds before the frog can jump again.")]
    [SerializeField] private float jumpCooldown = 0.15f;
    private float nextJumpTime = 0f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter = 0f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer; 
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float verticalVelocityThreshold = 0.1f;

    [Header("Pullable Box Settings")]
    [SerializeField] private Vector3 pullBoxCenter = new Vector3(0f, 0.5f, 2f);
    [SerializeField] private Vector3 pullBoxSize = new Vector3(2f, 1f, 4f);

    [Header("Grapple Box Settings")]
    [SerializeField] private Vector3 grappleBoxCenter = new Vector3(0f, 1.0f, 3f);
    [SerializeField] private Vector3 grappleBoxSize = new Vector3(4f, 2f, 6f);

    [Header("Tongue Settings")]
    [SerializeField] private GameObject hookTarget;
    [SerializeField] private float tongueExtendSpeed = 45f;
    [SerializeField] private float tongueRetractSpeed = 25f;
    [SerializeField] private float tongueMaxDistance = 12f;
    [SerializeField] private float tongueTipRadius = 0.24f;
    [SerializeField] private float grappleReelSpeed = 20f;
    [SerializeField] private float pullRetractSpeed = 25f;
    [SerializeField] private float pullTimeout = 3f;
    [SerializeField] private LayerMask tongueHitLayers = ~0;

    [Header("Tongue Easing")]
    [SerializeField] private AnimationCurve tongueExtendCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0.3f));
    [SerializeField] private AnimationCurve tongueRetractCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.2f));
    [SerializeField] private AnimationCurve grappleReelCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.12f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.45f));

    [Header("Tongue Line")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private bool useWorldSpace = true;
    [SerializeField] private float swayAmplitude = 0.12f;
    [SerializeField] private float swayFrequency = 8f;

    [Header("Detection Options")]
    [Tooltip("If true, the frog requires a clear line of sight to target an object. Turn off for debugging/playtesting if objects are un-targetable.")]
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Unstick Minigame")]
    [SerializeField] private float unstickBarMax = 100f;
    [SerializeField] private float unstickFillPerPress = 25f;
    [SerializeField] private Slider unstickSlider;
    [SerializeField] private CanvasGroup unstickCanvasGroup;
    [SerializeField] private float unstickBarDrainRate = 15f;
    [SerializeField] private GameObject unstickPrompt;
    [SerializeField] private float unstickPromptYOffset = 0.5f;

    public Slider UnstickSlider => unstickSlider;
    public CanvasGroup UnstickCanvasGroup => unstickCanvasGroup;
    #endregion

    #region State Variables
    private Interactable highlightedObject;
    private Transform closestObject;
    private SpriteRenderer _spriteRenderer;

    private float currentUnstickProgress = 0f;
    private bool unstickButtonPressed = false;

    public static event Action<Transformation, int, Interactable> AbilityUsed;

    private enum TongueState { Idle, Extending, Retracting, PullRetracting, GrappleReeling, UnstickMinigame, UnstickRetracting }
    private TongueState tongueState = TongueState.Idle;

    private bool _isDirectionLocked = false;
    public override bool IsDirectionLocked => _isDirectionLocked;
    
    private Vector3 tongueTipPosition;
    private Vector3 tongueDirection;
    private Vector3 cachedFacingDirection = Vector3.forward;
    
    private GameObject tongueTipObject;
    private Transform tongueHitTarget;
    private Coroutine tongueCoroutine;
    
    private bool originalKinematicState = false;
    private const float MIN_PULL_DISTANCE = 1.5f;
    private bool canJumpLock = true;
    #endregion

    #region Initialization & Lifecycle
    public override void Awake()
    {
        base.Awake();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (hookTarget != null) hookTarget.SetActive(false);
        if (unstickPrompt != null) unstickPrompt.SetActive(false);

        if (unstickSlider != null)
        {
            unstickSlider.maxValue = unstickBarMax;
            unstickSlider.value = 0f;
        }
        if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 0f;

        InitializeTongueLine();
        EnsureTongueTip();
    }

    private void InitializeTongueLine()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = useWorldSpace;
        }
        lineRenderer.positionCount = 3;
        var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.45f, 0.55f, 0.9f) };
        lineRenderer.material = mat;
        lineRenderer.startColor = new Color(1f, 0.45f, 0.55f, 0.9f);
        lineRenderer.endColor = new Color(0.9f, 0.35f, 0.45f, 0.95f);
        lineRenderer.widthMultiplier = 0.35f;
        lineRenderer.widthCurve = new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.25f, 1f), new Keyframe(0.75f, 0.85f), new Keyframe(1f, 0.6f));
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
    }

    private void EnsureTongueTip()
    {
        if (tongueTipObject != null) return;
        tongueTipObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tongueTipObject.name = "TongueTip";
        tongueTipObject.transform.localScale = Vector3.one * tongueTipRadius * 2f;
        
        var tipCollider = tongueTipObject.GetComponent<Collider>();
        if (tipCollider != null) Destroy(tipCollider);
        
        var tipRenderer = tongueTipObject.GetComponent<Renderer>();
        if (tipRenderer != null)
        {
            var tipMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.9f, 0.3f, 0.42f, 1f) };
            tipRenderer.material = tipMat;
        }
        
        DontDestroyOnLoad(tongueTipObject);
        tongueTipObject.SetActive(false);
    }

    public override void OnEnable()
    {
        EventDispatcher.AddListener<TogglePlayerMovement>(ToggleJump);
        currentUnstickProgress = 0f;
        unstickButtonPressed = false;
        if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 0f;
        if (unstickPrompt != null) unstickPrompt.SetActive(false);
        base.OnEnable();
    }

    public void OnDisable()
    {
        if (highlightedObject != null) highlightedObject.IsHighlighted = false;
        highlightedObject = null;
        closestObject = null;
        StopTongue();
        EventDispatcher.RemoveListener<TogglePlayerMovement>(ToggleJump);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (tongueState == TongueState.GrappleReeling && tongueHitTarget != null && collision.transform == tongueHitTarget)
        {
            StopTongue();
        }
    }
    #endregion

    #region Input & Movement
    public override void Ability1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Jump();
            AbilityUsed?.Invoke(Transformation.FROG, 1, null);
        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        if (context.performed && tongueState == TongueState.Idle)
        {
            _isDirectionLocked = true; // Lock instantly!

            Interactable intr = closestObject != null ? closestObject.GetComponent<Interactable>() : null;
            AbilityUsed?.Invoke(Transformation.FROG, 2, intr);
            FireTongue();
            EventDispatcher.Raise<StressAbility>(new StressAbility());
        }
    }

    public override void Unstick(InputAction.CallbackContext context)
    {
        if (context.performed && tongueState == TongueState.UnstickMinigame) unstickButtonPressed = true;
    }

    void ToggleJump(TogglePlayerMovement set) => canJumpLock = set.isEnabled;

    private void Jump()
    {
        if (!canJumpLock) return;
        
        bool isVerticallyStationary = Mathf.Abs(rb.velocity.y) < verticalVelocityThreshold;
        bool canJumpFromGround = isGrounded && isVerticallyStationary;
        bool canJumpFromCoyote = coyoteTimeCounter > 0f && !isGrounded;
        
        bool canJump = (canJumpFromGround || canJumpFromCoyote) && Time.time >= nextJumpTime && !Player.Instance.TransformationChecker();

        if (!canJump) return;

        coyoteTimeCounter = 0f;
        nextJumpTime = Time.time + jumpCooldown;
        isGrounded = false;
        
        Player.Instance?.SetGroundedState(false);
        Player.Instance?.SetJumpingState(true);
        Player.Instance?.RegisterAirborneImpulse();
        
        animator?.SetTrigger("Jump");
        PlayAbilitySound(ability1Sound);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void FixedUpdate()
    {
        Vector3 groundCheckPosition = transform.position - Vector3.up * groundCheckDistance;
        bool hitGround = Physics.CheckSphere(groundCheckPosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        bool fallingOrStill = rb.velocity.y <= 0f;
        bool wasGrounded = isGrounded;
        isGrounded = hitGround && fallingOrStill;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            Player.Instance?.SetGroundedState(true);
            Player.Instance?.SetJumpingState(false);
        }
        else
        {
            if (Player.Instance.IsJumping) coyoteTimeCounter = 0f;
            coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.fixedDeltaTime);
            Player.Instance?.SetGroundedState(false);
        }
    }

    private void Update()
    {
        if (tongueState == TongueState.Idle)
        {
            if (Player.Instance != null)
            {
                Vector3 dir = Player.Instance.AnimationBasedFacingDirection;
                if (dir.sqrMagnitude > 0.01f) cachedFacingDirection = dir;
            }
            DetectAndHighlightObjects();
        }
        UpdateTongueLine();
    }
    #endregion

    #region Object Detection & Highlighting
    private void DetectAndHighlightObjects()
    {
        GetPullBoxTransform(out Vector3 pullBoxWorldCenter, out Quaternion commonWorldRot);
        Vector3 grappleBoxWorldCenter = transform.position + commonWorldRot * grappleBoxCenter;
        
        Collider[] hookCols = Physics.OverlapBox(grappleBoxWorldCenter, grappleBoxSize * 0.5f, commonWorldRot);
        var hookables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Hookable"))
            .Where(i => IsPathClear(i.GetComponent<Collider>())); // NEW: Line of sight check

        Collider[] pullCols = Physics.OverlapBox(pullBoxWorldCenter, pullBoxSize * 0.5f, commonWorldRot);
        var pullables = pullCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Pullable"))
            .Where(i => IsPathClear(i.GetComponent<Collider>())); // NEW: Line of sight check

        var unstickables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Unstickable"))
            .Where(i => IsPathClear(i.GetComponent<Collider>())); // NEW: Line of sight check

        var all = hookables.Concat(pullables).Concat(unstickables);
        var nearest = all.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();

        if (tongueState == TongueState.PullRetracting && tongueHitTarget == nearest?.transform) return;

        if (nearest != highlightedObject)
        {
            if (highlightedObject != null) highlightedObject.IsHighlighted = false;
            closestObject = nearest != null ? nearest.transform : null;
            if (nearest != null) nearest.IsHighlighted = true;
            highlightedObject = nearest;
        }

        UpdateHookTargetIcon(hookables, pullables, unstickables);
    }

    // Helper method to verify there are no obstacles blocking the tongue's path
    private bool IsPathClear(Collider targetCol)
    {
        // NEW: Instantly approve the path if the toggle is turned off
        if (!requireLineOfSight) return true;

        if (targetCol == null) return false;

        Vector3 origin = GetTongueOrigin();
        
        // Target the mathematical center of the object's bounds for a rock-solid sightline
        Vector3 destination = targetCol.bounds.center;
        Vector3 direction = destination - origin;
        float totalDistance = direction.magnitude;

        // Cast slightly past the target to ensure the ray goes all the way through it
        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, totalDistance + 1f, tongueHitLayers, QueryTriggerInteraction.Ignore);
        
        // Sort hits by distance (closest to player first)
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform hitT = hit.transform;

            // 1. Ignore the player's own colliders
            if (hitT == transform || hitT.IsChildOf(player)) continue;

            // 2. If we hit the target or its children, the path is definitively clear!
            if (hit.collider == targetCol || hitT.IsChildOf(targetCol.transform)) return true;

            // 3. The Bounding Box Forgiveness Check:
            if (Mathf.Abs(totalDistance - hit.distance) < 0.8f) continue;

            // 4. We hit a true obstacle (like a separate crate or pillar) well before reaching the target
            return false; 
        }

        // If we didn't hit any true blocking obstacles, assume the path is clear
        return true;
    }

    private void UpdateHookTargetIcon(IEnumerable<Interactable> hookables, IEnumerable<Interactable> pullables, IEnumerable<Interactable> unstickables)
    {
        if (hookTarget == null) return;
        Transform targetForIcon = null;

        var nearestHookable = hookables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
        if (nearestHookable != null) targetForIcon = nearestHookable.transform;
        else
        {
            var nearestPullable = pullables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
            if (nearestPullable != null)
            {
                Collider objCol = nearestPullable.GetComponent<Collider>();
                Collider plrCol = player.GetComponentInChildren<Collider>();
                if (objCol != null && plrCol != null)
                {
                    float minAllowedDistance = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.z) + Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.z);
                    Vector3 currentFlatDir = player.position - nearestPullable.transform.position;
                    currentFlatDir.y = 0f;
                    if (currentFlatDir.magnitude > minAllowedDistance + 0.01f) targetForIcon = nearestPullable.transform;
                }
            }

            if (targetForIcon == null)
            {
                var nearestUnstickable = unstickables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
                if (nearestUnstickable != null) targetForIcon = nearestUnstickable.transform;
            }
        }

        bool shouldShowHookTarget = targetForIcon != null && tongueState == TongueState.Idle;
        hookTarget.SetActive(shouldShowHookTarget);

        if (shouldShowHookTarget)
        {
            Vector3 worldPoint = CalculateHookPoint(targetForIcon);
            hookTarget.transform.position = Camera.main.WorldToScreenPoint(worldPoint);
        }
    }
    #endregion

    #region Tongue Core Logic
    private void FireTongue()
    {
        tongueCoroutine = StartCoroutine(TongueCoroutine());
    }

    private Vector3 GetTongueOrigin()
    {
        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : transform.position;
    }

    private IEnumerator TongueCoroutine()
    {
        // 1. Setup & Animation
        Vector3 facingDir = cachedFacingDirection;
        bool isFacingFront = facingDir == Vector3.left || facingDir == Vector3.back;
        bool isFacingBack = facingDir == Vector3.forward || facingDir == Vector3.right;
        if (isFacingBack || isFacingFront) animator?.SetTrigger("Tongue");

        if (closestObject != null)
        {
            Vector3 hookPoint = CalculateHookPoint(closestObject);
            Vector3 toTarget = hookPoint - GetTongueOrigin();
            if (toTarget.sqrMagnitude > 0.01f) facingDir = toTarget.normalized;
        }

        tongueState = TongueState.Extending;
        tongueDirection = facingDir;
        tongueTipPosition = GetTongueOrigin();
        tongueHitTarget = null;
        Player.Instance.canMoveToggle(false);

        Transform lockedTarget = closestObject;
        highlightedObject = null;
        closestObject = null;
        if (hookTarget != null) hookTarget.SetActive(false);

        EnsureTongueTip();
        tongueTipObject.SetActive(true);
        tongueTipObject.transform.position = tongueTipPosition;
        PlayAbilitySound(ability2Sound);

        // 2. Main State Machine execution
        yield return StartCoroutine(HandleExtending(lockedTarget));

        if (tongueState == TongueState.Retracting) yield return StartCoroutine(ExecuteRetraction());
        else if (tongueState == TongueState.GrappleReeling) yield return StartCoroutine(HandleGrappleReeling());
        else if (tongueState == TongueState.PullRetracting) yield return StartCoroutine(HandlePullRetracting());
        else if (tongueState == TongueState.UnstickMinigame)
        {
            yield return StartCoroutine(HandleUnstickMinigame());
            if (tongueState == TongueState.UnstickRetracting) yield return StartCoroutine(HandleUnstickRetracting());
        }

        CleanupTongue();
    }

    private IEnumerator ExecuteRetraction()
    {
        tongueState = TongueState.Retracting;
        float retractStartDist = Vector3.Distance(tongueTipPosition, GetTongueOrigin());

        while (tongueState == TongueState.Retracting)
        {
            Vector3 origin = GetTongueOrigin();
            Vector3 toPlayer = origin - tongueTipPosition;
            float dist = toPlayer.magnitude;

            float retractT = retractStartDist > 0.01f ? Mathf.Clamp01(1f - dist / retractStartDist) : 1f;
            float retractStep = tongueRetractSpeed * tongueRetractCurve.Evaluate(retractT) * Time.fixedDeltaTime;

            if (dist <= retractStep)
            {
                tongueTipPosition = origin;
                break;
            }

            tongueTipPosition += toPlayer.normalized * retractStep;
            if (tongueTipObject != null) tongueTipObject.transform.position = tongueTipPosition;

            yield return new WaitForFixedUpdate();
        }
    }

    private void CleanupTongue()
    {
        tongueState = TongueState.Idle;
        tongueHitTarget = null;
        if (tongueTipObject != null) tongueTipObject.SetActive(false);
        Player.Instance.canMoveToggle(true);
        tongueCoroutine = null;

        _isDirectionLocked = false; // Unlock when finished
    }

    private void StopTongue()
    {
        if (tongueState == TongueState.Idle) return;
        if (tongueCoroutine != null)
        {
            StopCoroutine(tongueCoroutine);
            tongueCoroutine = null;
        }

        RestoreTargetPhysics();

        if (tongueState == TongueState.UnstickMinigame) HideUnstickUI();
        if (tongueState == TongueState.UnstickRetracting && tongueHitTarget != null) Destroy(tongueHitTarget.gameObject);
        if (tongueState == TongueState.GrappleReeling) rb.velocity = Vector3.zero;

        CleanupTongue();
    }

    private void CancelTongueGracefully()
    {
        if (tongueState == TongueState.Idle || tongueState == TongueState.Retracting) return;
        
        if (tongueCoroutine != null)
        {
            StopCoroutine(tongueCoroutine);
            tongueCoroutine = null;
        }

        RestoreTargetPhysics();

        if (tongueState == TongueState.UnstickMinigame) HideUnstickUI();
        if (tongueState == TongueState.UnstickRetracting && tongueHitTarget != null) Destroy(tongueHitTarget.gameObject);
        if (tongueState == TongueState.GrappleReeling) rb.velocity = Vector3.zero;

        tongueHitTarget = null;
        tongueCoroutine = StartCoroutine(RetractTongueCoroutineGraceful());
    }

    private IEnumerator RetractTongueCoroutineGraceful()
    {
        yield return StartCoroutine(ExecuteRetraction());
        CleanupTongue();
    }

    private void RestoreTargetPhysics()
    {
        if (tongueHitTarget != null && (tongueState == TongueState.PullRetracting || tongueState == TongueState.UnstickRetracting))
        {
            Rigidbody targetRb = tongueHitTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.velocity = Vector3.zero;
                targetRb.angularVelocity = Vector3.zero;
                targetRb.freezeRotation = false;
                targetRb.isKinematic = originalKinematicState;
            }
            animator?.SetBool("isPulling", false);
        }
    }
    #endregion

    #region Tongue Specific Phase Handlers
    private IEnumerator HandleExtending(Transform lockedTarget)
    {
        float distanceTraveled = 0f;
        
        // Cache the destination and distance if we have a guaranteed target
        float lockedDistance = 0f;
        Vector3 lockedDestination = Vector3.zero;
        Interactable lockedInteractable = null;

        if (lockedTarget != null)
        {
            lockedDestination = CalculateHookPoint(lockedTarget);
            lockedDistance = Vector3.Distance(GetTongueOrigin(), lockedDestination);
            lockedInteractable = lockedTarget.GetComponent<Interactable>();
        }

        while (tongueState == TongueState.Extending)
        {
            float extendT = Mathf.Clamp01(distanceTraveled / tongueMaxDistance);
            float step = tongueExtendSpeed * tongueExtendCurve.Evaluate(extendT) * Time.fixedDeltaTime;
            bool hitDetected = false;

            // --- GUARANTEED HIT LOGIC ---
            if (lockedTarget != null && lockedInteractable != null)
            {
                // Safety check: Did the target get destroyed or deactivated mid-flight?
                if (lockedTarget.gameObject == null || !lockedTarget.gameObject.activeInHierarchy)
                {
                    tongueState = TongueState.Retracting;
                    break;
                }

                distanceTraveled += step;
                
                // Have we mathematically reached the target?
                if (distanceTraveled >= lockedDistance)
                {
                    // Snap exactly to the target point and force the connection
                    tongueTipPosition = lockedDestination;
                    tongueHitTarget = lockedTarget;
                    hitDetected = true;

                    if (lockedInteractable.HasProperty("Hookable")) tongueState = TongueState.GrappleReeling;
                    else if (lockedInteractable.HasProperty("Pullable")) tongueState = TongueState.PullRetracting;
                    else if (lockedInteractable.HasProperty("Unstickable"))
                    {
                        AbilityUsed?.Invoke(Transformation.FROG, 2, lockedInteractable);
                        tongueState = TongueState.UnstickMinigame;
                    }
                    else tongueState = TongueState.Retracting;
                }
                else
                {
                    tongueTipPosition += tongueDirection * step;
                }
            }
            
            // --- NORMAL BLIND FIRE LOGIC ---
            else
            {
                if (Physics.SphereCast(tongueTipPosition, tongueTipRadius, tongueDirection, out RaycastHit hitInfo, step, tongueHitLayers, QueryTriggerInteraction.Ignore))
                {
                    bool isSelf = hitInfo.transform.IsChildOf(transform) || hitInfo.transform == transform || hitInfo.transform.IsChildOf(player) || hitInfo.transform == player;

                    if (!isSelf)
                    {
                        tongueTipPosition += tongueDirection * hitInfo.distance;
                        hitDetected = true;
                        var intr = hitInfo.transform.GetComponent<Interactable>();

                        if (intr != null && intr.isInteractable)
                        {
                            tongueHitTarget = hitInfo.transform;
                            if (intr.HasProperty("Hookable")) tongueState = TongueState.GrappleReeling;
                            else if (intr.HasProperty("Pullable")) tongueState = TongueState.PullRetracting;
                            else if (intr.HasProperty("Unstickable"))
                            {
                                AbilityUsed?.Invoke(Transformation.FROG, 2, intr);
                                tongueState = TongueState.UnstickMinigame;
                            }
                            else tongueState = TongueState.Retracting;
                        }
                        else tongueState = TongueState.Retracting;
                    }
                }

                if (!hitDetected)
                {
                    tongueTipPosition += tongueDirection * step;
                    distanceTraveled += step;
                }

                if (distanceTraveled >= tongueMaxDistance && tongueState == TongueState.Extending) 
                {
                    tongueState = TongueState.Retracting;
                }
            }

            tongueTipObject.transform.position = tongueTipPosition;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator HandleGrappleReeling()
    {
        Collider hookCollider = tongueHitTarget.GetComponent<Collider>();

        Vector3 ComputeArrivalPos() => hookCollider != null 
            ? new Vector3(hookCollider.bounds.center.x, hookCollider.bounds.max.y + 0.6f, hookCollider.bounds.center.z) 
            : tongueHitTarget.position + Vector3.up * 1.1f;

        float totalGrappleDist = Vector3.Distance(rb.position, ComputeArrivalPos());
        Vector3 reelDirection = totalGrappleDist > 0.01f ? (ComputeArrivalPos() - rb.position).normalized : Vector3.up;

        while (tongueState == TongueState.GrappleReeling && tongueHitTarget != null)
        {
            UpdateTongueTipOnObject(hookCollider);
            float currentDist = Vector3.Distance(rb.position, ComputeArrivalPos());
            float progress = totalGrappleDist > 0.01f ? Mathf.Clamp01(1f - currentDist / totalGrappleDist) : 1f;
            
            rb.velocity = reelDirection * grappleReelSpeed * grappleReelCurve.Evaluate(progress);

            if (currentDist <= 1.5f) break;
            yield return new WaitForFixedUpdate();
        }

        rb.velocity = Vector3.up * 4f;
    }

    private IEnumerator HandlePullRetracting()
    {
        Rigidbody pullableRb = tongueHitTarget.GetComponent<Rigidbody>();
        Collider objCol = tongueHitTarget.GetComponent<Collider>();
        
        if (pullableRb != null)
        {
            originalKinematicState = pullableRb.isKinematic;
            pullableRb.isKinematic = false;
            pullableRb.velocity = pullableRb.angularVelocity = Vector3.zero;
            pullableRb.freezeRotation = true;
        }

        animator?.SetBool("isPulling", true);
        Vector3 initialPullAxis = GetTongueOrigin() - tongueHitTarget.position;
        initialPullAxis.y = 0f;
        float totalPullDist = initialPullAxis.magnitude;
        Vector3 pullAxis = totalPullDist > 0.01f ? initialPullAxis / totalPullDist : Vector3.forward;
        
        UpdateTongueTipOnObject(objCol);
        float pullElapsed = 0f;

        while (tongueState == TongueState.PullRetracting && tongueHitTarget != null)
        {
            pullElapsed += Time.fixedDeltaTime;
            if (pullElapsed >= pullTimeout) break;

            Vector3 toPlayer = GetTongueOrigin() - tongueHitTarget.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude <= MIN_PULL_DISTANCE) break;

            float retractT = totalPullDist > 0.01f ? Mathf.Clamp01(1f - toPlayer.magnitude / totalPullDist) : 1f;
            float speed = pullRetractSpeed * tongueRetractCurve.Evaluate(retractT);

            if (pullableRb != null)
            {
                Vector3 desiredVel = pullAxis * speed;
                desiredVel.y = pullableRb.velocity.y;
                pullableRb.velocity = desiredVel;
            }

            UpdateTongueTipOnObject(objCol);

            Collider plrCol = player.GetComponentInChildren<Collider>();
            if (objCol != null && plrCol != null && Physics.ComputePenetration(objCol, objCol.transform.position, objCol.transform.rotation, plrCol, plrCol.transform.position, plrCol.transform.rotation, out _, out _))
            {
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        RestoreTargetPhysics();
        tongueHitTarget = null;
        yield return StartCoroutine(ExecuteRetraction());
    }

    private IEnumerator HandleUnstickMinigame()
    {
        Collider objCol = tongueHitTarget.GetComponent<Collider>();
        currentUnstickProgress = 0f;
        unstickButtonPressed = false;
        
        if (unstickSlider != null) { unstickSlider.maxValue = unstickBarMax; unstickSlider.value = 0f; }
        if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 1f;
        if (unstickPrompt != null) unstickPrompt.SetActive(true);

        while (tongueState == TongueState.UnstickMinigame && tongueHitTarget != null)
        {
            UpdateTongueTipOnObject(objCol);

            if (unstickPrompt != null && Camera.main != null)
            {
                Bounds b = objCol != null ? objCol.bounds : new Bounds(tongueHitTarget.position, Vector3.zero);
                unstickPrompt.transform.position = Camera.main.WorldToScreenPoint(new Vector3(tongueHitTarget.position.x, b.max.y + unstickPromptYOffset, tongueHitTarget.position.z));
            }

            if (unstickButtonPressed)
            {
                unstickButtonPressed = false;
                currentUnstickProgress = Mathf.Min(currentUnstickProgress + unstickFillPerPress, unstickBarMax);
            }

            if (currentUnstickProgress >= unstickBarMax) { if (unstickSlider != null) unstickSlider.value = currentUnstickProgress; break; }

            if (currentUnstickProgress > 0f) currentUnstickProgress = Mathf.Max(0f, currentUnstickProgress - unstickBarDrainRate * Time.fixedDeltaTime);
            if (unstickSlider != null) unstickSlider.value = currentUnstickProgress;

            yield return new WaitForFixedUpdate();
        }

        HideUnstickUI();
        if (tongueHitTarget != null && currentUnstickProgress >= unstickBarMax) tongueState = TongueState.UnstickRetracting;
        else tongueHitTarget = null;
    }

    private IEnumerator HandleUnstickRetracting()
    {
        Collider objCol = tongueHitTarget.GetComponent<Collider>();
        Rigidbody unstickRb = tongueHitTarget.GetComponent<Rigidbody>();
        bool wasKinematic = false;

        if (unstickRb != null)
        {
            wasKinematic = unstickRb.isKinematic;
            unstickRb.isKinematic = false;
            unstickRb.velocity = unstickRb.angularVelocity = Vector3.zero;
            unstickRb.freezeRotation = true;
        }

        Vector3 initialAxis = GetTongueOrigin() - tongueHitTarget.position;
        initialAxis.y = 0f;
        float totalUnstickDist = initialAxis.magnitude;
        Vector3 unstickAxis = totalUnstickDist > 0.01f ? initialAxis / totalUnstickDist : tongueDirection;
        
        UpdateTongueTipOnObject(objCol);
        float unstickElapsed = 0f;

        while (tongueState == TongueState.UnstickRetracting && tongueHitTarget != null)
        {
            unstickElapsed += Time.fixedDeltaTime;
            if (unstickElapsed >= pullTimeout) break;

            Vector3 origin = GetTongueOrigin();
            Vector3 toPlayer = origin - tongueHitTarget.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude <= MIN_PULL_DISTANCE) break;

            float retractT = totalUnstickDist > 0.01f ? Mathf.Clamp01(1f - toPlayer.magnitude / totalUnstickDist) : 1f;
            float speed = pullRetractSpeed * tongueRetractCurve.Evaluate(retractT);

            if (unstickRb != null)
            {
                Vector3 desiredVel = unstickAxis * speed;
                desiredVel.y = unstickRb.velocity.y;
                unstickRb.velocity = desiredVel;
            }
            else tongueHitTarget.position = Vector3.MoveTowards(tongueHitTarget.position, origin, speed * Time.fixedDeltaTime);

            UpdateTongueTipOnObject(objCol);

            Collider plrCol = player.GetComponentInChildren<Collider>();
            if (objCol != null && plrCol != null && Physics.ComputePenetration(objCol, objCol.transform.position, objCol.transform.rotation, plrCol, plrCol.transform.position, plrCol.transform.rotation, out _, out _)) break;

            yield return new WaitForFixedUpdate();
        }

        if (unstickRb != null)
        {
            unstickRb.velocity = unstickRb.angularVelocity = Vector3.zero;
            unstickRb.freezeRotation = false;
            unstickRb.isKinematic = wasKinematic;
        }

        if (tongueHitTarget != null)
        {
            if (CustomEventObjective.TryCompleteAnyForObject(tongueHitTarget.gameObject, out CustomEventObjective completedObjective))
                Debug.Log($"Pulled object '{tongueHitTarget.gameObject.name}' counted toward objective '{completedObjective.description}'.");
            Destroy(tongueHitTarget.gameObject);
        }

        tongueHitTarget = null;
        yield return StartCoroutine(ExecuteRetraction());
    }

    private void HideUnstickUI()
    {
        if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 0f;
        if (unstickSlider != null) unstickSlider.value = 0f;
        if (unstickPrompt != null) unstickPrompt.SetActive(false);
    }
    #endregion

    #region Visuals & Utilities
    private void UpdateTongueLine()
    {
        if (tongueState != TongueState.Idle)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter = GetComponentInChildren<Collider>()?.bounds.center ?? transform.position;
            Vector3 startPos = useWorldSpace ? startCenter : lineRenderer.transform.InverseTransformPoint(startCenter);
            Vector3 endPos = useWorldSpace ? tongueTipPosition : lineRenderer.transform.InverseTransformPoint(tongueTipPosition);

            bool shouldSway = tongueState == TongueState.Extending || tongueState == TongueState.Retracting;
            if (shouldSway && swayAmplitude > 0f)
            {
                lineRenderer.positionCount = 3;
                float sway = Mathf.Sin(Time.time * swayFrequency) * swayAmplitude;
                Vector3 swayDir = Vector3.Cross(tongueDirection, Vector3.up).normalized;
                if (swayDir.sqrMagnitude < 0.01f) swayDir = transform.right;
                Vector3 midPos = Vector3.Lerp(startPos, endPos, 0.5f) + swayDir * sway;
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, midPos);
                lineRenderer.SetPosition(2, endPos);
            }
            else
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
            }
        }
        else lineRenderer.enabled = false;
    }

    private void UpdateTongueTipOnObject(Collider objCol)
    {
        if (objCol == null || tongueTipObject == null) return;
        Vector3 playerPos = GetTongueOrigin();
        Vector3 surfacePoint = objCol.ClosestPoint(playerPos);
        Vector3 toPlayer = playerPos - surfacePoint;
        if (toPlayer.sqrMagnitude > 0.0001f) surfacePoint += toPlayer.normalized * tongueTipRadius;
        tongueTipPosition = surfacePoint;
        tongueTipObject.transform.position = tongueTipPosition;
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        // Rely purely on the Frog's cached memory, not the live Player script
        Vector3 dirVec = cachedFacingDirection;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }

    private Vector3 CalculateHookPoint(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        // Rely purely on the Frog's cached memory, not the live Player script
        Vector3 facingDir = cachedFacingDirection;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Collider targetCollider = target.GetComponent<Collider>();
        
        if (targetCollider != null)
        {
            float maxDistance = Vector3.Distance(transform.position, target.position) + 10f;
            
            // PRIMARY
            if (Physics.Raycast(rayStart, facingDir, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore) && hit.collider == targetCollider)
            {
                Vector3 toHit = hit.point - transform.position;
                Vector3 toTarget = target.position - transform.position;
                if (Vector3.Dot(toHit.normalized, facingDir) > 0.7f && toHit.magnitude <= toTarget.magnitude + 2f) return hit.point;
            }
            
            // FALLBACK 1
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, facingDir, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore) && hit.collider == targetCollider)
            {
                Vector3 toHit = hit.point - transform.position;
                Vector3 toTarget = target.position - transform.position;
                if (Vector3.Dot(toHit.normalized, facingDir) > 0.7f && toHit.magnitude <= toTarget.magnitude + 2f) return hit.point;
            }
            
            // FALLBACK 2
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            if (Vector3.Distance(transform.position, closestPoint) <= Vector3.Distance(transform.position, target.position)) return closestPoint;
            
            // FALLBACK 3
            Vector3 playerPos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPos2D = new Vector3(target.position.x, 0, target.position.z);
            float projection = Vector3.Dot(targetPos2D - playerPos2D, new Vector3(facingDir.x, 0, facingDir.z));
            Vector3 closestPointOnLine = playerPos2D + new Vector3(facingDir.x, 0, facingDir.z) * projection;
            closestPointOnLine.y = transform.position.y;
            return targetCollider.ClosestPoint(closestPointOnLine);
        }
        return target.position;
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && _spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
#endif
        GetPullBoxTransform(out Vector3 pullBoxWorldCenter_Gizmo, out Quaternion commonRot_Gizmo);
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position + commonRot_Gizmo * grappleBoxCenter, commonRot_Gizmo, grappleBoxSize);
        Gizmos.color = Color.yellow; Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = Matrix4x4.TRS(pullBoxWorldCenter_Gizmo, commonRot_Gizmo, pullBoxSize);
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMatrix;
        
        if (Player.Instance != null)
        {
            Vector3 facingDir = Player.Instance.AnimationBasedFacingDirection;
            Vector3 facingStart = transform.position + Vector3.up * 0.5f;
            Vector3 facingEnd = facingStart + facingDir * 2.0f;
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(facingStart, facingEnd);
            Gizmos.DrawLine(facingEnd, facingEnd + Quaternion.Euler(0, 30, 0) * -facingDir * 0.5f);
            Gizmos.DrawLine(facingEnd, facingEnd + Quaternion.Euler(0, -30, 0) * -facingDir * 0.5f);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(facingStart, facingDir * 10f);
        }

        if (tongueState != TongueState.Idle)
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(tongueTipPosition, tongueTipRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); Gizmos.DrawWireSphere(transform.position, tongueMaxDistance);
        }

        if (tongueState == TongueState.PullRetracting && tongueHitTarget != null && player != null)
        {
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(player.position, 0.3f);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, MIN_PULL_DISTANCE);
            Gizmos.color = Vector3.Distance(tongueHitTarget.position, player.position) > MIN_PULL_DISTANCE ? Color.green : Color.red;
            Gizmos.DrawLine(player.position, tongueHitTarget.position);
        }
        
        Vector3 groundCheckPosition = transform.position - Vector3.up * groundCheckDistance;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPosition, groundCheckRadius);
        Gizmos.DrawLine(transform.position, groundCheckPosition);
    }
    #endregion
}