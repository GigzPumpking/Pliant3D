using UnityEngine;
using System.Collections.Generic;

public class ColoredInteractable : Interactable
{
    private Animator animator;
    private Renderer[] renderers; // Array to store all renderers in this object and its children
    private Color[] originalColors; // Array to store the original colors of each renderer

    [SerializeField]
    private Color highlightColor = Color.green;

    [Tooltip("GameObjects whose renderers will change color on highlight. Defaults to this object if the list is empty.")]
    [SerializeField] private List<GameObject> colorTargets = new List<GameObject>();

    [SerializeField] private Animator linkedObjectAnimator;

    [SerializeField] private string linkedObjectAnimationTrigger = "dust";

    private void Awake()
    {
        animator = GetComponent<Animator>();

        var collectedRenderers = new List<Renderer>();
        if (colorTargets != null && colorTargets.Count > 0)
        {
            foreach (GameObject target in colorTargets)
            {
                if (target != null)
                    collectedRenderers.AddRange(target.GetComponentsInChildren<Renderer>());
            }
        }
        else
        {
            collectedRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        renderers = collectedRenderers.ToArray();

        if (animator == null)
        {
            Debug.LogWarning("Animator component missing on " + gameObject.name);
        }

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No Renderer components found on " + gameObject.name + " or its color targets.");
        }
        else
        {
            // Store the original colors of all renderers
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }

    public override void Interact()
    {
        if (!isInteractable)
        {
            Debug.Log("Can't interact with " + gameObject.name);
            return;
        }

        if (linkedObjectAnimator != null && !string.IsNullOrEmpty(linkedObjectAnimationTrigger))
        {
            linkedObjectAnimator.SetTrigger(linkedObjectAnimationTrigger);
        }
    }

    protected override void Highlight()
    {
        base.Highlight();

        // Change the color of all renderers to the highlight color
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = highlightColor;
            }
        }
    }

    protected override void Unhighlight()
    {
        base.Unhighlight();

        // Revert the color of all renderers to their original colors
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }
}
