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
    [SerializeField] private float raycastDistance = 0.5f;
    [SerializeField] private float yOffset = 0.2f;

    [Tooltip("How fast the player can be moving vertically and still be able to jump.")]
    [SerializeField] private float verticalVelocityThreshold = 0.1f;

    [Header("Pullable Box Settings")]
    [Tooltip("Center (local) of Pullable detection box")]
    [SerializeField] private Vector3 pullBoxCenter = new Vector3(0f, 0.5f, 2f);
    [Tooltip("Full size of Pullable detection box (width, height, depth)")]
    [SerializeField] private Vector3 pullBoxSize = new Vector3(2f, 1f, 4f);

    [Header("Grapple Box Settings (Replaces Sphere)")]
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
    private Vector3 pullDirection; // The direction of the pull

    private bool isGrappling = false;
    private Transform grappleTarget;
    private Coroutine grappleCoroutine;

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
    
    // *** NEW: Detects collision with the grapple target ***
    private void OnCollisionEnter(Collision collision)
    {
        if (isGrappling && collision.transform == grappleTarget)
        {
            StopGrapple();
        }
    }

    private void OnDrawGizmos()
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
    }
    
    private void OnDrawGizmosSelected() 
    {
        Vector3 rayOrigin = transform.position + Vector3.up * yOffset;
        Gizmos.color = isGrounded ? Color.green : Color.red; // Green if grounded, red if not
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
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
        animator?.SetTrigger("Jump");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void FixedUpdate()
    {
        // Define the ray's starting point
        Vector3 rayOrigin = transform.position + Vector3.up * yOffset;

        // Create a variable to store the collision information
        RaycastHit hitInfo;

        // Perform the raycast. It returns true if it hits something on the groundLayer,
        // and it populates 'hitInfo' with details about what it hit.
        isGrounded = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hitInfo, // The 'out' keyword passes the hit data back to our variable
            raycastDistance,
            groundLayer
        );

        // Check if the raycast was successful
        if (isGrounded)
        {
            Debug.Log($"Grounded on: {hitInfo.collider.name}", hitInfo.collider.gameObject);

            Debug.DrawLine(rayOrigin, hitInfo.point, Color.green);
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance, Color.red);
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
                Collider targetCollider = currentTarget.GetComponent<Collider>();

                if (isGrappling || targetCollider == null)
                {
                    // For grappling, or as a fallback, aim for the center.
                    endPoint = targetCollider?.bounds.center ?? currentTarget.position;
                }
                else // isPulling
                {
                    // For pulling, aim at the vertically centered closest point, just like the HookTarget.
                    Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
                    float verticalOffset = targetCollider.bounds.center.y - closestPoint.y;
                    endPoint = closestPoint + new Vector3(0, verticalOffset, 0);
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
                Collider targetCollider = targetForIcon.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    // Position the icon at the closest point on the collider's surface, then adjust to be vertically centered.
                    Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
                    float verticalOffset = targetCollider.bounds.center.y - closestPoint.y;
                    Vector3 centeredPoint = closestPoint + new Vector3(0, verticalOffset, 0);
                    hookTarget.transform.position = centeredPoint;
                }
                else
                {
                    hookTarget.transform.position = targetForIcon.position;
                }
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

    // *** NEW: Helper function to stop the grapple cleanly ***
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
        if (isPulling) return; // Already pulling an object

        Rigidbody pullableRb = t.GetComponent<Rigidbody>();
        if (pullableRb == null)
        {
            Debug.LogWarning("Pullable object is missing a Rigidbody component.");
            return;
        }

        currentPullObject = t;
        isPulling = true;
        pullElapsedTime = 0f;

        // --- 4-Direction Pull Logic ---
        Vector3 directionToObject = (t.position - player.position).normalized;
        directionToObject.y = 0;

        // Get player's local axes
        Vector3 forward = player.forward;
        Vector3 right = player.right;

        // Calculate dot products to find the dominant direction
        float dotForward = Vector3.Dot(directionToObject, forward);
        float dotBack = Vector3.Dot(directionToObject, -forward);
        float dotRight = Vector3.Dot(directionToObject, right);
        float dotLeft = Vector3.Dot(directionToObject, -right);

        // Find the max dot product
        float maxDot = Mathf.Max(dotForward, dotBack, dotRight, dotLeft);

        // Lock the pull direction to the dominant axis
        if (maxDot == dotForward) pullDirection = -forward;
        else if (maxDot == dotBack) pullDirection = forward;
        else if (maxDot == dotRight) pullDirection = -right;
        else pullDirection = right;
        // --- End 4-Direction Logic ---

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
        Collider objCol = currentPullObject.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();

        if (pullableRb == null || objCol == null || plrCol == null) { StopPullingObject(); return; }

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.z);
        float minAllowedDistance = objR + plrR;

        Vector3 vectorToObject = currentPullObject.position - player.position;
        vectorToObject.y = 0;

        // If the object is already at or within the stopping distance, stop the pull.
        if (vectorToObject.magnitude <= minAllowedDistance)
        {
            StopPullingObject();
            return;
        }

        float normalizedElapsedTime = Mathf.Clamp01(pullElapsedTime / 1.0f);
        float curveFactor = pullAccelerationCurve.Evaluate(normalizedElapsedTime);
        float effectivePullSpeed = pullSpeed * curveFactor;
        float moveAmount = effectivePullSpeed * Time.fixedDeltaTime;

        // Calculate the new position
        Vector3 newProposedPosition = currentPullObject.position + pullDirection * moveAmount;

        // Use Rigidbody.MovePosition for physics-based movement
        pullableRb.MovePosition(new Vector3(newProposedPosition.x, currentPullObject.position.y, newProposedPosition.z));
    }

    private void StopPullingObject()
    {
        if (isPulling)
        {
            Player.Instance.canMoveToggle(true);
        }
        isPulling = false;
        currentPullObject = null;
        pullElapsedTime = 0f;
        pullDirection = Vector3.zero;
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : Vector3.forward;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}