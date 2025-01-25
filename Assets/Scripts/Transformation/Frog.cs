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
    [SerializeField] private float hookDetectionRange = 5f; // Range to detect hookable objects
    private Hookable highlightedHookable; // Currently highlighted Hookable object
    private Transform hookableObject;

    [SerializeField] private float hookDuration = 1f;   
    [SerializeField] private float hookForce = 10f;

    public override void OnEnable()
    {
        base.OnEnable();
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

        if (!context.performed)
        {
            return;
        }

        GrapplingHook(hookableObject);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void GroundedChecker()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + new Vector3(0, yOffset, 0), Vector3.down * raycastDistance, out hit, raycastDistance);
    }

    private void DetectAndHighlightHookables()
    {
        // Detect all colliders within the hook detection range
        Collider[] colliders = Physics.OverlapSphere(transform.position, hookDetectionRange);

        // Filter for Hookable objects
        var hookables = colliders
            .Select(c => c.GetComponent<Hookable>())
            .Where(h => h != null && h.isInteractable)
            .ToList();

        // Find the closest Hookable
        Hookable closestHookable = hookables
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .FirstOrDefault();

        // Highlight the closest Hookable if it's not already highlighted
        if (closestHookable != highlightedHookable)
        {
            // Unhighlight the previously highlighted Hookable
            if (highlightedHookable != null)
            {
                highlightedHookable.IsHighlighted = false;
            }

            // Highlight the new closest Hookable
            if (closestHookable != null)
            {
                closestHookable.IsHighlighted = true;
                hookableObject = closestHookable.transform; // Set the closest hookable object for grappling
            }
            else
            {
                hookableObject = null; // Reset hookable object if none is found
            }

            highlightedHookable = closestHookable;
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

    private void FixedUpdate()
    {
        GroundedChecker();
    }

    private void Update()
    {
        DetectAndHighlightHookables();
    }
}
