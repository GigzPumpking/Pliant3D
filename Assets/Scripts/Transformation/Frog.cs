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

    [Header("Detection Ranges")]
    [Tooltip("Sphere radius for Hookable objects")]
    [SerializeField] private float detectionRange = 5f;

    [Tooltip("Center (local) of Pullable detection box")]
    [SerializeField] private Vector3 pullBoxCenter = new Vector3(0f, 0.5f, 2f);
    [Tooltip("Full size of Pullable detection box (width, height, depth)")]
    [SerializeField] private Vector3 pullBoxSize   = new Vector3(2f, 1f, 4f);

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

    public override void Awake()
    {
        base.Awake();

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
        // 1) Sphere for Hookable
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 2) Box for Pullable
        GetPullBoxTransform(out Vector3 center, out Quaternion rot);
        Vector3 halfExtents = pullBoxSize * 0.5f;

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, pullBoxSize);
        Gizmos.color  = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = old;
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

            // Use the frog’s collider center if you want the line to start at its middle too:
            Vector3 startCenter;
            var playerCol = GetComponentInChildren<Collider>();
            if (playerCol != null) startCenter = playerCol.bounds.center;
            else                  startCenter = transform.position;
            
            // Use the target’s collider center for the end point:
            Vector3 endCenter;
            var targetCol = closestObject.GetComponent<Collider>();
            if (targetCol != null) endCenter = targetCol.bounds.center;
            else                    endCenter = closestObject.position;

            // Now convert to local or world space to feed into the LineRenderer:
            Vector3 startPos = useWorldSpace
                ? startCenter
                : lineRenderer.transform.InverseTransformPoint(startCenter);
            Vector3 endPos   = useWorldSpace
                ? endCenter
                : lineRenderer.transform.InverseTransformPoint(endCenter);

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
        // 1) Hookable via Sphere
        var hookCols = Physics.OverlapSphere(transform.position, detectionRange);
        var hookables = hookCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Hookable"));

        // 2) Pullable via Box
        GetPullBoxTransform(out Vector3 center, out Quaternion rot);
        Vector3 halfExtents = pullBoxSize * 0.5f;
        Collider[] pullCols = Physics.OverlapBox(center, halfExtents, rot);

        var pullables = pullCols
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Pullable"));

        // combine with hookables, pick nearest
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
        // 1) Get colliders’ vertical half‑extents
        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null) yield break;

        float objHalfY = objCol.bounds.extents.y;
        float plrHalfY = plrCol.bounds.extents.y;
        float verticalGap = objHalfY + plrHalfY + stopOffset;

        // 2) Compute start & elevated target
        Vector3 start = player.transform.position;
        Vector3 objPos = t.position;
        Vector3 target = new Vector3(
            objPos.x,
            objPos.y + verticalGap,
            objPos.z
        );

        // 3) Rubber‑gum Lerp up to target
        float elapsed = 0f;
        while (elapsed < hookDuration)
        {
            float pct    = elapsed / hookDuration;
            float curveT = pullCurve.Evaluate(pct);
            player.transform.position = Vector3.Lerp(start, target, curveT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4) Snap exactly
        player.transform.position = target;
    }

    private void PullObject(Transform t)
    {
        StartCoroutine(PullObjectCoroutine(t));
    }

    private IEnumerator PullObjectCoroutine(Transform t)
    {
        // (unchanged)
        Collider objCol = t.GetComponent<Collider>();
        Collider plrCol = player.GetComponentInChildren<Collider>();
        if (objCol == null || plrCol == null) yield break;

        float objR = Mathf.Max(objCol.bounds.extents.x, objCol.bounds.extents.y, objCol.bounds.extents.z);
        float plrR = Mathf.Max(plrCol.bounds.extents.x, plrCol.bounds.extents.y, plrCol.bounds.extents.z);
        float gap  = objR + plrR + stopOffset;

        Vector3 start = t.position;
        float startY = start.y;

        Vector3 flatDir = player.position - start;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) yield break;
        flatDir.Normalize();

        Vector3 flatPlayer = new Vector3(player.position.x, startY, player.position.z);
        Vector3 target     = flatPlayer - flatDir * gap;

        float elapsed = 0f;
        while (elapsed < hookDuration)
        {
            float pct    = elapsed / hookDuration;
            float curveT = pullCurve.Evaluate(pct);
            Vector3 nextPos = Vector3.Lerp(start, target, curveT);
            t.position = new Vector3(nextPos.x, startY, nextPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        t.position = new Vector3(target.x, startY, target.z);
    }

    private void GetPullBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec;
        switch (Player.Instance.FacingDirection)
        {
            case Directions.LEFT:  dirVec = Vector3.left;  break;
            case Directions.RIGHT: dirVec = Vector3.right; break;
            case Directions.UP:    dirVec = Vector3.forward;  break;
            case Directions.DOWN:  dirVec = Vector3.back;   break;
            default:               dirVec = Vector3.forward;  break;
        }

        worldRot    = Quaternion.LookRotation(dirVec, Vector3.up);
        worldCenter = player.position + worldRot * pullBoxCenter;
    }
}