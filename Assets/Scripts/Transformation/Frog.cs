using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

public class Frog : FormScript
{
    protected override float baseSpeed { get; set; } = 6.0f;
    public float jumpForce = 6.0f;
    private bool isGrounded = true;

    [Tooltip("The time in seconds before the frog can jump again.")]
    [SerializeField] private float jumpCooldown = 0.15f;
    private float nextJumpTime = 0f;

    [Header("Coyote Time")]
    [Tooltip("Grace period after leaving the ground during which the frog can still jump.")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter = 0f;

    [Header("Ground Check")]

    [SerializeField] private LayerMask groundLayer; 
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [Tooltip("Vertical offset from player position to check for ground.")]
    [SerializeField] private float yOffset = 0.2f;

    [Tooltip("How fast the player can be moving vertically and still be able to jump.")]
    [SerializeField] private float verticalVelocityThreshold = 0.1f;

    [Header("Pullable Box Settings")]
    [Tooltip("Center (local) of Pullable detection box")]
    [SerializeField] private Vector3 pullBoxCenter = new Vector3(0f, 0.5f, 2f);
    [Tooltip("Full size of Pullable detection box (width, height, depth)")]
    [SerializeField] private Vector3 pullBoxSize = new Vector3(2f, 1f, 4f);

    [Header("Grapple Box Settings")]
    [Tooltip("Center (local) of Grapple detection box. Relative to player facing direction.")]
    [SerializeField] private Vector3 grappleBoxCenter = new Vector3(0f, 1.0f, 3f);
    [Tooltip("Full size of Grapple detection box. Height & Width are ~2x pullBox's.")]
    [SerializeField] private Vector3 grappleBoxSize = new Vector3(4f, 2f, 6f);

    private Interactable highlightedObject;
    private Transform closestObject;

    [Header("Tongue Settings")]
    [Tooltip("The icon to display over hookable/pullable targets.")]
    [SerializeField] private GameObject hookTarget;
    [Tooltip("Speed at which the tongue tip extends outward.")]
    [SerializeField] private float tongueExtendSpeed = 45f;
    [Tooltip("Speed at which the tongue retracts back to the player.")]
    [SerializeField] private float tongueRetractSpeed = 25f;
    [Tooltip("Maximum distance the tongue can reach before auto-retracting.")]
    [SerializeField] private float tongueMaxDistance = 12f;
    [Tooltip("Radius of the sphere collider at the tongue tip.")]
    [SerializeField] private float tongueTipRadius = 0.24f;
    [Tooltip("Peak speed at which the frog reels toward a Hookable target. Does not affect Pullable objects.")]
    [SerializeField] private float grappleReelSpeed = 20f;
    [Tooltip("Speed at which a Pullable object is dragged back during tongue retract.")]
    [SerializeField] private float pullRetractSpeed = 25f;
    [Tooltip("Maximum time in seconds the pull can last before auto-releasing.")]
    [SerializeField] private float pullTimeout = 3f;
    [Tooltip("Layers the tongue tip can collide with. Exclude ground layer if needed.")]
    [SerializeField] private LayerMask tongueHitLayers = ~0;

    [Header("Tongue Easing")]
    [Tooltip("Speed curve for extending. X = normalized distance (0-1), Y = speed multiplier (0–1). Ramps up fast, eases at end.")]
    [SerializeField] private AnimationCurve tongueExtendCurve = new AnimationCurve(
        new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0.3f));
    [Tooltip("Speed curve for retracting (empty). X = normalized distance (0=start, 1=arrived), Y = speed multiplier.")]
    [SerializeField] private AnimationCurve tongueRetractCurve = new AnimationCurve(
        new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.2f));
    [Tooltip("Speed curve for grapple reeling (Hookable targets only). X = normalized progress toward target (0=start, 1=arrived), Y = speed multiplier. Quick snap to peak, slight ease at landing.")]
    [SerializeField] private AnimationCurve grappleReelCurve = new AnimationCurve(
        new Keyframe(0f, 0.25f), new Keyframe(0.12f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.45f));

    [Header("Tongue Line")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private bool useWorldSpace = true;

    [Header("Unstick Minigame")]
    [Tooltip("Maximum fill value of the unstick bar.")]
    [SerializeField] private float unstickBarMax = 100f;
    [Tooltip("How much the bar fills per button press.")]
    [SerializeField] private float unstickFillPerPress = 25f;
    [Tooltip("Assign a UI Slider to display the unstick bar (same slot as Bulldozer's stamina slider).")]
    [SerializeField] private Slider unstickSlider;
    [Tooltip("(Optional) Canvas Group of the unstick bar UI for alpha control.")]
    [SerializeField] private CanvasGroup unstickCanvasGroup;
    [Tooltip("Rate at which the unstick bar drains per second when above 0.")]
    [SerializeField] private float unstickBarDrainRate = 15f;
    [Tooltip("Screen-space UI object shown during the minigame. Positioned in screen space above the unstickable object.")]
    [SerializeField] private GameObject unstickPrompt;
    [Tooltip("World-space vertical offset above the unstickable object's collider top used when placing the unstick prompt.")]
    [SerializeField] private float unstickPromptYOffset = 0.5f;

    [Header("Tongue Sway")]
    [Tooltip("How far the tongue midpoint sways perpendicular to its direction while extending/retracting.")]
    [SerializeField] private float swayAmplitude = 0.12f;
    [Tooltip("Speed of the oscillation while the tongue is in motion.")]
    [SerializeField] private float swayFrequency = 8f;

    private SpriteRenderer _spriteRenderer;

    // Unstick minigame state
    private float currentUnstickProgress = 0f;
    private bool unstickButtonPressed = false;

    //event raise channel for abilities
    public static event Action<Transformation, int, Interactable> AbilityUsed;

    // Tongue state machine
    private enum TongueState { Idle, Extending, Retracting, PullRetracting, GrappleReeling, UnstickMinigame, UnstickRetracting }
    private TongueState tongueState = TongueState.Idle;
    private Vector3 tongueTipPosition;
    private Vector3 tongueDirection;
    // Cached facing direction from the last idle frame — insulates tongue firing from
    // mid-animation sprite transitions that can briefly read as "BackRight" (Vector3.right).
    private Vector3 cachedFacingDirection = Vector3.forward;
    private GameObject tongueTipObject;
    private Transform tongueHitTarget;
    private Coroutine tongueCoroutine;
    private bool originalKinematicState = false;
    private const float MIN_PULL_DISTANCE = 1.5f;

    private bool canJumpLock = true;

    // Single source of truth for calculating hook/tongue attachment point
    private Vector3 CalculateHookPoint(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        Vector3 facingDir = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : Vector3.forward;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            // Use a longer distance to ensure we can hit thin objects
            float maxDistance = Vector3.Distance(transform.position, target.position) + 10f;
            RaycastHit hit;
            
            // PRIMARY: Raycast from player in facing direction
            if (Physics.Raycast(rayStart, facingDir, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore) 
                && hit.collider == targetCollider)
            {
                // Validate: hit point should be between player and object center (not beyond)
                Vector3 toHit = hit.point - transform.position;
                Vector3 toTarget = target.position - transform.position;
                
                // Check if hit is in front of us and not too far beyond target
                if (Vector3.Dot(toHit.normalized, facingDir) > 0.7f && toHit.magnitude <= toTarget.magnitude + 2f)
                {
                    return hit.point; // Valid hit on near side
                }
            }
            
            // FALLBACK 1: Try from slightly different height (might help with angles)
            Vector3 rayStartLower = transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(rayStartLower, facingDir, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider == targetCollider)
            {
                Vector3 toHit = hit.point - transform.position;
                Vector3 toTarget = target.position - transform.position;
                
                if (Vector3.Dot(toHit.normalized, facingDir) > 0.7f && toHit.magnitude <= toTarget.magnitude + 2f)
                {
                    return hit.point;
                }
            }
            
            // FALLBACK 2: Get closest point on surface facing the player
            // This ensures we get the near side, not far side
            Vector3 playerPos = transform.position;
            Vector3 closestPoint = targetCollider.ClosestPoint(playerPos);
            
            // Validate this is on the player-facing side by checking if it's closer than center
            float distToClosest = Vector3.Distance(playerPos, closestPoint);
            float distToCenter = Vector3.Distance(playerPos, target.position);
            
            if (distToClosest <= distToCenter)
            {
                return closestPoint; // This is on the near side
            }
            
            // FALLBACK 3: Project onto facing ray and get closest point
            Vector3 playerPos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPos2D = new Vector3(target.position.x, 0, target.position.z);
            Vector3 toTarget2D = (targetPos2D - playerPos2D);
            
            float projection = Vector3.Dot(toTarget2D, new Vector3(facingDir.x, 0, facingDir.z));
            Vector3 closestPointOnLine = playerPos2D + new Vector3(facingDir.x, 0, facingDir.z) * projection;
            closestPointOnLine.y = transform.position.y;
            
            return targetCollider.ClosestPoint(closestPointOnLine);
        }
        
        return target.position;
    }

    // Jump and ground detection

    public override void Awake()
    {
        base.Awake();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("Frog script requires a SpriteRenderer component on a child GameObject or the same GameObject.");
        }

        if (hookTarget != null)
        {
            hookTarget.SetActive(false);
        }

        if (unstickPrompt != null)
            unstickPrompt.SetActive(false);

        if (unstickSlider != null)
        {
            unstickSlider.maxValue = unstickBarMax;
            unstickSlider.value = 0f;
        }
        if (unstickCanvasGroup != null)
            unstickCanvasGroup.alpha = 0f;

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = useWorldSpace;
        }
        lineRenderer.positionCount = 3;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.45f, 0.55f, 0.9f);
        lineRenderer.material = mat;
        lineRenderer.startColor = new Color(1f, 0.45f, 0.55f, 0.9f);
        lineRenderer.endColor = new Color(0.9f, 0.35f, 0.45f, 0.95f);
        lineRenderer.widthMultiplier = 0.35f;
        lineRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.85f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.75f, 0.85f),
            new Keyframe(1f, 0.6f));
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;

        EnsureTongueTip();
    }

    /// <summary>Creates (or re-creates) the tongue tip sphere if it has been destroyed (e.g. scene reset).</summary>
    private void EnsureTongueTip()
    {
        if (tongueTipObject != null) return;

        tongueTipObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tongueTipObject.name = "TongueTip";
        tongueTipObject.transform.localScale = Vector3.one * tongueTipRadius * 2f;
        // Remove the default collider — detection is done via SphereCast
        var tipCollider = tongueTipObject.GetComponent<Collider>();
        if (tipCollider != null) Destroy(tipCollider);
        // Style: bulbous pink flesh-tone sphere matching the tongue line renderer
        var tipRenderer = tongueTipObject.GetComponent<Renderer>();
        if (tipRenderer != null)
        {
            var tipMat = new Material(Shader.Find("Sprites/Default"));
            tipMat.color = new Color(0.9f, 0.3f, 0.42f, 1f);
            tipRenderer.material = tipMat;
        }
        // Keep it alive across scene reloads (Player is DontDestroyOnLoad)
        DontDestroyOnLoad(tongueTipObject);
        tongueTipObject.SetActive(false);
    }

    public override void OnEnable()
    {
        EventDispatcher.AddListener<TogglePlayerMovement>(ToggleJump);
        currentUnstickProgress = 0f;
        unstickButtonPressed = false;
        if (unstickCanvasGroup != null)
            unstickCanvasGroup.alpha = 0f;
        if (unstickPrompt != null)
            unstickPrompt.SetActive(false);
        base.OnEnable();
    }

    public void OnDisable()
    {
        if (highlightedObject != null)
            highlightedObject.IsHighlighted = false;
        highlightedObject = null;
        closestObject = null;
        
        // Stop any ongoing tongue action
        StopTongue();
        EventDispatcher.RemoveListener<TogglePlayerMovement>(ToggleJump);
    }

    void ToggleJump(TogglePlayerMovement set)
    {
        canJumpLock = set.isEnabled;
    }
    
    // *** Detects collision with the tongue's grapple target ***
    private void OnCollisionEnter(Collision collision)
    {
        if (tongueState == TongueState.GrappleReeling && tongueHitTarget != null
            && collision.transform == tongueHitTarget)
        {
            StopTongue();
        }
    }

    private void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        #endif

        GetPullBoxTransform(out Vector3 pullBoxWorldCenter_Gizmo, out Quaternion commonRot_Gizmo);

        Matrix4x4 oldMatrix = Gizmos.matrix;

        // 1) Box for Grappleable (Yellow)
        Vector3 grappleBoxWorldCenter_Gizmo = transform.position + commonRot_Gizmo * grappleBoxCenter;
        Gizmos.matrix = Matrix4x4.TRS(grappleBoxWorldCenter_Gizmo, commonRot_Gizmo, grappleBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // 2) Box for Pullable (Cyan)
        Gizmos.matrix = Matrix4x4.TRS(pullBoxWorldCenter_Gizmo, commonRot_Gizmo, pullBoxSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        Gizmos.matrix = oldMatrix;
        
        // 3) Draw facing direction for debugging (White arrow)
        if (Player.Instance != null)
        {
            Vector3 facingDir = Player.Instance.AnimationBasedFacingDirection;
            Gizmos.color = Color.white;
            Vector3 facingStart = transform.position + Vector3.up * 0.5f;
            Vector3 facingEnd = facingStart + facingDir * 2.0f;
            Gizmos.DrawLine(facingStart, facingEnd);
            // Draw arrow head for facing direction
            Vector3 facingRight = Quaternion.Euler(0, 30, 0) * -facingDir * 0.5f;
            Vector3 facingLeft = Quaternion.Euler(0, -30, 0) * -facingDir * 0.5f;
            Gizmos.DrawLine(facingEnd, facingEnd + facingRight);
            Gizmos.DrawLine(facingEnd, facingEnd + facingLeft);
            
            // Draw extended raycast line (orange) to show tongue direction
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(facingStart, facingDir * 10f);
        }

        // 4) Visualize tongue tip and max range
        if (tongueState != TongueState.Idle)
        {
            // Draw tongue tip position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tongueTipPosition, tongueTipRadius);

            // Draw tongue max range circle
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, tongueMaxDistance);
        }

        // Visualize pull retract target
        if (tongueState == TongueState.PullRetracting && tongueHitTarget != null && player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, 0.3f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, MIN_PULL_DISTANCE);

            float currentDist = Vector3.Distance(tongueHitTarget.position, player.position);
            Gizmos.color = currentDist > MIN_PULL_DISTANCE ? Color.green : Color.red;
            Gizmos.DrawLine(player.position, tongueHitTarget.position);
        }
        
        // 5) Visualize ground check sphere
        Vector3 groundCheckPosition = transform.position - Vector3.up * groundCheckDistance;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPosition, groundCheckRadius);
        
        // Draw line from player to check position
        Gizmos.DrawLine(transform.position, groundCheckPosition);
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
        if (context.performed) // Button pressed (started)
        {
            Jump();
            AbilityUsed?.Invoke(Transformation.FROG, 1, null);
        }
        else if (context.canceled) // Button released
        {

        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // In unstick minigame: each press fills the bar instead of cancelling
            if (tongueState == TongueState.UnstickMinigame)
            {
                unstickButtonPressed = true;
                return;
            }

            // If tongue is already active, cancel gracefully (retract back to player)
            if (tongueState != TongueState.Idle)
            {
                CancelTongueGracefully();
                return;
            }

            Interactable intr = closestObject != null ? closestObject.GetComponent<Interactable>() : null;
            AbilityUsed?.Invoke(Transformation.FROG, 2, intr);
            FireTongue();
            EventDispatcher.Raise<StressAbility>(new StressAbility());
        }
    }

    private void Jump()
    {
        if (!canJumpLock) return;
        
        // Vertical velocity check should only apply when grounded, not during coyote time
        // (coyote time allows jumping even when falling)
        bool isVerticallyStationary = Mathf.Abs(rb.velocity.y) < verticalVelocityThreshold;
        bool canJumpFromGround = isGrounded && isVerticallyStationary;
        bool canJumpFromCoyote = coyoteTimeCounter > 0f && !isGrounded;
        
        bool canJump = 
            (canJumpFromGround || canJumpFromCoyote) && // 1. On ground with no vertical movement OR in coyote time window.
            Time.time >= nextJumpTime &&                 // 2. Cooldown must have expired.
            !Player.Instance.TransformationChecker();    // 3. Not currently transforming.

        // If any of the above conditions are false, we can't jump.
        if (!canJump)
        {
            return;
        }

        // Consume coyote time when jumping
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
        // Check for ground using a simple overlap sphere at the frog's feet
        Vector3 groundCheckPosition = transform.position - Vector3.up * groundCheckDistance;

        // Simple overlap check - is there ground where the frog's feet are?
        bool hitGround = Physics.CheckSphere(
            groundCheckPosition,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        bool fallingOrStill = rb.velocity.y <= 0f;
        bool wasGrounded = isGrounded;
        isGrounded = hitGround && fallingOrStill;

        // Update coyote time counter
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime; // Reset coyote time when grounded
            Player.Instance?.SetGroundedState(true);
            Player.Instance?.SetJumpingState(false);
            Debug.DrawLine(transform.position, groundCheckPosition, Color.green);
        }
        else
        {
            // Only decrement coyote time if we just left the ground (not during a jump)
            if (wasGrounded && !Player.Instance.IsJumping)
            {
                // Just left the ground naturally (walked off edge)
                // Coyote time counter will be > 0, allowing a jump
            }
            else if (Player.Instance.IsJumping)
            {
                // During a jump, immediately consume coyote time
                coyoteTimeCounter = 0f;
            }
            
            // Decrement coyote time
            coyoteTimeCounter -= Time.fixedDeltaTime;
            if (coyoteTimeCounter < 0f) coyoteTimeCounter = 0f;
            
            Player.Instance?.SetGroundedState(false);
            Debug.DrawLine(transform.position, groundCheckPosition, Color.red);
        }

    }

    private void Update()
    {
        // Don't detect new objects while tongue is active
        if (tongueState == TongueState.Idle)
        {
            // Cache facing direction only while idle so mid-animation sprite transitions
            // (which can briefly produce incorrect directions) never affect the next tongue fire.
            if (Player.Instance != null)
            {
                Vector3 dir = Player.Instance.AnimationBasedFacingDirection;
                if (dir.sqrMagnitude > 0.01f)
                    cachedFacingDirection = dir;
            }
            DetectAndHighlightObjects();
        }
        UpdateTongueLine();
    }

    private void UpdateTongueLine()
    {
        // Line renderer follows tongue tip position in all active states
        if (tongueState != TongueState.Idle)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter = GetComponentInChildren<Collider>()?.bounds.center ?? transform.position;

            Vector3 startPos = useWorldSpace ? startCenter : lineRenderer.transform.InverseTransformPoint(startCenter);
            Vector3 endPos = useWorldSpace ? tongueTipPosition : lineRenderer.transform.InverseTransformPoint(tongueTipPosition);

            // Sway: sinusoidal midpoint offset during extend/retract; taut during grapple/pull
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
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void DetectAndHighlightObjects()
    {
        GetPullBoxTransform(out Vector3 pullBoxWorldCenter, out Quaternion commonWorldRot);
        
        // Debug: Log the facing direction being used
        if (Player.Instance != null)
        {
            Vector3 facingDir = Player.Instance.AnimationBasedFacingDirection;
        }

        Vector3 grappleBoxWorldCenter = transform.position + commonWorldRot * grappleBoxCenter;
        Vector3 grappleHalfExtents = grappleBoxSize * 0.5f;
        Collider[] hookCols = Physics.OverlapBox(grappleBoxWorldCenter, grappleHalfExtents, commonWorldRot);

        var hookables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Hookable"));

        Vector3 pullHalfExtents = pullBoxSize * 0.5f;
        Collider[] pullCols = Physics.OverlapBox(pullBoxWorldCenter, pullHalfExtents, commonWorldRot);

        var pullables = pullCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Pullable"));

        var unstickables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Unstickable"));

        var all = hookables.Concat(pullables).Concat(unstickables);
        var nearest = all
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

        if (tongueState == TongueState.PullRetracting && tongueHitTarget == nearest?.transform) return;

        if (nearest != highlightedObject)
        {
            if (highlightedObject != null)
                highlightedObject.IsHighlighted = false;

            if (nearest != null)
            {
                nearest.IsHighlighted = true;
                closestObject = nearest.transform;
            }
            else
            {
                closestObject = null;
            }
            highlightedObject = nearest;
        }

        // Handle HookTarget visibility and position
        if (hookTarget != null)
        {
            Transform targetForIcon = null;

            // Prioritize showing the icon for any hookable object in range.
            var nearestHookable = hookables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
            if (nearestHookable != null)
            {
                targetForIcon = nearestHookable.transform;
            }
            else
            {
                // If no hookables, check for a valid pullable object.
                var nearestPullable = pullables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
                if (nearestPullable != null)
                {
                    // Check if the pullable object is actually movable.
                    Collider objCol = nearestPullable.GetComponent<Collider>();
                    Collider plrCol = player.GetComponentInChildren<Collider>();
                    if (objCol != null && plrCol != null)
                    {
                        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.z);
                        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.z);
                        float minAllowedDistance = objR + plrR;

                        Vector3 currentFlatDir = player.position - nearestPullable.transform.position;
                        currentFlatDir.y = 0f;

                        if (currentFlatDir.magnitude > minAllowedDistance + 0.01f)
                        {
                            targetForIcon = nearestPullable.transform;
                        }
                    }
                }

                // If no pullables either, check for an unstickable object.
                if (targetForIcon == null)
                {
                    var nearestUnstickable = unstickables.OrderBy(i => Vector3.Distance(transform.position, i.transform.position)).FirstOrDefault();
                    if (nearestUnstickable != null)
                        targetForIcon = nearestUnstickable.transform;
                }
            }

            bool shouldShowHookTarget = targetForIcon != null && tongueState == TongueState.Idle;
            hookTarget.SetActive(shouldShowHookTarget);

            if (shouldShowHookTarget)
            {
                // Convert world-space hook point to screen position for overlay UI
                Vector3 worldPoint = CalculateHookPoint(targetForIcon);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPoint);

                hookTarget.transform.position = screenPos;
            }
        }
    }

    // ==================== TONGUE SYSTEM ====================

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
        // --- Setup ---
        // Use the cached idle-frame direction instead of reading AnimationBasedFacingDirection
        // live — the tongue animation may already be playing at this point, causing the
        // sprite-based direction to transiently read as the wrong cardinal (e.g. Vector3.right).
        Vector3 facingDir = cachedFacingDirection;

        // Trigger the animation based on the current facing direction BEFORE aim-assist
        // overwrites facingDir with a diagonal toward the target, which would never match
        // the cardinal-direction equality checks below.
        bool isFacingFront = facingDir == Vector3.left || facingDir == Vector3.back;
        // BackLeft = Vector3.forward, BackRight = Vector3.right (from UpdateAnimationBasedDirection)
        bool isFacingBack = facingDir == Vector3.forward || facingDir == Vector3.right;
        if (isFacingBack || isFacingFront)
        {
            animator?.SetTrigger("Tongue");
        }

        // Aim-assist: if a target is currently highlighted, aim at the same
        // point the hook target icon is placed (CalculateHookPoint) for consistency.
        if (closestObject != null)
        {
            Vector3 hookPoint = CalculateHookPoint(closestObject);
            Vector3 toTarget = hookPoint - GetTongueOrigin();
            if (toTarget.sqrMagnitude > 0.01f)
            {
                facingDir = toTarget.normalized;
            }
        }

        tongueState = TongueState.Extending;
        tongueDirection = facingDir;
        tongueTipPosition = GetTongueOrigin();
        tongueHitTarget = null;
        Player.Instance.canMoveToggle(false);

        // Lock onto the highlighted target before clearing it so the extend phase
        // can bypass any intervening colliders and connect directly.
        Transform lockedTarget = closestObject;

        highlightedObject = null;
        closestObject = null;
        if (hookTarget != null) hookTarget.SetActive(false);

        // Ensure tongue tip exists (may have been destroyed by a scene reset)
        EnsureTongueTip();

        // Show tongue tip sphere
        tongueTipObject.SetActive(true);
        tongueTipObject.transform.position = tongueTipPosition;

        // === EXTEND PHASE ===
        // Tongue tip sphere travels outward via SphereCast each physics step.
        float distanceTraveled = 0f;

		//Play sound
		PlayAbilitySound(ability2Sound);
        while (tongueState == TongueState.Extending)
        {
            // Evaluate extend curve: 0 at launch, 1 at max distance
            float extendT = Mathf.Clamp01(distanceTraveled / tongueMaxDistance);
            float speedMul = tongueExtendCurve.Evaluate(extendT);
            float step = tongueExtendSpeed * speedMul * Time.fixedDeltaTime;
            bool hitDetected = false;

            // SphereCast forward from tip to detect the first solid hit
            RaycastHit hitInfo;
            if (Physics.SphereCast(tongueTipPosition, tongueTipRadius, tongueDirection,
                out hitInfo, step, tongueHitLayers, QueryTriggerInteraction.Ignore))
            {
                // Ignore self / player colliders
                bool isSelf = hitInfo.transform.IsChildOf(transform) || hitInfo.transform == transform
                           || hitInfo.transform.IsChildOf(player) || hitInfo.transform == player;

                // If a target was locked when the tongue fired, ignore any collider that
                // isn't that target — the icon confirmed a clear line-of-intent to it.
                bool isBlocker = lockedTarget != null
                    && hitInfo.transform != lockedTarget
                    && !hitInfo.transform.IsChildOf(lockedTarget);

                if (!isSelf && !isBlocker)
                {
                    // Advance tip to the hit surface
                    tongueTipPosition += tongueDirection * hitInfo.distance;
                    hitDetected = true;

                    var intr = hitInfo.transform.GetComponent<Interactable>();
                    if (intr != null && intr.isInteractable && intr.HasProperty("Hookable"))
                    {
                        tongueHitTarget = hitInfo.transform;
                        tongueState = TongueState.GrappleReeling;
                    }
                    else if (intr != null && intr.isInteractable && intr.HasProperty("Pullable"))
                    {
                        tongueHitTarget = hitInfo.transform;
                        tongueState = TongueState.PullRetracting;
                    }
                    else if (intr != null && intr.isInteractable && intr.HasProperty("Unstickable"))
                    {
                        // Tongue reaches the sticker — pause for minigame before pulling.
                        AbilityUsed?.Invoke(Transformation.FROG, 2, intr);
                        tongueHitTarget = hitInfo.transform;
                        tongueState = TongueState.UnstickMinigame;
                    }
                    else
                    {
                        // Hit a wall or non-interactable solid — retract empty
                        tongueState = TongueState.Retracting;
                    }
                }
            }

            if (!hitDetected)
            {
                tongueTipPosition += tongueDirection * step;
                distanceTraveled += step;
            }

            tongueTipObject.transform.position = tongueTipPosition;

            // Max distance reached — begin retract
            if (distanceTraveled >= tongueMaxDistance && tongueState == TongueState.Extending)
            {
                tongueState = TongueState.Retracting;
            }

            yield return new WaitForFixedUpdate();
        }

        // === RETRACT PHASE (empty — no target hit) ===
        if (tongueState == TongueState.Retracting)
        {
            float retractStartDist = Vector3.Distance(tongueTipPosition, GetTongueOrigin());
            while (tongueState == TongueState.Retracting)
            {
                Vector3 origin = GetTongueOrigin();
                Vector3 toPlayer = origin - tongueTipPosition;
                float dist = toPlayer.magnitude;

                // Evaluate retract curve: 0 at start of retract, 1 when arriving
                float retractT = retractStartDist > 0.01f
                    ? Mathf.Clamp01(1f - dist / retractStartDist)
                    : 1f;
                float speedMul = tongueRetractCurve.Evaluate(retractT);
                float retractStep = tongueRetractSpeed * speedMul * Time.fixedDeltaTime;

                if (dist <= retractStep)
                {
                    tongueTipPosition = origin;
                    break;
                }

                tongueTipPosition += toPlayer.normalized * retractStep;
                tongueTipObject.transform.position = tongueTipPosition;
                yield return new WaitForFixedUpdate();
            }
        }

        // === GRAPPLE REEL PHASE (pull frog toward Hookable) ===
        else if (tongueState == TongueState.GrappleReeling && tongueHitTarget != null)
        {
            Collider hookCollider = tongueHitTarget.GetComponent<Collider>();

            // Arrival destination: slightly above the top of the collider so the frog
            // clears the edge rather than stopping flush with the surface.
            Vector3 ComputeArrivalPos()
            {
                if (hookCollider != null)
                    return new Vector3(hookCollider.bounds.center.x,
                                       hookCollider.bounds.max.y + 0.6f,
                                       hookCollider.bounds.center.z);
                return tongueHitTarget.position + Vector3.up * 1.1f;
            }

            float totalGrappleDist = Vector3.Distance(rb.position, ComputeArrivalPos());
            // Straight-line direction is fixed at launch so the path never curves.
            Vector3 reelDirection = totalGrappleDist > 0.01f
                ? (ComputeArrivalPos() - rb.position).normalized
                : Vector3.up;

            while (tongueState == TongueState.GrappleReeling && tongueHitTarget != null)
            {
                Vector3 arrivalPos = ComputeArrivalPos();

                // Keep tongue tip visually anchored to the near-side surface of the object.
                UpdateTongueTipOnObject(hookCollider);

                float currentDist = Vector3.Distance(rb.position, arrivalPos);
                // progress: 0 = just launched, 1 = arrived
                float progress = totalGrappleDist > 0.01f
                    ? Mathf.Clamp01(1f - currentDist / totalGrappleDist)
                    : 1f;
                float speedMul = grappleReelCurve.Evaluate(progress);

                // Straight-line velocity — no mid-flight steering.
                rb.velocity = reelDirection * grappleReelSpeed * speedMul;

                if (currentDist <= 1.5f)
                {
                    break;
                }

                yield return new WaitForFixedUpdate();
            }

            // Small upward pop so the frog clears the top edge and lands on the surface.
            rb.velocity = Vector3.up * 4f;
        }

        // === PULL RETRACT PHASE (retract tongue, dragging Pullable back) ===
        else if (tongueState == TongueState.PullRetracting && tongueHitTarget != null)
        {
            Rigidbody pullableRb = tongueHitTarget.GetComponent<Rigidbody>();
            Collider objCol = tongueHitTarget.GetComponent<Collider>();
            if (pullableRb != null)
            {
                originalKinematicState = pullableRb.isKinematic;
                // Keep as a physics body so it naturally collides with walls/objects
                // instead of clipping through or stopping abruptly.
                pullableRb.isKinematic = false;
                pullableRb.velocity = Vector3.zero;
                pullableRb.angularVelocity = Vector3.zero;
                // Freeze rotation so the object doesn't tumble during pull
                pullableRb.freezeRotation = true;
            }

            animator?.SetBool("isPulling", true);

            float massMul = 1f;

            // Capture the pull direction as a straight line from object to player (XZ only).
            Vector3 pullDest = GetTongueOrigin();
            Vector3 initialPullAxis = pullDest - tongueHitTarget.position;
            initialPullAxis.y = 0f;
            float totalPullDist = initialPullAxis.magnitude;
            Vector3 pullAxis = totalPullDist > 0.01f ? initialPullAxis / totalPullDist : Vector3.forward;

            // Keep tongue tip anchored to the object's near-side surface
            UpdateTongueTipOnObject(objCol);

            float pullElapsed = 0f;

            while (tongueState == TongueState.PullRetracting && tongueHitTarget != null)
            {
                // Timeout: auto-release if the pull takes too long (object stuck)
                pullElapsed += Time.fixedDeltaTime;
                if (pullElapsed >= pullTimeout)
                {
                    break;
                }
                Vector3 origin = GetTongueOrigin();
                Vector3 toPlayer = origin - tongueHitTarget.position;
                toPlayer.y = 0f;
                float remainingDist = toPlayer.magnitude;

                // Close enough to player — done
                if (remainingDist <= MIN_PULL_DISTANCE)
                {
                    break;
                }

                // Evaluate retract curve for easing, then scale by mass
                float retractT = totalPullDist > 0.01f
                    ? Mathf.Clamp01(1f - remainingDist / totalPullDist)
                    : 1f;
                float easeMul = tongueRetractCurve.Evaluate(retractT);
                float speed = pullRetractSpeed * easeMul * massMul;

                // Apply velocity along the pull axis — physics handles wall collisions
                if (pullableRb != null)
                {
                    Vector3 desiredVel = pullAxis * speed;
                    desiredVel.y = pullableRb.velocity.y; // preserve gravity
                    pullableRb.velocity = desiredVel;
                }

                pullElapsed += Time.fixedDeltaTime;

                // Keep tongue tip on the surface of the object facing the player
                UpdateTongueTipOnObject(objCol);

                // Stop if object collides with the player
                Collider plrCol = player.GetComponentInChildren<Collider>();
                if (objCol != null && plrCol != null)
                {
                    if (Physics.ComputePenetration(
                        objCol, objCol.transform.position, objCol.transform.rotation,
                        plrCol, plrCol.transform.position, plrCol.transform.rotation,
                        out Vector3 penDir, out float penDist))
                    {
                        break;
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Clean up pullable
            if (pullableRb != null)
            {
                pullableRb.velocity = Vector3.zero;
                pullableRb.angularVelocity = Vector3.zero;
                pullableRb.freezeRotation = false;
                pullableRb.isKinematic = originalKinematicState;
            }
            animator?.SetBool("isPulling", false);
            tongueHitTarget = null;

            // Retract tongue visually back to the player instead of vanishing
            tongueState = TongueState.Retracting;
            float retractStartDist2 = Vector3.Distance(tongueTipPosition, GetTongueOrigin());
            while (tongueState == TongueState.Retracting)
            {
                Vector3 retractOrigin = GetTongueOrigin();
                Vector3 toFrog = retractOrigin - tongueTipPosition;
                float dist = toFrog.magnitude;

                float retractT2 = retractStartDist2 > 0.01f
                    ? Mathf.Clamp01(1f - dist / retractStartDist2)
                    : 1f;
                float speedMul2 = tongueRetractCurve.Evaluate(retractT2);
                float retractStep2 = tongueRetractSpeed * speedMul2 * Time.fixedDeltaTime;

                if (dist <= retractStep2)
                {
                    tongueTipPosition = retractOrigin;
                    break;
                }

                tongueTipPosition += toFrog.normalized * retractStep2;
                if (tongueTipObject != null)
                    tongueTipObject.transform.position = tongueTipPosition;

                yield return new WaitForFixedUpdate();
            }
        }

        // === UNSTICK MINIGAME + RETRACT (tongue pauses on Unstickable; fill bar, then pull) ===
        else if (tongueState == TongueState.UnstickMinigame && tongueHitTarget != null)
        {
            Collider objCol = tongueHitTarget.GetComponent<Collider>();

            // --- Minigame phase: player presses the tongue button to fill the bar ---
            currentUnstickProgress = 0f;
            unstickButtonPressed = false;
            if (unstickSlider != null)
            {
                unstickSlider.maxValue = unstickBarMax;
                unstickSlider.value = 0f;
            }
            if (unstickCanvasGroup != null)
                unstickCanvasGroup.alpha = 1f;
            if (unstickPrompt != null)
                unstickPrompt.SetActive(true);

            while (tongueState == TongueState.UnstickMinigame && tongueHitTarget != null)
            {
                UpdateTongueTipOnObject(objCol);

                // Keep prompt positioned above the unstickable object in screen space
                if (unstickPrompt != null && Camera.main != null)
                {
                    Bounds b = objCol != null ? objCol.bounds : new Bounds(tongueHitTarget.position, Vector3.zero);
                    Vector3 worldTop = new Vector3(tongueHitTarget.position.x, b.max.y + unstickPromptYOffset, tongueHitTarget.position.z);
                    unstickPrompt.transform.position = Camera.main.WorldToScreenPoint(worldTop);
                }

                if (unstickButtonPressed)
                {
                    unstickButtonPressed = false;
                    currentUnstickProgress = Mathf.Min(currentUnstickProgress + unstickFillPerPress, unstickBarMax);
                }

                // Check completion before drain so a button press that reaches 100 isn't undone
                if (currentUnstickProgress >= unstickBarMax)
                {
                    if (unstickSlider != null)
                        unstickSlider.value = currentUnstickProgress;
                    break;
                }

                if (currentUnstickProgress > 0f)
                    currentUnstickProgress = Mathf.Max(0f, currentUnstickProgress - unstickBarDrainRate * Time.fixedDeltaTime);

                if (unstickSlider != null)
                    unstickSlider.value = currentUnstickProgress;

                yield return new WaitForFixedUpdate();
            }

            // Hide bar and prompt
            if (unstickCanvasGroup != null)
                unstickCanvasGroup.alpha = 0f;
            if (unstickSlider != null)
                unstickSlider.value = 0f;
            if (unstickPrompt != null)
                unstickPrompt.SetActive(false);

            // Only pull if bar was fully filled (not cancelled externally)
            if (tongueHitTarget != null && currentUnstickProgress >= unstickBarMax)
            {
                tongueState = TongueState.UnstickRetracting;

                Rigidbody unstickRb = tongueHitTarget.GetComponent<Rigidbody>();
                bool wasKinematic = false;
                if (unstickRb != null)
                {
                    wasKinematic = unstickRb.isKinematic;
                    unstickRb.isKinematic = false;
                    unstickRb.velocity = Vector3.zero;
                    unstickRb.angularVelocity = Vector3.zero;
                    unstickRb.freezeRotation = true;
                }

                Vector3 pullDest = GetTongueOrigin();
                Vector3 initialAxis = pullDest - tongueHitTarget.position;
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
                    float remainingDist = toPlayer.magnitude;

                    if (remainingDist <= MIN_PULL_DISTANCE) break;

                    float retractT = totalUnstickDist > 0.01f
                        ? Mathf.Clamp01(1f - remainingDist / totalUnstickDist)
                        : 1f;
                    float easeMul = tongueRetractCurve.Evaluate(retractT);
                    float speed = pullRetractSpeed * easeMul;
                    float step = speed * Time.fixedDeltaTime;

                    if (unstickRb != null)
                    {
                        Vector3 desiredVel = unstickAxis * speed;
                        desiredVel.y = unstickRb.velocity.y;
                        unstickRb.velocity = desiredVel;
                    }
                    else
                    {
                        // No Rigidbody — move transform directly
                        tongueHitTarget.position = Vector3.MoveTowards(
                            tongueHitTarget.position, origin, step);
                    }

                    UpdateTongueTipOnObject(objCol);

                    // Stop when the object physically overlaps the player
                    Collider plrCol = player.GetComponentInChildren<Collider>();
                    if (objCol != null && plrCol != null)
                    {
                        if (Physics.ComputePenetration(
                            objCol, objCol.transform.position, objCol.transform.rotation,
                            plrCol, plrCol.transform.position, plrCol.transform.rotation,
                            out Vector3 _, out float _))
                        {
                            break;
                        }
                    }

                    yield return new WaitForFixedUpdate();
                }

                // Arrived — destroy the sticker
                if (unstickRb != null)
                {
                    unstickRb.velocity = Vector3.zero;
                    unstickRb.angularVelocity = Vector3.zero;
                    unstickRb.freezeRotation = false;
                    unstickRb.isKinematic = wasKinematic;
                }
                if (tongueHitTarget != null)
                    Destroy(tongueHitTarget.gameObject);
                tongueHitTarget = null;

                // Retract tongue visually
                tongueState = TongueState.Retracting;
                float unstickRetractDist = Vector3.Distance(tongueTipPosition, GetTongueOrigin());
                while (tongueState == TongueState.Retracting)
                {
                    Vector3 retractOrigin = GetTongueOrigin();
                    Vector3 toFrog = retractOrigin - tongueTipPosition;
                    float dist = toFrog.magnitude;

                    float retractT = unstickRetractDist > 0.01f
                        ? Mathf.Clamp01(1f - dist / unstickRetractDist)
                        : 1f;
                    float speedMul = tongueRetractCurve.Evaluate(retractT);
                    float retractStep = tongueRetractSpeed * speedMul * Time.fixedDeltaTime;

                    if (dist <= retractStep)
                    {
                        tongueTipPosition = retractOrigin;
                        break;
                    }

                    tongueTipPosition += toFrog.normalized * retractStep;
                    if (tongueTipObject != null)
                        tongueTipObject.transform.position = tongueTipPosition;

                    yield return new WaitForFixedUpdate();
                }
            }
        }

        // === CLEANUP ===
        CleanupTongue();
    }

    /// <summary>Cancels the current tongue action, releases any held object, and retracts the tongue visually.</summary>
    private void CancelTongueGracefully()
    {
        if (tongueState == TongueState.Idle) return;
        // Already retracting — let it finish
        if (tongueState == TongueState.Retracting) return;

        // Stop the active coroutine
        if (tongueCoroutine != null)
        {
            StopCoroutine(tongueCoroutine);
            tongueCoroutine = null;
        }

        // Release pulled object if any
        if (tongueState == TongueState.PullRetracting && tongueHitTarget != null)
        {
            Rigidbody pullableRb = tongueHitTarget.GetComponent<Rigidbody>();
            if (pullableRb != null)
            {
                pullableRb.velocity = Vector3.zero;
                pullableRb.angularVelocity = Vector3.zero;
                pullableRb.freezeRotation = false;
                pullableRb.isKinematic = originalKinematicState;
            }
            animator?.SetBool("isPulling", false);
        }

        // Hide bar and release target if minigame was in progress (do not destroy the object)
        if (tongueState == TongueState.UnstickMinigame)
        {
            if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 0f;
            if (unstickSlider != null) unstickSlider.value = 0f;
            tongueHitTarget = null;
        }

        // Destroy unstickable if cancel interrupted mid-pull
        if (tongueState == TongueState.UnstickRetracting && tongueHitTarget != null)
        {
            Rigidbody unstickRb = tongueHitTarget.GetComponent<Rigidbody>();
            if (unstickRb != null)
            {
                unstickRb.velocity = Vector3.zero;
                unstickRb.angularVelocity = Vector3.zero;
                unstickRb.freezeRotation = false;
                unstickRb.isKinematic = originalKinematicState;
            }
            Destroy(tongueHitTarget.gameObject);
        }

        // Stop frog movement if grappling
        if (tongueState == TongueState.GrappleReeling)
        {
            rb.velocity = Vector3.zero;
        }

        tongueHitTarget = null;

        // Start retract animation
        tongueState = TongueState.Retracting;
        tongueCoroutine = StartCoroutine(RetractTongueCoroutine());
    }

    /// <summary>Animates the tongue tip back to the player, then cleans up.</summary>
    private IEnumerator RetractTongueCoroutine()
    {
        float retractStartDist = Vector3.Distance(tongueTipPosition, GetTongueOrigin());

        while (tongueState == TongueState.Retracting)
        {
            Vector3 origin = GetTongueOrigin();
            Vector3 toPlayer = origin - tongueTipPosition;
            float dist = toPlayer.magnitude;

            float retractT = retractStartDist > 0.01f
                ? Mathf.Clamp01(1f - dist / retractStartDist)
                : 1f;
            float speedMul = tongueRetractCurve.Evaluate(retractT);
            float retractStep = tongueRetractSpeed * speedMul * Time.fixedDeltaTime;

            if (dist <= retractStep)
            {
                tongueTipPosition = origin;
                break;
            }

            tongueTipPosition += toPlayer.normalized * retractStep;
            if (tongueTipObject != null)
                tongueTipObject.transform.position = tongueTipPosition;

            yield return new WaitForFixedUpdate();
        }

        CleanupTongue();
    }

    /// <summary>Force-stop the tongue from any state (used by OnDisable, external callers).</summary>
    private void StopTongue()
    {
        if (tongueState == TongueState.Idle) return;

        if (tongueCoroutine != null)
        {
            StopCoroutine(tongueCoroutine);
            tongueCoroutine = null;
        }

        // If we were pulling, restore the pulled object
        if (tongueState == TongueState.PullRetracting && tongueHitTarget != null)
        {
            Rigidbody pullableRb = tongueHitTarget.GetComponent<Rigidbody>();
            if (pullableRb != null)
            {
                pullableRb.velocity = Vector3.zero;
                pullableRb.angularVelocity = Vector3.zero;
                pullableRb.isKinematic = originalKinematicState;
            }
            animator?.SetBool("isPulling", false);
        }

        // Hide bar without destroying if minigame was active (player did not fill bar)
        if (tongueState == TongueState.UnstickMinigame)
        {
            if (unstickCanvasGroup != null) unstickCanvasGroup.alpha = 0f;
            if (unstickSlider != null) unstickSlider.value = 0f;
        }

        // Destroy unstickable on force-stop
        if (tongueState == TongueState.UnstickRetracting && tongueHitTarget != null)
        {
            Rigidbody unstickRb = tongueHitTarget.GetComponent<Rigidbody>();
            if (unstickRb != null)
            {
                unstickRb.velocity = Vector3.zero;
                unstickRb.angularVelocity = Vector3.zero;
                unstickRb.isKinematic = originalKinematicState;
            }
            Destroy(tongueHitTarget.gameObject);
        }

        if (tongueState == TongueState.GrappleReeling)
        {
            rb.velocity = Vector3.zero;
        }

        CleanupTongue();
    }

    private void CleanupTongue()
    {
        tongueState = TongueState.Idle;
        tongueHitTarget = null;
        if (tongueTipObject != null) tongueTipObject.SetActive(false);
        Player.Instance.canMoveToggle(true);
        tongueCoroutine = null;
    }

    /// <summary>
    /// Positions the tongue tip sphere on the player-facing surface of the pulled object's
    /// collider so the visual always looks attached, even on thin/flat objects like notecards.
    /// The sphere is offset outward by its radius so it sits flush against the surface
    /// instead of clipping through.
    /// </summary>
    private void UpdateTongueTipOnObject(Collider objCol)
    {
        if (objCol == null || tongueTipObject == null) return;

        Vector3 playerPos = GetTongueOrigin();
        // Get the closest point on the object's collider surface to the player
        Vector3 surfacePoint = objCol.ClosestPoint(playerPos);

        // Offset the sphere outward by its radius so it doesn't clip through thin objects.
        // The normal points from the surface toward the player.
        Vector3 toPlayer = playerPos - surfacePoint;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            surfacePoint += toPlayer.normalized * tongueTipRadius;
        }

        tongueTipPosition = surfacePoint;
        tongueTipObject.transform.position = tongueTipPosition;
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : Vector3.forward;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}