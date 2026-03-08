using UnityEngine;

/// <summary>
/// Place on a child GameObject with a trigger collider to prevent the Bulldozer
/// from pushing/pulling the parent Pushable while inside this zone.
/// 
/// Typical usage: add two trigger colliders to a ramp —
///   1. One covering the walkable surface (prevents pushing while standing on it).
///   2. One covering the incline face (prevents pushing/pulling from the front).
///
/// The Bulldozer detects these zones via OnTriggerEnter/Exit and excludes the
/// referenced Pushable from its push logic.
/// </summary>
public class PushExclusionZone : MonoBehaviour
{
    [Tooltip("The Pushable to exclude from being pushed while the bulldozer is in this zone. " +
             "If left empty, automatically finds the Pushable in parent hierarchy.")]
    [SerializeField] private Pushable pushable;

    /// <summary>The Pushable this zone protects.</summary>
    public Pushable Pushable => pushable;

    private void Awake()
    {
        if (pushable == null)
            pushable = GetComponentInParent<Pushable>();
    }
}
