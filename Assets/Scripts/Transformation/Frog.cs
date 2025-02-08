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
    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private float detectionRange = 5f; // Range to detect objects
    private Interactable highlightedObject; // Currently highlighted object
    private Transform closestObject; // The closest hookable or pullable object

    [SerializeField] private float hookDuration = 1f;   
    [SerializeField] private float hookForce = 10f;

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnDisable()
    {
        // Unhighlight the highlighted object
        if (highlightedObject != null)
        {
            highlightedObject.IsHighlighted = false;
            highlightedObject = null;
        }
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
        Debug.Log("Frog Ability 1");

        if (!isGrounded || !context.performed)
        {
            return;
        }

        isGrounded = false;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        Debug.Log("Frog Ability 2");

        if (!context.performed || closestObject == null)
        {
            return;
        }

        // Check for properties to decide the appropriate action
        Interactable interactable = closestObject.GetComponent<Interactable>();
        if (interactable != null)
        {
            if (interactable.HasProperty("Hookable"))
            {
                GrapplingHook(closestObject);
            }
            else if (interactable.HasProperty("Pullable"))
            {
                PullObject(closestObject);
            }
        }

        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void GroundedChecker()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + new Vector3(0, yOffset, 0), Vector3.down * raycastDistance, out hit, raycastDistance);
    }

    private void DetectAndHighlightObjects()
    {
        // Detect all colliders within the detection range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

        // Filter for interactable objects with "Hookable" or "Pullable" properties
        var interactables = colliders
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null && i.isInteractable && (i.HasProperty("Hookable") || i.HasProperty("Pullable")))
            .ToList();

        // Find the closest object
        Interactable closestInteractable = interactables
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();

        // Highlight the closest object if it's not already highlighted
        if (closestInteractable != highlightedObject)
        {
            // Unhighlight the previously highlighted object
            if (highlightedObject != null)
            {
                highlightedObject.IsHighlighted = false;
            }

            // Highlight the new closest object
            if (closestInteractable != null)
            {
                closestInteractable.IsHighlighted = true;
                closestObject = closestInteractable.transform; // Set the closest object
            }
            else
            {
                closestObject = null; // Reset closest object if none are found
            }

            highlightedObject = closestInteractable;
        }
    }

    private void GrapplingHook(Transform objectToHookTo)
    {
        if (objectToHookTo == null) return;

        // Start Coroutine
        StartCoroutine(GrapplingHookCoroutine(objectToHookTo));
    }

    IEnumerator GrapplingHookCoroutine(Transform objectToHookTo)
    {
        float timeElapsed = 0f;
        float duration = hookDuration;
        Vector3 originalPosition = player.transform.position;
        Vector3 targetPosition = objectToHookTo.position;

        if (targetPosition.y < originalPosition.y)
        {
            targetPosition.y = originalPosition.y;
        }

        while (timeElapsed < duration)
        {
            player.transform.position = Vector3.Lerp(originalPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PullObject(Transform objectToPull)
    {
        if (objectToPull == null) return;

        // Add logic to pull the object towards the player
        StartCoroutine(PullObjectCoroutine(objectToPull));
    }

    IEnumerator PullObjectCoroutine(Transform objectToPull)
    {
        float timeElapsed = 0f;
        float duration = hookDuration;
        Vector3 originalPosition = objectToPull.position;
        Vector3 targetPosition = player.transform.position;

        while (timeElapsed < duration)
        {
            objectToPull.position = Vector3.Lerp(originalPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void FixedUpdate()
    {
        GroundedChecker();
    }

    private void Update()
    {
        DetectAndHighlightObjects();
    }
}
