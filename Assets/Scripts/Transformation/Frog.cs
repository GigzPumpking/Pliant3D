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
    [SerializeField] private float yOffset = 0.5f;

    // Removed detectionRange as it's no longer used by OverlapSphere for hookables
    // [Header("Detection Ranges")]
    // [Tooltip("Sphere radius for Hookable objects")]
    // [SerializeField] private float detectionRange = 5f; // Obsolete

    [Header("Pullable Box Settings")]
    [Tooltip("Center (local) of Pullable detection box")]
    [SerializeField] private Vector3 pullBoxCenter = new Vector3(0f, 0.5f, 2f);
    [Tooltip("Full size of Pullable detection box (width, height, depth)")]
    [SerializeField] private Vector3 pullBoxSize   = new Vector3(2f, 1f, 4f);

    [Header("Grapple Box Settings (Replaces Sphere)")]
    [Tooltip("Center (local) of Grapple detection box. Relative to player facing direction.")]
    [SerializeField] private Vector3 grappleBoxCenter = new Vector3(0f, 1.0f, 3f); // Default: Y is double pullBox.y, Z is further
    [Tooltip("Full size of Grapple detection box. Height & Width are ~2x pullBox's.")]
    [SerializeField] private Vector3 grappleBoxSize   = new Vector3(4f, 2f, 6f); // Default: W & H are 2x pullBox, Depth is custom

    private Interactable highlightedObject;
    private Transform     closestObject;

    [Header("Hook & Pull Settings")]
    [SerializeField] private float hookDuration = 1f;
    [SerializeField] private float hookForce    = 10f;

    [Header("Pull Behavior")]
    [SerializeField] private float stopOffset   = 0.1f;
    [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Grapple Line")]
    [SerializeField] private LineRenderer lineRenderer;  // drag in inspector
    [SerializeField] private bool useWorldSpace = true;  // match your LR setting

    private SpriteRenderer _spriteRenderer;

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
    }

    private void OnDrawGizmos() {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            // Assuming animator is handled by FormScript or not strictly needed for basic GetPullBoxTransform Gizmo
        }
        #endif

        // Get transform for boxes once
        GetPullBoxTransform(out Vector3 pullBoxWorldCenter_Gizmo, out Quaternion commonRot_Gizmo);

        Matrix4x4 oldMatrix = Gizmos.matrix; // Store matrix once

        // 1) Box for Grappleable (Yellow)
        Vector3 grappleBoxWorldCenter_Gizmo = transform.position + commonRot_Gizmo * grappleBoxCenter;
        Gizmos.matrix = Matrix4x4.TRS(grappleBoxWorldCenter_Gizmo, commonRot_Gizmo, grappleBoxSize);
        Gizmos.color = Color.yellow; // Grapple box color
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // 2) Box for Pullable (Cyan)
        Gizmos.matrix = Matrix4x4.TRS(pullBoxWorldCenter_Gizmo, commonRot_Gizmo, pullBoxSize);
        Gizmos.color  = Color.cyan; // Pull box color
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        Gizmos.matrix = oldMatrix; // Restore original matrix
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
        if (!context.performed || closestObject == null) return;

        var intr = closestObject.GetComponent<Interactable>();
        if (intr.HasProperty("Hookable"))
            GrapplingHook(closestObject);
        else if (intr.HasProperty("Pullable"))
            PullObject(closestObject);

        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void FixedUpdate()
    {
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * yOffset,
            Vector3.down,
            raycastDistance
        );
    }

    private void Update()
    {
        DetectAndHighlightObjects();
        UpdateGrappleLine();
    }

    private void UpdateGrappleLine()
    {
        if (closestObject != null)
        {
            lineRenderer.enabled = true;
            Vector3 startCenter;
            var playerCol = GetComponentInChildren<Collider>();
            if (playerCol != null) startCenter = playerCol.bounds.center;
            else                  startCenter = transform.position;

            Vector3 endCenter;
            var targetCol = closestObject.GetComponent<Collider>();
            if (targetCol != null) endCenter = targetCol.bounds.center;
            else                    endCenter = closestObject.position;

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
        // Get the primary orientation and the specific center for the pull box
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
        // pullBoxWorldCenter is the 'center' output from GetPullBoxTransform, commonWorldRot is its 'rot'
        Collider[] pullCols = Physics.OverlapBox(pullBoxWorldCenter, pullHalfExtents, commonWorldRot);

        var pullables = pullCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Pullable"));

        // Combine results and find the nearest
        var all = hookables.Concat(pullables);
        var nearest = all
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

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
        Collider plrCol = player.GetComponentInChildren<Collider>(); // 'player' ref
        if (objCol == null || plrCol == null) yield break;

        float objHalfY = objCol.bounds.extents.y;
        float plrHalfY = plrCol.bounds.extents.y;
        float verticalGap = objHalfY + plrHalfY + stopOffset;

        Vector3 start = player.transform.position; // 'player' ref
        Vector3 objPos = t.position;
        Vector3 target = new Vector3(objPos.x, objPos.y + verticalGap, objPos.z);

        float elapsed = 0f;
        while (elapsed < hookDuration)
        {
            float pct    = elapsed / hookDuration;
            float curveT = pullCurve.Evaluate(pct);
            player.transform.position = Vector3.Lerp(start, target, curveT); // 'player' ref

            elapsed += Time.deltaTime;
            yield return null;
        }
        player.transform.position = target; // 'player' ref
    }

    private void PullObject(Transform t)
    {
        StartCoroutine(PullObjectCoroutine(t));
    }

    private IEnumerator PullObjectCoroutine(Transform t)
    {
        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>(); // 'player' ref
        if (objCol == null || plrCol == null) yield break;

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.y, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.y, plrCol.bounds.extents.z);
        float gap  = objR + plrR + stopOffset;

        Vector3 startPos = t.position; // Renamed 'start' to 'startPos' to avoid conflict
        float startY = startPos.y;

        Vector3 flatDir = player.position - startPos; // 'player' ref
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) yield break;
        flatDir.Normalize();

        Vector3 flatPlayer = new Vector3(player.position.x, startY, player.position.z); // 'player' ref
        Vector3 target     = flatPlayer - flatDir * gap;

        float elapsed = 0f;
        while (elapsed < hookDuration)
        {
            float pct    = elapsed / hookDuration;
            float curveT = pullCurve.Evaluate(pct);
            Vector3 nextPos = Vector3.Lerp(startPos, target, curveT);
            t.position = new Vector3(nextPos.x, startY, nextPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }
        t.position = new Vector3(target.x, startY, target.z);
    }

    private string GetCurrentAnimationName()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return "";
        }
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0 && clipInfo[0].clip != null)
        {
            return clipInfo[0].clip.name;
        }
        return "";
    }

    // This GetPullBoxTransform method uses the "perfect directions" as you last provided.
    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec = Vector3.forward; 

        if (_spriteRenderer == null)
        {
            Debug.LogWarning("GetPullBoxTransform: SpriteRenderer is null. Cannot determine direction from sprite. Defaulting direction.");
            worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
            worldCenter = transform.position + worldRot * pullBoxCenter;
            return;
        }
        if (animator == null) 
        {
            Debug.LogWarning("GetPullBoxTransform: Animator is null. Cannot determine direction from animation. Defaulting direction.");
            worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
            worldCenter = transform.position + worldRot * pullBoxCenter;
            return;
        }

        bool isFlippedX = _spriteRenderer.flipX;
        string animationName = GetCurrentAnimationName(); 
        bool directionSet = false;

        Debug.Log($"GetPullBoxTransform: Current animation name is '{animationName}' with flipX = {isFlippedX}");

        if (animationName.StartsWith("Idle Back") || 
            animationName.StartsWith("Jump Back") || 
            animationName.StartsWith("Walk Back"))
        {
            if (!isFlippedX) 
            {
                dirVec = Vector3.forward;    // Your "perfect" direction for West (FACING LEFT)
            }
            else 
            {
                dirVec = Vector3.right; // Your "perfect" direction for North (FACING UP)
            }
            directionSet = true;
        }
        else if (animationName.StartsWith("Idle Front") || 
                 animationName.StartsWith("Jump Front") || 
                 animationName.StartsWith("Walk Front"))
        {
            if (!isFlippedX)
            {
                dirVec = Vector3.left;   // Your "perfect" direction for South (FACING DOWN)
            }
            else 
            {
                dirVec = Vector3.back;  // Your "perfect" direction for East (FACING RIGHT)
            }
            directionSet = true;
        }

        if (!directionSet)
        {
            if (!string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning($"GetPullBoxTransform: Animation '{animationName}' not handled for custom pull box direction. Defaulting to Vector3.forward.");
            }
            else if (animator.runtimeAnimatorController != null)
            {
                Debug.LogWarning("GetPullBoxTransform: Could not determine current animation clip name. Defaulting direction to Vector3.forward.");
            }
        }

        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = transform.position + worldRot * pullBoxCenter;
    }
}