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

    [SerializeField] private float detectionRange = 5f;
    private Interactable highlightedInteractable;

    [SerializeField] private float sprintModifier = 4.67f;

    private bool isPushing = false;    // track current push state

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnDisable()
    {
        // ensure we’re no longer pushing
        PushState(false);

        if (highlightedInteractable != null)
        {
            highlightedInteractable.IsHighlighted = false;
            highlightedInteractable = null;
        }
    }

    /// <summary>
    /// Turn physics‐pushing on/off.
    /// </summary>
    public void PushState(bool state)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);
        rb.mass = state ? 1000f : 1f;
        isPushing = state;
    }

    /// <summary>
    /// Ability1 → Sprint
    /// </summary>
    public override void Ability1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            speed = baseSpeed * sprintModifier;
        }
        else if (context.canceled)
        {
            speed = baseSpeed;
        }
    }

    /// <summary>
    /// Ability2 → Toggle push AND break any breakable in range
    /// </summary>
    public override void Ability2(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // toggle push on/off
        bool newState = !isPushing;
        PushState(newState);
        Debug.Log($"PushState toggled: {newState}");

        // if we just started pushing, also break the highlighted
        if (newState)
        {
            if (highlightedInteractable != null && highlightedInteractable.HasProperty("Breakable"))
            {
                Debug.Log("Deactivating Breakable Object: " + highlightedInteractable.name);
                highlightedInteractable.gameObject.SetActive(false);
                highlightedInteractable = null;
            }
            else
            {
                Debug.Log("No Breakable object found within range.");
            }
        }
    }

    private void DetectAndHighlightBreakables()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

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