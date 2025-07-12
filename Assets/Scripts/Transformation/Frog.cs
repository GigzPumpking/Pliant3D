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
    [SerializeField] private Vector3 pullBoxSize   = new Vector3(2f, 1f, 4f);

    [Header("Grapple Box Settings (Replaces Sphere)")]
    [Tooltip("Center (local) of Grapple detection box. Relative to player facing direction.")]
    [SerializeField] private Vector3 grappleBoxCenter = new Vector3(0f, 1.0f, 3f);
    [Tooltip("Full size of Grapple detection box. Height & Width are ~2x pullBox's.")]
    [SerializeField] private Vector3 grappleBoxSize   = new Vector3(4f, 2f, 6f);

    private Interactable highlightedObject;
    private Transform     closestObject;

    [Header("Hook & Pull Settings")]
    [SerializeField] private float hookDuration = 1f; // This will now act as max pull duration
    [SerializeField] private float hookForce    = 10f; // This is unused in the original script for pull, but keeping it.
    [SerializeField] private float pullSpeed = 5f; // Max speed at which the object is pulled.
    [Tooltip("Defines how the pull speed accelerates over time while holding the button.")]
    [SerializeField] private AnimationCurve pullAccelerationCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1); // New: Acceleration curve

    [Header("Pull Behavior")]
    [SerializeField] private float stopOffset   = -2f;
    [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // This curve might be less relevant for continuous pull.

    [Header("Grapple Line")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private bool useWorldSpace = true;

    private SpriteRenderer _spriteRenderer;

    // New variables for continuous pull
    private bool isPulling = false;
    private Transform currentPullObject;
    private Vector3 pullStartObjectPosition;
    private Vector3 pullTargetPosition;
    private float initialPullDistance;
    private float currentPulledDistance = 0f;
    private float pullElapsedTime = 0f; // New: To track how long the pull button has been held

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
        lineRenderer.endColor   = new Color(1f, 0f, 0f, 0.75f);
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth   = 0.25f;
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
        closestObject   = null;
        // Stop any ongoing pull
        StopPullingObject(); // Call StopPullingObject to ensure player movement is re-enabled
    }

    private void OnDrawGizmos() {
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
        Gizmos.color  = Color.cyan;
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
        if (closestObject == null) return;

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

        // Apply continuous pull if active
        if (isPulling && currentPullObject != null)
        {
            pullElapsedTime += Time.fixedDeltaTime; // Increment elapsed time
            ApplyContinuousPull();
        }
    }

    private void Update()
    {
        DetectAndHighlightObjects();
        UpdateGrappleLine();
    }

    private void UpdateGrappleLine()
    {
        // Only show line renderer if an object is highlighted OR actively being pulled
        if (highlightedObject != null || isPulling)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter;
            var playerCol = GetComponentInChildren<Collider>();
            if (playerCol != null) startCenter = playerCol.bounds.center;
            else                  startCenter = transform.position;

            Vector3 endCenter;
            var targetCol = (isPulling && currentPullObject != null) ? currentPullObject.GetComponent<Collider>() : closestObject?.GetComponent<Collider>();
            if (targetCol != null) endCenter = targetCol.bounds.center;
            else                    endCenter = (isPulling && currentPullObject != null) ? currentPullObject.position : (closestObject != null ? closestObject.position : startCenter); // Fallback

            Vector3 startPos = useWorldSpace ? startCenter : lineRenderer.transform.InverseTransformPoint(startCenter);
            Vector3 endPos   = useWorldSpace ? endCenter : lineRenderer.transform.InverseTransformPoint(endCenter);

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

        // 1) Hookable objects detection (using a new, larger box)
        Vector3 grappleBoxWorldCenter = transform.position + commonWorldRot * grappleBoxCenter;
        Vector3 grappleHalfExtents = grappleBoxSize * 0.5f;
        Collider[] hookCols = Physics.OverlapBox(grappleBoxWorldCenter, grappleHalfExtents, commonWorldRot);
        
        var hookables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Hookable"));

        // 2) Pullable objects detection (using existing pull box logic)
        Vector3 pullHalfExtents = pullBoxSize * 0.5f;
        Collider[] pullCols = Physics.OverlapBox(pullBoxWorldCenter, pullHalfExtents, commonWorldRot);

        var pullables = pullCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Pullable"));

        // Combine results and find the nearest
        var all = hookables.Concat(pullables);
        var nearest = all
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

        // Don't change highlighting if currently pulling the highlighted object
        if (isPulling && currentPullObject == nearest?.transform) return;

        if (nearest != highlightedObject)
        {
            if (highlightedObject != null)
                highlightedObject.IsHighlighted = false;

            if (nearest != null)
            {
                nearest.IsHighlighted = true;
                closestObject         = nearest.transform;
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
        StartCoroutine(GrapplingHookCoroutine(t));
    }

    private IEnumerator GrapplingHookCoroutine(Transform t)
    {
        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null) yield break;

        float objHalfY = objCol.bounds.extents.y;
        float plrHalfY = plrCol.bounds.extents.y;
        float verticalGap = objHalfY + plrHalfY + stopOffset;

        Vector3 start = player.transform.position;
        Vector3 objPos = t.position;
        Vector3 target = new Vector3(objPos.x, objPos.y + verticalGap, objPos.z);

        float elapsed = 0f;
        Player.Instance.canMoveToggle(false); // Disable player movement when grappling
        while (elapsed < hookDuration)
        {
            float pct    = elapsed / hookDuration;
            float curveT = pullCurve.Evaluate(pct);
            player.transform.position = Vector3.Lerp(start, target, curveT);

            elapsed += Time.deltaTime;
            yield return null;
        }
        player.transform.position = target;
        Player.Instance.canMoveToggle(true); // Re-enable player movement after grappling
    }

    private void StartPullingObject(Transform t)
    {
        if (isPulling) return; // Already pulling an object

        currentPullObject = t;
        isPulling = true;
        pullElapsedTime = 0f; // Reset elapsed time when starting a new pull
        pullStartObjectPosition = t.position;

        // Disable player movement
        Player.Instance.canMoveToggle(false);

        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null)
        {
            StopPullingObject(); // Stop if colliders are missing
            return;
        }

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.y, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.y, plrCol.bounds.extents.z);
        float gap  = objR + plrR + stopOffset;

        Vector3 flatDir = player.position - pullStartObjectPosition;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f)
        {
            pullTargetPosition = pullStartObjectPosition; // Prevent division by zero
        }
        else
        {
            flatDir.Normalize();
            Vector3 flatPlayer = new Vector3(player.position.x, pullStartObjectPosition.y, player.position.z);
            pullTargetPosition = flatPlayer - flatDir * gap;
        }
        
        initialPullDistance = Vector3.Distance(pullStartObjectPosition, pullTargetPosition);
        currentPulledDistance = 0f; // Reset pulled distance
    }

    private void ApplyContinuousPull()
    {
        if (currentPullObject == null)
        {
            StopPullingObject();
            return;
        }

        // Calculate the maximum distance the object *could* be pulled towards the player.
        // This takes into account the `stopOffset` and the object/player collider sizes.
        Collider objCol = currentPullObject.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null)
        {
            StopPullingObject();
            return;
        }

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.y, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.y, plrCol.bounds.extents.z);
        float minAllowedDistance = objR + plrR + stopOffset;

        Vector3 currentFlatDir = player.position - currentPullObject.position;
        currentFlatDir.y = 0f;

        // If the object is already at or very close to the minimum distance, stop pulling.
        if (currentFlatDir.magnitude <= minAllowedDistance + 0.01f) // Added a small epsilon
        {
            StopPullingObject();
            return;
        }
        
        Vector3 pullDirection = currentFlatDir.normalized;

        float normalizedElapsedTime = Mathf.Clamp01(pullElapsedTime / hookDuration); // Clamped between 0 and 1
        float curveFactor = pullAccelerationCurve.Evaluate(normalizedElapsedTime);

        // Calculate the effective pull speed for this frame
        float effectivePullSpeed = pullSpeed * curveFactor;

        // Calculate the movement amount for this frame
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
        if (isPulling) // Only re-enable if we were actually pulling
        {
            Player.Instance.canMoveToggle(true); // Re-enable player movement
        }
        isPulling = false;
        currentPullObject = null;
        currentPulledDistance = 0f;
        pullElapsedTime = 0f; // Reset elapsed time when stopping
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec;

        if (Player.Instance != null)
        {
            dirVec = Player.Instance.AnimationBasedFacingDirection;
        }
        else
        {
            // Fallback for when the game isn't running (e.g., OnDrawGizmos in editor).
            dirVec = Vector3.forward;
        }
        
        // Use the direction vector to create the rotation for the boxes.
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);

        // Calculate the world-space center of the pull box based on this rotation.
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}