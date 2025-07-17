using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Bulldozer : FormScript
{
    protected override float baseSpeed { get; set; } = 7.0f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    private Interactable highlightedInteractable;

    [Header("Breakable Detection Box")]
    [Tooltip("Center (local) of the Breakable detection box, relative to the player's facing direction.")]
    [SerializeField] private Vector3 breakBoxCenter = new Vector3(0f, 0.5f, 1.5f);
    [Tooltip("Full size (width, height, depth) of the Breakable detection box.")]
    [SerializeField] private Vector3 breakBoxSize = new Vector3(2f, 1f, 3f);

    [SerializeField] private float sprintModifier = 4.67f;

    private bool isPushing = false;

    private BoxCollider pushCollider;

    private CapsuleCollider normalCollider;

    public override void Awake()
    {
        base.Awake();

        pushCollider = GetComponentInChildren<BoxCollider>();
        normalCollider = GetComponentInChildren<CapsuleCollider>();
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnDisable()
    {
        PushState(false);

        // Ensure sprint state is reset when the form is disabled.
        if (animator != null)
        {
            animator.SetBool("isSprinting", false);
        }
        speed = baseSpeed;

        if (highlightedInteractable != null)
        {
            highlightedInteractable.IsHighlighted = false;
            highlightedInteractable = null;
        }
    }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        GetBreakBoxTransform(out Vector3 boxWorldCenter, out Quaternion boxWorldRot);

        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Set the gizmo's transform to match the detection box
        Gizmos.matrix = Matrix4x4.TRS(boxWorldCenter, boxWorldRot, breakBoxSize);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange for breakables
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        Gizmos.matrix = oldMatrix;
        #endif
    }

    /// <summary>
    /// Turn physics‐pushing on/off.
    /// </summary>
    public void PushState(bool state)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);
        rb.mass = state ? 500f : 1f;
        isPushing = state;
        
        if (pushCollider != null)
        {
            pushCollider.enabled = state;
        }

        if (normalCollider != null)
        {
            normalCollider.enabled = !state;
        }
    }

    /// <summary>
    /// Ability1 → Hold to push AND tap to break any breakable in the box.
    /// </summary>
    public override void Ability1(InputAction.CallbackContext context)
    {
        // --- On Button Press ---
        if (context.performed)
        {
            PushState(true);

            if (highlightedInteractable != null && highlightedInteractable.HasProperty("Breakable"))
            {
                Debug.Log("Deactivating Breakable Object: " + highlightedInteractable.name);
                highlightedInteractable.gameObject.SetActive(false);
                highlightedInteractable = null;
            }
        }
        // --- On Button Release ---
        else if (context.canceled)
        {
            PushState(false);
        }
    }

    /// <summary>
    /// Ability2 → Sprint
    /// </summary>
    public override void Ability2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            speed = baseSpeed * sprintModifier;
            // Set the "isSprinting" bool to true on the animator.
            animator?.SetBool("isSprinting", true);
        }
        else if (context.canceled)
        {
            speed = baseSpeed;
            // Set the "isSprinting" bool to false on the animator.
            animator?.SetBool("isSprinting", false);
        }
    }

    /// <summary>
    /// Gets the world-space center and rotation for the detection box from the Player.
    /// </summary>
    private void GetBreakBoxTransform(out Vector3 worldCenter, out Quaternion worldRot)
    {
        Vector3 dirVec;

        // Get the definitive direction from the Player singleton.
        if (Player.Instance != null)
        {
            dirVec = Player.Instance.AnimationBasedFacingDirection;
        }
        else
        {
            // Fallback for when the game isn't running (e.g., OnDrawGizmos in editor).
            dirVec = transform.forward;
        }
        
        // Use the direction vector to create the rotation for the box.
        worldRot = Quaternion.LookRotation(dirVec, Vector3.up);

        // Calculate the world-space center of the box based on this rotation.
        worldCenter = transform.position + worldRot * breakBoxCenter;
    }

    private void DetectAndHighlightBreakables()
    {
        // Get the box's current transform from our new helper method.
        GetBreakBoxTransform(out Vector3 boxWorldCenter, out Quaternion boxWorldRot);
        Vector3 halfExtents = breakBoxSize * 0.5f;

        // Find all colliders within the oriented box.
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

    private void Update()
    {
        DetectAndHighlightBreakables();
    }
}