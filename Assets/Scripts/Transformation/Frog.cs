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

    [Header("Ground Check")]
    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private float yOffset = 0.2f;

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

    // *** NEW: State variables for grappling ***
    private bool isGrappling = false;
    private Transform grappleTarget;
    private Coroutine grappleCoroutine;

    public override void Awake()
    {
        base.Awake();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("Frog script requires a SpriteRenderer component on a child GameObject or the same GameObject.");
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

    public override void Ability1(InputAction.CallbackContext context)
    {
        if (!isGrounded || !context.performed || Player.Instance.TransformationChecker() == true) return;
        isGrounded = false;
        animator?.SetTrigger("Jump");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
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

    private void FixedUpdate()
    {
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * yOffset,
            Vector3.down,
            raycastDistance
        );

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
        // Show line if highlighting, pulling, OR grappling
        if (highlightedObject != null || isPulling || isGrappling)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter = GetComponentInChildren<Collider>()?.bounds.center ?? transform.position;

            // Determine target for the line
            Transform currentTarget = null;
            if (isGrappling) currentTarget = grappleTarget;
            else if (isPulling) currentTarget = currentPullObject;
            else currentTarget = closestObject;

            Vector3 endCenter = currentTarget?.GetComponent<Collider>()?.bounds.center ?? currentTarget?.position ?? startCenter;

            Vector3 startPos = useWorldSpace ? startCenter : lineRenderer.transform.InverseTransformPoint(startCenter);
            Vector3 endPos = useWorldSpace ? endCenter : lineRenderer.transform.InverseTransformPoint(endCenter);

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
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

        currentPullObject = t;
        isPulling = true;
        pullElapsedTime = 0f;
        
        Player.Instance.canMoveToggle(false);

        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null)
        {
            StopPullingObject();
            return;
        }
    }
    
    // Unchanged methods from here...
    private void ApplyContinuousPull()
    {
        if (currentPullObject == null) { StopPullingObject(); return; }

        Collider objCol = currentPullObject.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null) { StopPullingObject(); return; }

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.z);
        float minAllowedDistance = objR + plrR;

        Vector3 currentFlatDir = player.position - currentPullObject.position;
        currentFlatDir.y = 0f;

        if (currentFlatDir.magnitude <= minAllowedDistance + 0.01f) { StopPullingObject(); return; }
        
        Vector3 pullDirection = currentFlatDir.normalized;

        float normalizedElapsedTime = Mathf.Clamp01(pullElapsedTime / 1.0f); // Assuming 1s to full speed
        float curveFactor = pullAccelerationCurve.Evaluate(normalizedElapsedTime);
        float effectivePullSpeed = pullSpeed * curveFactor;
        float moveAmount = effectivePullSpeed * Time.fixedDeltaTime;

        Vector3 newProposedPosition = currentPullObject.position + pullDirection * moveAmount;
        Vector3 vectorToPlayer = player.position - newProposedPosition;
        vectorToPlayer.y = 0f;

        if (vectorToPlayer.magnitude < minAllowedDistance)
        {
            newProposedPosition = new Vector3(player.position.x, currentPullObject.position.y, player.position.z) - pullDirection * minAllowedDistance;
        }

        currentPullObject.position = new Vector3(newProposedPosition.x, currentPullObject.position.y, newProposedPosition.z);
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
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Player.Instance != null ? Player.Instance.AnimationBasedFacingDirection : Vector3.forward;
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}