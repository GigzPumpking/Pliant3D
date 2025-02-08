using UnityEngine;

public class ColoredInteractable : Interactable
{
    private Animator animator;
    private Renderer[] renderers; // Array to store all renderers in this object and its children
    private Color[] originalColors; // Array to store the original colors of each renderer

    [SerializeField]
    private Color highlightColor = Color.green;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>();

        if (animator == null)
        {
            Debug.LogError("Animator component missing on " + gameObject.name);
        }

        if (renderers.Length == 0)
        {
            Debug.LogError("No Renderer components found on " + gameObject.name + " or its children.");
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
