using UnityEngine;

/// <summary>
/// Attach to objects that the Bulldozer can push.
/// Objects stay kinematic (frozen) until actively pushed, at which point the
/// Rigidbody switches to non-kinematic so full physics (rolling, momentum,
/// friction) takes over. After pushing stops, the object decelerates naturally
/// and re-freezes once it settles.
/// Requires an Interactable component with a "Pushable" property on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    private Rigidbody rb;
    private bool isBeingPushed = false;

    [Header("Physics")]
    [Tooltip("Drag applied while being pushed. Higher = more resistance / less sliding.")]
    [SerializeField] private float pushDrag = 4f;
    [Tooltip("Angular drag while being pushed. Lower = more rolling.")]
    [SerializeField] private float pushAngularDrag = 0.5f;
    [Tooltip("Maximum velocity this object can reach (units/sec). 0 = no limit.")]
    [SerializeField] private float maxSpeed = 0f;

    [Header("Settling")]
    [Tooltip("Speed below which the object is considered settled and re-freezes.")]
    [SerializeField] private float settleSpeedThreshold = 0.15f;
    [Tooltip("Seconds the object must remain below settleSpeedThreshold before re-freezing.")]
    [SerializeField] private float settleDelay = 0.4f;

    [Header("Constraints")]
    [Tooltip("If true, the push force is snapped to the nearest world axis (X or Z).")]
    [SerializeField] private bool constrainToAxes = false;

    [Header("Chain Push")]
    [Tooltip("Fraction of this object's velocity transferred to a kinematic pushable it collides with (0-1).")]
    [SerializeField] private float chainForceMultiplier = 0.8f;

    // Tracks how long velocity has been below the settle threshold
    private float settleTimer = 0f;
    // Saved drag/angularDrag values from before we started pushing
    private float savedDrag;
    private float savedAngularDrag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    /// <summary>
    /// Call once when the bulldozer first contacts this object.
    /// Unfreezes the Rigidbody so physics can act on it.
    /// </summary>
    public void BeginPush()
    {
        if (isBeingPushed) return;
        isBeingPushed = true;
        settleTimer = 0f;

        savedDrag = rb.drag;
        savedAngularDrag = rb.angularDrag;

        rb.isKinematic = false;
        rb.drag = pushDrag;
        rb.angularDrag = pushAngularDrag;
    }

    /// <summary>
    /// Apply a push force this physics frame. Call from FixedUpdate while pushing.
    /// </summary>
    public void Push(Vector3 force)
    {
        if (rb.isKinematic) return;

        force.y = 0f;

        if (constrainToAxes)
        {
            if (Mathf.Abs(force.x) >= Mathf.Abs(force.z))
                force.z = 0f;
            else
                force.x = 0f;
        }

        rb.AddForce(force, ForceMode.Force);

        // Clamp velocity if a max speed is set
        if (maxSpeed > 0f)
        {
            Vector3 hVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (hVel.magnitude > maxSpeed)
            {
                hVel = hVel.normalized * maxSpeed;
                rb.velocity = new Vector3(hVel.x, rb.velocity.y, hVel.z);
            }
        }

        // Reset settle timer while force is being applied
        settleTimer = 0f;
    }

    /// <summary>
    /// Call once when the bulldozer stops pushing. The object will coast to a
    /// stop via drag and then re-freeze.
    /// </summary>
    public void EndPush()
    {
        if (!isBeingPushed) return;
        isBeingPushed = false;

        // Restore original drag so gravity acts normally during coast/fall
        rb.drag = savedDrag;
        rb.angularDrag = savedAngularDrag;
        // Don't immediately freeze — let FixedUpdate handle settling
    }

    private void FixedUpdate()
    {
        // Nothing to do if frozen
        if (rb.isKinematic) return;

        // If still being actively pushed, don't try to settle
        if (isBeingPushed) return;

        // Don't settle while falling — only allow re-freeze when vertical speed is low
        float vSpeed = Mathf.Abs(rb.velocity.y);
        float hSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        if (hSpeed <= settleSpeedThreshold && vSpeed <= settleSpeedThreshold)
        {
            settleTimer += Time.fixedDeltaTime;
            if (settleTimer >= settleDelay)
            {
                Freeze();
            }
        }
        else
        {
            settleTimer = 0f;
        }
    }

    private void Freeze()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.drag = savedDrag;
        rb.angularDrag = savedAngularDrag;
        rb.isKinematic = true;
        settleTimer = 0f;
    }

    /// <summary>
    /// When a moving pushable hits a kinematic pushable, wake it up so
    /// continuous force can be applied.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        WakeNeighborIfNeeded(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        WakeNeighborIfNeeded(collision);
        ApplyContinuousChainForce(collision);
    }

    private void WakeNeighborIfNeeded(Collision collision)
    {
        if (rb.isKinematic) return;

        Pushable other = collision.collider.GetComponent<Pushable>();
        if (other == null) other = collision.collider.GetComponentInParent<Pushable>();
        if (other == null || other == this) return;

        if (other.rb.isKinematic)
        {
            other.WakeFromChain();
        }
    }

    private void ApplyContinuousChainForce(Collision collision)
    {
        // Only propagate if this object is actively moving (non-kinematic)
        if (rb.isKinematic) return;

        Pushable other = collision.collider.GetComponent<Pushable>();
        if (other == null) other = collision.collider.GetComponentInParent<Pushable>();
        if (other == null || other == this) return;

        // Continuous force in the direction from this object toward the neighbor
        Vector3 pushDir = (other.transform.position - transform.position);
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.001f) return;
        pushDir.Normalize();

        Vector3 hVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = Vector3.Dot(hVel, pushDir);
        if (speed <= 0f) return; // Only push in the direction we're moving toward neighbor

        other.rb.AddForce(pushDir * speed * rb.mass * chainForceMultiplier, ForceMode.Force);
    }

    /// <summary>
    /// Unfreeze this object due to being hit by another pushable (chain push).
    /// Uses original drag values since the bulldozer isn't directly pushing this.
    /// </summary>
    private void WakeFromChain()
    {
        if (!rb.isKinematic) return;

        savedDrag = rb.drag;
        savedAngularDrag = rb.angularDrag;

        rb.isKinematic = false;
        settleTimer = 0f;
        // Keep original drag — only direct bulldozer pushes get pushDrag
    }
}
