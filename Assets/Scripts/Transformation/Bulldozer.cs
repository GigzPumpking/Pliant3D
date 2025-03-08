using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Bulldozer : FormScript
{
    protected override float baseSpeed { get; set; } = 3.0f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    [SerializeField] private float detectionRange = 5f; // Range to detect breakable objects
    private Interactable highlightedInteractable; // Currently highlighted Interactable object

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnDisable()
    {
        PushState(false);

        // Unhighlight the highlighted Interactable
        if (highlightedInteractable != null)
        {
            highlightedInteractable.IsHighlighted = false;
            highlightedInteractable = null;
        }
    }

    public void PushState(bool state)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);

        rb.mass = state ? 1000 : 1;
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PushState(true);
        }
        else if (context.canceled)
        {
            PushState(false);
        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (highlightedInteractable != null && highlightedInteractable.HasProperty("Breakable"))
        {
            Debug.Log("Deactivating Breakable Object: " + highlightedInteractable.name);
            highlightedInteractable.gameObject.SetActive(false); // Deactivate the closest breakable object
            highlightedInteractable = null; // Reset the highlighted object
        }
        else
        {
            Debug.Log("No Breakable object found within range.");
        }
    }

    private void DetectAndHighlightBreakables()
    {
        // Detect all colliders within the detection range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

        // Filter for Interactable objects with the "Breakable" property
        var breakables = colliders
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && i.HasProperty("Breakable"))
            .ToList();

        // Find the closest breakable object
        Interactable closestInteractable = breakables
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

        // Highlight the closest Interactable if it's not already highlighted
        if (closestInteractable != highlightedInteractable)
        {
            // Unhighlight the previously highlighted Interactable
            if (highlightedInteractable != null)
            {
                highlightedInteractable.IsHighlighted = false;
            }

            // Highlight the new closest Interactable
            if (closestInteractable != null)
            {
                closestInteractable.IsHighlighted = true;
            }

            highlightedInteractable = closestInteractable;
        }
    }

    private void Update()
    {
        DetectAndHighlightBreakables();
    }
}
