using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Frog : FormScript
{
    protected override float baseSpeed { get; set; } = 6.0f;
    public float jumpForce = 6.0f;
    private bool isGrounded = true;

    [Tooltip("The time in seconds before the frog can jump again.")]
    [SerializeField] private float jumpCooldown = 0.75f;
    private float nextJumpTime = 0f;

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

    [Header("Hook & Pull Settings")]
    [Tooltip("The icon to display over hookable targets.")]
    [SerializeField] private GameObject hookTarget;
    [Tooltip("Max duration/timeout for the grapple hook.")]
    [SerializeField] private float hookDuration = 2f; // Now acts as a timeout
    [Tooltip("The speed at which the frog moves towards the grapple point.")]
    [SerializeField] private float hookSpeed = 30f; // Speed of the grapple pull
    [SerializeField] private float pullSpeed = 5f; // Max speed at which the object is pulled.
    [Tooltip("Defines how the pull speed accelerates over time while holding the button.")]
    [SerializeField] private AnimationCurve pullAccelerationCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1);

    [Header("Grapple Line")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private bool useWorldSpace = true;

    private SpriteRenderer _spriteRenderer;

    // State variables for pull and grapple
    private bool isPulling = false;
    private Transform currentPullObject;
    private float pullElapsedTime = 0f;
    private Vector3 pullTargetPoint; // Single source of truth: the point we're pulling toward
    private float lastDistanceToTarget = 0f; // Track distance to hook point
    private float stuckTime = 0f;
    private const float STUCK_TIMEOUT = 0.3f;
    private const float MIN_PULL_DISTANCE = 2.0f; // Minimum distance to stop pulling

    private bool isGrappling = false;
    private Transform grappleTarget;
    private Coroutine grappleCoroutine;

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

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = useWorldSpace;
        }
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0f, 0f, 0.75f);
        lineRenderer.material = mat;
        lineRenderer.startColor = new Color(1f, 0f, 0f, 0.75f);
        lineRenderer.endColor = new Color(1f, 0f, 0f, 0.75f);
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth = 0.25f;
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnDisable()
    {
        if (highlightedObject != null)
            highlightedObject.IsHighlighted = false;
        highlightedObject = null;
        closestObject = null;
        
        // Stop any ongoing actions
        StopPullingObject();
        if (isGrappling) 
        {
            StopGrapple();
        }
    }
    
    // *** Detects collision with the grapple target ***
    private void OnCollisionEnter(Collision collision)
    {
        if (isGrappling && collision.transform == grappleTarget)
        {
            StopGrapple();
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

        // 4) Visualize pull distance calculation when pulling
        if (isPulling && currentPullObject != null && player != null)
        {
            // Draw player position reference (Magenta sphere)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, 0.3f);

            // Draw yellow sphere at pull target point (where tongue attaches)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pullTargetPoint, 0.25f);

            // Draw minimum distance boundary (Cyan wire sphere)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, MIN_PULL_DISTANCE);

            // Draw line from player to target point (Green = safe, Red = too close)
            float currentDist = Vector3.Distance(pullTargetPoint, player.position);
            Gizmos.color = currentDist > MIN_PULL_DISTANCE ? Color.green : Color.red;
            Gizmos.DrawLine(player.position, pullTargetPoint);

            // Draw pull direction arrow (Blue) - from hook point toward player
            Vector3 pullDir = (player.position - pullTargetPoint).normalized;
            pullDir.y = 0;
            Gizmos.color = Color.blue;
            Vector3 arrowStart = pullTargetPoint; // Start from hook point, not object center
            Vector3 arrowEnd = arrowStart + pullDir * 1.0f;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            // Draw arrow head
            Vector3 arrowRight = Quaternion.Euler(0, 30, 0) * -pullDir * 0.3f;
            Vector3 arrowLeft = Quaternion.Euler(0, -30, 0) * -pullDir * 0.3f;
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowRight);
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowLeft);
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
        }
        else if (context.canceled) // Button released
        {

        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        if (closestObject == null || isGrappling) return; // Prevent new actions while grappling

        var intr = closestObject.GetComponent<Interactable>();
        if (intr == null) return;

        if (context.performed) // Button pressed (started)
        {
            if (intr.HasProperty("Hookable"))
            {
                GrapplingHook(closestObject);
            }
            else if (intr.HasProperty("Pullable"))
            {
                StartPullingObject(closestObject);
            }
            EventDispatcher.Raise<StressAbility>(new StressAbility());
        }
        else if (context.canceled) // Button released
        {
            StopPullingObject();
        }
    }

    private void Jump()
    {
        bool isVerticallyStationary = Mathf.Abs(rb.velocity.y) < verticalVelocityThreshold;
        
        bool canJump = 
            isGrounded &&                // 1. Raycast must hit the ground.
            isVerticallyStationary &&    // 2. Must not be rising or falling.
            Time.time >= nextJumpTime && // 3. Cooldown must have expired.
            !Player.Instance.TransformationChecker(); // 4. Not currently transforming.

        // If any of the above conditions are false, we can't jump.
        if (!canJump)
        {
            return;
        }

        nextJumpTime = Time.time + jumpCooldown;

        isGrounded = false;
        Player.Instance?.SetGroundedState(false);
        Player.Instance?.SetJumpingState(true);
        Player.Instance?.RegisterAirborneImpulse();
        animator?.SetTrigger("Jump");
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
        isGrounded = hitGround && fallingOrStill;

        if (isGrounded)
        {
            Player.Instance?.SetGroundedState(true);
            Player.Instance?.SetJumpingState(false);
            Debug.DrawLine(transform.position, groundCheckPosition, Color.green);
        }
        else
        {
            Player.Instance?.SetGroundedState(false);
            Debug.DrawLine(transform.position, groundCheckPosition, Color.red);
        }

        if (isPulling && currentPullObject != null)
        {
            pullElapsedTime += Time.fixedDeltaTime;
            ApplyContinuousPull();
        }
    }

    private void Update()
    {
        // Don't detect new objects while grappling
        if (!isGrappling)
        {
            DetectAndHighlightObjects();
        }
        UpdateGrappleLine();
    }

    private void UpdateGrappleLine()
    {
        // Show line if pulling OR grappling
        if (isPulling || isGrappling)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter = GetComponentInChildren<Collider>()?.bounds.center ?? transform.position;

            // Determine target for the line
            Transform currentTarget = null;
            if (isGrappling)
                currentTarget = grappleTarget;
            else if (isPulling)
                currentTarget = currentPullObject;

            if (currentTarget != null)
            {
                Vector3 endPoint;

                if (isGrappling)
                {
                    // For grappling, aim for the center
                    Collider targetCollider = currentTarget.GetComponent<Collider>();
                    endPoint = targetCollider?.bounds.center ?? currentTarget.position;
                }
                else // isPulling
                {
                    // Use single source of truth for pull target
                    endPoint = pullTargetPoint;
                }

                Vector3 startPos = useWorldSpace ? startCenter : lineRenderer.transform.InverseTransformPoint(startCenter);
                Vector3 endPos = useWorldSpace ? endPoint : lineRenderer.transform.InverseTransformPoint(endPoint);

                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
            }
            else
            {
                lineRenderer.enabled = false;
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

        var all = hookables.Concat(pullables);
        var nearest = all
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

        if (isPulling && currentPullObject == nearest?.transform) return;

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
            }

            bool shouldShowHookTarget = targetForIcon != null && !isGrappling && !isPulling;
            hookTarget.SetActive(shouldShowHookTarget);

            if (shouldShowHookTarget)
            {
                // Use single source of truth for hook point calculation
                hookTarget.transform.position = CalculateHookPoint(targetForIcon);
            }
        }
    }

    private void GrapplingHook(Transform t)
    {
        grappleCoroutine = StartCoroutine(GrapplingHookCoroutine(t));
    }

    private IEnumerator GrapplingHookCoroutine(Transform t)
    {
        if (isGrappling) yield break; // Exit if already grappling

        animator?.SetTrigger("Tongue");

        // --- Setup Phase ---
        isGrappling = true;
        grappleTarget = t;
        Player.Instance.canMoveToggle(false);
        float elapsedTime = 0f;

        // Make sure the grapple target stays highlighted by clearing other potential targets
        highlightedObject = null;
        closestObject = null;
        if (hookTarget != null)
        {
            hookTarget.SetActive(false);
        }

        // --- Loop Phase ---
        // This loop constantly pulls the frog towards the target until a collision or timeout.
        while (isGrappling)
        {
            // 1. Check for timeout
            if (elapsedTime >= hookDuration)
            {
                Debug.LogWarning("Grapple timed out.");
                break; // Exit the loop if it takes too long
            }

            // 2. Constantly update velocity to move towards the target
            // This overrides forces like gravity or drag.
            Vector3 direction = (grappleTarget.position - rb.position).normalized;
            rb.velocity = direction * hookSpeed;

            // 3. Increment timer and wait for the next physics update
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- Cleanup Phase ---
        // This part is reached only if the loop breaks due to a timeout.
        // The OnCollisionEnter method handles cleanup if a collision occurs.
        if (isGrappling)
        {
            StopGrapple();
        }
    }

    // *** Helper function to stop the grapple cleanly ***
    private void StopGrapple()
    {
        if (!isGrappling) return;

        if (grappleCoroutine != null)
        {
            StopCoroutine(grappleCoroutine);
            grappleCoroutine = null;
        }

        rb.velocity = Vector3.zero;
        isGrappling = false;
        grappleTarget = null;
        Player.Instance.canMoveToggle(true);
    }

    private void StartPullingObject(Transform t)
    {
        if (isPulling) return;

        Rigidbody pullableRb = t.GetComponent<Rigidbody>();
        if (pullableRb == null)
        {
            Debug.LogWarning("Pullable object is missing a Rigidbody component.");
            return;
        }

        currentPullObject = t;
        isPulling = true;
        pullElapsedTime = 0f;
        stuckTime = 0f;
        
        // Calculate the hook point - this is our target
        pullTargetPoint = CalculateHookPoint(t);
        lastDistanceToTarget = Vector3.Distance(pullTargetPoint, transform.position);

        Debug.Log($"[PULL START] Object: {t.name}, HookPoint: {pullTargetPoint}, InitialDist: {lastDistanceToTarget:F3}");

        Player.Instance.canMoveToggle(false);

        if (hookTarget != null)
        {
            hookTarget.SetActive(false);
        }
    }

    private void ApplyContinuousPull()
    {
        if (currentPullObject == null) { StopPullingObject(); return; }

        Rigidbody pullableRb = currentPullObject.GetComponent<Rigidbody>();
        if (pullableRb == null) { StopPullingObject(); return; }

        // Recalculate hook point each frame (object might have rotated/moved)
        pullTargetPoint = CalculateHookPoint(currentPullObject);
        
        // Calculate distance from hook point to player
        float currentDistanceToTarget = Vector3.Distance(pullTargetPoint, transform.position);

        // STOP CONDITION 1: Reached minimum distance
        if (currentDistanceToTarget <= MIN_PULL_DISTANCE)
        {
            Debug.Log($"[PULL STOP] Reached target ({currentDistanceToTarget:F3} <= {MIN_PULL_DISTANCE})");
            StopPullingObject();
            return;
        }

        // STOP CONDITION 2: Check for collision with player
        Collider objCol = currentPullObject.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol != null && plrCol != null)
        {
            // Check if colliders are touching or overlapping
            if (Physics.ComputePenetration(
                objCol, objCol.transform.position, objCol.transform.rotation,
                plrCol, plrCol.transform.position, plrCol.transform.rotation,
                out Vector3 direction, out float distance))
            {
                Debug.LogWarning($"[PULL COLLISION] Object colliding with player. Penetration: {distance:F3}");
                StopPullingObject();
                return;
            }
        }

        // STOP CONDITION 3: Not getting closer (stuck/sliding sideways)
        float distanceChange = lastDistanceToTarget - currentDistanceToTarget;
        if (distanceChange < -0.01f) // Moving away (with tiny tolerance)
        {
            stuckTime += Time.fixedDeltaTime;
            if (stuckTime >= STUCK_TIMEOUT)
            {
                Debug.LogWarning($"[PULL STUCK] Not getting closer. DistChange: {distanceChange:F4}");
                StopPullingObject();
                return;
            }
        }
        else
        {
            stuckTime = 0f; // Reset if making progress
        }
        
        lastDistanceToTarget = currentDistanceToTarget;

        // Calculate pull direction: from hook point toward player
        // This ensures the object moves so the hook point approaches the player
        Vector3 pullDirection = (transform.position - pullTargetPoint).normalized;
        pullDirection.y = 0;

        // Apply movement - physics will naturally handle collisions
        float normalizedTime = Mathf.Clamp01(pullElapsedTime / 1.0f);
        float speedFactor = pullAccelerationCurve.Evaluate(normalizedTime);
        float moveAmount = pullSpeed * speedFactor * Time.fixedDeltaTime;

        Vector3 newPosition = currentPullObject.position + pullDirection * moveAmount;
        newPosition.y = currentPullObject.position.y; // Preserve Y
        
        Debug.Log($"[PULL] Dist: {currentDistanceToTarget:F3}, Change: {distanceChange:F4}, Moving: {moveAmount:F3}");
        
        // MovePosition with ContinuousDynamic collision detection will handle obstacles naturally
        pullableRb.MovePosition(newPosition);
    }

    private void StopPullingObject()
    {
        if (isPulling)
        {
            Debug.Log($"[PULL END] Stopping pull");
            Player.Instance.canMoveToggle(true);
            
            if (currentPullObject != null)
            {
                Rigidbody pullableRb = currentPullObject.GetComponent<Rigidbody>();
                if (pullableRb != null)
                {
                    pullableRb.velocity = Vector3.zero;
                    pullableRb.angularVelocity = Vector3.zero;
                }
            }
        }
        isPulling = false;
        currentPullObject = null;
        pullElapsedTime = 0f;
        stuckTime = 0f;
        lastDistanceToTarget = 0f;
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : Vector3.forward;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}